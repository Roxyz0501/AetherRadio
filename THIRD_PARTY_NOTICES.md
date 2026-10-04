# Third-party references and notices

## OrchestrionPlugin — selected metadata and conceptual reference

- Project: https://github.com/perchbirdd/OrchestrionPlugin
- Original author: Meli (ff-meli); maintained by perchbird and contributors.
- License: MIT. Copyright (c) 2020 Meli. Full text: `licenses/Orchestrion-MIT.txt`.
- Pinned revision: `e1031563f03d0bd7ae833326e0ac9f05aa67a9bb`.
- Reused files: `Orchestrion/Data/xiv_bgm_ja.csv` and `xiv_bgm_metadata.csv`.
  These provide BGM IDs, Japanese song titles, search terms and estimated durations.
  They are embedded unchanged. Credits for the metadata include the upstream
  spreadsheet maintainers and community contributors, including MagowDeath.
- The BGMController and BGMPlayer source was consulted to understand scene
  priority and the existing concept of maintaining a local scene override.
  BGMPlayer's implementation calls the FFXIVClientStructs BGMSystem API and
  was independently written; no upstream controller code or assets were copied.
- This is a standalone project, not a fork or a copy of the upstream repository.
  Roxyz0501 does not claim authorship of the reused metadata.

## Runtime API dependencies, supplied by Dalamud

- Dalamud / Dalamud.NET.Sdk, goatcorp and contributors:
  https://github.com/goatcorp/Dalamud (AGPL-3.0-or-later).
  Plugin service APIs, ImGui bindings and hook provider; runtime assemblies are
  supplied by the user's Dalamud installation and not bundled in the release.
- FFXIVClientStructs, aers and contributors:
  https://github.com/aers/FFXIVClientStructs (MIT).
  BGMSystem and SoundManager structures, generated delegates, SetBGM/ResetBGM
  and the Music, TimeStretchBGM and Orchestrion SoundBus volume functions.
  No native signature patterns or library source files are copied into this plugin.
- Lumina / Lumina.Excel, NotAdam and contributors:
  https://github.com/NotAdam/Lumina and https://github.com/NotAdam/Lumina.Excel (MIT).
  Read-only access to installed game sheets and file existence checks.

## Assets and game data

`images/icon.png` is an original generated icon. It does not use game artwork.
Game audio, game data files, and game runtime binaries are not distributed.
The catalogue is assembled from the user's installed game data at runtime.
