// SPDX-License-Identifier: MIT
using HelpMe.Models;
using HelpMe.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace HelpMe.Views;

/// <summary>Shares a small localized trial countdown and Store link across terminal surfaces.</summary>
internal static class TrialNotice
{

    #region Methods

    /// <summary>Creates the current notice with optional onboarding guidance, omitting paid and source builds.</summary>
    internal static IRenderable Create(IAnsiConsole console, ILocalizationService text, bool detailed = false)
    {
        var markup = MarkupFor(text, TerminalSession.For(console).License?.Invoke());

        if (markup.Length == 0) return Text.Empty;

        return detailed
            ? new Rows(new Markup(markup), new Text(text.Text("Trial.Explanation"), Style.Parse(TerminalTheme.Muted)))
            : new Markup(markup);
    }

    /// <summary>Formats one countdown or expired notice followed by a dash and a clickable Store action.</summary>
    internal static string MarkupFor(ILocalizationService text, AppLicense? license)
    {
        var parts = Describe(text, license);

        return parts.HasValue ? Format(parts.Value.Message, parts.Value.Label) : string.Empty;
    }

    /// <summary>Keeps prompt rows within the viewport while preserving the clickable Store action.</summary>
    internal static IReadOnlyList<string> Rows(ILocalizationService text, AppLicense? license, int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        var parts = Describe(text, license);

        if (!parts.HasValue) return [];

        var (message, label) = parts.Value;

        if (TerminalText.Width(message + " - " + label) <= width) return [Format(message, label)];

        var prefix = width > 2 ? "- " : string.Empty;

        return
        [
            $"[{TerminalTheme.Muted}]{Markup.Escape(TerminalText.Clip(message, width))}[/]",
            prefix + $"[underline {TerminalTheme.Info} link={AppLicense.StoreUrl}]{Markup.Escape(TerminalText.Clip(label, width - prefix.Length))}[/]"
        ];
    }

    #endregion

    #region Implementation

    /// <summary>Resolves the live countdown or inactive entitlement and its localized Store action.</summary>
    private static (string Message, string Label)? Describe(ILocalizationService text, AppLicense? license)
    {
        var now = DateTimeOffset.UtcNow;

        license = license?.At(now);

        if (license is null || license.State is AppLicenseState.Source or AppLicenseState.Development or AppLicenseState.Full)
            return null;

        var message = license.State switch
        {
            AppLicenseState.Trial => text.Text(license.DaysRemaining(now) == 1 ? "Trial.OneDay" : "Trial.Days", license.DaysRemaining(now)),
            AppLicenseState.Expired => text.Text("Trial.Expired"),
            AppLicenseState.Unlicensed => text.Text("Trial.Unlicensed"),
            _ => text.Text("Trial.Unavailable")
        };
        var label = text.Text(license.State == AppLicenseState.Trial ? "Trial.Store" : "Trial.Purchase");

        return (message, label);
    }

    /// <summary>Builds the shared one-line countdown and hyperlink without changing terminal cell content.</summary>
    private static string Format(string message, string label) =>
        $"[{TerminalTheme.Muted}]{Markup.Escape(message)} - [/][underline {TerminalTheme.Info} link={AppLicense.StoreUrl}]{Markup.Escape(label)}[/]";

    #endregion
}
