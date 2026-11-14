# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
Single supported entry point for local development and CI packaging.
#>
[CmdletBinding()]
param(
    [ValidateSet('help', 'preflight', 'status', 'restore', 'build', 'test', 'smoke', 'clean', 'format', 'check-format', 'check-xml', 'notices', 'path', 'changelog', 'deb', 'install-deb', 'msix', 'install-debug-msix', 'store-msix')]
    [string]$Command = 'help',
    [switch]$Help,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64')][string]$Runtime,
    [string]$OutputDirectory,
    [string]$PublishDirectory,
    [string]$PackagePath,
    [string]$AssetsFile,
    [ValidateSet('x64', 'arm64')][string]$Architecture,
    [string]$Version,
    [string]$CertificateThumbprint,
    [ValidateSet('Debug', 'Store')][string]$Channel,
    [switch]$Unsigned,
    [string]$SdkBinDirectory,
    [uri]$TimestampServer,
    [ValidateSet('install', 'remove', 'status')][string]$Action = 'status',
    [string]$ExecutablePath,
    [switch]$Yes,
    [switch]$Verify,
    [switch]$NoBuild
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$commands = Join-Path $PSScriptRoot 'Common/commands'
$solution = Join-Path $repository 'HelpMe.slnx'
. (Join-Path $PSScriptRoot 'Common/artifacts.ps1')

# Stop on a failed external tool instead of returning a successful dispatcher status.
function Invoke-HelpMeDotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE." }
}

if ($Help -or $Command -eq 'help') {
    @'
HelpMe repository commands
  pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command <command> [options]

Development
  preflight, status, restore, build, test, smoke, clean
  format, check-format, check-xml, changelog
  Default configuration: Debug. Use -Configuration Release explicitly for CI.
  Development output: artifacts/build. Tests and CLI smoke checks are explicit commands.

Linux
  deb [-Runtime linux-x64|linux-arm64] [-Configuration Debug|Release]
      [-OutputDirectory artifacts/<task>]
  install-deb -PackagePath artifacts/<path>/helpme_<version>_<architecture>.deb
  Package version comes from HelpMe.csproj. Packaging does not install or publish.

Windows
  msix -PublishDirectory <path> -Architecture x64|arm64 -Channel Debug|Store
       [-Version <four-part-version>] [-Unsigned] [-CertificateThumbprint <thumbprint>]
  install-debug-msix -Version <four-part-version> [-Architecture x64|arm64]
  store-msix [-Version <three-part-version>]
  Store artifacts are built in GitHub Actions. Windows distribution is MSIX-only.

Utilities
  notices -Runtime <rid> -OutputDirectory artifacts/<payload> [-AssetsFile <path>]
  path -Action status|install|remove [-ExecutablePath <path>] [-Yes]
  changelog [-Verify]

See scripts/README.md for output retention, cleanup, and release instructions.
'@ | Write-Host
    exit 0
}
$previousArtifacts = $env:ArtifactsPath
$previousUseArtifacts = $env:UseArtifactsOutput
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = Join-Path $repository 'artifacts/build'
try {
    switch ($Command) {
        'restore' { Invoke-HelpMeDotNet @('restore', $solution) }
        'build' { Invoke-HelpMeDotNet @('build', $solution, '--configuration', $Configuration, '--warnaserror') }
        'test' {
            $results = Join-Path $repository 'artifacts/test-results'
            $arguments = @('test', $solution, '--configuration', $Configuration, '--results-directory', $results)
            if ($NoBuild) { $arguments += @('--no-build', '--no-restore') }
            Invoke-HelpMeDotNet $arguments
        }
        'smoke' {
            $oldData = $env:HELPME_DATA_DIR
            $oldKey = $env:OPENAI_API_KEY
            $oldAdminKey = $env:OPENAI_ADMIN_KEY
            try {
                $env:HELPME_DATA_DIR = Join-Path $repository "artifacts/cli-smoke-data/$([guid]::NewGuid().ToString('N'))"
                $env:OPENAI_API_KEY = $null
                $env:OPENAI_ADMIN_KEY = $null
                foreach ($argument in @('--version', '--help', '--third-party')) {
                    Invoke-HelpMeDotNet @('run', '--project', (Join-Path $repository 'HelpMe/HelpMe.csproj'), '--configuration', $Configuration,
                        '--no-build', '--', $argument, '--no-animation', '--no-emoji')
                }
            }
            finally {
                $env:HELPME_DATA_DIR = $oldData
                $env:OPENAI_API_KEY = $oldKey
                $env:OPENAI_ADMIN_KEY = $oldAdminKey
            }
        }
        'clean' {
            $arguments = @('clean', $solution, '--configuration', $Configuration, '--verbosity', 'quiet')
            if ($Runtime) { $arguments += @('--runtime', $Runtime) }
            Invoke-HelpMeDotNet $arguments
            Remove-HelpMeArtifactPath (Join-Path $repository 'artifacts/build')
            Remove-HelpMeArtifactPath (Join-Path $repository 'artifacts/test-results')
            Remove-HelpMeArtifactPath (Join-Path $repository 'artifacts/notice-check')
            Remove-HelpMeArtifactPath (Join-Path $repository 'artifacts/cli-smoke-data')
        }
        'preflight' { & (Join-Path $commands 'preflight.ps1') }
        'status' { & (Join-Path $commands 'repo-status.ps1') }
        'format' { & (Join-Path $commands 'format.ps1') -Verify:$Verify }
        'check-format' { & (Join-Path $commands 'format.ps1') -Verify }
        'check-xml' { & (Join-Path $commands 'check-xml-comments.ps1') }
        'changelog' { & (Join-Path $PSScriptRoot 'Generate-Changelog.ps1') -Check:$Verify }
        'path' { & (Join-Path $commands 'hm-path.ps1') -Action $Action -ExecutablePath $ExecutablePath -Yes:$Yes }
        'notices' {
            if (-not $Runtime -or -not $OutputDirectory) { throw 'notices requires -Runtime and -OutputDirectory.' }
            $arguments = @{ Runtime = $Runtime; OutputDirectory = (Resolve-HelpMeArtifactPath $OutputDirectory) }
            if ($AssetsFile) { $arguments.AssetsFile = $AssetsFile }
            & (Join-Path $commands 'export-third-party-notices.ps1') @arguments
        }
        'deb' {
            if ($Runtime -and $Runtime -notin @('linux-x64', 'linux-arm64')) { throw 'deb accepts Linux runtimes only.' }
            $arguments = @{ Configuration = $Configuration }
            if ($Runtime) { $arguments.Runtime = $Runtime }
            if ($OutputDirectory) { $arguments.OutputDirectory = $OutputDirectory }
            & (Join-Path $commands 'build-deb.ps1') @arguments
        }
        'install-deb' {
            if (-not $PackagePath) { throw 'install-deb requires -PackagePath.' }
            & (Join-Path $commands 'install-deb.ps1') -PackagePath $PackagePath
        }
        'msix' {
            if (-not $PublishDirectory -or -not $Architecture -or -not $Channel) { throw 'msix requires -PublishDirectory, -Architecture and -Channel.' }
            if ($Channel -eq 'Store' -and $env:GITHUB_ACTIONS -ne 'true') { throw 'Store packaging runs only in GitHub Actions.' }
            $arguments = @{ PublishDirectory = $PublishDirectory; Architecture = $Architecture; Channel = $Channel; Unsigned = $Unsigned }
            foreach ($name in @('OutputDirectory', 'Version', 'CertificateThumbprint', 'SdkBinDirectory', 'TimestampServer')) {
                $value = Get-Variable -Name $name -ValueOnly
                if ($value) { $arguments[$name] = $value }
            }
            & (Join-Path $commands 'package-msix.ps1') @arguments
        }
        'install-debug-msix' {
            if (-not $Version) { throw 'install-debug-msix requires the owner-selected -Version.' }
            $arguments = @{ Version = $Version }
            foreach ($name in @('Architecture', 'CertificateThumbprint', 'SdkBinDirectory', 'TimestampServer')) {
                $value = Get-Variable -Name $name -ValueOnly
                if ($value) { $arguments[$name] = $value }
            }
            & (Join-Path $commands 'install-debug-msix.ps1') @arguments
        }
        'store-msix' {
            if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Store packaging runs only in GitHub Actions.' }
            $arguments = @{ Version = $(if ($Version) { $Version } else { Get-HelpMeSourceVersion }) }
            if ($SdkBinDirectory) { $arguments.SdkBinDirectory = $SdkBinDirectory }
            & (Join-Path $commands 'build-store-msix.ps1') @arguments
        }
    }
}
finally {
    $env:ArtifactsPath = $previousArtifacts
    $env:UseArtifactsOutput = $previousUseArtifacts
}
