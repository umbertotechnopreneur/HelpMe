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

/// <summary>Records the lifecycle and metadata of one audited application session.</summary>
public sealed record AiSessionRecord(
    string Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Language,
    string Model,
    string Kind,
    string Status,
    string MetadataJson);

/// <summary>Records a timestamped event within an audited session.</summary>
public sealed record AiSessionEventRecord(
    string Id,
    string SessionId,
    DateTimeOffset OccurredAt,
    string EventType,
    string PayloadJson);

/// <summary>Describes one locally persisted audit action and its structured metadata.</summary>
public sealed record ActivityAuditRecord(
    string Id,
    DateTimeOffset OccurredAt,
    string? SessionId,
    string ActivityType,
    string Outcome,
    string PayloadJson);
