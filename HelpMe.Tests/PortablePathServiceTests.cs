// SPDX-License-Identifier: MIT
using HelpMe.Models;
using HelpMe.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpMe.Tests;

/// <summary>Verifies that Unix PATH integration rejects ambiguous entries.</summary>
public sealed class PortablePathServiceTests
{
    /// <summary>Writes only managed blocks in disposable profiles and restores their original contents on removal.</summary>
    [Theory]
    [InlineData(".bashrc")]
    [InlineData(".zshrc")]
    [InlineData("config.fish")]
    [InlineData("profile.ps1")]
    public async Task UnixProfile_InstallTwiceThenRemove_PreservesUserContent(string name)
    {
        using var fixture = new RegressionFixture();
        var profile = Path.Combine(fixture.Paths.DataDirectory, name);
        Directory.CreateDirectory(fixture.Paths.DataDirectory);
        const string original = "# My settings\n\n";
        await File.WriteAllTextAsync(profile, original);
        var plan = new PortablePathPlan(PortablePathAction.Install, "/tmp/hm's folder", profile, "", false, true);
        await PortablePathService.ApplyUnixProfileAsync(plan, default);
        var installed = await File.ReadAllTextAsync(profile);
        await PortablePathService.ApplyUnixProfileAsync(plan, default);
        Assert.Equal(installed, await File.ReadAllTextAsync(profile));
        await PortablePathService.ApplyUnixProfileAsync(plan with { Action = PortablePathAction.Remove }, default);
        Assert.Equal(original, await File.ReadAllTextAsync(profile));
    }

    /// <summary>Covers login and non-login Bash profiles while honoring XDG and Zsh configuration directories.</summary>
    [Fact]
    public void UnixTargets_UseShellSpecificConfiguration()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Equal([Path.Combine(root, ".bashrc"), Path.Combine(root, ".profile")],
            PortablePathService.UnixPersistenceTargets(root, "bash", null, null));
        var config = Path.Combine(root, "config");
        Assert.Equal([Path.Combine(config, "fish", "config.fish")],
            PortablePathService.UnixPersistenceTargets(root, "fish", config, null));
        Assert.Equal([Path.Combine(config, "powershell", "profile.ps1")],
            PortablePathService.UnixPersistenceTargets(root, "pwsh", config, null));
        Assert.Equal([Path.Combine(config, ".zshrc"), Path.Combine(config, ".zprofile")],
            PortablePathService.UnixPersistenceTargets(root, "zsh", null, config));
        Assert.Throws<UnsupportedPathShellException>(() => PortablePathService.UnixPersistenceTargets(root, null, null, null));
    }

    /// <summary>Checks that an unrepresentable Unix entry is rejected before the profile or process PATH changes.</summary>
    [Fact]
    public async Task ApplyAsync_RejectsUnixSeparatorBeforeWriting()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        // An invalid destination also prevents any write if validation regresses.
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        var service = new PortablePathService(NullLogger<PortablePathService>.Instance);
        var plan = new PortablePathPlan(PortablePathAction.Install, "/opt/hm::dev", string.Empty, "", false, true);

        await Assert.ThrowsAsync<AmbiguousUnixPathException>(() => service.ApplyAsync(plan, default));
        Assert.Equal(originalPath, Environment.GetEnvironmentVariable("PATH"));
    }
}
