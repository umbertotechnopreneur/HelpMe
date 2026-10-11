// SPDX-License-Identifier: MIT
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
#if WINDOWS
using System.Runtime.InteropServices;
using Windows.Services.Store;
#endif

namespace PromptMeUp.Services;

public interface IAppLicenseService
{
    AppLicense Current { get; }
    Task RefreshAsync(CancellationToken cancellationToken);
}

/// <summary>Reads the packaged Windows app license; source and Debug builds have no commercial trial.</summary>
public sealed class AppLicenseService(ILogger<AppLicenseService> logger, TimeProvider clock) : IAppLicenseService
{
    private AppLicense _license = new(AppLicenseState.Unavailable);

    public AppLicense Current => _license.At(clock.GetUtcNow());

    /// <summary>Uses the Store's cached entitlement and expiration, failing closed when licensing is unavailable.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsDevelopmentBuild)
        {
            _license = new(AppLicenseState.Development);
            logger.LogDebug("Commercial licensing is disabled in Debug builds.");
            return;
        }
#if WINDOWS
        uint length = 0;
        var identityResult = GetCurrentPackageFullName(ref length, IntPtr.Zero);
        if (identityResult == 15700) // APPMODEL_ERROR_NO_PACKAGE identifies an unpackaged source build.
        {
            _license = new(AppLicenseState.Source);
            return;
        }
        _license = new(AppLicenseState.Unavailable);
        try
        {
            if (identityResult != 122)
            {
                throw new InvalidOperationException("Unable to resolve the Windows package identity.");
            }
            var window = GetConsoleWindow();
            if (window == IntPtr.Zero)
            {
                throw new InvalidOperationException("A console window is required to initialize Store licensing.");
            }
            var context = StoreContext.GetDefault();
            WinRT.Interop.InitializeWithWindow.Initialize(context, window);
            var license = await context.GetAppLicenseAsync().AsTask(cancellationToken).ConfigureAwait(false);
            _license = new(license.IsTrial
                ? license.IsActive ? AppLicenseState.Trial : AppLicenseState.Expired
                : license.IsActive ? AppLicenseState.Full : AppLicenseState.Unlicensed,
                license.IsTrial ? license.ExpirationDate : null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Store license could not be read. ErrorType={ErrorType}, HResult={HResult}",
                exception.GetType().Name, exception.HResult);
        }
#else
        _license = new(AppLicenseState.Source);
        logger.LogDebug("Commercial licensing is not used by this source build.");
        await Task.CompletedTask;
#endif
    }

    private static bool IsDevelopmentBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

#if WINDOWS
    /// <summary>Checks package identity without reading a manifest or using executable location as entitlement.</summary>
    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);

    /// <summary>Obtains this process's console owner for the desktop Store API.</summary>
    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();
#endif
}
