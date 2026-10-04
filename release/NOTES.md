BGMPlayer 0.1.6.0

- プレイヤー音量を最大200%まで調整できるようにしました。100%は従来と同じ音量です。
- メインとミニプレイヤーにループボタンを追加。「リスト全体 → 1曲 → オフ」の順で切り替わります。
- アイコンに「1」が付くと1曲ループになり、自動曲送りが止まります。
- 音量とループの設定は保存され、次回も引き継がれます。

音量はゲーム側の上限まで上げられます。マスター音量・ミュートは引き続き有効です。
SE・ボイス・環境音は変更しません。1曲ループはゲーム本来のループを使います。

`/xlplugins` からBGMPlayerを更新してください。開くコマンドは `/bgmplayer` です。

ゲームの音量設定を書き換える処理やパケット送信はありません。
Releaseビルド、72件の管理コード・オフラインデータチェック、単体UIの操作・保存を確認しました。
実ゲーム内での音量とループ再生の実機確認は未完了です。

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
