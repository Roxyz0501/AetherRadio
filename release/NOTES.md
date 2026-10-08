BGMPlayer 0.1.8.0

- 設定の「言語 / Language」で日本語・English・Deutsch・Français・한국어・简体中文・繁體中文を選べるようにしました。
- 初回だけゲーム言語 → Dalamud UI言語 → Englishの順で選びます。保存後はログインやキャラクター切替で上書きしません。
- タブ、ボタン、ヘルプ、エラー、ジャンル、曲名未登録の表示、支援タブを翻訳しました。
- 言語変更はすぐに反映され、入力中の検索やリスト名は残ります。長い訳文の折返しと文字表示も調整しました。
- 正式な曲名・場所名はゲームデータの言語です。補足の曲名は日本語の場合があります。

設定はDalamudの設定ボタン、または `/bgmplayer` の設定タブから開けます。
1曲ループの初期設定、マイリスト、音量などはそのまま引き継がれます。
パケット送信やゲームの音量設定変更は追加していません。

UI resources are embedded for all seven languages. Windows language fonts are used without bundling font files.
Clean Release build, 188 managed/offline data checks and 34 isolated ImGui checks passed.
In-game font-atlas rendering, IME/DPI behavior and native audio acceptance remain unverified.

This is a public preview. The shared custom repository is updated separately.

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
