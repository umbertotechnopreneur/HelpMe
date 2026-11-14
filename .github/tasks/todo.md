# Open tasks

- [ ] Commit and push the packaging branch, open its pull request, verify Linux x64 and ARM64 CI, and remove only the retired macOS required check. Keep the release unpublished.

- [ ] Validate Windows and Linux ARM64 separately. When authorized, verify paste, resize, and navigation in real terminal emulators; the Linux CLI and simulated-terminal smoke checks do not establish compatibility with every terminal or architecture.

- [ ] Harden AppPaths for missing or unwritable Linux data locations and report directory-creation failures through the startup error flow. Review private file permissions for settings, SQLite, and logs; keep storage global and preserve HELPME_DATA_DIR.
- [ ] When authorized, verify optional Linux desktop integration on Wayland and X11, including unavailable clipboard tools and missing PDF readers. Verify PATH changes only with disposable profiles; compare Bash login and interactive behavior, ZDOTDIR, and XDG_CONFIG_HOME.
- [ ] Review the remaining reference guides for outdated names, links, and packaging instructions. Windows distribution is MSIX-only. Keep Linux claims limited to verified behavior.
- [ ] Check package identity and capture real HelpMe screenshots when authorized. Current screenshots show PromptMeUp.
- [ ] Validate Store licensing when authorized: acquired license, trial countdown and expiration, purchase, offline behavior, x64, and ARM64. Keep Partner Center settings, submission steps, and owner checkpoints in the private product notes.
- [ ] Review the pending Store workflow and manifest changes through the required delivery process. Consolidate package validation findings before any separately authorized build or submission; this task does not authorize a version change.
