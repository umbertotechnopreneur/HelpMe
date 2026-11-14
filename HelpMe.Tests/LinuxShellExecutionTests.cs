// SPDX-License-Identifier: MIT
using System.Diagnostics;
using HelpMe.Models;
using HelpMe.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpMe.Tests;

/// <summary>Exercises both approved Linux interpreters through the real bounded process supervisor.</summary>
public sealed class LinuxShellExecutionTests
{
    /// <summary>Preserves exact text, Unicode, stderr, and exit status across argument and stdin transport.</summary>
    [Theory]
    [InlineData(CommandShell.Bash, false)]
    [InlineData(CommandShell.Bash, true)]
    [InlineData(CommandShell.PowerShell, false)]
    [InlineData(CommandShell.PowerShell, true)]
    public async Task ExecuteAsync_UnicodeAndLongSource_PreservesInterpreterSemantics(CommandShell shell, bool longSource)
    {
        if (!OperatingSystem.IsLinux()) return;
        var source = shell == CommandShell.Bash
            ? "printf '%s\\n' 'Tiếng Việt' 'literal $HOME ; \"quoted\"'; printf '%s\\n' 'fixture-error' >&2; exit 7"
            : "[Console]::WriteLine('Tiếng Việt'); [Console]::WriteLine('literal $HOME ; \"quoted\"'); [Console]::Error.WriteLine('fixture-error'); exit 7";
        if (longSource) source = "#" + new string('x', 40_000) + "\n" + source;
        var result = await RunAsync(source, shell);
        Assert.Equal(7, result.ExitCode);
        Assert.Equal("Tiếng Việt\nliteral $HOME ; \"quoted\"", result.StandardOutput);
        Assert.Equal("fixture-error", result.StandardError);
        Assert.Equal(source, result.Command);
        Assert.Equal(shell, result.Shell);
        Assert.False(result.TimedOut);
    }

    /// <summary>Ignores Bash startup files and imported functions, while removing credentials from children.</summary>
    [Fact]
    public async Task Bash_PrivilegedStartup_IgnoresInheritedCodeAndCredentials()
    {
        if (!OperatingSystem.IsLinux()) return;
        using var fixture = new RegressionFixture();
        Directory.CreateDirectory(fixture.Paths.DataDirectory);
        var startup = Path.Combine(fixture.Paths.DataDirectory, "startup.sh");
        await File.WriteAllTextAsync(startup, "printf hijacked; exit 92\n");
        const string command = "printf '%s' clean; test -z \"${OPENAI_API_KEY-}\" && test -z \"${OPENAI_ADMIN_KEY-}\"";
        var start = CommandExecutionService.BuildStartInfo(Approve(command, CommandShell.Bash));
        start.Environment["BASH_ENV"] = startup;
        start.Environment["ENV"] = startup;
        start.Environment["BASH_FUNC_printf%%"] = "() { builtin printf hijacked; }";
        start.Environment["SHELLOPTS"] = "xtrace";
        start.Environment["OPENAI_API_KEY"] = "fixture";
        start.Environment["OPENAI_ADMIN_KEY"] = "fixture";
        var result = await BoundedProcessRunner.RunAsync(start, command, TimeSpan.FromSeconds(15), default);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("clean", result.StandardOutput);
        Assert.DoesNotContain("hijacked", result.StandardOutput + result.StandardError);
    }

    /// <summary>Bounds stdout and stderr when either interpreter generates more output than can be retained.</summary>
    [Theory]
    [InlineData(CommandShell.Bash)]
    [InlineData(CommandShell.PowerShell)]
    public async Task ExecuteAsync_OutputLimit_BoundsBothStreams(CommandShell shell)
    {
        if (!OperatingSystem.IsLinux()) return;
        var command = shell == CommandShell.Bash
            ? "printf 'x%.0s' {1..40000}; printf 'y%.0s' {1..40000} >&2"
            : "[Console]::Write(('x' * 40000)); [Console]::Error.Write(('y' * 40000))";
        var result = await RunAsync(command, shell);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.OutputTruncated);
        Assert.InRange(result.StandardOutput.Length, 1, 32768);
        Assert.InRange(result.StandardError.Length, 1, 32768);
    }

    /// <summary>Returns partial output at a finite deadline instead of leaving either shell running.</summary>
    [Theory]
    [InlineData(CommandShell.Bash)]
    [InlineData(CommandShell.PowerShell)]
    public async Task ExecuteAsync_Timeout_RetainsPartialOutput(CommandShell shell)
    {
        if (!OperatingSystem.IsLinux()) return;
        var command = shell == CommandShell.Bash ? "printf started; sleep 30" : "[Console]::Write('started'); Start-Sleep -Seconds 30";
        var result = await RunAsync(command, shell, TimeSpan.FromSeconds(5));
        Assert.True(result.TimedOut);
        Assert.Null(result.ExitCode);
        Assert.Contains("started", result.StandardOutput);
    }

    /// <summary>Stops only the approved command after live output and returns control without a timeout result.</summary>
    [Theory]
    [InlineData(CommandShell.Bash)]
    [InlineData(CommandShell.PowerShell)]
    public async Task ExecuteAsync_StopToken_ReturnsPartialEvidence(CommandShell shell)
    {
        if (!OperatingSystem.IsLinux()) return;
        using var stop = new CancellationTokenSource();
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = shell == CommandShell.Bash ? "printf ready; sleep 30" : "[Console]::Write('ready'); [Console]::Out.Flush(); Start-Sleep -Seconds 30";
        var running = new CommandExecutionService(NullLogger<CommandExecutionService>.Instance).ExecuteAsync(
            Approve(command, shell), Timeout.InfiniteTimeSpan, watchdog.Token,
            update => { if (string.Concat(update.Spans.Select(span => span.Text)).Contains("ready", StringComparison.Ordinal)) ready.TrySetResult(); }, stop.Token);
        try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(15)); }
        finally { await stop.CancelAsync(); }
        var result = await running.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Cancelled);
        Assert.False(result.TimedOut);
        Assert.Contains("ready", result.StandardOutput);
    }

    /// <summary>Stops an orphaned Bash descendant that retains output after its original shell has exited.</summary>
    [Fact]
    public async Task Bash_DescendantHoldsOutput_DeadlineStopsDescendant()
    {
        if (!OperatingSystem.IsLinux()) return;
        var result = await RunAsync("sleep 30 & child=$!; printf 'child-id=%s\\nparent-finished\\n' \"$child\"", CommandShell.Bash, TimeSpan.FromSeconds(5));
        var pidLine = Assert.Single(result.StandardOutput.Split('\n'), line => line.StartsWith("child-id=", StringComparison.Ordinal));
        var pid = int.Parse(pidLine[9..], System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            Assert.True(result.TimedOut);
            Assert.True(result.OutputTruncated);
            Assert.Contains("parent-finished", result.StandardOutput);
            Assert.True(SpinWait.SpinUntil(() => HasStopped(pid), TimeSpan.FromSeconds(2)));
        }
        finally
        {
            if (!HasStopped(pid))
            {
                using var child = Process.GetProcessById(pid);
                child.Kill(entireProcessTree: true);
                child.WaitForExit(5000);
            }
        }
    }

    /// <summary>Reports missing interpreters as startup failures rather than successful empty output.</summary>
    [Fact]
    public async Task SharedRunner_MissingInterpreter_ReportsNativeFailure()
    {
        if (!OperatingSystem.IsLinux()) return;
        await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(() => BoundedProcessRunner.RunAsync(
            new ProcessStartInfo("hm-fixture-missing-interpreter"), "Missing interpreter fixture", TimeSpan.FromSeconds(10), default));
    }

    /// <summary>Runs only the exact synthetic command represented by a test approval, with an independent watchdog.</summary>
    private static async Task<CommandExecutionResult> RunAsync(string command, CommandShell shell, TimeSpan? timeout = null)
    {
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await new CommandExecutionService(NullLogger<CommandExecutionService>.Instance).ExecuteAsync(
            Approve(command, shell), timeout ?? TimeSpan.FromSeconds(15), watchdog.Token);
    }

    /// <summary>Creates an internal test capability after deterministic review of the exact fixture shell and text.</summary>
    private static ApprovedCommand Approve(string command, CommandShell shell) => ApprovedCommand.Create(command,
        CommandRiskAssessmentService.AssessLocally(command, "en", shell));

    /// <summary>Checks only the fixture descendant and accepts a zombie already awaiting the kernel's reaper.</summary>
    private static bool HasStopped(int pid)
    {
        try
        {
            var stat = File.ReadAllText($"/proc/{pid}/stat");
            return stat[stat.LastIndexOf(')') + 2] is 'Z' or 'X';
        }
        catch (IOException exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return true;
        }
    }
}
