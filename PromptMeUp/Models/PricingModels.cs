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

/// <summary>Stores model pricing and token-cost units used for local estimates.</summary>
public sealed record AiModelPrice(
    string Provider,
    string Model,
    string ServiceTier,
    string ContextWindow,
    string Currency,
    decimal InputUsdPerMillionTokens,
    decimal? CachedInputUsdPerMillionTokens,
    decimal? CacheWriteUsdPerMillionTokens,
    decimal OutputUsdPerMillionTokens,
    string SourceUrl,
    DateTimeOffset RetrievedAt);

/// <summary>Records provider-reported organization costs for a billing interval.</summary>
public sealed record OrganizationCost(
    string Id,
    DateTimeOffset BucketStart,
    DateTimeOffset BucketEnd,
    decimal Amount,
    string Currency,
    string? LineItem,
    string? ProjectId,
    DateTimeOffset RetrievedAt);

/// <summary>Reports the outcome and timing of a pricing refresh.</summary>
public sealed record PricingRefreshResult(
    int PriceRows,
    int OrganizationCostRows,
    bool PricesRefreshed,
    bool OrganizationCostsAvailable,
    DateTimeOffset CompletedAt);

/// <summary>Combines usage totals, estimated costs, and available provider billing data.</summary>
public sealed record CostOverview(
    DateTimeOffset? LastPricingSync,
    DateTimeOffset? LastOrganizationCostSync,
    decimal EstimatedCostTodayUsd,
    decimal EstimatedCostCurrentMonthUsd,
    decimal? ActualOrganizationCostCurrentMonthUsd,
    int RequestsToday,
    long InputTokensToday,
    long OutputTokensToday,
    long TotalTokensToday,
    IReadOnlyList<AiModelPrice> Prices);

/// <summary>Summarizes request counts, token usage, and costs for local reporting.</summary>
public sealed record AiRequestSummary(
    decimal EstimatedCostTodayUsd,
    decimal EstimatedCostCurrentMonthUsd,
    int RequestsToday,
    long InputTokensToday,
    long OutputTokensToday,
    long TotalTokensToday);
