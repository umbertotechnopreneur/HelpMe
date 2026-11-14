# Build and package HelpMe

I keep development and packaging commands behind one entry point:

```powershell
pwsh -NoProfile -File ./scripts/HelpMe.ps1
```

With no arguments it prints help. Implementations under `Common/` are private helpers. Use the entry point locally and in CI; do not copy those implementations into another launcher.

## Local development

Install Git, PowerShell 7, and the .NET 10 SDK. Local builds default to Debug.

```powershell
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command preflight
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command restore
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command build
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command check-format
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command check-xml
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command clean
```

Use `-Command format` to apply formatting, `-Command status` for Git status, and `-Command changelog -Verify` to check the changelog. The changelog generator remains an internal utility called by this entry point. `-Command smoke` explicitly runs the basic CLI checks with a disposable data directory and no OpenAI credentials. Tests can reuse a matching build with `-NoBuild`.

When you explicitly want automated tests:

```powershell
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command test
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command clean
```

Builds use `artifacts/build`; test results use `artifacts/test-results`. `clean` runs `dotnet clean` and removes those transient directories, disposable CLI smoke data, and the CI notice-check output. It leaves packages, backups, and unrelated task directories alone. No command commits, pushes, changes a version, or publishes a release.

## Linux Debian packages

Packaging targets glibc-based Debian/Ubuntu systems on x64 (`amd64`) and ARM64 (`arm64`). It is not an Alpine/musl package and does not establish compatibility with every Debian derivative. macOS builds and packages are outside the current scope.

Run the builder on Linux with `dpkg-deb`, `chmod`, and the development tools above:

```powershell
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command deb -Runtime linux-x64
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command deb -Runtime linux-arm64 -Configuration Release
```

The builder reads the version from `HelpMe/HelpMe.csproj`; it does not add a revision or change the project. Debug and Release outputs stay separate:

```text
artifacts/deb/debug/linux-x64/helpme_<version>_amd64.deb
artifacts/deb/release/linux-arm64/helpme_<version>_arm64.deb
```

Each output directory retains the package, `SHA256SUMS.txt`, and `package.json`. Intermediate publish and build trees are cleaned even after failure. An existing package is preserved: reinstall it directly, or use a fresh `-OutputDirectory artifacts/<task>`. Do not change versions to work around existing output.

The package includes the .NET runtime and declares native libraries as dependencies. Files install under `/usr/lib/helpme`, with `/usr/bin/hm` as the command. The builder uses a native temporary directory to preserve Unix permissions even when your checkout is on NTFS. It does not run the app, install the package, alter shell profiles, or touch saved HelpMe data.

Install a matching native package explicitly:

```powershell
pwsh -NoProfile -File ./scripts/HelpMe.ps1 -Command install-deb -PackagePath ./artifacts/deb/debug/linux-x64/helpme_<version>_amd64.deb
```

The installer uses `sudo -n apt-get install --reinstall --no-install-recommends`, then checks package status and version. Reinstallation ensures that an explicitly selected local package replaces the installed contents even when its owner-selected version is unchanged. If sudo needs authentication, run `sudo apt install --reinstall ./path/to/package.deb` in your own terminal. Removal uses `sudo apt remove helpme`; your user data remains separate from installed files.

Bash can run conversational commands without PowerShell. PowerShell 7 is still required for PowerShell commands and the generated script, file-preview, and skill wrappers. Clipboard and PDF integrations are optional; see the [command guide](../docs/CLI_REFERENCE.md#linux-path-clipboard-and-pdf-integration).

## CI and GitHub releases

The quality workflow builds and tests on Windows x64, Linux x64, and Linux ARM64. It then builds Release Debian packages on native x64 and ARM64 Linux runners and retains them as CI artifacts. The release workflow reuses these checks and packages when preparing a draft. Windows distribution remains MSIX-only through the separate Store workflow. No macOS job or package is produced.

The owner chooses the source version and creates an annotated `v<version>` tag only after that source is on `main`. A release run verifies the tag and source before preparing a draft with both Debian packages and checksums. You review and publish the draft. A manual rehearsal builds downloadable CI artifacts without creating a release or tag. Merging into `main` does not publish anything.

The Store workflow creates unsigned Windows x64 and ARM64 MSIX files for separate Partner Center submission. It does not upload to the Store. `store-msix` is restricted to GitHub Actions. The `msix` command packages an already prepared Windows payload; `install-debug-msix` retains the explicit owner-selected four-part version and current-user certificate flow.

Keep only the latest validated package for each channel and architecture, its checksum, and minimal metadata. Preserve pending Store uploads, requested rollback packages, backups, and unresolved failure evidence. Clean generated output with literal, verified paths under `artifacts/`; never wipe the whole directory. Full repository rules are in [AGENTS.md](../AGENTS.md).
