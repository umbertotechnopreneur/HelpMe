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

/// <summary>Identifies whether a saved plan step is pending, running, awaiting review, or complete.</summary>
public enum PlanStepStatus
{
    Pending,
    Running,
    NeedsReview,
    Completed
}

/// <summary>Describes a plan command, its verification, and its persisted execution state.</summary>
public sealed record PlanStep(
    string Label,
    string Command,
    string Verification,
    string Expected,
    PlanStepStatus Status = PlanStepStatus.Pending);

/// <summary>Stores the goal, working directory, and ordered steps of a resumable plan.</summary>
public sealed record ExecutionPlan(
    int Version,
    string Id,
    string Goal,
    string Directory,
    List<PlanStep> Steps);
