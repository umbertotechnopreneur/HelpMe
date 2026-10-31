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

using System.IO.Compression;
using Microsoft.Extensions.Logging;
using HelpMe.Models;

namespace HelpMe.Services;

/// <summary>Loads the bundled Lenna illustration used by the terminal view.</summary>
public interface ILennaImageService
{
    #region Methods

    /// <summary>Loads the requested value from its configured local source.</summary>
    TerminalImage Load();

    #endregion
}

/// <summary>Reads the bundled Lenna portrait without filesystem access or image decoder dependencies.</summary>
public sealed class LennaImageService(
    ILocalizationService text,
    ILogger<LennaImageService> logger) : ILennaImageService
{
    #region Fields

    private const string ResourceName = "HelpMe.Assets.lenna.rgb.gz";
    private const int ImageWidth = 78;
    private const int ImageHeight = 78;
    private const int PixelByteCount = ImageWidth * ImageHeight * 3;
    private const int MaximumResourceBytes = PixelByteCount + 1024;

    #endregion

    #region Methods

    /// <summary>Decodes exactly one bounded RGB portrait and reports damaged resources in the active language.</summary>
    public TerminalImage Load()
    {
        try
        {
            using var resource = typeof(LennaImageService).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException("The bundled Lenna resource is missing.");
            if (resource.Length is < 1 or > MaximumResourceBytes)
            {
                throw new InvalidDataException("The bundled Lenna resource has an unsupported size.");
            }

            using var decoded = new GZipStream(resource, CompressionMode.Decompress);
            var pixels = new byte[PixelByteCount];
            decoded.ReadExactly(pixels);
            if (decoded.ReadByte() != -1)
            {
                throw new InvalidDataException("The bundled Lenna resource contains extra pixel data.");
            }

            logger.LogDebug("Loaded the bundled Lenna portrait at {Width} by {Height} pixels.", ImageWidth, ImageHeight);
            return new TerminalImage(ImageWidth, ImageHeight, pixels);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            logger.LogError(exception, "Could not decode the bundled Lenna portrait.");
            throw new InvalidOperationException(text.Text("Lenna.LoadError"), exception);
        }
    }

    #endregion
}
