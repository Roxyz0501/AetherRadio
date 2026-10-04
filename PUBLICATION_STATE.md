# BGMPlayer publication state

Updated: 2026-10-04 (JST).
Classification: new standalone plugin, explicitly selected by the user.
Author: Roxyz0501.

- Public preview version: 0.1.5.0.
- Dedicated repository: https://github.com/Roxyz0501/AetherRadio
- Repository visibility: public.
- Public Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v0.1.5.0
- Release tag: `v0.1.5.0`; published as a prerelease, not a draft.
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v0.1.5.0/AetherRadio-0.1.5.0.zip
- Native in-game acceptance tests remain outstanding, as documented in the release.
- ZIP SHA-256: `928D376B8C2015D7D719D7156AD11EE6CFC5B217F6DB80FD51AD8B1D4121DF3B`.
- Verified unauthenticated HTTP 200 for RepoUrl, IconUrl (image content type) and ZIP.
- Downloaded public ZIP matches the SHA-256 above; packaged author, URLs and license checked.
- Shared repository: the existing Roxyz0501/DalamudPluginRepo.
- Shared repository entry: published in registration commit
  `3681cb2c021c8ad0a1a216dde4163f2ff817d878`.
- Registration verified through both public main and commit-pinned raw indexes.
- Release ZIP: `artifacts/AetherRadio-0.1.5.0.zip`.
- No plugin dependencies; runtime dependencies documented in `release/dependencies.json`.
- Release source commit: `b88776bfe4ad2cf7d7127e3a7680d8ecaf68a2ce`.

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
