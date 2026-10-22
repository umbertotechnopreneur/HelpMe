// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
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

namespace PromptMeUp.Models;

/// <summary>Identifies whether to inspect, install, or remove the command path entry.</summary>
public enum PortablePathAction
{
    Install,
    Remove,
    Status
}

/// <summary>Describes a proposed command-path update before it is applied.</summary>
public sealed record PortablePathPlan(
    PortablePathAction Action,
    string ExecutableDirectory,
    string PersistenceTarget,
    string Preview,
    bool IsPresent,
    bool RequiresChange);

/// <summary>Reports the result and user guidance for a command-path operation.</summary>
public sealed record PortablePathResult(
    PortablePathAction Action,
    string ExecutableDirectory,
    string PersistenceTarget,
    bool Changed,
    bool IsPresent);

/// <summary>Carries guidance returned by a secret-store operation without exposing credentials.</summary>
public sealed record SecretStoreResult(string Guidance);
