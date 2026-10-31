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

/// <summary>Identifies the application action selected by command-line parsing.</summary>
public enum AppCommand
{
    Main,
    Help,
    Version,
    Setup,
    AiSettings,
    Theme,
    Status,
    Query,
    Direct,
    Diagnose,
    Script,
    Plan,
    Preview,
    Chat,
    Memories,
    Skills,
    Learning,
    Proposals,
    Dream,
    Heartbeat,
    Remember,
    Forget,
    TestAi,
    Costs,
    PrepareLogs,
    ThirdParty,
    Where,
    InstallFont,
    Path,
    Reset,
    Lenna,
    About
}

/// <summary>Contains parsed command arguments and explicit invocation flags.</summary>
public sealed record CommandLineOptions(
    AppCommand Command,
    string? Query,
    string? Language,
    bool NoAnimation,
    bool NoEmoji,
    bool Yes,
    bool DryRun,
    string? PathAction,
    string? InputFile = null,
    string? OutputFile = null,
    string? ResumeId = null,
    string? PreviewAction = null,
    string? Prefix = null,
    string? Pattern = null,
    bool ResetAll = false);

/// <summary>Carries either valid invocation options or a parsing error.</summary>
public sealed record CommandLineParseResult(CommandLineOptions? Options, string? Error)
{

    #region Properties

    public bool Succeeded => Options is not null && Error is null;

    #endregion

}
