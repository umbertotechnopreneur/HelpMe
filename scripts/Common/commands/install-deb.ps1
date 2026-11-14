# SPDX-License-Identifier: MIT
[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsLinux) { throw 'Debian installation requires Linux.' }
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$name = & dpkg-deb --field $package Package
$version = & dpkg-deb --field $package Version
$architecture = & dpkg-deb --field $package Architecture
if ($LASTEXITCODE -ne 0 -or $name -cne 'helpme') { throw 'Not a HelpMe Debian package.' }
$native = & dpkg --print-architecture
if ($LASTEXITCODE -ne 0 -or $native -cne $architecture) { throw 'Install only a package matching this machine architecture.' }
& sudo -n apt-get install -y --reinstall --no-install-recommends $package
if ($LASTEXITCODE -ne 0) { throw 'Debian installation failed. Run the same apt install command in your terminal if sudo requires authentication.' }
$status = & dpkg-query -W '-f=${Status} ${Version}' helpme
if ($LASTEXITCODE -ne 0 -or $status -cne "install ok installed $version") { throw 'Installed package status or version differs from the requested package.' }
Write-Host "Installed helpme $version ($architecture)."
