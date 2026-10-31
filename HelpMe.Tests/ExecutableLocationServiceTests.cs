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


using HelpMe.Services;

namespace HelpMe.Tests;

/// <summary>Verifies executable location service behavior.</summary>
public sealed class ExecutableLocationServiceTests
{

    #region Methods

    /// <summary>Verifies that location inspection is absolute, internally consistent, and non-mutating.</summary>
    [Fact]
    public void Resolve_ReturnsExecutableDirectoryAndChangeDirectoryCommand()
    {
        var result = new ExecutableLocationService().Resolve();

        Assert.True(Path.IsPathFullyQualified(result.ExecutablePath));
        Assert.Equal(Path.GetDirectoryName(result.ExecutablePath), result.DirectoryPath);
        Assert.Contains(result.DirectoryPath, result.ChangeDirectoryCommand, StringComparison.Ordinal);
        Assert.Contains(
            OperatingSystem.IsLinux() ? result.DirectoryPath : result.ExecutablePath,
            result.OpenFolderPreview,
            StringComparison.Ordinal);
    }

    #endregion
}
