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

/// <summary>Stores one role-tagged conversation message and its context-accounting metadata.</summary>
public sealed record ChatMessage(string Role, string Content)
{

    #region Properties

    /// <summary>Marks command results that retain the provider's user role but need separate context accounting.</summary>
    public bool IsToolOutput { get; init; }

    #endregion

}

/// <summary>Records provider token counts for usage and cost accounting.</summary>
public sealed record AiUsageMetrics(
    long InputTokens,
    long CachedInputTokens,
    long CacheWriteTokens,
    long OutputTokens,
    long ReasoningTokens,
    long TotalTokens);

/// <summary>Combines cumulative session usage with the available cost estimate.</summary>
public sealed record AiSessionAccounting(AiUsageMetrics Usage, decimal? EstimatedCostUsd);

/// <summary>Specifies the optional input-token limit for a conversation.</summary>
public sealed record ConversationContextLimits(int? MaxInputTokens)
{

    #region Properties

    public static ConversationContextLimits Default { get; } = new(MaxInputTokens: null);

    #endregion

}

/// <summary>Associates a provider response with its model and reported token usage.</summary>
public sealed record AiResponseAccounting(string? Id, string? Model, AiUsageMetrics Usage);

/// <summary>Separates estimated input, cache, and output costs in US dollars.</summary>
public sealed record AiCostBreakdown(
    decimal InputUsd,
    decimal CachedInputUsd,
    decimal CacheWriteUsd,
    decimal OutputUsd,
    decimal TotalUsd);

/// <summary>Describes context occupancy, reserved output, and message-category token counts.</summary>
public sealed record AiContextUsage(
    long InputTokens,
    long OutputTokens,
    long SystemInstructionTokens,
    long ConversationTokens,
    long LatestUserPromptTokens,
    long ContextWindowTokens,
    bool IsInputEstimate)
{

    #region Properties

    public long InputBudgetTokens { get; init; }

    public int ReservedOutputTokens { get; init; }

    public long UserMessageTokens { get; init; }

    public long ToolOutputTokens { get; init; }

    public long AssistantMessageTokens { get; init; }

    public long GuideTokens { get; init; }

    #endregion

}

/// <summary>Pairs a suggested command with the label shown during local review.</summary>
public sealed record SuggestedCommand(string Label, string Command);

/// <summary>Carries the parsed model answer, suggested actions, and request accounting.</summary>
public sealed record AiResponse(
    string Id,
    string Model,
    string Text,
    AiUsageMetrics Usage,
    AiContextUsage ContextUsage,
    AiCostBreakdown? CostBreakdown,
    decimal? EstimatedCostUsd,
    int HttpStatusCode,
    long ElapsedMilliseconds,
    string? ProviderRequestId)
{

    #region Properties

    public IReadOnlyList<SuggestedCommand> SuggestedCommands { get; init; } = [];

    public IReadOnlyList<string> GuideTopics { get; init; } = [];

    public decimal? TurnCostUsd { get; init; }

    public int RequestCount { get; init; } = 1;

    #endregion

}

/// <summary>Records request timing, redacted content, usage, and failure details for the local audit.</summary>
public sealed record AiRequestLog(
    string Id,
    string ConversationId,
    string PromptId,
    DateTimeOffset OccurredAt,
    DateTimeOffset? CompletedAt,
    string EndpointHost,
    string RequestedModel,
    string? ReturnedModel,
    string UserPrompt,
    string? AssistantResponse,
    AiUsageMetrics Usage,
    decimal? EstimatedCostUsd,
    int? HttpStatusCode,
    long? ElapsedMilliseconds,
    string? ProviderResponseId,
    string? ProviderRequestId,
    bool Succeeded,
    string? FailureCode);

/// <summary>Stores versioned prompt metadata and localized instruction text.</summary>
public sealed record PromptDefinition(
    string Id,
    int Version,
    string Description,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Texts,
    IReadOnlyDictionary<string, string> Metadata)
{

    #region Methods

    /// <summary>Returns localized prompt text with English as the explicit resource fallback.</summary>
    public string ResolveText(string language) =>
        Texts.TryGetValue(language, out var localized)
            ? localized
            : Texts.TryGetValue("en", out var english)
                ? english
                : throw new InvalidDataException($"Prompt '{Id}' has no text for '{language}' or English fallback.");

    #endregion

}

/// <summary>Carries the model response returned by a connection diagnostic.</summary>
public sealed record ConnectionTestResult(AiResponse Response);
