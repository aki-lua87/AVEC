# AVEC構成変更の確認（2026-10-04）

- C#コンパイル成功。UdonSharp全体コンパイルは新規エラー0。
- 4プレイヤーのConnector登録、既存シーンの接続検証、Playlist／RemoteのPrefab参照を確認。
- 変更前のPlaylistSetup Inspector値・Viewの既存公開フィールド・全Transform値を照合し、差分なし。
- DefaultPlaylistは空のまま保持。シーンはインメモリで維持し、未保存変更を閉じたり読み直したりしていない。
- 13個の配布定義についてCollectPathsを確認。出力範囲はAVEC内だけで、第三者プレイヤーとシーンを含まない。
- Coreと機能のC#・asmdefに、動的取得や特定プレイヤーへの直接依存がないことを確認。
- 一時的な検証用PlaylistDataを用いて、末尾への挿入、既存タブ保持、プレビューの深いコピー、本体の未保存編集による挿入拒否を確認。検証データは削除済み。
- チャンネルID正規化、YouTube以外のホストの拒否を確認。HTTP要求は実行していない。
- Playモード、VRChat実機、別プロジェクトへのインポート、配布パッケージ生成は未実施。

今回、解消済みコンパイルエラーのConsole表示がUdonSharpの開始を阻害していたため、履歴47件を`Library/AVEC-ConsoleHistory-before-compile.json`へ保管して表示を整理した後、再コンパイルした。

手動確認: 各Connectorで再生・停止・自然終了・キュー、Remote操作、DynamicPlaylistの取得プレビューと挿入、実行時タイトル更新、複数人同期・途中参加を確認する。旧版を利用する環境への更新はMIGRATION.ja.mdのGUID保持手順に従う。
