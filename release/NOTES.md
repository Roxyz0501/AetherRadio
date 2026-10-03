Aether Radio 0.1.2.0

- 音量スライダーがゲーム設定を直接書き換える処理を削除。
- プラグイン側に音量を保存し、再生中だけBGMの音量系統に反映。
- SE・ボイス・環境音・マスター音量は変更しません。
- 停止・ログアウト・終了時にBGM音量を復元。途中でゲーム側の音量を変えた場合は、その変更を保持。
- メイン画面とミニプレイヤーの音量を同期。

100%はゲーム側で設定したBGM音量です。ゲーム側でBGMがミュートされている場合は音が出ません。
旧版で変更済みのゲーム音量は、必要に応じてゲーム設定から調整してください。
シークには未対応です。現在のバーは曲送りまでの目安表示です。

Releaseビルド、33項目の管理コード・オフラインデータテスト、単体UI操作を確認済み。
実ゲーム上の聴感・全コンテンツでの復帰確認は未完了です。

`/xlplugins` から更新できます。`/aetherradio` で開きます。
https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
