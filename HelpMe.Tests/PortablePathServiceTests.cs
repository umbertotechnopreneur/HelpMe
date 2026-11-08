// SPDX-License-Identifier: MIT
using HelpMe.Models;
using HelpMe.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpMe.Tests;

/// <summary>Verifies that Unix PATH integration rejects ambiguous entries.</summary>
public sealed class PortablePathServiceTests
{
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
