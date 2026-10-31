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

/// <summary>Contains the explanation and source text of a generated script.</summary>
public sealed record ScriptArtifact(string Explanation, string Source);

/// <summary>Identifies the user-selected follow-up for a reviewed script draft.</summary>
public enum ScriptAction
{
    Save,
    Execute,
    DoNothing,
    Validate,
    Revise
}
