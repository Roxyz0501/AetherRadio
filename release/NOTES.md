BGMPlayer 1.1.0.0

- ミニプレイヤーに「ゲームBGMへ戻す」と、お気に入り登録・解除の星ボタンを追加しました。
- レイドを「ノーマルレイド（零式含む）」「アライアンス」「特殊コンテンツ」に分けました。絶・滅、エウレカ、ボズヤなどは特殊コンテンツから探せます。
- 設定に「新しい拡張にも過去の曲を表示する」を追加しました。オフにすると同じ音源は関連先が確認できる最も古い拡張だけに表示します。「すべて」や個人のリストは対象外です。
- このログイン中に再生した最新30曲を「最近の再生」から選べます。ログアウトでクリアされます。
- 新しい表示は7言語に対応しています。保存済みの設定・マイリストは引き継ぎます。
- 分類表をキャッシュし、起動時のシート取得要求を削減しました。

`/bgmplayer` で開きます。設定の重複表示は初期状態ではオンです。
拡張の関連データを使った分類のため、正確な初出パッチを判定するものではありません。

This remains a public preview. Clean Release build, managed/offline catalogue checks and isolated ImGui interaction checks passed. Live-game audio, font-atlas, IME and DPI acceptance remain unverified. Existing release assets are unchanged.

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors. Full notice and license are included in the ZIP.
