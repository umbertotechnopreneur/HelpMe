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

using System.Globalization;

namespace HelpMe.Services;

/// <summary>Describes a supported language by its code and display names.</summary>
public sealed record SupportedLanguage(string Code, string NativeName, string CultureName, string Flag);

/// <summary>Lists the languages supported by HelpMe's localized terminal interface.</summary>
public static class SupportedLanguages
{
    #region Properties

    public static IReadOnlyList<SupportedLanguage> All { get; } =
    [
        new("it", "Italiano", "it-IT", "🇮🇹"),
        new("en", "English", "en-US", "🇺🇸"),
        new("fr", "Français", "fr-FR", "🇫🇷"),
        new("de", "Deutsch", "de-DE", "🇩🇪"),
        new("es", "Español", "es-ES", "🇪🇸"),
        new("vi", "Tiếng Việt", "vi-VN", "🇻🇳")
    ];

    public static IReadOnlyList<string> Codes { get; } = All.Select(language => language.Code).ToArray();

    #endregion

    #region Methods

    /// <summary>Checks whether a two-letter interface language is advertised by the product.</summary>
    public static bool IsSupported(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && All.Any(language => string.Equals(language.Code, value.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the canonical lower-case code for one supported language.</summary>
    public static string Normalize(string value) =>
        All.First(language => string.Equals(language.Code, value.Trim(), StringComparison.OrdinalIgnoreCase)).Code;

    /// <summary>Uses the current UI culture when supported and otherwise selects English.</summary>
    public static string ResolveSystemLanguage()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return IsSupported(twoLetter) ? Normalize(twoLetter) : "en";
    }

    /// <summary>Resolves the display culture used for localized numbers, dates, and text formatting.</summary>
    public static CultureInfo Culture(string language) =>
        CultureInfo.GetCultureInfo(All.First(item => item.Code == Normalize(language)).CultureName);

    #endregion
}

/// <summary>Selects the UI language and resolves localized message templates.</summary>
public interface ILocalizationService
{
    #region Properties

    string Language { get; }

    CultureInfo Culture { get; }

    #endregion

    #region Methods

    /// <summary>Selects the supported language used for subsequent UI messages.</summary>
    void SetLanguage(string language);

    /// <summary>Formats a localized message using the selected UI language.</summary>
    string Text(string key, params object?[] args);

    #endregion
}

/// <summary>Resolves localized UI strings and formats their parameter values for the active language.</summary>
public sealed class LocalizationService : ILocalizationService
{
    #region Properties

    public string Language { get; private set; } = "en";

    public CultureInfo Culture => SupportedLanguages.Culture(Language);

    #endregion

    #region Methods

    /// <summary>Applies one supported interface language for the current invocation.</summary>
    public void SetLanguage(string language) => Language = SupportedLanguages.Normalize(language);

    /// <summary>Resolves one UI string and formats its values using the selected language culture.</summary>
    public string Text(string key, params object?[] args)
    {
        var template = UiTextCatalog.TryGet(key, Language, out var translation)
            ? translation
            : key;
        return args.Length == 0 ? template : string.Format(Culture, template, args);
    }

    #endregion
}
