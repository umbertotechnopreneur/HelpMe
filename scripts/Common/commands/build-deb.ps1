# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
Build a self-contained Debian package, retaining only the package, checksum and metadata.
#>
[CmdletBinding()]
param(
    [ValidateSet('linux-x64', 'linux-arm64')][string]$Runtime,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsLinux) { throw 'Debian packaging requires Linux.' }
. (Join-Path $PSScriptRoot '../artifacts.ps1')
. (Join-Path $PSScriptRoot '../bundled-skills.ps1')
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
foreach ($tool in @('dotnet', 'dpkg-deb', 'chmod', 'gzip')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Missing packaging tool: $tool" }
}
if (-not $Runtime) {
    $architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    if ($architecture -notin @('x64', 'arm64')) { throw 'Debian packaging supports x64 and ARM64.' }
    $Runtime = "linux-$architecture"
}
$debArchitecture = if ($Runtime -eq 'linux-x64') { 'amd64' } else { 'arm64' }
$version = Get-HelpMeSourceVersion
if (-not $OutputDirectory) { $OutputDirectory = "artifacts/deb/$($Configuration.ToLowerInvariant())/$Runtime" }
$output = Resolve-HelpMeArtifactPath $OutputDirectory
$packageName = "helpme_${version}_${debArchitecture}.deb"
$packagePath = Join-Path $output $packageName
if (Test-Path -LiteralPath $packagePath) { throw 'Package already exists. Reuse it for installation or choose another output directory; do not change the version to bypass this check.' }
if ((Test-Path -LiteralPath $output) -and @(Get-ChildItem -LiteralPath $output -Force).Count) { throw 'Choose a fresh package output directory to preserve existing deliverables and failure evidence.' }
$work = Join-Path $output 'work'
if (Test-Path -LiteralPath $work) { throw 'Inspect and clean the previous task work directory before packaging.' }
[IO.Directory]::CreateDirectory($work) | Out-Null
$build = Join-Path $work 'build'
$publish = Join-Path $work 'publish'
$previousArtifacts = $env:ArtifactsPath
$previousUseArtifacts = $env:UseArtifactsOutput
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = $build
$staging = $null
$published = $false
$validated = $false
try {
    # A native temporary filesystem preserves Debian permissions even when the repository is on NTFS.
    $staging = (New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("helpme-deb-" + [guid]::NewGuid().ToString('N')))).FullName
    $payload = Join-Path $staging 'usr/lib/helpme'
    [IO.Directory]::CreateDirectory((Join-Path $staging 'DEBIAN')) | Out-Null
    [IO.Directory]::CreateDirectory($payload) | Out-Null
    $published = $true
    & dotnet publish (Join-Path $repository 'HelpMe/HelpMe.csproj') --configuration $Configuration --runtime $Runtime --self-contained true `
        -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:TreatWarningsAsErrors=true --output $publish
    if ($LASTEXITCODE -ne 0) { throw 'Debian publish failed.' }
    & (Join-Path $PSScriptRoot 'export-third-party-notices.ps1') -OutputDirectory $publish -Runtime $Runtime `
        -AssetsFile (Join-Path $build 'obj/HelpMe/project.assets.json')
    foreach ($required in @('hm', 'hm.dll', 'libcoreclr.so', 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'THIRD_PARTY_INVENTORY.json', 'docs/helpme-quick-reference.pdf', 'prompt/query-system.yaml')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $required) -PathType Leaf)) { throw "Missing package content: $required" }
    }
    Assert-HelpMeNoLinks $publish
    if (@(Get-ChildItem -LiteralPath $publish -Recurse -Force | Where-Object { $_.LinkType }).Count) { throw 'Publish content must not contain links.' }
    $bundledSkills = @(Get-BundledSkillFiles)
    foreach ($file in $bundledSkills) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $file) -PathType Leaf)) { throw "Missing bundled skill file: $file" }
    }
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $publish 'skills') -Recurse -File) {
        $relative = [IO.Path]::GetRelativePath($publish, $file.FullName).Replace('\', '/')
        if ($bundledSkills -cnotcontains $relative) { throw "Unexpected bundled skill content: $relative" }
    }
    Copy-Item -Path (Join-Path $publish '*') -Destination $payload -Recurse
    $bin = Join-Path $staging 'usr/bin'
    $documentation = Join-Path $staging 'usr/share/doc/helpme'
    [IO.Directory]::CreateDirectory($bin) | Out-Null
    [IO.Directory]::CreateDirectory($documentation) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repository 'LICENSE') -Destination (Join-Path $documentation 'copyright')
    New-Item -ItemType SymbolicLink -Path (Join-Path $bin 'hm') -Target '../lib/helpme/hm' | Out-Null
    $installedSize = [math]::Ceiling((Get-ChildItem -LiteralPath $staging -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1024)
    @"
Package: helpme
Version: $version
Architecture: $debArchitecture
Section: utils
Priority: optional
Maintainer: Umberto Giacobbi <umberto.giacobbi@gmail.com>
Homepage: https://github.com/umbertotechnopreneur/PromptMeUp
Installed-Size: $installedSize
Depends: bash, libc6 (>= 2.35), libgcc-s1, libstdc++6, zlib1g, libgssapi-krb5-2, libssl3t64 | libssl3, libicu78 | libicu76 | libicu74 | libicu72 | libicu70
Suggests: powershell, wl-clipboard | xclip | xsel, xdg-utils
Description: HelpMe console assistant ($Configuration)
 Ask about terminal commands and review them before execution.
 Includes the .NET runtime. AI features require your own OpenAI API account.
 PowerShell 7 is required for PowerShell commands and generated workflow wrappers.
"@ | Set-Content -LiteralPath (Join-Path $staging 'DEBIAN/control') -Encoding utf8NoBOM
    Get-ChildItem -LiteralPath $staging -Recurse -Force | Where-Object { -not $_.LinkType } | ForEach-Object {
        & chmod $(if ($_.PSIsContainer) { '755' } else { '644' }) -- $_.FullName
        if ($LASTEXITCODE -ne 0) { throw 'Cannot set Debian payload permissions.' }
    }
    & chmod 755 -- $staging (Join-Path $payload 'hm')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot set executable permissions.' }
    & dpkg-deb --build --root-owner-group -Zxz $staging $packagePath
    if ($LASTEXITCODE -ne 0) { throw 'dpkg-deb failed.' }
    $identity = @(& dpkg-deb --field $packagePath Package Version Architecture)
    if ($LASTEXITCODE -ne 0 -or ($identity -join "`n") -cne "Package: helpme`nVersion: $version`nArchitecture: $debArchitecture") { throw 'Debian package identity verification failed.' }
    $hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $packageName" | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding utf8NoBOM
    $revision = & git -C $repository rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot record package source commit.' }
    $dirty = @(& git -C $repository status --porcelain).Count -gt 0
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine source working-tree status.' }
    [ordered]@{ file = $packageName; version = $version; runtime = $Runtime; architecture = $debArchitecture; configuration = $Configuration; sha256 = $hash; sourceCommit = $revision; sourceDirty = $dirty } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'package.json') -Encoding utf8NoBOM
    $validated = $true
    Write-Host "Created $packagePath"
}
finally {
    $cleanFailed = $false
    if ($published) {
        & dotnet clean (Join-Path $repository 'HelpMe.slnx') --configuration $Configuration --runtime $Runtime "-p:PublishDir=$publish" --verbosity quiet
        $cleanFailed = $LASTEXITCODE -ne 0
    }
    $env:ArtifactsPath = $previousArtifacts
    $env:UseArtifactsOutput = $previousUseArtifacts
    Remove-HelpMeArtifactPath $work
    if ($staging -and (Test-Path -LiteralPath $staging)) {
        Assert-HelpMeNoLinks $staging
        # Remove only the known relative launcher link before checking the generated temporary tree.
        $launcher = Join-Path $staging 'usr/bin/hm'
        if (Test-Path -LiteralPath $launcher) { Remove-Item -LiteralPath $launcher -Force }
        if (@(Get-ChildItem -LiteralPath $staging -Recurse -Force | Where-Object { $_.LinkType }).Count) { throw "Unexpected link in staging: $staging" }
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
    if (-not $validated) {
        foreach ($file in @($packageName, 'SHA256SUMS.txt', 'package.json')) {
            $failedOutput = Join-Path $output $file
            if (Test-Path -LiteralPath $failedOutput) { Remove-Item -LiteralPath $failedOutput -Force }
        }
    }
    if ($cleanFailed) { throw 'Debian build cleanup failed.' }
}
