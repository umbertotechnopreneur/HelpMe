# Open tasks

- [ ] Validate Windows and Linux ARM64 separately. When authorized, verify paste, resize, and navigation in real terminal emulators; the Linux CLI and simulated-terminal smoke checks do not establish compatibility with every terminal or architecture.

- [ ] Harden AppPaths for missing or unwritable Linux data locations and report directory-creation failures through the startup error flow. Review private file permissions for settings, SQLite, and logs; keep storage global and preserve HELPME_DATA_DIR.
- [ ] When authorized, verify optional Linux desktop integration on Wayland and X11, including unavailable clipboard tools and missing PDF readers. Verify PATH changes only with disposable profiles; compare Bash login and interactive behavior, ZDOTDIR, and XDG_CONFIG_HOME.
- [ ] Review the remaining reference guides for outdated names, links, and packaging instructions. Windows distribution is MSIX-only. Keep Linux claims limited to verified behavior.
- [ ] Check package identity and capture real HelpMe screenshots when authorized. Current screenshots show PromptMeUp.
- [ ] Validate Store licensing when authorized: acquired license, trial countdown and expiration, purchase, offline behavior, x64, and ARM64. Keep Partner Center settings, submission steps, and owner checkpoints in the private product notes.
- [ ] Review the pending Store workflow and manifest changes through the required delivery process. Consolidate package validation findings before any separately authorized build or submission; this task does not authorize a version change.
- [ ] Inspect and remove obsolete task output at the locations below. Preserve pending Store deliverables, reinstall or rollback packages, required import-scan records, unresolved failure evidence, and user data. Verify each path before removal; do not wipe `artifacts/` as a whole.
  - Remove transient output under `artifacts/msix/debug/command-output-20261019/`: `publish`, `package/payload`, `build`, `imports`, `scratch`, `logs`, and `empty-feed`. Preserve pending deliverables and required import-scan records.
  - Remove the disposable welcome profile under `artifacts/smoke/layout-refinement-20260927/`.
  - Remove temporary publish and payload directories from `artifacts/msix/debug/1.0.0.0-startup-fix-20260924/` and the superseded `1.0.0.1-startup-fix-20260924/` build. Preserve pending deliverables.
  - Remove `artifacts/msix/debug/1.0.0.0-oobe-20260924-021718/publish` and `x64/payload`, preserving pending deliverables.
  - Remove `artifacts/msix/debug/0.1.8.5/publish` and `x64/payload` while preserving pending deliverables.
  - Remove `artifacts/msix/debug/0.0.1.2211/publish`, `publish-nosymbols`, and `x64/payload` while preserving pending deliverables.
  - Remove `artifacts/msix/direct-20260921-debug/publish` and `artifacts/msix/direct-20260921-debug/package/payload` while preserving pending deliverables.
  - Remove `artifacts/msix/banner-ef06355-debug-publish` and `artifacts/msix/banner-ef06355-debug/payload` while preserving pending deliverables.
  - Remove `artifacts/msix/chat-68142c7-debug-publish` and `artifacts/msix/chat-68142c7-debug/payload` while preserving pending deliverables.
  - Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260918-chat-ui` while preserving pending deliverables.
  - Remove temporary source, publish, and payload directories under `artifacts/msix/install-20260917-memory` while preserving pending deliverables.
  - Remove disposable local package, IDE, and smoke-test directories while preserving pending deliverables and user data.
  - Remove the temporary `publish`, `x64/payload`, and disposable local test-data backup `appdata-backup` directories under `artifacts/msix/debug/1.0.0.0-layout-5a68a80-20260926/`, preserving pending deliverables.
