# Validation — 2026-10-08

Version: 0.1.8.0 (public preview). Author: Roxyz0501.

## 0.1.8.0 localization

- Seven complete embedded dictionaries, 107 keys each: ja, en, de, fr, ko, zh-Hans, zh-Hant. Tests reject missing/empty values and mismatched composite-format placeholders; all format strings are exercised. Missing runtime lookups fall back to English, then a diagnostic key.
- Public APIs verified against the installed API 15 SDK/XML and a successful build: `IClientState.ClientLanguage`, `Dalamud.Utility.ClientLanguageExtensions.ToCode()` and `IDalamudPluginInterface.UiLanguage`. Game language has priority. No supported public launcher-language integration was found; no private launcher files, internal settings reflection or OS language detection is used.
- Nullable `Configuration.Language` distinguishes unset from English. Concrete normalized values are saved once; saved choices short-circuit source reads. Tests cover all seven values, regional codes, explicit Chinese scripts, ambiguous zh, unknown values, exceptions, Auto/invalid settings, existing Japanese/English choices, restart/session changes, and preservation of playlists/favorites/volume/repeat enums.
- All UI-owned text, current error messages, command help, tooltips, generated unknown titles and Support content use the selected language. ImGui labels use stable IDs; changing language does not recreate the UI or catalogue. Configuration opens directly on the Settings tab.
- Font setup uses public `IUiBuilder.CreateFontAtlas`, `IFontAtlas.NewDelegateFontHandle`, `AddDalamudDefaultFont`, `AttachWindowsDefaultFont` and `IFontHandle.Push`. A single plugin-owned atlas merges the translation character ranges from Windows fonts, checks missing glyphs after building and is disposed on unload. No fonts are redistributed and switching languages does not rebuild the atlas.
- Official game names remain in the client data language. Exact-path Orchestrion names take priority when available; other titles keep the attributed Japanese supplementary metadata. No synthetic Korean/Chinese game-name translations. Genre classification uses the official Japanese ContentType sheet for stable keys on all game clients, then translates the UI label.
- 170 managed checks passed; 188 with Japanese/English/German/French offline game data. The catalogue retains all 1,293 installed BGM entries. Direct duty associations are identical across the four data languages.
- 34 isolated ImGui checks passed: every translation character and native selector name has a glyph using the installed Windows fonts; all seven actual combo selections immediately save; search and pending playlist input survive; command help updates; mini repeat/volume/drag still operate. Actual library/settings/Support/mini draws were inspected, including German/French wrapping and Korean/Chinese text. Support links were not opened by tests.
- Clean Release build passed with zero warnings/errors after refreshing a cached NuGet audit connection failure. Release ZIP validation passed via `tools/Build-Release.ps1`, including manifest author/URLs, seven embedded resources, upstream notices, and no host SDK assemblies.
- Still unverified in the running game: Dalamud-managed font atlas integration, optional-font behavior on other Windows installations, IME/DPI/gamepad behavior, and native audio playback/restore/loop acceptance. Unit font coverage and software-rendered previews do not establish those live results.

## 0.1.7.0 default repeat mode

- User requested one-track repeat as the default while 0.1.6.0 publication was in progress. 0.1.6.0 is retained as a GitHub prerelease; the shared index advances directly from 0.1.5.0 to 0.1.7.0.
- New configurations default to RepeatMode.One. Version-1 configurations migrate once to one-track repeat and persist schema version 2; subsequent mode choices remain untouched.
- Four migration checks cover new defaults, existing settings, unrelated data preservation and user choices on reload. Total: 67 managed checks, 76 including offline game data.

## 0.1.6.0 volume boost and player repeat controls

- User confirmed playback at 100% matches normal game BGM and requested a higher level. Both sliders now permit 0–200%; 100% retains the original behavior. No saved volume is automatically increased.
- Boost uses the existing temporary Music/TimeStretchBGM mixer gain, capped at the native 0–1 bus range. Master volume and mute remain effective; the multiplier does not guarantee twice the perceived loudness. Orchestrion suppression and restoration are retained.
- Eight new managed checks cover boost, no cumulative gain/repeated writes, 100% return, mute/stop restoration, out-of-range input, game-side changes, silent baselines and the native ceiling.
- Main and mini players have a repeat button cycling All → One → Off. One uses the existing native track loop and disables automatic track advance. Queue/config values are synchronized and saved on click; no estimated-duration restart is added.
- The isolated ImGui harness clicks both sliders above 100%, cycles repeat from both players, verifies queue state and JSON persistence, and passes existing command/filter/drag/pin checks. The actual UI was rendered and inspected for layout/overlap.
- Total: 63 managed checks; 72 including offline game data. Clean Release build: zero warnings/errors.
- Live listening remains unverified. Non-looping game tracks still follow their native playback behavior; the loop button does not synthesize loops for short effect tracks.

## 0.1.5.0 orchestrion coexistence

- User reports that selecting a BGMPlayer track does not replace an already-playing in-game orchestrion.
- The native SoundManager exposes a separate Orchestrion bus, outside the existing Music/TimeStretchBGM volume handling. While playing, the volume session now suppresses only that additional music bus and restores its latest baseline on stop, logout and unload.
- No OrchestrionManager playback/stop methods, furniture commands, network functions, system-config writes or non-music buses are used. The existing native SetVolume adapter is restricted to three explicitly named music buses.
- Eleven new tests cover suppression at full volume, mute/unmute, unchanged-frame writes, restoration, external volume changes, initially silent orchestrions, sound-manager replacement and cleanup failure. Lifecycle tests also assert orchestrion restoration on stop/logout/unload.
- Total: 55 managed checks, 64 including offline game-data checks. The tests use an injected mixer and do not establish audible behavior inside the running game.
- Live acceptance remains outstanding: play an orchestrion in a room, select a BGMPlayer song, change/advance both playlists, enter/leave the room, stop, log out and unload. Confirm the selected BGM is audible and the current orchestrion track returns on stop, with SE unchanged.
- The previous UI is unchanged.

## 0.1.4.0 login/logout lifecycle

- Login/logout events and a frame/command state check cover active and stopped playback.
- Logout clears the selected track, queue, shuffle bag/history, clock and playback error; saved configuration is unchanged.
- Each session releases and disposes its native hooks. The next manual play constructs a fresh engine. Initialization failures can be retried without per-frame initialization or autoplay.
- Native cleanup discards old scene requests at logout. It resets scene 0 only when the same valid BGMSystem still contains the player's selection; newer game requests are preserved.
- Both hook disables and volume restoration are attempted even if a cleanup step throws. Ownership is cleared before cleanup; hook disposal uses finally blocks.
- Native hooks pass through game requests while logged out. Playback checks both BGM scene readiness and the sound manager before starting.
- 20 new tests exercise the real Player with injected playback services: active/paused logout, missed/early/repeated events, no autoplay, all playback entry points, saved configuration, volume restoration, fresh engines, initialization/readiness retries, cleanup failure and unload.
- Total: 44 managed checks, 53 including the installed game-data catalogue. Native hook behavior is source-reviewed; injected services do not execute game audio functions.
- Clean Release build has zero warnings/errors. Existing command, UI filtering/search/scroll, volume, mini movement/pinning and position persistence checks pass in the isolated ImGui harness.
- Running-game logout/relogin, title BGM and live audio restoration still require confirmation.

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
7. While playing at reduced volume with BGM lock enabled, log out and check that title BGM is normal.
   Log back in, confirm no selected track or autoplay, and play a different track. Repeat after stopping first,
   and with another character. Confirm lists, favorites, volume and position are retained.
   Unload/reload and check that no override survives; inspect Dalamud logs for errors.
8. Verify public RepoUrl, IconUrl (HTTP 200 + image type), release ZIP URLs, and
   packaged metadata before adding the shared repository entry.
