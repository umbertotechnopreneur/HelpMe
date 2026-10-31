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

using System.Collections.Immutable;

namespace HelpMe.Models;

/// <summary>Stores a bounded, immutable RGB image independently of terminal rendering.</summary>
public sealed class TerminalImage
{

    #region Fields

    private const int MaximumDimension = 1024;

    #endregion

    #region Initialization

    /// <summary>Copies a complete row-major RGB8 image after validating its dimensions.</summary>
    public TerminalImage(int width, int height, ReadOnlySpan<byte> pixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaximumDimension);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaximumDimension);

        if (pixels.Length != checked(width * height * 3))
        {
            throw new ArgumentException("Pixel data must contain exactly three bytes per image pixel.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = ImmutableArray.Create(pixels.ToArray());
    }

    #endregion

    #region Properties

    public int Width { get; }

    public int Height { get; }

    public ImmutableArray<byte> Pixels { get; }

    #endregion

}
