# Validation — 2026-10-03

Version: 0.1.0.0 (public preview). Author: Roxyz0501.

## Completed

- .NET 10 / Dalamud API 15 clean Release build: zero warnings, zero errors.
- 12 managed playback queue checks: empty/single queues, deduplication, ordering,
  history, manual skipping, repeat off/all/one, complete shuffle cycles,
  shuffle cycle boundaries and selection retention.
- Six additional checks against an installed Japanese game data set:
  full BGM-row coverage, complete fallback categorization, successful sheet loading,
  field mapping, duty mapping and bundled Japanese title resolution.
- Catalogue: 1,293 installed BGM entries; 1,015 with meaningful song-title metadata;
  625 in Other; two sheet entries reference missing audio files and are excluded.
  Placeholder titles such as `???` fall back to the BGM ID and file name.
  A song can appear under multiple expansions/locations.
- Release package checked for author, RepoUrl and IconUrl retention, icon and
  upstream MIT license. Host SDK assemblies are not bundled.
- Publication recheck: clean Release build and all 18 checks passed. Public
  repository, image and download URLs returned HTTP 200 without authentication.
  The downloaded ZIP's SHA-256 and packaged metadata match the released artifact.
- Source inspected for packet/network/event-send code: none implemented.
- Source scan for common credential patterns and personal workstation paths:
  no matches in distribution source (build outputs excluded).
- Original icon: PNG, 512 × 512; built-in image generation; prompt recorded.
- Library, settings, Support tab and mini player rendered from the actual RadioUi
  class using an isolated ImGui context and Japanese font; visual check of clipping,
  controls and placement. Japanese search input was sent through the real input
  widget and verified. The Support link was not opened by the test.
- The initial standalone input test exposed a native InputTextEx failure. Text
  fields now use the public InputText API with bounded UTF-8 buffers; library
  rendering and Japanese typing pass after this change.

## Not verified in the running game

- Native playback, restoring event BGM, entering/leaving duties, boss transitions,
  battle, mounts, cutscenes, logout and plugin unload/reload.
- Tracks with special playback modes, disabled restart flags or non-looping audio.
- Host-specific UI behavior under Dalamud (IME, gamepad navigation, DPI/font scales).
- Actual audio completion is not observed. Automatic advance uses metadata
  estimates with a configurable fallback interval.

## In-game acceptance sequence

1. Load the development DLL, open `/aetherradio`, type a search and change filters.
2. Play a field, duty, mount and short non-looping track; check volume.
3. With lock enabled, trigger battle and content BGM changes; confirm chosen audio
   remains. Stop and confirm the latest normal BGM returns.
4. Disable lock and verify the next game BGM request releases the override.
5. Create/rename/reorder a playlist, test shuffle/repeat, reload and verify persistence.
6. Check each mini-player corner, free placement, long titles and high UI scaling.
7. Stop, logout and unload/reload; confirm hooks are released and no audio override
   survives. Check Dalamud logs for errors.
8. Verify public RepoUrl, IconUrl (HTTP 200 + image type), release ZIP URLs, and
   packaged metadata before adding the shared repository entry.
