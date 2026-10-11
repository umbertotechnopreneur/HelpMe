// SPDX-License-Identifier: MIT
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Application;

/// <summary>Blocks operational commands before storage, provider calls, or process execution after expiration.</summary>
public sealed class AppLicenseGuard(IAppLicenseService licenses, IAnsiConsole console, ILocalizationService text)
{
    public bool AllowsCommands => licenses.Current.AllowsCommands;

    /// <summary>Refreshes entitlement while preserving only Help and About when the license is inactive.</summary>
    public async Task<bool> CheckAsync(AppCommand command, CancellationToken cancellationToken)
    {
        await licenses.RefreshAsync(cancellationToken).ConfigureAwait(false);
        if (command is AppCommand.Help or AppCommand.About || AllowsCommands) return true;
        console.Write(TrialNotice.Create(console, text));
        console.WriteLine();
        console.MarkupLine(Markup.Escape(text.Text("Trial.Blocked")));
        return false;
    }

    /// <summary>Rechecks entitlement before an already-open session starts paid work.</summary>
    public async Task RequireAsync(CancellationToken cancellationToken)
    {
        if (!await CheckAsync(AppCommand.Chat, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException(text.Text("Trial.Blocked"));
    }
}
