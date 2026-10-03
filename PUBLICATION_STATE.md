# Aether Radio publication state

Updated: 2026-10-04 (JST).
Classification: new standalone plugin, explicitly selected by the user.
Author: Roxyz0501.

- Public preview version: 0.1.2.0.
- Dedicated repository: https://github.com/Roxyz0501/AetherRadio
- Repository visibility: public.
- Public Release: https://github.com/Roxyz0501/AetherRadio/releases/tag/v0.1.2.0
- Release tag: `v0.1.2.0`; published as a prerelease, not a draft.
- ZIP: https://github.com/Roxyz0501/AetherRadio/releases/download/v0.1.2.0/AetherRadio-0.1.2.0.zip
- Native in-game acceptance tests remain outstanding, as documented in the release.
- ZIP SHA-256: `A3B350C36D5A0ADD3AE8BEB13D415635C6995A5E5B389271964E4264DD5A17D8`.
- Verified unauthenticated HTTP 200 for RepoUrl, IconUrl (image content type) and ZIP.
- Downloaded public ZIP matches the SHA-256 above; packaged author, URLs and license checked.
- Shared repository: the existing Roxyz0501/DalamudPluginRepo.
- Shared repository entry: published in registration commit
  `670ddec1fbea499fd5224e00d556a415104c2c79`.
- Registration verified through both public main and commit-pinned raw indexes.
- Release ZIP: `artifacts/AetherRadio-0.1.2.0.zip`.
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
playback engine was proposed to the user but has not been selected or implemented.
