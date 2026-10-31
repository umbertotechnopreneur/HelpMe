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

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using HelpMe.Services;
using HelpMe.Views;

namespace HelpMe.Application;

/// <summary>Coordinates explicit PDF opening and gives local recovery guidance if the reader cannot start.</summary>
public sealed class CommandGuideWorkflow(CommandGuideService guide, IConsoleShellView shell,
    ILocalizationService text, ILogger<CommandGuideWorkflow> logger)
{

    #region Properties

    public string DocumentPath => guide.DocumentPath;

    #endregion

    #region Methods

    /// <summary>Opens the bundled guide on request while keeping failures visible and onboarding complete.</summary>
    public void Open()
    {
        try
        {
            guide.Open();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or Win32Exception or InvalidOperationException or NotSupportedException)
        {
            logger.LogWarning("Command guide launch failed. ExceptionType={ExceptionType}", exception.GetType().FullName);
            shell.RenderWarning(text.Text(exception is FileNotFoundException ? "Guide.Missing" : "Guide.OpenFailed", DocumentPath));
        }
    }

    #endregion

}
