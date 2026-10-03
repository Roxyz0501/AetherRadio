Aether Radio 0.1.1.0

- ミニプレイヤーの上部をドラッグして移動できるように修正。位置の保存と固定・解除に対応。
- LUNEを参考に、暗いグレーと水色の配色、線画の再生ボタンに変更。
- 拡張とジャンルをタブで選び、コンテンツ名をプルダウンで絞り込む表示に変更。
- 曲一覧のページ送りを廃止。画面内の行だけを描画するスクロール一覧に変更。
- 曲名不明の曲は内部ファイル名ではなく「曲名未登録」と場所名・識別番号を表示。
- プラグイン説明文を簡潔に整理。

`/xlplugins` から更新できます。`/aetherradio` で開きます。
ミニプレイヤーは「AETHER RADIO」をドラッグしてください。固定中は「解除」を押すと移動できます。

音声制御と通信に関する処理は変更していません。ゲームサーバーへのパケット送信機能はありません。
曲名データが未登録の曲に正式名称を推測して付けることはしていません。
ゲーム内の全コンテンツでの音声再生・固定・復帰は検証未完了です。検証範囲は VALIDATION.md を参照してください。

カスタムリポジトリ:
https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
