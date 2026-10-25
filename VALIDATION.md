# Validation — 2026-10-12

## 2026-10-22 persistence remediation (1.1.1.0)

- 60-second changed-only configuration persistence, bounded retries, three valid backups, recovery and shutdown flushing implemented. See [PERSISTENCE_REMEDIATION.md](PERSISTENCE_REMEDIATION.md) for scope, failure cases and measurements.
- Clean Release: zero warnings/errors. 235 managed and 76 isolated ImGui checks passed. No production configuration migration or installed DLL replacement.
- Synthetic continuous edits plus logout/exit flushing: 3 changed commits / 1,841 payload bytes; idle and post-dispose writes 0. UI scenario: 3 commits including fixture initialization / 1,348 payload bytes. These are application payload counts, not SSD physical I/O.
- Session-only recent playback remains in memory; daily persistent history and HistoryFiles UI are not applicable. Canonical standard tabs remain byte-identical. Existing native audio and font-atlas limitations still apply.


## 1.1.0.0 library and mini-player controls

- 200 managed checks, 218 including the installed Japanese/English/German/French game catalogues. All 1,293 installed tracks remain available. Tests cover normal/Savage versus alliance/special classifications, audio-path reuse across IDs, different arrangements, earliest-known expansion selection, All/Other access, config migration/persistence, bounded recent history, failure exclusion and logout invalidation.
- 71 isolated ImGui checks passed. Added actual mini restore/favorite clicks, recent navigation and logout refresh, visible-list cache invalidation, favorites bypassing duplicate suppression and unchanged-history cache reuse. Seven-language glyph coverage and shared tabs remain covered. Software-rendered Japanese library and German mini-player layouts inspected; no clipping observed at the harness font size.
- All seven locale dictionaries contain 117 nonempty keys with matching format placeholders. Canonical shared tabs remain unchanged.
- Measured local data-service calls: initial catalogue construction requests 11 sheets (previously 4,810), and checks 1,295 audio paths. These are service requests, not physical reads. Target: at most 12 initial requests; one existence check per candidate BGM row. Initial full local BGM enumeration is required to expose all installed tracks; no external transfers or database writes.
- Tab/language/filter navigation, duplicate toggle filtering and history updates make 0 additional catalogue requests. Unchanged views/history frames cause 0 config saves. Favorite clicks save once each; restore saves zero times. History stays in memory (30 IDs maximum) and clears on logout/unload. Filter options and sorted catalogue are reused; filtered rows update only when their cache key changes. The catalogue is immutable for the plugin session and reloads on plugin initialization; no polling or external synchronization was added.
- Audio hooks, packet behavior, gain, game settings and one-track-loop defaults were not changed. Mini restore uses the existing Player.Stop path; isolated tests verify dispatch and retained manual resume, not live audio output. No game-packet transmission code was introduced.
- Running-game audio, Dalamud font-atlas, IME/DPI behavior and other Windows font installations remain unverified.


Version: 1.0.0.1 (public preview). Author: Roxyz0501.

## 1.0.0.1 canonical trailing tab group

- Re-copied the latest canonical `StandardPluginTabs.cs` exactly, including `DrawStandardTrailingTabs`. The plugin calls it once after the Library tab and before ending the main tab bar. There are no plugin-specific tabs after this group.
- The visible order remains Library, Settings, Language settings, Support. All three drawing callbacks are cached to avoid per-frame delegate allocation. The existing Settings-open request is queued through the SDK's current ImGui tab bar before layout, so the canonical helper remains unchanged and installer Settings access is preserved.
- 59 isolated ImGui checks passed. In addition to the existing 45 checks, the harness inspects actual tab IDs and horizontal positions in all seven languages, and confirms that installer Settings selects Settings even after a different tab was selected. Language selection, saved choice, glyphs, pending input, Support sizing, command help and mini controls passed.
- The measured initial catalogue counts remain 4,810 local sheet requests and 1,295 file-existence checks. Tab navigation/language changes make zero additional catalogue requests; same-language reselection and unchanged frames make zero saves. No catalogue, network or persistence implementation changed.
- Clean Release build and 170 managed checks passed with zero warnings/errors after retrying the NuGet audit with approved network access. Source secret/private-path checks and release packaging validation passed. Native game font-atlas, IME/DPI and audio acceptance remain unverified. Existing published 1.0.0.0 assets are retained; the change uses a separate patch release.

## 1.0.0.0 common language and Support tabs

- Copied the canonical `StandardPluginTabs.cs` without code or namespace changes. The release script compares it against the workspace template when that template is available, ignoring only checkout line endings. Standalone repository builds do not require a sibling workspace template.
- The localized `Language settings###LanguageSettings` tab owns the seven-language selector. It is no longer in Settings. `Support###Support` calls the same canonical style/content methods, including language order, native names, fixed IDs, colors, spacing, button size and click-only Ko-fi navigation. BGMPlayer's surrounding UI is retained.
- Seven embedded dictionaries now have 110 complete keys each. Existing first-launch detection, normalization, migration, persistence, command help, audio/session logic and catalogue data have not changed.
- 188 managed/offline checks passed, including 170 managed checks and the installed Japanese/English/German/French catalogue. All 1,293 installed tracks remain available.
- 45 isolated ImGui checks passed. They cover glyph coverage in every language, actual language selection with one save per change, preservation of pending text, command help, mini-player repeat/volume/drag, and opening Support in all seven languages with the canonical 300-or-more by 46 pixel button. Antialiased vertex bounds allow a 1.1 pixel inset tolerance. The tests do not click the external link.
- Software-rendered language/settings/Support previews were inspected for translation layout and glyphs. The selector's language order/native names are checked against saved configuration codes.
- Clean Release build passed with zero warnings/errors after retrying the NuGet audit through approved network access. Packaging verified the manifest identity/version/URLs, all seven embedded language resources, existing MIT notices, original icon and absence of host SDK binaries. Source checks found no credential or personal workspace path matches.
- Measured in the isolated harness: the initial catalogue build made 4,810 local sheet-service requests and 1,295 local file-existence checks. These counts are API calls, not physical disk or network reads. Subsequent tab navigation and seven language changes made **0** additional catalogue requests; reselecting the saved language and 120 unchanged frames caused **0** saves. Each actual language change saved once. The unchanged-view target is zero catalogue reads and zero saves; no new DB/network access or polling was introduced.
- Running-game font-atlas integration, IME/DPI behavior and native audio acceptance remain unverified. The isolated font and rendering checks do not establish those live results.

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

## 2026-10-25 full recheck and main mini-player toggle (1.1.1.0)

Reviewed playback/session/volume ownership, queue behavior, catalogue/filter caches, UI controls, localization, commands, fonts and deferred atomic persistence. Added the main transport toggle using the existing ShowMini/HideMini translations and the same saved ShowMini value as Settings and the mini close button. Seven-language actual ImGui clicks verify immediate hide/show, one dirty notification per click, and zero immediate file writes.

Fixed oversized-primary recovery (including repair preserving the entire oversized original), post-dispose playback recreation, and direct native scene restoration metadata (zero BGM uses ResetBGM; fade and initial-volume fields are retained). Regression tests cover oversized files and all late player entry points. Native scene restoration was code-reviewed and compiled; actual native audio behavior remains unverified.

Clean Release has zero warnings/errors. 238 managed checks / 256 with four offline game-data languages passed; 90 isolated ImGui checks passed. Catalogue retains 1,293 installed tracks, all genre coverage and no internal file-name titles. Initial catalogue sheet requests remain 11; subsequent navigation reads are zero. Synthetic UI scenario has three commits / 1,348 logical payload bytes including initial fixture, zero writes during unchanged frames or immediate toggles. No production configuration or installed plugin DLL was changed. Physical SSD writes and live-game audio/lifecycle/font-atlas/IME/DPI behavior are not measured by these tests.
