// SPDX-License-Identifier: MIT
using System.Globalization;
using HelpMe.Models;

namespace HelpMe.Services;

/// <summary>Resolves an explicit shell or a bounded Linux parent chain without trusting terminal branding.</summary>
internal static class CommandShellResolver
{
    private static readonly Lazy<string?> CallingShell = new(DetectCallingShellName);

    /// <summary>Parses the public shell names without accepting executable paths or arguments.</summary>
    internal static CommandShell Parse(string value) => value.Trim().ToLowerInvariant() switch
    {
        "auto" => CommandShell.Auto,
        "pwsh" => CommandShell.PowerShell,
        "bash" => CommandShell.Bash,
        _ => throw new ArgumentException("Unsupported command shell.", nameof(value))
    };

    /// <summary>Formats a validated preference for persistence and form choices.</summary>
    internal static string ToStorageValue(CommandShell shell) => shell switch
    {
        CommandShell.Auto => "auto",
        CommandShell.PowerShell => "pwsh",
        CommandShell.Bash => "bash",
        _ => throw new ArgumentOutOfRangeException(nameof(shell))
    };

    /// <summary>Applies an optional invocation override while preserving the saved preference.</summary>
    internal static AppSettings ForInvocation(AppSettings settings, CommandShell? shell) => settings with
    {
        CommandShell = Resolve(shell ?? settings.PreferredCommandShell),
        CommandShellOverride = shell
    };

    /// <summary>Returns a stable shell for this process or leaves an ambiguous caller unresolved.</summary>
    internal static CommandShell Resolve(CommandShell preference)
    {
        if (!Enum.IsDefined(preference)) throw new ArgumentOutOfRangeException(nameof(preference));
        return preference == CommandShell.Auto ? CallingShell.Value switch
        {
            "bash" => CommandShell.Bash,
            "pwsh" => CommandShell.PowerShell,
            _ => CommandShell.Auto
        } : preference;
    }

    /// <summary>Requires a supported concrete shell before generation, review, or execution.</summary>
    internal static CommandShell Require(CommandShell preference, string language)
    {
        var shell = Resolve(preference);
        if (shell == CommandShell.Auto || shell == CommandShell.Bash && OperatingSystem.IsWindows())
        {
            UiTextCatalog.TryGet("Shell.Choose", SupportedLanguages.Normalize(language), out var message);
            throw new InvalidOperationException(message);
        }
        return shell;
    }

    /// <summary>Formats the interpreter contract shown to users and supplied to AI.</summary>
    internal static string Describe(CommandShell shell) => shell switch
    {
        CommandShell.PowerShell => "PowerShell 7 (pwsh -NoLogo -NoProfile -NonInteractive)",
        CommandShell.Bash => "Bash (bash --noprofile --norc -p)",
        _ => throw new InvalidOperationException("A concrete command shell is required.")
    };

    /// <summary>Uses Linux executable identity, skipping only known launchers; other platforms retain PowerShell.</summary>
    internal static string? CallingShellName => CallingShell.Value;

    /// <summary>Identifies the first calling shell without consulting terminal branding or the login-shell variable.</summary>
    private static string? DetectCallingShellName()
    {
        if (!OperatingSystem.IsLinux()) return "pwsh";
        try
        {
            var pid = Environment.ProcessId;
            var visited = new HashSet<int>();
            for (var depth = 0; depth < 8 && pid > 1 && visited.Add(pid); depth++)
            {
                var stat = File.ReadAllText($"/proc/{pid}/stat");
                var end = stat.LastIndexOf(')');
                if (end < 0) return null;
                var fields = stat[(end + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 2 || !int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out pid))
                    return null;
                var executable = File.ResolveLinkTarget($"/proc/{pid}/exe", returnFinalTarget: true);
                var name = executable?.Name;
                if (name is "bash" or "pwsh" or "zsh" or "fish") return name;
                if (name is not ("dotnet" or "hm" or "env" or "timeout")) return null;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // An inaccessible or disappearing parent is ambiguous, never evidence for another shell.
        }
        return null;
    }
}
