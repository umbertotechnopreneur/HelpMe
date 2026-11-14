// SPDX-License-Identifier: MIT
using System.Text.Json;
using HelpMe.Models;
using HelpMe.Services;

namespace HelpMe.Tests;

/// <summary>Protects the interpreter boundary and conservative Bash review without starting processes.</summary>
public sealed class CommandShellTests
{
    /// <summary>Persists the user's shell choice without saving an invocation override.</summary>
    [Fact]
    public async Task Settings_ShellPreference_PersistsIndependentlyOfEffectiveShell()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = await fixture.Database.LoadSettingsAsync(default);
        var saved = original with { PreferredCommandShell = CommandShell.Bash, ScriptLanguage = ScriptLanguage.Python };
        await fixture.Database.SaveSettingsAsync(saved, default);
        var session = CommandShellResolver.ForInvocation(saved, CommandShell.PowerShell);
        await fixture.Database.SaveSettingsAsync(session, default);
        var reloaded = await fixture.Database.LoadSettingsAsync(default);
        Assert.Equal(CommandShell.Bash, reloaded.PreferredCommandShell);
        Assert.Equal(ScriptLanguage.Python, reloaded.ScriptLanguage);
        Assert.Equal("bash", await fixture.ScalarAsync("SELECT command_shell FROM app_settings;"));
    }

    /// <summary>Adds Auto to an older database without losing its existing script and user preferences.</summary>
    [Fact]
    public async Task Settings_VersionEight_AddsCommandShellWithoutChangingScriptLanguage()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = (await fixture.Database.LoadSettingsAsync(default)) with { ScriptLanguage = ScriptLanguage.Python, Language = "vi" };
        await fixture.Database.SaveSettingsAsync(original, default);
        await fixture.ScalarAsync("ALTER TABLE app_settings DROP COLUMN command_shell; PRAGMA user_version = 8;");
        await fixture.Database.InitializeAsync(default);
        Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
        Assert.Equal("auto", await fixture.ScalarAsync("SELECT command_shell FROM app_settings;"));
    }

    /// <summary>Distinguishes an absent switch from an explicit Auto override of a saved preference.</summary>
    [Fact]
    public void Invocation_ExplicitAuto_OverridesSavedShell()
    {
        var parser = new CommandLineParser(new LocalizationService());
        Assert.Null(parser.Parse(["--chat"]).Options!.Shell);
        var selected = parser.Parse(["--shell", "auto", "--chat"]).Options!;
        Assert.Equal(CommandShell.Auto, selected.Shell);
        var settings = AppSettings.Default with { PreferredCommandShell = CommandShell.Bash };
        Assert.Equal(CommandShell.Bash, CommandShellResolver.ForInvocation(settings, null).CommandShell);
        Assert.Equal(CommandShellResolver.Resolve(CommandShell.Auto), CommandShellResolver.ForInvocation(settings, selected.Shell).CommandShell);
    }

    /// <summary>Keeps a CLI override active while a Setup save changes the preference for later invocations.</summary>
    [Fact]
    public void Invocation_SettingsSave_DoesNotReplaceExplicitOverride()
    {
        var session = CommandShellResolver.ForInvocation(AppSettings.Default, CommandShell.PowerShell);
        var edited = session with { PreferredCommandShell = CommandShell.Bash };
        var continued = CommandShellResolver.ForInvocation(edited, session.CommandShellOverride);
        Assert.Equal(CommandShell.PowerShell, continued.CommandShell);
        Assert.Equal(CommandShell.Bash, continued.PreferredCommandShell);
        Assert.Equal(CommandShell.Bash, CommandShellResolver.ForInvocation(edited, null).CommandShell);
    }

    /// <summary>Rejects shell injection, duplicate options, and missing option values.</summary>
    [Theory]
    [InlineData("--shell", "bash -c")]
    [InlineData("--shell", "/bin/bash")]
    [InlineData("--shell", "")]
    [InlineData("--shell=bash", "--shell=pwsh")]
    public void Parser_InvalidShell_Rejects(string first, string second)
    {
        var parser = new CommandLineParser(new LocalizationService());
        Assert.False(parser.Parse([first, second]).Succeeded);
    }

    /// <summary>Keeps shell selection independent of the question and its execution mode.</summary>
    [Theory]
    [InlineData("bash", CommandShell.Bash)]
    [InlineData("pwsh", CommandShell.PowerShell)]
    [InlineData("auto", CommandShell.Auto)]
    public void Parser_Shell_PreservesQuestion(string name, CommandShell shell)
    {
        var parser = new CommandLineParser(new LocalizationService());
        var result = parser.Parse(["--shell", name, "--direct", "inspect files"]);
        Assert.True(result.Succeeded);
        Assert.Equal(shell, result.Options!.Shell);
        Assert.Equal("inspect files", result.Options.Query);
        Assert.Equal(AppCommand.Direct, result.Options.Command);
    }

    /// <summary>Never permits Bash expansion, writes, or unknown programs through direct execution.</summary>
    [Theory]
    [InlineData("echo $(touch marker)")]
    [InlineData("ls > marker")]
    [InlineData("pwd; touch marker")]
    [InlineData("git\nstatus")]
    [InlineData("ls\n-alh")]
    [InlineData("p'w'd")]
    [InlineData("find . -exec rm {} +")]
    [InlineData("source ./script")]
    [InlineData("bash -c 'pwd'")]
    [InlineData("chmod 777 file")]
    [InlineData("rm -rf directory")]
    [InlineData("Get-Date")]
    public void Bash_ComplexOrMutatingCommand_BlocksDirect(string command)
    {
        var assessment = CommandRiskAssessmentService.AssessLocally(command, "en", CommandShell.Bash) with { UsedAi = true };
        Assert.False(assessment.CanRunDirect);
        Assert.Equal(CommandShell.Bash, assessment.Shell);
    }

    /// <summary>Retains the AI-review requirement for literal inspection commands.</summary>
    [Theory]
    [InlineData("pwd")]
    [InlineData("ls -lah")]
    [InlineData("git status --short")]
    public void Bash_Inspection_StillRequiresAi(string command)
    {
        var assessment = CommandRiskAssessmentService.AssessLocally(command, "en", CommandShell.Bash);
        Assert.False(assessment.CanRunDirect);
        Assert.True((assessment with { UsedAi = true }).CanRunDirect);
    }

    /// <summary>Ensures approval cannot carry an unresolved interpreter into the runner.</summary>
    [Fact]
    public void Approval_AutomaticShell_Rejects()
    {
        var assessment = new CommandRiskAssessment(15, CommandRiskLevel.Low, "Inspect", false, null) { Shell = CommandShell.Auto };
        Assert.Throws<ArgumentException>(() => ApprovedCommand.Create("pwd", assessment));
    }

    /// <summary>Transports the exact approved Bash command as one argument without interpolation.</summary>
    [Fact]
    public void Bash_StartInfo_PreservesCommandAndDisablesStartup()
    {
        if (OperatingSystem.IsWindows()) return;
        const string command = "printf '%s' 'quoted value; $HOME'";
        var approved = ApprovedCommand.Create(command,
            CommandRiskAssessmentService.AssessLocally(command, "en", CommandShell.Bash));
        var start = CommandExecutionService.BuildStartInfo(approved);
        Assert.Equal("bash", start.FileName);
        Assert.Equal(["--noprofile", "--norc", "-p", "-c", command], start.ArgumentList);
        Assert.False(start.Environment.ContainsKey("BASH_ENV"));
        Assert.False(start.Environment.ContainsKey("ENV"));
    }

    /// <summary>Preserves the plan interpreter through serialization independently of the next session.</summary>
    [Fact]
    public void Plan_Serialization_PreservesRequiredShell()
    {
        var plan = new ExecutionPlan(1, Guid.NewGuid().ToString("N"), "Inspect", "/work", []) { Shell = CommandShell.Bash };
        var json = JsonSerializer.Serialize(plan);
        Assert.Equal(CommandShell.Bash, JsonSerializer.Deserialize<ExecutionPlan>(json)!.Shell);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ExecutionPlan>("{}"));
    }

    /// <summary>Supplies the actual execution shell while keeping script language independent.</summary>
    [Fact]
    public void RuntimeContext_Bash_DoesNotClaimPowerShellExecution()
    {
        var service = new RuntimeContextService(new ScriptLanguageCatalog(), new SensitiveDataRedactor());
        var context = service.GetCurrent(ScriptLanguage.PowerShell, CommandShell.Bash);
        Assert.StartsWith("Bash (", context.CommandShell);
        Assert.Equal("PowerShell 7", context.PreferredScriptInterpreter);
    }
}
