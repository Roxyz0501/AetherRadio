BGMPlayer 1.1.2.0

- 通常BGM表に加え、オーケストリオンのファイル表からもインストール済みの曲を拾うようにしました。
- 「闘う者達」「更に闘う者達」を追加。「闘う者たち」でも検索できます。コンテンツとの対応がない曲は「その他」から選べます。
- 曲名が未登録でも、ファイル表と実ファイルがある曲は一覧に含めます。お気に入り・マイリストにも対応しています。
- 追加曲は端末内のBGM再生処理で扱います。オーケストリオンのサーバー向け再生要求は使用しません。

検証環境では全2,179曲（追加886曲）を収集できました。Releaseビルドは警告・エラーなし。248 managed checks / 269 with offline game data and 92 isolated ImGui checks passed.
追加曲の実ゲーム内での音出し、曲切り替え、ループ、停止・ログアウト時の音声復帰は未確認です。公開プレビューとして配布します。

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors. Full notice and license are included in the ZIP. Game audio files are not included.
