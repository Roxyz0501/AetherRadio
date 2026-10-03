# Validation — 2026-10-04

Version: 0.1.3.0 (public preview). Author: Roxyz0501.

## 0.1.3.0 naming and command verification

- Display name and UI labels changed to BGMPlayer; primary command is /bgmplayer.
- InternalName, assembly/config identity and ImGui IDs remain AetherRadio so existing installations and settings update in place.
- Installed 0.1.2.0 logs showed successful loading without a matching draw exception. Saved window placement alone does not establish the cause of the user's failed-open report.
- Opening explicitly centers the main window within the active viewport, constrains its size and requests focus.
- Isolated ImGui test invokes the registered /bgmplayer handler with the window closed and positioned outside the viewport; verifies visible placement and focus.
- Command tests also check stop with surrounding whitespace, legacy /aetherradio, cleanup and preservation of an unrelated legacy registration.
- Existing UI search/filter/scroll, volume and mini movement checks passed.
- Native in-game execution of the new command remains unverified.
- User declined seeking; no alternate playback engine was added.

## 0.1.2.0 volume verification

- Removed the IGameConfig dependency and all system-config writes from the plugin.
- Native adapter permits only SoundBus.Music and SoundBus.TimeStretchBGM, not master, SE, voice, environment or mute controls.
- Twelve new mixer-session tests cover attenuation, silence/unmute, no cumulative attenuation, no repeated writes, restoration, external volume changes, manager replacement, clamping and invalid values.
- Main and mini volume sliders update one persisted plugin setting and save once per completed interaction in the ImGui harness.
- Total: 33 managed/offline checks. Live listening, bus response and restoration in the running game remain unverified.
- Seek investigation: installed and upstream BGMSystem/ISoundData APIs provide no established seek operation. The advance timer is not presented as a working seek control.

## 0.1.1.0 UI verification

- Actual RadioUi rendered in an isolated ImGui context with title-bar-only movement enabled.
- Dragging the mini header detaches corner placement; one save occurs on mouse release.
- Pinned movement is ignored; saved Vector2 position survives Newtonsoft JSON roundtrip.
- Expansion and genre tabs filter together; Japanese search accepts typed text.
- All 1,293 tracks remain in a continuous list; the bottom is reachable and rendering stays below 20,000 vertices in the harness.
- Library, filtered list, end of list, settings, Support and moved mini-player previews inspected.
- ContentType-based trial, raid and dungeon classification checked against local game data.
- Unknown titles never substitute raw BGM_EX file names; they remain explicitly marked unknown.
- BgmPlayback.cs and Player.cs are unchanged from 0.1.0.0. No packet send or network hook added.

## Completed

- .NET 10 / Dalamud API 15 clean Release build: zero warnings, zero errors.
- 12 managed playback queue checks: empty/single queues, deduplication, ordering,
  history, manual skipping, repeat off/all/one, complete shuffle cycles,
  shuffle cycle boundaries and selection retention.
- Nine additional checks against an installed Japanese game data set:
  full BGM-row coverage, complete fallback categorization, successful sheet loading,
  field mapping, duty mapping and bundled Japanese title resolution.
- Catalogue: 1,293 installed BGM entries; 1,015 with meaningful song-title metadata;
  625 without any expansion mapping (628 exclusively in the Other genre); two sheet entries reference missing audio files and are excluded.
  Placeholder titles such as `???` are marked as unknown, with a location when available and a stable BGM ID.
  A song can appear under multiple expansions/locations.
- Release package checked for author, RepoUrl and IconUrl retention, icon and
  upstream MIT license. Host SDK assemblies are not bundled.
- Publication recheck: clean Release build and all 21 checks passed. Public
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

1. Load the development DLL, open `/bgmplayer`, type a search and change filters.
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
