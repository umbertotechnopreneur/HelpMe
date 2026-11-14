# Latest completed task

## 2026-11-15 — Debian packaging and script consolidation

- Added self-contained Debian packaging for Linux x64 and ARM64, with checksums, attribution, package metadata, and automatic intermediate cleanup.
- Built and installed the current x64 Debug package without changing the source version or saved HelpMe data.
- Routed development and CI commands through `scripts/HelpMe.ps1`, consolidated the build guide in `scripts/README.md`, and removed obsolete portable and Windows EXE installers.
- Prepared native Linux release jobs and draft creation. Removed macOS build targets and cleaned obsolete generated output while preserving pending packages, backups, and import-scan records.
- Added native Linux ARM64 build, test, and CLI smoke jobs. The shared quality workflow creates both Debian packages for pull requests and release drafts.

Public update: HelpMe has Debian packaging for Linux x64 and ARM64, a shared build command, and one build and packaging guide. GitHub release automation prepares a draft for manual publication.
