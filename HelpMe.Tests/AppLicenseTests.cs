// SPDX-License-Identifier: MIT
using HelpMe.Application;
using HelpMe.Models;
using HelpMe.Services;
using HelpMe.Views;
using Spectre.Console;

namespace HelpMe.Tests;

/// <summary>Verifies app license behavior.</summary>
public sealed class AppLicenseTests
{

    #region Methods

    /// <summary>Checks the last three days, partial days, and the exact expiration boundary.</summary>
    [Theory]
    [InlineData(72, 3)]
    [InlineData(48, 2)]
    [InlineData(24, 1)]
    [InlineData(0.5, 1)]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void Trial_UsesUtcDeadlineAndRoundsRemainingDays(double hours, int days)
    {
        var now = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        var license = new AppLicense(AppLicenseState.Trial, now.AddHours(hours));
        Assert.Equal(days, license.DaysRemaining(now));
        Assert.Equal(hours > 0, license.At(now).AllowsCommands);
        Assert.Equal(hours <= 0 ? AppLicenseState.Expired : AppLicenseState.Trial, license.At(now).State);
    }

    /// <summary>Rejects incomplete trial data instead of granting an unlimited trial.</summary>
    [Fact]
    public void Trial_WithoutExpiration_BlocksCommands() =>
        Assert.False(new AppLicense(AppLicenseState.Trial).At(DateTimeOffset.UtcNow).AllowsCommands);

    /// <summary>Checks every CLI command against the expired license allowlist before resolving feature services.</summary>
    [Fact]
    public async Task ExpiredLicense_AllowsOnlyHelpAndAbout()
    {
        var text = new LocalizationService();
        var guard = CreateGuard(new(AppLicenseState.Expired), text);
        foreach (var command in Enum.GetValues<AppCommand>())
        {
            Assert.Equal(command is AppCommand.About or AppCommand.Help, await guard.CheckAsync(command, default));
        }
    }

    /// <summary>Distinguishes an unavailable entitlement from an active paid or development license.</summary>
    [Theory]
    [InlineData(AppLicenseState.Unavailable, false)]
    [InlineData(AppLicenseState.Unlicensed, false)]
    [InlineData(AppLicenseState.Full, true)]
    [InlineData(AppLicenseState.Development, true)]
    [InlineData(AppLicenseState.Source, true)]
    public async Task Guard_UsesEntitlementRatherThanPackagePresence(AppLicenseState state, bool enabled)
    {
        var guard = CreateGuard(new(state), new LocalizationService());
        Assert.Equal(enabled, await guard.CheckAsync(AppCommand.Reset, default));
    }

    /// <summary>Checks that expiration blocks routing before any command handler can touch data or providers.</summary>
    [Fact]
    public async Task Router_ExpiredLicense_DoesNotResolveOperationalHandlers()
    {
        var text = new LocalizationService();
        var router = new ApplicationCommandRouter(null!, null!, null!, null!,
            CreateGuard(new(AppLicenseState.Expired), text), null!);
        var options = new CommandLineParser(text).Parse(["--setup"]).Options!;
        Assert.Equal(3, await router.DispatchAsync(options, AppSettings.Default, 0, default));
    }

    /// <summary>Ensures the six UI translations provide the countdown and a Store purchase link.</summary>
    [Theory]
    [InlineData("it")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void Notices_ShowCountdownAndPurchaseInEveryLanguage(string language)
    {
        var text = new LocalizationService();
        text.SetLanguage(language);
        var active = TrialNotice.MarkupFor(text, new(AppLicenseState.Trial, DateTimeOffset.UtcNow.AddHours(12)));
        var expired = TrialNotice.MarkupFor(text, new(AppLicenseState.Expired));
        Assert.Contains(text.Text("Trial.OneDay"), active, StringComparison.Ordinal);
        Assert.Contains(AppLicense.StoreUrl, active, StringComparison.Ordinal);
        Assert.Contains(text.Text("Trial.Purchase"), expired, StringComparison.Ordinal);
        Assert.Contains(" - ", expired, StringComparison.Ordinal);
        Assert.Empty(TrialNotice.MarkupFor(text, new(AppLicenseState.Full)));
    }

    /// <summary>Prevents wrapped notice rows from corrupting the prompt viewport on narrow terminals.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(40)]
    [InlineData(100)]
    public void NoticeRows_FitNarrowPromptWidths(int width)
    {
        var rows = TrialNotice.Rows(new LocalizationService(),
            new(AppLicenseState.Trial, DateTimeOffset.UtcNow.AddHours(12)), width);
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.InRange(TerminalText.Width(Markup.Remove(row)), 0, width));
        Assert.Contains(rows, row => row.Contains("link=" + AppLicense.StoreUrl, StringComparison.Ordinal));
    }

    /// <summary>Provides an in-memory license and terminal without calling the Store or writing app data.</summary>
    internal static AppLicenseGuard CreateGuard(AppLicense license, ILocalizationService text)
    {
        var service = TestProxy.Create<IAppLicenseService>((method, _) => method.Name switch
        {
            "get_Current" => license.At(DateTimeOffset.UtcNow),
            "RefreshAsync" => Task.CompletedTask,
            _ => throw new NotSupportedException(method.Name)
        });
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()), Ansi = AnsiSupport.No });
        TerminalSession.For(console).License = () => service.Current;
        return new AppLicenseGuard(service, console, text);
    }

    #endregion
}
