# SPDX-License-Identifier: MIT
# VBWR B
#
# Project: HelpMe
# Repository: https://github.com/umbertotechnopreneur/PromptMeUp
# Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
#
# VibeWare: Human intent, AI execution, and plenty of tokens
# Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
#
# Modified with AI: OpenAI Codex; added this header on 2026-10-10.
# Human guidance: Umberto Giacobbi; requested VibeWare branding.
#
# Copyright (c) 2026 Umberto Giacobbi
# License: MIT - see LICENSE
#
# VBWR E

<#
.SYNOPSIS
  Single entry point for HelpMe repository operations.

.DESCRIPTION
  Opens an interactive menu when no parameters are supplied. Use -Command for
  automation and CI. Command implementations remain private under Common.
#>
[CmdletBinding()]
param(
    [string]$Command,
    [switch]$Help,
    [switch]$Verify,
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Runtime,
    [string]$OutputDirectory,
    [string]$PublishDirectory,
    [ValidateSet('x64', 'arm64')]
    [string]$Architecture,
    [string]$IsccPath,
    [string]$Version,
    [ValidateSet('x64', 'arm64')]
    [string[]]$PortableArchitectures,
    [ValidateSet('x64')]
    [string[]]$MsiArchitectures,
    [string]$PackageIdentifier,
    [string]$ArtifactBaseUrl,
    [string]$Wix3BinDirectory,
    [switch]$SkipMsi,
    [switch]$SkipRestore,
    [switch]$SkipWingetValidation,
    [switch]$PlanOnly,
    [ValidatePattern('^[A-Fa-f0-9]{40}$')]
    [string]$CertificateThumbprint,
    [ValidateSet('Debug', 'Store')]
    [string]$Channel,
    [switch]$Unsigned,
    [string]$SdkBinDirectory,
    [uri]$TimestampServer,
    [ValidateSet('install', 'remove', 'status')]
    [string]$Action,
    [string]$ExecutablePath,
    [switch]$Yes
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$commandRoot = Join-Path $scriptRoot 'Common\commands'
$boundParameters = $PSBoundParameters

. (Join-Path $scriptRoot 'Common\menu-ui.ps1')

# Writes the consolidated command reference.
function Show-HelpMeHelp {
    @'
USAGE
  pwsh -NoProfile -File .\scripts\HelpMe.ps1
  pwsh -NoProfile -File .\scripts\HelpMe.ps1 -Command <command> [options]

COMMANDS
  menu                         Open the interactive launcher.
  preflight                    Check required repository tools and files.
  status                       Show the current Git working-tree status.
  format                       Apply dotnet formatting; add -Verify to check only.
  check-format                 Verify formatting without changing files.
  check-xml                    Check XML summaries on C# implementation methods.
  notices                      Export third-party notices for -Runtime.
  path                         Run hm --path with -Action install|remove|status.
  portable                     Build one portable archive.
  release-artifacts            Build or plan the legacy release artifact set.
  windows-installer            Package a prepared portable Windows payload.
  msix                         Package a prepared Windows publish directory as MSIX.
  install-debug-msix           Build, sign, install, and check a Debug MSIX for -Version.
  store-msix                   Build unsigned x64 and ARM64 Store MSIX packages.
  help                         Show this reference.

ALIASES
  repo-status, check-xml-comments, export-third-party-notices,
  build-portable-release, build-release-artifacts,
  build-windows-installer, package-msix, build-store-msix

EXAMPLES
  pwsh -NoProfile -File .\scripts\HelpMe.ps1 -Command preflight
  pwsh -NoProfile -File .\scripts\HelpMe.ps1 -Command format -Verify
  pwsh -NoProfile -File .\scripts\HelpMe.ps1 -Command notices -Runtime win-x64 -OutputDirectory .\artifacts\notices
  pwsh -NoProfile -File .\scripts\HelpMe.ps1 -Command msix -PublishDirectory .\artifacts\publish -Architecture x64 -Channel Debug -CertificateThumbprint <thumbprint>
  pwsh -NoProfile -File .\scripts\HelpMe.ps1 -Command install-debug-msix -Version 1.0.0.2
'@ | Write-Host
}

# Adds one named value to a private command parameter map when supplied.
function Add-HelpMeValueArgument {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Arguments,
        [Parameter(Mandatory)][string]$Name,
        $Value
    )

    if ($null -eq $Value) {
        return
    }
    if ($Value -is [string] -and [string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $Arguments[$Name] = $Value
}

# Adds one enabled switch to a private command parameter map.
function Add-HelpMeSwitchArgument {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Arguments,
        [Parameter(Mandatory)][string]$Name,
        [switch]$Enabled
    )

    if ($Enabled) {
        $Arguments[$Name] = $true
    }
}

# Invokes one private command implementation in its own script scope.
function Invoke-HelpMeInternalCommand {
    param(
        [Parameter(Mandatory)][string]$ScriptName,
        [Collections.IDictionary]$Arguments = @{}
    )

    $scriptPath = Join-Path $commandRoot $ScriptName
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw "Internal HelpMe command not found: $scriptPath"
    }

    & $scriptPath @Arguments | Out-Host
    return 0
}

# Requires a non-empty parameter before a direct command is dispatched.
function Assert-HelpMeParameter {
    param(
        [Parameter(Mandatory)][string]$Name,
        $Value
    )

    if ($null -eq $Value -or ($Value -is [string] -and [string]::IsNullOrWhiteSpace($Value))) {
        throw "-$Name is required for -Command $Command."
    }
}

# Creates the exact parameter map for a selected command.
function Get-HelpMeCommandArguments {
    param([Parameter(Mandatory)][string]$NormalizedCommand)

    $arguments = @{}
    switch ($NormalizedCommand) {
        'format' {
            Add-HelpMeSwitchArgument $arguments 'Verify' -Enabled:$Verify
        }
        'check-format' {
            $arguments['Verify'] = $true
        }
        'notices' {
            Assert-HelpMeParameter 'Runtime' $Runtime
            Assert-HelpMeParameter 'OutputDirectory' $OutputDirectory
            Add-HelpMeValueArgument $arguments 'Runtime' $Runtime
            Add-HelpMeValueArgument $arguments 'OutputDirectory' $OutputDirectory
        }
        'path' {
            Add-HelpMeValueArgument $arguments 'Action' $Action
            Add-HelpMeValueArgument $arguments 'ExecutablePath' $ExecutablePath
            Add-HelpMeSwitchArgument $arguments 'Yes' -Enabled:$Yes
        }
        'portable' {
            Assert-HelpMeParameter 'Runtime' $Runtime
            Add-HelpMeValueArgument $arguments 'Runtime' $Runtime
            Add-HelpMeValueArgument $arguments 'OutputDirectory' $OutputDirectory
        }
        'release-artifacts' {
            Add-HelpMeValueArgument $arguments 'Version' $Version
            Add-HelpMeValueArgument $arguments 'PortableArchitectures' $PortableArchitectures
            Add-HelpMeValueArgument $arguments 'MsiArchitectures' $MsiArchitectures
            Add-HelpMeValueArgument $arguments 'PackageIdentifier' $PackageIdentifier
            Add-HelpMeValueArgument $arguments 'ArtifactBaseUrl' $ArtifactBaseUrl
            Add-HelpMeValueArgument $arguments 'Wix3BinDirectory' $Wix3BinDirectory
            Add-HelpMeSwitchArgument $arguments 'SkipMsi' -Enabled:$SkipMsi
            Add-HelpMeSwitchArgument $arguments 'SkipRestore' -Enabled:$SkipRestore
            Add-HelpMeSwitchArgument $arguments 'SkipWingetValidation' -Enabled:$SkipWingetValidation
            Add-HelpMeSwitchArgument $arguments 'PlanOnly' -Enabled:$PlanOnly
        }
        'windows-installer' {
            Assert-HelpMeParameter 'PublishDirectory' $PublishDirectory
            Assert-HelpMeParameter 'OutputDirectory' $OutputDirectory
            Assert-HelpMeParameter 'Architecture' $Architecture
            Add-HelpMeValueArgument $arguments 'PublishDirectory' $PublishDirectory
            Add-HelpMeValueArgument $arguments 'OutputDirectory' $OutputDirectory
            Add-HelpMeValueArgument $arguments 'Architecture' $Architecture
            Add-HelpMeValueArgument $arguments 'IsccPath' $IsccPath
        }
        'msix' {
            Assert-HelpMeParameter 'PublishDirectory' $PublishDirectory
            Add-HelpMeValueArgument $arguments 'PublishDirectory' $PublishDirectory
            Add-HelpMeValueArgument $arguments 'OutputDirectory' $OutputDirectory
            Add-HelpMeValueArgument $arguments 'CertificateThumbprint' $CertificateThumbprint
            Add-HelpMeValueArgument $arguments 'Architecture' $Architecture
            Add-HelpMeValueArgument $arguments 'Channel' $Channel
            Add-HelpMeValueArgument $arguments 'Version' $Version
            Add-HelpMeSwitchArgument $arguments 'Unsigned' -Enabled:$Unsigned
            Add-HelpMeValueArgument $arguments 'SdkBinDirectory' $SdkBinDirectory
            Add-HelpMeValueArgument $arguments 'TimestampServer' $TimestampServer
        }
        'install-debug-msix' {
            Assert-HelpMeParameter 'Version' $Version
            Add-HelpMeValueArgument $arguments 'Version' $Version
            Add-HelpMeValueArgument $arguments 'Architecture' $Architecture
            Add-HelpMeValueArgument $arguments 'CertificateThumbprint' $CertificateThumbprint
            Add-HelpMeValueArgument $arguments 'SdkBinDirectory' $SdkBinDirectory
            Add-HelpMeValueArgument $arguments 'TimestampServer' $TimestampServer
        }
        'store-msix' {
            Add-HelpMeValueArgument $arguments 'Version' $Version
            Add-HelpMeValueArgument $arguments 'SdkBinDirectory' $SdkBinDirectory
        }
    }
    return $arguments
}

# Maps public command names and compatibility aliases to internal implementations.
function Resolve-HelpMeCommand {
    param([Parameter(Mandatory)][string]$Value)

    switch ($Value.Trim().ToLowerInvariant()) {
        'menu' { return [pscustomobject]@{ Name = 'menu'; Script = $null } }
        'help' { return [pscustomobject]@{ Name = 'help'; Script = $null } }
        'preflight' { return [pscustomobject]@{ Name = 'preflight'; Script = 'preflight.ps1' } }
        'status' { return [pscustomobject]@{ Name = 'status'; Script = 'repo-status.ps1' } }
        'repo-status' { return [pscustomobject]@{ Name = 'status'; Script = 'repo-status.ps1' } }
        'format' { return [pscustomobject]@{ Name = 'format'; Script = 'format.ps1' } }
        'check-format' { return [pscustomobject]@{ Name = 'check-format'; Script = 'format.ps1' } }
        'check-xml' { return [pscustomobject]@{ Name = 'check-xml'; Script = 'check-xml-comments.ps1' } }
        'check-xml-comments' { return [pscustomobject]@{ Name = 'check-xml'; Script = 'check-xml-comments.ps1' } }
        'notices' { return [pscustomobject]@{ Name = 'notices'; Script = 'export-third-party-notices.ps1' } }
        'export-third-party-notices' { return [pscustomobject]@{ Name = 'notices'; Script = 'export-third-party-notices.ps1' } }
        'path' { return [pscustomobject]@{ Name = 'path'; Script = 'hm-path.ps1' } }
        'portable' { return [pscustomobject]@{ Name = 'portable'; Script = 'build-portable-release.ps1' } }
        'build-portable-release' { return [pscustomobject]@{ Name = 'portable'; Script = 'build-portable-release.ps1' } }
        'release-artifacts' { return [pscustomobject]@{ Name = 'release-artifacts'; Script = 'build-release-artifacts.ps1' } }
        'build-release-artifacts' { return [pscustomobject]@{ Name = 'release-artifacts'; Script = 'build-release-artifacts.ps1' } }
        'windows-installer' { return [pscustomobject]@{ Name = 'windows-installer'; Script = 'build-windows-installer.ps1' } }
        'build-windows-installer' { return [pscustomobject]@{ Name = 'windows-installer'; Script = 'build-windows-installer.ps1' } }
        'msix' { return [pscustomobject]@{ Name = 'msix'; Script = 'package-msix.ps1' } }
        'package-msix' { return [pscustomobject]@{ Name = 'msix'; Script = 'package-msix.ps1' } }
        'install-debug-msix' { return [pscustomobject]@{ Name = 'install-debug-msix'; Script = 'install-debug-msix.ps1' } }
        'store-msix' { return [pscustomobject]@{ Name = 'store-msix'; Script = 'build-store-msix.ps1' } }
        'build-store-msix' { return [pscustomobject]@{ Name = 'store-msix'; Script = 'build-store-msix.ps1' } }
        default { throw "Unknown HelpMe command '$Value'. Use -Command help." }
    }
}

# Reads a required value for an interactive command.
function Read-HelpMeRequiredValue {
    param([Parameter(Mandatory)][string]$Prompt)

    while ($true) {
        $value = Read-Host "  $Prompt"
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
        Write-Host '  A value is required.' -ForegroundColor Red
    }
}

# Runs the interactive launcher until the user exits.
function Invoke-HelpMeMenu {
    Initialize-HelpMeMenu
    while ($true) {
        Write-HelpMeMenuHeader
        Write-HelpMeMenuItem 1 '🔎' 'Preflight' 'Check required tools and files'
        Write-HelpMeMenuItem 2 '🌿' 'Git status' 'Show branch and local changes'
        Write-HelpMeMenuItem 3 '✅' 'Verify formatting' 'Read-only dotnet format check'
        Write-HelpMeMenuItem 4 '🧹' 'Apply formatting' 'Apply supported dotnet fixes'
        Write-HelpMeMenuItem 5 '📝' 'Check XML comments' 'Validate C# method summaries'
        Write-HelpMeMenuItem 6 '🛤️' 'PATH helper' 'Inspect, install, or remove the hm entry'
        Write-HelpMeMenuItem 7 '📜' 'Export notices' 'Prepare third-party attribution files'
        Write-HelpMeMenuItem 8 '📦' 'Portable build' 'Build one existing portable target'
        Write-HelpMeMenuItem 9 '🪟' 'Windows installer' 'Package a prepared portable payload'
        Write-HelpMeMenuItem 10 '🧭' 'Release plan' 'Preview the release artifact workflow'
        Write-HelpMeMenuItem 11 '🔐' 'Package MSIX' 'Create a signed or unsigned MSIX'
        Write-HelpMeMenuItem 12 '🏪' 'Build Store MSIX' 'Build unsigned x64 and ARM64 packages'

        $choice = Read-HelpMeMenuChoice -Maximum 12
        if ($choice -eq 0) {
            return
        }

        $result = 0
        try {
            switch ($choice) {
            1 { $result = Invoke-HelpMeInternalCommand 'preflight.ps1' }
            2 { $result = Invoke-HelpMeInternalCommand 'repo-status.ps1' }
            3 { $result = Invoke-HelpMeInternalCommand 'format.ps1' @{ Verify = $true } }
            4 { $result = Invoke-HelpMeInternalCommand 'format.ps1' }
            5 { $result = Invoke-HelpMeInternalCommand 'check-xml-comments.ps1' }
            6 {
                $pathAction = Read-HelpMeRequiredValue 'Action [install|remove|status]'
                $pathArguments = @{ Action = $pathAction }
                if ($pathAction -in @('install', 'remove')) {
                    $confirm = Read-Host '  Pass --yes to hm? [y/N]'
                    if ($confirm -match '^(?i:y|yes)$') {
                        $pathArguments['Yes'] = $true
                    }
                }
                $result = Invoke-HelpMeInternalCommand 'hm-path.ps1' $pathArguments
            }
            7 {
                $noticeRuntime = Read-HelpMeRequiredValue 'Runtime [win-x64|win-arm64|linux-x64|linux-arm64|osx-x64|osx-arm64]'
                $noticeOutput = Read-HelpMeRequiredValue 'Output directory'
                $result = Invoke-HelpMeInternalCommand 'export-third-party-notices.ps1' @{ Runtime = $noticeRuntime; OutputDirectory = $noticeOutput }
            }
            8 {
                $portableRuntime = Read-HelpMeRequiredValue 'Runtime [win-x64|win-arm64|linux-x64|linux-arm64|osx-x64|osx-arm64]'
                $portableOutput = Read-Host '  Output directory [artifacts/portable]'
                $portableArguments = @{ Runtime = $portableRuntime }
                if (-not [string]::IsNullOrWhiteSpace($portableOutput)) {
                    $portableArguments['OutputDirectory'] = $portableOutput.Trim()
                }
                $result = Invoke-HelpMeInternalCommand 'build-portable-release.ps1' $portableArguments
            }
            9 {
                $installerPublish = Read-HelpMeRequiredValue 'Prepared publish directory'
                $installerOutput = Read-HelpMeRequiredValue 'Output directory below artifacts'
                $installerArchitecture = Read-HelpMeRequiredValue 'Architecture [x64|arm64]'
                $result = Invoke-HelpMeInternalCommand 'build-windows-installer.ps1' @{ PublishDirectory = $installerPublish; OutputDirectory = $installerOutput; Architecture = $installerArchitecture }
            }
            10 { $result = Invoke-HelpMeInternalCommand 'build-release-artifacts.ps1' @{ PlanOnly = $true } }
            11 {
                $msixPublish = Read-HelpMeRequiredValue 'Prepared Windows publish directory'
                $msixArchitecture = Read-HelpMeRequiredValue 'Architecture [x64|arm64]'
                $msixChannel = Read-HelpMeRequiredValue 'Channel [Debug|Store]'
                $thumbprint = Read-Host '  Certificate thumbprint, or leave blank for unsigned'
                $msixArguments = @{ PublishDirectory = $msixPublish; Architecture = $msixArchitecture; Channel = $msixChannel }
                if ([string]::IsNullOrWhiteSpace($thumbprint)) {
                    $msixArguments['Unsigned'] = $true
                }
                else {
                    $msixArguments['CertificateThumbprint'] = $thumbprint.Trim()
                }
                $result = Invoke-HelpMeInternalCommand 'package-msix.ps1' $msixArguments
            }
            12 {
                $storeVersion = Read-Host '  Three-part Store version [1.0.0]'
                $storeArguments = @{}
                if (-not [string]::IsNullOrWhiteSpace($storeVersion)) {
                    $storeArguments['Version'] = $storeVersion.Trim()
                }
                $result = Invoke-HelpMeInternalCommand 'build-store-msix.ps1' $storeArguments
            }
            }
        }
        catch {
            Write-Host "`n  $($_.Exception.Message)" -ForegroundColor Red
            $result = 1
        }

        if ($result -ne 0) {
            Write-Host "`n  Command failed with exit code $result." -ForegroundColor Red
        }
        Wait-HelpMeMenu
    }
}

Set-Location -LiteralPath $repositoryRoot
if ($Help) {
    Show-HelpMeHelp
    exit 0
}
if ([string]::IsNullOrWhiteSpace($Command)) {
    if ($boundParameters.Count -gt 0) {
        throw '-Command is required when command options are supplied.'
    }
    Invoke-HelpMeMenu
    exit 0
}

$resolvedCommand = Resolve-HelpMeCommand $Command
if ($resolvedCommand.Name -eq 'menu') {
    Invoke-HelpMeMenu
    exit 0
}
if ($resolvedCommand.Name -eq 'help') {
    Show-HelpMeHelp
    exit 0
}

$commandArguments = Get-HelpMeCommandArguments $resolvedCommand.Name
$exitCode = Invoke-HelpMeInternalCommand $resolvedCommand.Script $commandArguments
exit $exitCode
