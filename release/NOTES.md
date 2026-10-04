BGMPlayer 0.1.5.0

- オーケストリオン再生中の競合に対応。BGMPlayerの再生中だけ、自分に聞こえるオーケストリオン音を抑えます。
- 停止・ログアウト・プラグイン終了時にはオーケストリオン音量を戻します。
- 音量の復帰処理で一部にエラーが起きても、残りの音量の復帰を試みます。
- 家具の曲やプレイリスト、他のプレイヤーに聞こえる音は変更しません。

`/xlplugins` からBGMPlayerを更新してください。開くコマンドは `/bgmplayer` です。

ゲームの音量設定を書き換える処理やパケット送信はありません。
Releaseビルドと64件の管理コード・オフラインデータチェックを実施しました。
実ゲーム内でのオーケストリオンとの切り替え・音声復帰は未検証です。

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
