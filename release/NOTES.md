BGMPlayer 1.1.3.0

- 追加曲の再生時に、無音用のBGM_Nullを選んでしまう不具合を修正しました。タイトル・ランチャー・キャラクター選択画面用の曲も除外します。
- ゲームが現在再生中・切り替え中のBGMと重ならない曲を使うようにしました。
- 追加曲が8秒以内に音声読み込み処理へ到達しない場合は、再生中の表示を続けずに停止してエラーを表示します。

Clean Release: zero warnings/errors. 251 managed checks / 273 with offline game data and 92 isolated ImGui checks passed. 実データで初回・連続選曲とも無音ファイルを使わないことを確認しました。実ゲーム内の音出しは未確認のため公開プレビューです。

https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json

Includes MIT-licensed song metadata from OrchestrionPlugin by Meli, perchbird and contributors. Notices and license are included; game audio is not bundled.
