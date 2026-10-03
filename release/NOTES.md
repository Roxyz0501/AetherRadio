BGMPlayer 0.1.4.0

- ログアウト時に再生とBGM固定を解除し、選択中の曲・再生キュー・経過時間をリセットします。
- 次のログインでは、曲を選んだときに再生エンジンを作り直します。前回の曲は自動再生しません。
- 停止中にログアウトした場合もリセットします。
- 初期化が間に合わなかった場合も、再度曲を選んで再生できます。
- マイリスト・お気に入り・音量・ウィンドウ位置は保持します。

`/xlplugins` からBGMPlayerを更新してください。開くコマンドは `/bgmplayer` です。

ゲームの音量設定を書き換える処理やパケット送信はありません。
Releaseビルドと53件の管理コード・オフラインデータチェックを実施しました。
実ゲーム内でのログアウト・再ログインと音声復帰は未検証です。

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
