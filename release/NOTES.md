BGMPlayer 0.1.3.0

- プラグイン名をAether RadioからBGMPlayerに変更。
- `/bgmplayer` で開く、`/bgmplayer stop` で通常のゲームBGMへ戻します。
- 開くときにウィンドウを画面内へ再配置し、手前に表示するよう修正。
- 旧コマンド `/aetherradio` も互換用に維持。
- 既存のマイリスト・お気に入り・音量・ミニプレイヤー位置を引き継ぎます。

`/xlplugins` で既存のAether Radioを更新してください。更新後の表示名はBGMPlayerです。
シークは今回追加していません。

再生中のBGMだけを調整する音量仕様は継続しています。ゲーム設定を書き換える処理やパケット送信はありません。
Releaseビルド、管理コード・オフラインデータテスト、コマンドと単体UIの操作を検証しています。
実ゲーム内での今回のコマンド実行は未検証です。

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors.
Full notice and license are included in the ZIP.
