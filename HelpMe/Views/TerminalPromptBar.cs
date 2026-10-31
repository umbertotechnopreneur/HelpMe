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

namespace HelpMe.Views;

/// <summary>Composes the reusable session strip with optional prompt separators.</summary>
internal sealed class TerminalPromptBar(
    ILocalizationService text,
    ShellRuntimeStatus? status,
    bool showBorders = true,
    bool showStatus = true,
    bool showBreakdown = true,
    TerminalSession? session = null)
{

    #region Methods

    /// <summary>Returns complete status rows without splitting a metric label from its value.</summary>
    internal IReadOnlyList<string> Header(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        var rows = new List<string>();

        if (showBorders) rows.Add(ThemeSeparator.Markup(width));

        if (showStatus) rows.AddRange(new TerminalSessionStrip(text, status, session).Rows(width, showBreakdown));

        rows.AddRange(TrialNotice.Rows(text, session?.License?.Invoke(), width));

        return rows;
    }

    /// <summary>Returns an optional bottom separator without enclosing the conversation in a card.</summary>
    internal IReadOnlyList<string> Footer(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        return showBorders ? [ThemeSeparator.Markup(width)] : [];
    }

    #endregion
}
