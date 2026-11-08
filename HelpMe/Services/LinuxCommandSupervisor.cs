// SPDX-License-Identifier: MIT
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace HelpMe.Services;

/// <summary>Keeps orphaned Linux command descendants under one live parent until output has drained.</summary>
internal sealed class LinuxCommandSupervisor : IDisposable
{
    internal const string Switch = "--internal-linux-command-supervisor";
    private readonly NamedPipeServerStream _control;
    private readonly byte[] _request;

    /// <summary>Redirects the launch through a short-lived copy of hm with a private control connection.</summary>
    internal LinuxCommandSupervisor(ProcessStartInfo startInfo)
    {
        var pipeName = Guid.NewGuid().ToString("N");
        _request = JsonSerializer.SerializeToUtf8Bytes(startInfo.ArgumentList.Prepend(startInfo.FileName));
        startInfo.ArgumentList.Clear();
        var host = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the running host.");
        if (Assembly.GetEntryAssembly() == typeof(LinuxCommandSupervisor).Assembly && Path.GetFileName(host) != "dotnet")
        {
            startInfo.FileName = host;
        }
        else
        {
            startInfo.FileName = Path.GetFileName(host) == "dotnet" ? host : "dotnet";
            startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "hm.dll"));
        }
        startInfo.ArgumentList.Add(Switch);
        startInfo.ArgumentList.Add(pipeName);
        _control = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    }

    /// <summary>Sends the exact argument vector and waits for the command shell to exit.</summary>
    internal async Task WaitForCommandExitAsync(Process supervisor, CancellationToken cancellationToken)
    {
        using var connecting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connection = _control.WaitForConnectionAsync(connecting.Token);
        var exited = supervisor.WaitForExitAsync(connecting.Token);
        if (await Task.WhenAny(connection, exited).ConfigureAwait(false) == exited && !connection.IsCompletedSuccessfully)
        {
            await connecting.CancelAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            throw new IOException("The Linux command supervisor exited before connecting.");
        }
        await connection.ConfigureAwait(false);
        await connecting.CancelAsync().ConfigureAwait(false);
        VerifyPeer(_control.SafePipeHandle, supervisor.Id);
        await _control.WriteAsync(_request, cancellationToken).ConfigureAwait(false);
        await _control.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        var notice = new byte[1];
        await _control.ReadExactlyAsync(notice, cancellationToken).ConfigureAwait(false);
        if (notice[0] == 0)
        {
            var errno = new byte[sizeof(int)];
            await _control.ReadExactlyAsync(errno, cancellationToken).ConfigureAwait(false);
            throw new Win32Exception(BinaryPrimitives.ReadInt32LittleEndian(errno));
        }
    }

    /// <summary>Allows the supervisor to exit after both command output streams are drained.</summary>
    internal void Release() => _control.WriteByte(1);

    /// <summary>Closes the private connection and removes its socket.</summary>
    public void Dispose() => _control.Dispose();

    /// <summary>Adopts orphaned descendants and stays alive until the caller finishes or kills the tree.</summary>
    [SupportedOSPlatform("linux")]
    internal static async Task<int> RunChildAsync(string pipeName)
    {
        if (!Guid.TryParseExact(pipeName, "N", out _))
        {
            return 1;
        }
        using var control = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await control.ConnectAsync(5000).ConfigureAwait(false);
        VerifyPeer(control.SafePipeHandle, GetParentPid());
        const int setChildSubreaper = 36;
        if (Prctl(setChildSubreaper, 1, 0, 0, 0) != 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        using var reader = new StreamReader(control, Encoding.UTF8, leaveOpen: true);
        var request = await reader.ReadLineAsync().ConfigureAwait(false) ?? throw new EndOfStreamException();
        var argv = JsonSerializer.Deserialize<string[]>(request);
        if (argv is not [var executable, ..])
        {
            throw new InvalidDataException("Missing command executable.");
        }
        var target = new ProcessStartInfo(executable) { UseShellExecute = false };
        EnvironmentSecretService.RemoveFromChildEnvironment(target);
        foreach (var argument in argv.Skip(1))
        {
            target.ArgumentList.Add(argument);
        }
        using var devNull = File.OpenHandle("/dev/null", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var child = new Process { StartInfo = target };
        try
        {
            child.Start();
        }
        catch (Win32Exception exception)
        {
            var failure = new byte[1 + sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(failure.AsSpan(1), exception.NativeErrorCode);
            await control.WriteAsync(failure).ConfigureAwait(false);
            return 1;
        }
        // Only the command and its descendants should keep the caller's pipes open.
        for (var descriptor = 0; descriptor <= 2; descriptor++)
        {
            if (Dup2(devNull.DangerousGetHandle().ToInt32(), descriptor) < 0)
            {
                var error = new Win32Exception(Marshal.GetLastPInvokeError());
                child.Kill(entireProcessTree: true);
                throw error;
            }
        }
        await child.WaitForExitAsync().ConfigureAwait(false);
        control.WriteByte(1);
        await control.ReadExactlyAsync(new byte[1]).ConfigureAwait(false);
        return child.ExitCode;
    }

    /// <summary>Checks the kernel-reported peer PID before transmitting or accepting command data.</summary>
    private static void VerifyPeer(SafePipeHandle socket, int expectedPid)
    {
        const int socketLevel = 1, peerCredentials = 17;
        uint size = 12; // Linux struct ucred is a pid_t, uid_t, and gid_t.
        if (GetPeerCredentials(socket, socketLevel, peerCredentials, out var credentials, ref size) != 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        if (size != 12 || credentials.Pid != expectedPid)
        {
            throw new IOException("Unexpected Linux command supervisor connection.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PeerCredentials
    {
        public int Pid;
        public uint Uid;
        public uint Gid;
    }

    /// <summary>Reads the peer credentials of the connected Unix socket.</summary>
    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    private static extern int GetPeerCredentials(SafePipeHandle socket, int level, int option, out PeerCredentials value, ref uint size);

    /// <summary>Returns the PID of the process that launched this supervisor.</summary>
    [DllImport("libc", EntryPoint = "getppid")]
    private static extern int GetParentPid();

    /// <summary>Sets a Linux process attribute using the native word-sized prctl arguments.</summary>
    [DllImport("libc", EntryPoint = "prctl", SetLastError = true)]
    private static extern int Prctl(int option, nuint arg2, nuint arg3, nuint arg4, nuint arg5);

    /// <summary>Replaces an inherited standard descriptor after launching the command.</summary>
    [DllImport("libc", EntryPoint = "dup2", SetLastError = true)]
    private static extern int Dup2(int source, int destination);
}
