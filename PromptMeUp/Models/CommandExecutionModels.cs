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

/// <summary>Selects confirmation or direct execution through the shared authorization gate.</summary>
public enum CommandExecutionMode
{
    Confirm,
    Direct
}

/// <summary>Classifies the local risk assessment used to gate command execution.</summary>
public enum CommandRiskLevel
{
    Unknown,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>Combines deterministic risk scoring with optional advisory AI review.</summary>
public sealed record CommandRiskAssessment(
    int Score,
    CommandRiskLevel Level,
    string DescriptionMarkdown,
    bool UsedAi,
    string? Advisory)
{

    #region Properties

    public bool CanRunDirect => UsedAi && Score is >= 0 and < 60
        && Level is CommandRiskLevel.Low or CommandRiskLevel.Medium;

    #endregion

}

/// <summary>Describes a locally reviewed command that may be executed in the selected mode.</summary>
public sealed class ApprovedCommand
{

    #region Initialization

    /// <summary>Creates the execution capability after the workflow completes the selected authorization gate.</summary>
    private ApprovedCommand(string text, CommandRiskAssessment assessment)
    {
        Text = text;
        Assessment = assessment;
        ApprovedAt = DateTimeOffset.UtcNow;
        AuthorizationId = Guid.NewGuid().ToString("N");
    }

    #endregion

    #region Properties

    public string Text { get; }

    public CommandRiskAssessment Assessment { get; }

    public DateTimeOffset ApprovedAt { get; }

    internal string AuthorizationId { get; }

    #endregion

    #region Methods

    /// <summary>Creates the short-lived capability after confirmation or an explicitly requested direct countdown.</summary>
    internal static ApprovedCommand Create(string text, CommandRiskAssessment assessment) => new(text, assessment);

    #endregion

}

/// <summary>Captures command output, termination status, timing, and truncation flags.</summary>
public sealed record CommandExecutionResult(
    string Command,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    bool OutputTruncated,
    long ElapsedMilliseconds)
{

    #region Properties

    public bool Cancelled { get; init; }

    public bool OutputWasStreamed { get; init; }

    #endregion

}

/// <summary>Describes one styled segment of streamed terminal output.</summary>
public sealed record CommandOutputSpan(string Text, int Foreground, bool Bold);

/// <summary>Carries streamed output segments and their error-stream and line-completion flags.</summary>
public sealed record CommandOutputUpdate(IReadOnlyList<CommandOutputSpan> Spans, bool IsError, bool CompleteLine);
