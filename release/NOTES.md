BGMPlayer 1.1.1.0

- メインプレイヤーのリピートボタン横に、ミニプレイヤーの表示切り替えを追加しました。表示中は水色になります。
- プラグイン終了後の再生要求を受け付けないようにし、後からフックが作り直される経路を閉じました。
- 大きすぎる設定ファイルもバックアップから復元できるようにしました。元のファイルは残します。
- ゲーム側がBGMを直接切り替えた場合の復帰情報を修正し、無音状態やフェード設定を保持します。

- 設定はすぐ画面に反映し、ファイルへの保存を60秒ごと・変更がある場合だけにしました。
- ログアウト、ゲームの正常終了、プラグイン無効化時に未保存の設定を保存します。ドラッグ途中の位置や音量も対象です。
- 正常な設定を3世代バックアップし、本体が破損している場合は正常なバックアップから復元します。破損ファイルは残します。
- 保存に3回連続で失敗した場合は自動保存を止め、設定画面から再試行できるようにしました。
- 終了時の読み込み処理、保存処理、イベント購読の回収を整えました。

強制終了・クラッシュでは、未保存の最大約60秒分が失われる場合があります。
保存できない状態が続く場合は、設定画面の案内を確認してください。

Clean Release build: zero warnings/errors. 238 managed checks (256 with offline game data) and 90 isolated ImGui checks passed using synthetic settings. Live-game shutdown timing, native audio and font-atlas/IME/DPI acceptance remain unverified; this release remains a public preview.

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors. Full notice and license are included in the ZIP.
