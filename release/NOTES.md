BGMPlayer 1.0.0.1

- 最新の共通タブコードを取り込みました。「ライブラリ → 設定 → 言語設定 → 支援」の順序を共通関数で固定しています。
- 1.0.0.0と同じタブ配置です。Dalamudの設定ボタンから設定タブを直接開く動作も維持しています。
- 保存済みの言語、マイリスト、1曲ループ、音量、ウィンドウ位置を引き継ぎます。

`/bgmplayer` を開き、「言語設定」タブから変更できます。
正式な曲名・場所名はゲームデータの言語です。補足の曲名は日本語の場合があります。

UI resources are embedded for all seven languages. Windows language fonts are used without bundling font files.
Clean Release build (zero warnings/errors), 170 managed checks and 59 isolated ImGui checks passed, including tab order and direct Settings access in all seven languages.
Tab navigation and language changes perform no additional catalogue reads. Unchanged views do not save configuration.
In-game font-atlas rendering, IME/DPI behavior and native audio acceptance remain unverified.

This remains a public preview with the live-game limitations above. Previously published 1.0.0.0 assets are retained unchanged.

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
