# YTTL Titles Extension

ユーザーリストへURLを追加した時、再生せずにタイトルを取得する任意拡張です。
YTTL本体は同梱しません。現在の実装はVizVid同梱の`VVMW.ThirdParties.Yttl.YttlManager`用です。
単体版YTTLやiwaSync3用YTTL Connectorを導入する必要はありません。

## 導入と接続

1. AVEC-Core / AVEC-Playlistと、利用するプレイヤー用Playlist Connectorを導入します。
2. VizVid同梱のYTTLが利用できるプロジェクトに限り、`AVEC-Extension-YttlTitles`を追加します。
3. 同じシーンへVizVid付属の`YTTL Manager.prefab`を配置します。
4. PlaylistSetupの「YTTLタイトル取得（拡張）」でManagerを指定します。1件だけなら「シーン内のYTTLを接続」も利用できます。
5. 既存設定の確認・更新には「YTTL接続を適用」を使います。UI全体の再生成は不要です。

再生するプレイヤーはVizVidに限定しません。YTTL用にAVEC-Connector-VizVid-Playlistを導入する必要もありません。
拡張を入れない場合、基本プレイリストにYTTL設定は表示されず、URL追加・保存・選択再生・削除は引き続き利用できます。
取得失敗・未対応URL・未接続の場合はURLを表示します。取得済みのタイトルは通常のユーザーリストデータとして保存されます。
DynamicPlaylistのサーバーからのタイトル取得は別機能で、この拡張を必要としません。

## 既存プロジェクトからの更新

旧版のVizVid Connectorに含まれていた`VizVidPlaylistTitles`を、この拡張へ移動しました。
スクリプト・Program Asset・コンパイル済みProgramのGUIDと型名を保持しているため、拡張も導入する更新では既存の接続を引き継ぎます。
PlaylistSetupの旧`yttlManager`は`titleMetadataSource`へシリアライズ名を移行します。

旧版から更新してYTTL連携を外す場合は、旧版のうちに各SetupのManagerを空にして接続を解除し、シーン上の`VizVidPlaylistTitles`と対応するBacking UdonBehaviourを除去してください。
その後、旧`Connectors/VizVid/Playlist/VizVidPlaylistTitles.cs`・`.asset`とその専用コンパイル済みProgramも除去します。
UnityPackageの更新だけでは旧版に含まれたアセットは削除されません。ネイティブプレイヤーやYTTL本体のファイルは削除対象に含めません。
他の機能やプレイヤー内部が独自にYTTLを使う場合、その依存関係はAVECでは変更しません。

## 配布と手動確認

`Tools > Common Assets > Release Builder`の`AVEC-Extension-YttlTitles`で個別出力できます。
基本のPlaylistやVizVid Connectorには、この拡張の実装・Programを収録しません。

- 拡張なし: YTTLなしのプロジェクトでコンパイルし、URL追加・保存・選択再生・削除を確認。
- 拡張あり: タイトル取得、通信失敗時のURL表示、追加直後の削除への遅延応答、再訪時の保存済みタイトルを確認。
- 既存設定: 更新後もManager・Resolverの参照とプレイリストの手動調整が維持されていることを確認。

YTTL本体・ネイティブプレイヤーのソースやPrefabは変更・再配布しません。
配布元の現状や将来の可用性に依存しない基本機能を維持し、この拡張は既に必要な依存関係を導入済みの環境向けに提供します。
