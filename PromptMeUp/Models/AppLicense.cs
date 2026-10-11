// SPDX-License-Identifier: MIT
namespace PromptMeUp.Models;

public enum AppLicenseState
{
    Source,
    Development,
    Full,
    Trial,
    Expired,
    Unlicensed,
    Unavailable
}

/// <summary>Describes Store entitlement without storing or inventing a local trial start date.</summary>
public sealed record AppLicense(AppLicenseState State, DateTimeOffset? ExpiresAt = null)
{
    public const string StoreProductId = "9P2WJBVQMW4J";
    public const string StoreUrl = "https://apps.microsoft.com/detail/" + StoreProductId;

    /// <summary>Evaluates trial expiration against UTC even when the process stays open overnight.</summary>
    public AppLicense At(DateTimeOffset now) => State == AppLicenseState.Trial
        && (!ExpiresAt.HasValue || ExpiresAt.Value <= now)
            ? this with { State = AppLicenseState.Expired } : this;

    /// <summary>Rounds remaining partial days up so the last active day is displayed as one day.</summary>
    public int DaysRemaining(DateTimeOffset now) => State == AppLicenseState.Trial && ExpiresAt.HasValue
        ? (int)Math.Clamp(Math.Ceiling((ExpiresAt.Value - now).TotalDays), 0, int.MaxValue) : 0;

    public bool AllowsCommands => State is AppLicenseState.Source or AppLicenseState.Development
        or AppLicenseState.Full or AppLicenseState.Trial;
}
