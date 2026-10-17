# BGMPlayer publication state

Updated: 2026-10-18 (JST).
Classification: new standalone plugin, explicitly selected by the user.
Author: Roxyz0501.

## 2026-10-18 — BGMPlayer 1.1.0.0 public preview

- Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v1.1.0.0
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v1.1.0.0/AetherRadio-1.1.0.0.zip
- Source/tag commit: `b53f098f33176029a9617678715bafd6c4b7c577`.
- ZIP SHA-256: `BB9A19E45C8493694A7CFB5E946E2C2D2B756435CFF3A50C9029C9F7A647AD92`.
- Added mini-player restore/favorite buttons; normal/Savage, alliance and special-content genres; optional suppression of reused audio in later expansion tabs; session-only recent history capped at 30 tracks. Seven-language UI and canonical shared tabs retained.
- Clean Release: zero warnings/errors; 200 managed checks, 218 with four offline game-data languages, and 71 isolated ImGui checks passed. Initial local sheet-service requests reduced from 4,810 to 11; subsequent navigation/filter/history reads and unchanged saves are zero. All 1,293 installed tracks remain available.
- Anonymous repository, PNG icon and ZIP returned HTTP 200. Downloaded ZIP matched the hash; packaged manifest version/author/RepoUrl/IconUrl and required license were verified.
- Shared index commit: `b242aec049c4247441e3c53e5e8954d541830adb`. Both anonymous main and commit-pinned URLs returned HTTP 200, eight entries, version 1.1.0.0 and the new ZIP URL. Seven other plugin entries were unchanged.
- Live-game audio/font-atlas/IME/DPI acceptance remains unverified; release is a public prerelease, not a draft. No installed DLL replacement; previous assets retained. Source/index/ledger writes used the GitHub connector, with CLI only for unsupported release/asset operations and local Git synchronization.

## Previously verified public release: 1.0.0.1

- Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v1.0.0.1
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v1.0.0.1/AetherRadio-1.0.0.1.zip
- Source commit: `1559df1a627c03ec2105d84a03c40e50e208bf90`; SHA-256: `BDC3970FDCA020CC10EA0B357D34A8023C9B16DBAD10C5263B31F9B1F0CA121E`.
- Copied the latest canonical StandardPluginTabs.cs exactly and call DrawStandardTrailingTabs once after Library. The unchanged visible order is Library, Settings, Language settings, Support. Saved language, lists, playback settings and direct installer Settings access are retained.
- Clean Release build: zero warnings/errors. 170 managed and 59 isolated ImGui checks passed, including actual tab IDs/positions and direct Settings access in all seven languages. Navigation causes zero extra catalogue reads; unchanged views cause zero saves.
- Anonymous repository, image icon and ZIP returned HTTP 200. The downloaded ZIP hash, manifest identity/version/URLs and license notices were verified. Game font-atlas, IME/DPI and native audio acceptance remain unverified; this is a public preview.
- Shared registration `7ac6802cf3fae1674b9e72f42ef829d8065eed04` updated only AetherRadio, preserving all six other entries after incorporating concurrent updates. The anonymous commit-pinned index returned HTTP 200 with seven entries and version 1.0.0.1. The normal main URL initially returned cached 1.0.0.0.
- Existing 1.0.0.0 release assets were retained unchanged. Source/index/ledger writes use the GitHub connector; CLI is used only for unsupported Release/asset operations and local Git-object synchronization.

## Previously verified public release: 1.0.0.0

- Public preview published as a prerelease, not a draft, following the user's common-tab update/publication request.
- Source release commit: `6148f21ce6d476dd3a2b2e90e9a3e64eb80b3b1c`.
- Public Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v1.0.0.0
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v1.0.0.0/AetherRadio-1.0.0.0.zip
- ZIP SHA-256: `DCF7C7C75C8418B0A8E7014F6458511444F1939E3BD45BEAA0CCE52287039A42`.
- Copied the exact canonical shared-tab implementation. The independent localized Language settings tab owns the seven-language selector; Support uses the canonical colors, IDs, spacing, button dimensions and click-only Ko-fi behavior. Saved language, lists, playback preferences and the surrounding player UI are preserved.
- Clean Release build: zero warnings/errors. 188 managed/offline checks and 45 isolated ImGui checks passed. All seven embedded dictionaries have 110 keys. Native font-atlas integration, IME/DPI and audio acceptance in the running game remain unverified.
- Anonymous RepoUrl, IconUrl and ZIP returned HTTP 200; the icon has an image content type. The downloaded ZIP hash, manifest version/name/author/URLs, original icon and licenses were verified.
- Shared repository registration: `71d9d7e991fd84bce941aa042c3acce16c5d71a5`. Only AetherRadio changed; all six other entries are preserved.
- The anonymous commit-pinned and normal `main` index URLs returned HTTP 200 with all seven entries and BGMPlayer 1.0.0.0. The normal URL initially returned cached 0.1.7.0; revalidation returned the new entry.
- Source and package checks retained third-party MIT notices and found no credential/personal-path matches; host SDK binaries and Windows font files are not bundled.

## Previously verified public release: 0.1.8.0

- Public preview version: 0.1.8.0, published on 2026-10-08 (JST) as a prerelease, not a draft.
- Source release commit: `1ba32a791ef1b09b647d1acfc41671981cb077df`.
- Public Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v0.1.8.0
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v0.1.8.0/AetherRadio-0.1.8.0.zip
- Seven-language implementation and publication complete. The user authorized publication in the coordinating localization chat on 2026-10-08; this was verified from the original user message.
- Clean Release build: zero warnings/errors. 170 managed checks, 188 including four official game-data languages, and 34 isolated ImGui checks passed.
- Seven embedded dictionaries (107 keys each), stable UI IDs, saved choice preservation and public SDK language detection are included.
- Release ZIP SHA-256: `DAEBF342B3926C70A6A72A6A59DBDE33A0E03C9EBF8084B7F6334C776C9C34B2`.
- Public RepoUrl, IconUrl (image content type) and download URL returned HTTP 200 without authentication. The downloaded public ZIP matches the local hash; packaged version, InternalName, display name, author, URLs and upstream license verified.
- No credential/personal-path matches in distributable source. Existing MIT metadata notices, original icon and author/URL fields retained. Host SDK assemblies and Windows font files are not bundled.
- Managed font-atlas integration, IME/DPI and native audio remain unverified inside the running game.
- At the time of this release, shared registration was delegated to the coordinating chat and the index still showed 0.1.7.0. The subsequent 1.0.0.0 update supersedes that pending registration.

## Previously verified public release: 0.1.7.0

- Public preview version: 0.1.7.0.
- Dedicated repository: https://github.com/Roxyz0501/AetherRadio
- Repository visibility: public.
- Public Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v0.1.7.0
- Release tag: `v0.1.7.0`; published as a prerelease, not a draft.
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v0.1.7.0/AetherRadio-0.1.7.0.zip
- Native in-game acceptance tests remain outstanding, as documented in the release.
- ZIP SHA-256: `9FB0D2FCE535398FA1533D5011106CA7636F1F5FF969B93118B797CD33224AC5`.
- Verified unauthenticated HTTP 200 for RepoUrl, IconUrl (image content type) and ZIP.
- Downloaded public ZIP matches the SHA-256 above; packaged author, URLs and license checked.
- Shared repository: the existing Roxyz0501/DalamudPluginRepo.
- Shared repository entry: published in registration commit
  `df4ea31165314001adc0f0ea1359de65b8679f38`.
- Registration verified through both public main and commit-pinned raw indexes.
- Release ZIP: `artifacts/AetherRadio-0.1.7.0.zip`.
- No plugin dependencies; runtime dependencies documented in `release/dependencies.json`.
- Release source commit: `a89de4a24b1ec595da2872373314a15476c0c769`.

On 2026-10-03 the user explicitly requested publication after being informed that
in-game playback/lock/restore testing was outstanding. Publish as an initial preview
with that limitation documented, after release packaging and public URL checks.
This does not change the recorded standalone classification.

0.1.1.0: user-requested movable mini player, LUNE-inspired appearance, expansion/genre tabs,
continuous scrolling and readable unknown titles. 21 managed/offline checks and isolated
ImGui interaction tests passed. Audio control is unchanged; no packet send code added.

0.1.2.0: replaced system-config writes with a transient BGM bus multiplier.
33 managed/offline checks and isolated ImGui volume interactions passed. Public
ZIP hash, author, URLs and license verified. Live listening remains unverified.
Seeking remains unsupported by the current native playback backend; a separate
playback engine was proposed; the user subsequently declined seeking on 2026-10-04.

0.1.3.0: display name BGMPlayer and /bgmplayer primary command. Existing AetherRadio
InternalName, assembly and config identity retained for in-place updates. Commands
now reveal the window on screen and focus it; legacy command remains available.
Release build, 24 managed checks and command/UI harness passed. Public main and
commit-pinned indexes, repository/icon/download and packaged identity verified.
Actual execution of the new command in the running game remains unverified.

0.1.4.0: logout resets selection, queue/history, elapsed time and playback errors,
releases/disposes native hooks and restores transient BGM volume. Next manual
play creates a fresh engine; persistent playlists/settings are unchanged.
44 managed checks, 53 including offline catalogue checks, and the command/UI
harness passed. Clean Release build, public repository/icon/ZIP, packaged
identity/license, SHA-256 and both shared index URLs verified. Native in-game
logout/relogin and audio restoration remain unverified.

0.1.5.0: the volume session additionally suppresses the local Orchestrion bus
during playback and restores it on stop/logout/unload. Furniture commands,
game config and non-music buses are untouched. 55 managed checks and 64 including
offline game data passed; clean Release build has zero warnings/errors.
Public repository/icon/ZIP, package metadata/license/hash and both shared index
URLs verified. Actual in-game orchestrion switching/restoration remains unverified.

0.1.6.0: added 0-200% BGM gain (native bus ceiling retained) and repeat buttons
in both players. Published as a GitHub prerelease, but not added to the shared
index: the user then requested one-track repeat as the default during publication.
Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v0.1.6.0
ZIP SHA-256: `B6187C4BE7A8242D7430535A9B820ADBCADD9A229FBE952BFF508D5D433FA103`.

0.1.7.0: one-track repeat by default; version-1 configs migrate once and subsequent
user choices persist. Includes the volume/repeat controls above. 67 managed checks,
76 including offline catalogue checks, and the isolated UI/command harness pass.
Public repository/icon/ZIP, package identity/license/hash and both shared index
URLs verified. No runtime game sound settings or non-music buses are changed.
Native live listening and loop acceptance remain unverified.
