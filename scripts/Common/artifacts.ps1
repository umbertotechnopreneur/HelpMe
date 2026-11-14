# SPDX-License-Identifier: MIT
Set-StrictMode -Version Latest

# Reject links in a generated path and every existing ancestor before creating or deleting output.
function Assert-HelpMeNoLinks {
    param([Parameter(Mandatory)][string]$Path)
    $cursor = [IO.Path]::GetFullPath($Path)
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if ($item.LinkType) { throw "Generated output cannot traverse a link: $cursor" }
        }
        $parent = [IO.Path]::GetDirectoryName($cursor)
        if ($parent -eq $cursor) { break }
        $cursor = $parent
    }
}

# Resolve one subdirectory of artifacts, without permitting deletion of the shared artifacts root.
function Resolve-HelpMeArtifactPath {
    param([Parameter(Mandatory)][string]$Path)
    $repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $root = [IO.Path]::GetFullPath((Join-Path $repository 'artifacts'))
    $absolute = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($Path, $repository))
    if (-not $absolute.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
        throw 'Generated output must be in a subdirectory of artifacts.'
    }
    Assert-HelpMeNoLinks $absolute
    return $absolute
}

# Remove only explicitly selected generated output after validating its containment and descendants.
function Remove-HelpMeArtifactPath {
    param([Parameter(Mandatory)][string]$Path)
    $absolute = Resolve-HelpMeArtifactPath $Path
    if (-not (Test-Path -LiteralPath $absolute)) { return }
    if (@(Get-ChildItem -LiteralPath $absolute -Recurse -Force | Where-Object { $_.LinkType }).Count) {
        throw "Inspect generated links before cleanup: $absolute"
    }
    Remove-Item -LiteralPath $absolute -Recurse -Force
    if (Test-Path -LiteralPath $absolute) { throw "Cleanup incomplete: $absolute" }
}

# Resolve the owner's existing source version without inventing a package revision.
function Get-HelpMeSourceVersion {
    $repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    [xml]$project = Get-Content -LiteralPath (Join-Path $repository 'HelpMe/HelpMe.csproj') -Raw
    $version = [string]$project.Project.PropertyGroup.Version
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a three-part source version.' }
    return $version
}
