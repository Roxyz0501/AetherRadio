# BGMPlayer publication state

Updated: 2026-10-04 (JST).
Classification: new standalone plugin, explicitly selected by the user.
Author: Roxyz0501.

- Public preview version: 0.1.3.0.
- Dedicated repository: https://github.com/Roxyz0501/AetherRadio
- Repository visibility: public.
- Public Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v0.1.3.0
- Release tag: `v0.1.3.0`; published as a prerelease, not a draft.
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v0.1.3.0/AetherRadio-0.1.3.0.zip
- Native in-game acceptance tests remain outstanding, as documented in the release.
- ZIP SHA-256: `072E89308944CD298E80D9CA932414E1B1042DDF681C67592A826052CD9F2A59`.
- Verified unauthenticated HTTP 200 for RepoUrl, IconUrl (image content type) and ZIP.
- Downloaded public ZIP matches the SHA-256 above; packaged author, URLs and license checked.
- Shared repository: the existing Roxyz0501/DalamudPluginRepo.
- Shared repository entry: published in registration commit
  `92e0724d34951dc6ed9a4286af5ca2f4fdc56ff9`.
- Registration verified through both public main and commit-pinned raw indexes.
- Release ZIP: `artifacts/AetherRadio-0.1.3.0.zip`.
- No plugin dependencies; runtime dependencies documented in `release/dependencies.json`.

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
