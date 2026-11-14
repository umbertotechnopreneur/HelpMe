// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: HelpMe
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */

using System.Diagnostics;
using System.Text;

namespace HelpMe.Services;

/// <summary>Copies an exact previewed shell command without invoking a shell.</summary>
public interface ICommandClipboard
{
    #region Methods

    /// <summary>Returns whether the host clipboard accepted the command.</summary>
    bool TryCopy(string command);

    #endregion
}

/// <summary>Copies exact command text through optional platform clipboard tools with a bounded deadline.</summary>
public sealed class CommandClipboard : ICommandClipboard
{
    #region Methods

    /// <summary>Copies the exact command through standard input, never through command-line interpolation.</summary>
    public bool TryCopy(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        foreach (var start in CreateCandidates())
        {
            if (CopyAsync(start, command).GetAwaiter().GetResult()) return true;
        }
        return false;
    }

    /// <summary>Selects clipboard tools only for an available graphical session, without invoking a shell.</summary>
    private static IEnumerable<ProcessStartInfo> CreateCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "clip.exe"))
            { StandardInputEncoding = new UnicodeEncoding(false, true) };
        }
        else if (OperatingSystem.IsLinux())
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            {
                var wayland = new ProcessStartInfo("wl-copy") { StandardInputEncoding = new UTF8Encoding(false) };
                wayland.ArgumentList.Add("--type");
                wayland.ArgumentList.Add("text/plain;charset=utf-8");
                yield return wayland;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")))
            {
                var xclip = new ProcessStartInfo("xclip") { StandardInputEncoding = new UTF8Encoding(false) };
                xclip.ArgumentList.Add("-selection");
                xclip.ArgumentList.Add("clipboard");
                xclip.ArgumentList.Add("-in");
                yield return xclip;
                var xsel = new ProcessStartInfo("xsel") { StandardInputEncoding = new UTF8Encoding(false) };
                xsel.ArgumentList.Add("--clipboard");
                xsel.ArgumentList.Add("--input");
                yield return xsel;
            }
        }
    }

    /// <summary>Bounds stdin transfer and tool exit together, keeping command text out of arguments and diagnostics.</summary>
    private static async Task<bool> CopyAsync(ProcessStartInfo start, string command)
    {
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        EnvironmentSecretService.RemoveFromChildEnvironment(start);
        using var process = new Process { StartInfo = start };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            if (!process.Start()) return false;
            await process.StandardInput.WriteAsync(command.AsMemory(), deadline.Token).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(deadline.Token).ConfigureAwait(false);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or InvalidOperationException or OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (Exception cleanupException) when (cleanupException is InvalidOperationException or System.ComponentModel.Win32Exception)
            { /* No child was started, or it has already exited or cannot be terminated. */ }
            return false;
        }
    }

    #endregion
}
