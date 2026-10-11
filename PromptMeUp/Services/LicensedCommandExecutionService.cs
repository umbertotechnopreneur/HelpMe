// SPDX-License-Identifier: MIT
using PromptMeUp.Application;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Rechecks licensing after interactive approval and before launching a child process.</summary>
public sealed class LicensedCommandExecutionService(CommandExecutionService commands, AppLicenseGuard license) : ICommandExecutionService
{
    /// <summary>Runs an approved command only while the current Store entitlement still permits work.</summary>
    public async Task<CommandExecutionResult> ExecuteAsync(ApprovedCommand command, TimeSpan timeout,
        CancellationToken cancellationToken, Action<CommandOutputUpdate>? output = null, CancellationToken stopToken = default)
    {
        await license.RequireAsync(cancellationToken).ConfigureAwait(false);
        return await commands.ExecuteAsync(command, timeout, cancellationToken, output, stopToken).ConfigureAwait(false);
    }
}
