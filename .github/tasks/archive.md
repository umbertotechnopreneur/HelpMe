# Latest completed task

## 2026-11-15 — Linux delivery and architecture checks

- Committed and pushed Linux shell support, Debian packaging, and the consolidated build commands, then opened the pull request.
- Verified 1,164 tests and CLI smoke checks on Windows x64, Linux x64, and Linux ARM64. Both native Linux Debian packaging jobs passed.
- Moved Debian package creation into the shared quality workflow so pull requests and release drafts use the same checks and build steps.
- Removed only the retired macOS required check, preserving all other remote branch protection settings.
- Kept source versions unchanged and left release publication, tags, and merge untouched.

Public update: HelpMe has Debian packaging for Linux x64 and ARM64, a shared build command, and one build and packaging guide. GitHub release automation prepares a draft for manual publication.
