BGMPlayer 1.0.0.0

- 日本語・English・Deutsch・Français・한국어・简体中文・繁體中文の選択を、独立した「言語設定」タブに移しました。
- 言語設定と支援タブを、Roxyz0501の各プラグインで使う共通コードに揃えました。
- 支援タブの色、余白、ボタンの大きさを統一しました。Ko-fiはボタンを押したときだけ開きます。
- プレイヤーの外観や操作は従来どおりです。保存済みの言語、マイリスト、1曲ループ、音量、ウィンドウ位置を引き継ぎます。
- 言語は初回だけゲーム → Dalamud UI → Englishの順で選びます。変更はすぐに反映・保存し、同じ言語の再選択では保存しません。

`/bgmplayer` を開き、「言語設定」タブから変更できます。
正式な曲名・場所名はゲームデータの言語です。補足の曲名は日本語の場合があります。

UI resources are embedded for all seven languages. Windows language fonts are used without bundling font files.
Clean Release build (zero warnings/errors), 188 managed/offline data checks and 45 isolated ImGui checks passed.
Tab navigation and language changes perform no additional catalogue reads. Unchanged views do not save configuration.
In-game font-atlas rendering, IME/DPI behavior and native audio acceptance remain unverified.

This remains a public preview with the live-game limitations above.

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
