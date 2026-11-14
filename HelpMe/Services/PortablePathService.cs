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

using Microsoft.Extensions.Logging;
using HelpMe.Models;

namespace HelpMe.Services;

/// <summary>Reports that the install directory would create multiple entries in the Unix PATH.</summary>
internal sealed class AmbiguousUnixPathException() : InvalidOperationException("The install directory contains the Unix PATH separator.");

/// <summary>Reports that PATH integration cannot select a profile from the calling shell.</summary>
internal sealed class UnsupportedPathShellException() : InvalidOperationException("The calling shell has no supported PATH profile.");

/// <summary>Plans and applies user-approved changes that expose the command on PATH.</summary>
public interface IPortablePathService
{
    #region Methods

    /// <summary>Describes the changes required to expose the command on the user PATH.</summary>
    PortablePathPlan CreatePlan(PortablePathAction action);

    /// <summary>Applies the user-approved PATH integration plan.</summary>
    Task<PortablePathResult> ApplyAsync(PortablePathPlan plan, CancellationToken cancellationToken);

    #endregion
}

/// <summary>Inspects and applies the optional portable command path without elevating the process.</summary>
public sealed class PortablePathService : IPortablePathService
{
    #region Fields

    private const string MarkerStart = "# >>> HelpMe hm PATH >>>";
    private const string MarkerEnd = "# <<< HelpMe hm PATH <<<";
    private readonly ILogger<PortablePathService> _logger;

    #endregion

    #region Initialization

    /// <summary>Creates the portable PATH manager.</summary>
    public PortablePathService(ILogger<PortablePathService> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #endregion

    #region Methods

    /// <summary>Builds a human-readable, non-mutating PATH operation preview.</summary>
    public PortablePathPlan CreatePlan(PortablePathAction action)
    {
        var executableDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        ValidateInstallDirectory(action, executableDirectory);
        var targets = ResolvePersistenceTargets();
        var target = string.Join(", ", targets);
        var presence = targets.Select(path => IsPersisted(executableDirectory, path)).ToArray();
        var isPresent = presence.All(value => value);
        var requiresChange = action switch
        {
            PortablePathAction.Install => !isPresent,
            PortablePathAction.Remove => presence.Any(value => value),
            _ => false
        };
        var preview = action switch
        {
            PortablePathAction.Install => $"Add '{executableDirectory}' to {target}.",
            PortablePathAction.Remove => $"Remove the HelpMe-managed entry '{executableDirectory}' from {target}.",
            _ => $"Inspect whether '{executableDirectory}' is persisted in {target}."
        };
        return new PortablePathPlan(action, executableDirectory, target, preview, isPresent, requiresChange)
        { PersistenceTargets = targets };
    }

    /// <summary>Applies an already previewed PATH plan to user scope without installing or moving binaries.</summary>
    public async Task<PortablePathResult> ApplyAsync(PortablePathPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateInstallDirectory(plan.Action, plan.ExecutableDirectory);
        if (plan.Action == PortablePathAction.Status || !plan.RequiresChange)
        {
            return new PortablePathResult(plan.Action, plan.ExecutableDirectory, plan.PersistenceTarget, false, plan.IsPresent);
        }

        if (OperatingSystem.IsWindows())
        {
            ApplyWindowsPath(plan);
        }
        else
        {
            foreach (var profile in TargetsFor(plan))
            {
                await ApplyUnixProfileAsync(plan with { PersistenceTarget = profile }, cancellationToken).ConfigureAwait(false);
            }
        }

        UpdateCurrentProcessPath(plan);
        var isPresent = TargetsFor(plan).All(path => IsPersisted(plan.ExecutableDirectory, path));
        _logger.LogInformation(
            "Portable PATH updated. Action={Action}, Target={Target}, Changed={Changed}, Present={Present}",
            plan.Action,
            plan.PersistenceTarget,
            true,
            isPresent);
        return new PortablePathResult(plan.Action, plan.ExecutableDirectory, plan.PersistenceTarget, true, isPresent);
    }

    #endregion

    #region Implementation

    /// <summary>Rejects Unix directories that cannot be represented as one PATH entry before any install writes.</summary>
    private static void ValidateInstallDirectory(PortablePathAction action, string directory)
    {
        if (action == PortablePathAction.Install && !OperatingSystem.IsWindows() && directory.Contains(Path.PathSeparator))
        {
            throw new AmbiguousUnixPathException();
        }
    }

    /// <summary>Adds or removes the executable directory from the Windows user PATH.</summary>
    private static void ApplyWindowsPath(PortablePathPlan plan)
    {
        var entries = SplitPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)).ToList();
        entries.RemoveAll(entry => PathsEqual(entry, plan.ExecutableDirectory));
        if (plan.Action == PortablePathAction.Install)
        {
            entries.Add(plan.ExecutableDirectory);
        }

        Environment.SetEnvironmentVariable(
            "PATH",
            string.Join(Path.PathSeparator, entries.Distinct(PathComparer)),
            EnvironmentVariableTarget.User);
    }

    /// <summary>Maintains one clearly marked PATH block in the active Unix shell profile.</summary>
    internal static async Task ApplyUnixProfileAsync(PortablePathPlan plan, CancellationToken cancellationToken)
    {
        var profile = plan.PersistenceTarget;
        var directory = Path.GetDirectoryName(profile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var current = File.Exists(profile)
            ? await File.ReadAllTextAsync(profile, cancellationToken).ConfigureAwait(false)
            : string.Empty;
        var withoutManagedBlock = RemoveManagedBlock(current);
        var updated = withoutManagedBlock;
        if (plan.Action == PortablePathAction.Install)
        {
            var shellLine = BuildUnixPathLine(profile, plan.ExecutableDirectory);
            updated = string.IsNullOrEmpty(withoutManagedBlock)
                ? $"{MarkerStart}{Environment.NewLine}{shellLine}{Environment.NewLine}{MarkerEnd}{Environment.NewLine}"
                : $"{withoutManagedBlock}{(withoutManagedBlock.EndsWith('\n') ? string.Empty : Environment.NewLine)}{MarkerStart}{Environment.NewLine}{shellLine}{Environment.NewLine}{MarkerEnd}{Environment.NewLine}";
        }

        await File.WriteAllTextAsync(profile, updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates the child process environment so status checks are immediately consistent.</summary>
    private static void UpdateCurrentProcessPath(PortablePathPlan plan)
    {
        var current = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? string.Empty;
        if (plan.Action == PortablePathAction.Remove && !OperatingSystem.IsWindows() && plan.ExecutableDirectory.Contains(Path.PathSeparator))
        {
            // Older installs prepended the whole string. Splitting it first would leave its injected entries behind.
            var prefix = plan.ExecutableDirectory + Path.PathSeparator;
            if (current.StartsWith(prefix, StringComparison.Ordinal) || current == plan.ExecutableDirectory)
            {
                Environment.SetEnvironmentVariable("PATH", current.Length < prefix.Length ? null : current[prefix.Length..]);
            }
            return;
        }
        var entries = SplitPath(current).ToList();
        entries.RemoveAll(entry => PathsEqual(entry, plan.ExecutableDirectory));
        if (plan.Action == PortablePathAction.Install)
        {
            entries.Insert(0, plan.ExecutableDirectory);
        }

        Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, entries), EnvironmentVariableTarget.Process);
    }

    /// <summary>Checks the platform-specific persistent user configuration for the exact executable directory.</summary>
    private static bool IsPersisted(string executableDirectory, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            return SplitPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User))
                .Any(entry => PathsEqual(entry, executableDirectory));
        }

        if (!File.Exists(target))
        {
            return false;
        }

        var content = File.ReadAllText(target);
        var start = content.IndexOf(MarkerStart, StringComparison.Ordinal);
        var end = start < 0 ? -1 : content.IndexOf(MarkerEnd, start, StringComparison.Ordinal);
        if (end <= start) return false;
        var block = content[start..end];
        return block.Contains(BuildUnixPathLine(target, executableDirectory), StringComparison.Ordinal)
            || block.Contains($"export PATH={QuoteForPosixShell(executableDirectory)}:\"$PATH\"", StringComparison.Ordinal)
            || block.Contains($"fish_add_path --global {QuoteForPosixShell(executableDirectory)}", StringComparison.Ordinal);
    }

    /// <summary>Returns the Windows user scope or a profile file appropriate for the current Unix shell.</summary>
    private static IReadOnlyList<string> ResolvePersistenceTargets()
    {
        if (OperatingSystem.IsWindows())
        {
            return ["Windows user PATH"];
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            throw new InvalidOperationException("The user profile directory could not be resolved.");
        }

        var shell = OperatingSystem.IsLinux() ? CommandShellResolver.CallingShellName
            : Path.GetFileName(Environment.GetEnvironmentVariable("SHELL"));
        return UnixPersistenceTargets(userProfile, shell,
            Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"), Environment.GetEnvironmentVariable("ZDOTDIR"));
    }

    /// <summary>Chooses explicit shell profiles, covering interactive and login shells without changing unrelated files.</summary>
    internal static IReadOnlyList<string> UnixPersistenceTargets(string home, string? shell, string? configHome, string? zDotDir)
    {
        var config = AbsoluteDirectoryOrDefault(configHome, Path.Combine(home, ".config"));
        var zsh = AbsoluteDirectoryOrDefault(zDotDir, home);
        return shell switch
        {
            "bash" => [Path.Combine(home, ".bashrc"), new[] { ".bash_profile", ".bash_login", ".profile" }
                .Select(name => Path.Combine(home, name)).FirstOrDefault(File.Exists) ?? Path.Combine(home, ".profile")],
            "zsh" => [Path.Combine(zsh, ".zshrc"), Path.Combine(zsh, ".zprofile")],
            "fish" => [Path.Combine(config, "fish", "config.fish")],
            "pwsh" => [Path.Combine(config, "powershell", "profile.ps1")],
            _ => throw new UnsupportedPathShellException()
        };
    }

    /// <summary>Honors absolute shell configuration directories and rejects relative environment overrides.</summary>
    private static string AbsoluteDirectoryOrDefault(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && Path.IsPathFullyQualified(value) ? Path.GetFullPath(value) : fallback;

    /// <summary>Returns the exact profiles captured in the preview instead of detecting the shell again during writes.</summary>
    private static IReadOnlyList<string> TargetsFor(PortablePathPlan plan) =>
        plan.PersistenceTargets.Count > 0 ? plan.PersistenceTargets : [plan.PersistenceTarget];

    /// <summary>Builds an idempotent PATH entry in the syntax of the selected profile.</summary>
    private static string BuildUnixPathLine(string profile, string directory)
    {
        if (IsFishProfile(profile))
            return $"fish_add_path --global '{directory.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal)}'";
        if (profile.EndsWith(".ps1", StringComparison.Ordinal))
        {
            var literal = "'" + directory.Replace("'", "''", StringComparison.Ordinal) + "'";
            return $"if ($env:PATH.Split([IO.Path]::PathSeparator) -cnotcontains {literal}) {{ $env:PATH = {literal} + [IO.Path]::PathSeparator + $env:PATH }}";
        }
        var quoted = QuoteForPosixShell(directory);
        return $"case \":$PATH:\" in *:{quoted}:*) ;; *) export PATH={quoted}:\"$PATH\" ;; esac";
    }

    /// <summary>Removes only the block owned by HelpMe and leaves surrounding profile content untouched.</summary>
    private static string RemoveManagedBlock(string content)
    {
        var start = content.IndexOf(MarkerStart, StringComparison.Ordinal);
        if (start < 0)
        {
            return content;
        }

        var end = content.IndexOf(MarkerEnd, start, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidDataException("The HelpMe PATH block is incomplete; edit it manually before retrying.");
        }

        end += MarkerEnd.Length;
        while (end < content.Length && content[end] is '\r' or '\n')
        {
            end++;
        }

        return content.Remove(start, end - start);
    }

    /// <summary>Splits PATH while dropping blank entries and surrounding quotes.</summary>
    private static IEnumerable<string> SplitPath(string? value) =>
        (value ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Trim('"'))
            .Where(entry => !string.IsNullOrWhiteSpace(entry));

    /// <summary>Compares normalized paths using platform-appropriate casing.</summary>
    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return PathComparer.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.ExpandEnvironmentVariables(left))),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.ExpandEnvironmentVariables(right))));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return PathComparer.Equals(left.Trim(), right.Trim());
        }
    }

    /// <summary>Quotes one literal path for POSIX-compatible shell configuration.</summary>
    private static string QuoteForPosixShell(string value) => "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

    /// <summary>Identifies fish configuration because its PATH syntax differs from POSIX shells.</summary>
    private static bool IsFishProfile(string profile) => profile.EndsWith("config.fish", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Properties

    private static StringComparer PathComparer { get; } = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    #endregion
}
