// SPDX-License-Identifier: MIT
using PromptMeUp.Application;

namespace PromptMeUp.Services;

/// <summary>Stops provider requests from sessions that remain open past trial expiration.</summary>
public sealed class LicenseGuardHandler(AppLicenseGuard license) : DelegatingHandler
{
    #region Methods

    /// <summary>Checks licensing before sending any provider request, including streaming follow-ups.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await license.RequireAsync(cancellationToken).ConfigureAwait(false);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
