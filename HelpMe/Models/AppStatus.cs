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

namespace HelpMe.Models;

/// <summary>Summarizes local configuration, storage, and provider readiness.</summary>
public sealed record AppStatus(
    AppSettings Settings,
    bool HasApiKey,
    bool HasAdminKey,
    DateTimeOffset? LastPricingSync,
    string DatabasePath,
    string LogsDirectory,
    string PromptDirectory,
    int PromptCount)
{

    #region Properties

    public ArtifactLimits ArtifactLimits { get; init; } = ArtifactLimits.Default;

    #endregion

}

/// <summary>Reports whether a requested font installation changed the local machine.</summary>
public sealed record FontInstallResult(bool Changed, bool DryRun, string FontName, string Message);
