# AVEC — Aki's Video Enhancement Components

VRChatの動画プレイヤーに、共通プレイリストUIとジェスチャーリモコンを追加するアセットです。iwaSync3 / KineL / VizVid / YamaPlayerに対応します。

## 主な機能

- タブ付きプレイリスト、順番・シャッフル・単曲リピート再生。
- 再生キュー・再生履歴（各最大32件）。同期再生では入力者名と動画タイトルも共有。
- 再生／一時停止、音量調整、スピーカーアイコンによるミュート。
- ジェスチャーリモコン、操作ガイド、手元への召喚。
- 任意のExtensionによるYouTube取り込み・動的タイトル更新、プレイリストとリモコンの連携。

## 導入

確認環境は **Unity 2022.3.22f1 / VRChat Worlds SDK 3.10.5**。UdonSharpはSDK同梱版を使用します。プレイヤー本体・SDK・AudioLinkは同梱しません。

対応確認版: iwaSync3 3.8.0 / KineL 2.5.5 / VizVid 1.8.2 / YamaPlayer 2.0.0-beta.7。

1. SDK入りのWorldプロジェクトに、使用する動画プレイヤー本体を導入します。
2. `AVEC-Core`と、必要な`AVEC-Playlist`／`AVEC-Remote`を導入します。
3. 使用するプレイヤー・機能に対応した`AVEC-Connector-プレイヤー名-機能名`を導入します。未導入プレイヤー用のConnectorは入れないでください。
4. `Assets/aki_lua87/AVEC/Prefabs/`から`CommonVideoPlaylist.prefab`または`CommonGestureRemote.prefab`を配置し、ルートのSetup Inspectorで接続先を選択します。

GitHubのソースを利用する場合は、`Assets/aki_lua87/AVEC`のCore・必要な機能・Connector・Extensionと、必要なPrefabを`.meta`ごと同じパスへ配置してください。Cloneだけで開けるUnityプロジェクトや、UPMのGit URLから導入するパッケージではありません。全Connectorを配置する場合は4種類すべてのプレイヤーが必要です。

## プレイリスト

Inspectorの「新規プレイリストを作成」から専用データを作り、「プレイリストを編集する」でタブ・動画を編集して保存します。同じデータを複数のUIで共有できます。DefaultPlaylistはサンプル動画入りです。

動画行の「＋」またはURL入力で再生キューへ追加します。キューは追加順に再生し、プレイリストより優先します。▶はキューの再生開始・再開、「クリア」は再生待ちの項目を削除します。再生履歴は行を選ぶと再度再生できます。音量・ミュートは自分だけに適用されます。

YouTubeから取り込む場合は`AVEC-Extension-DynamicPlaylist`を導入し、「動的プレイリストを開く」でID／URLを入力してプレビュー・保存します。取り込みと動的タイトル更新は外部サービス`https://vrc.akakitune87.net`へアクセスします。動的URLはサービスの稼働とAllow Untrusted URLsに依存し、最大20枠です。バックエンドはこのリポジトリに含みません。

## リモコンとガイド

操作の割り当てはワールド内のガイドを参照してください。手元ガイドはリモコンを持って右スティックを下へ長押し（初期値約1.33秒）、DesktopではHキー長押しで表示します。常設の操作一覧には`CommonGestureGuide.prefab`を配置し、リモコンと同じGestureDataを指定します。

持っていないリモコンは右スティックの下長押し（初期値3秒）で召喚できます。各Setup Inspectorの「召喚を有効にする」でON／OFFを設定します。複数台をONにすると複数台が来るため、通常は1台だけを有効にします。

プレイリストも操作する場合は`AVEC-Extension-PlaylistRemote`を導入し、同じプレイヤーへ接続したPlaylistViewを指定します。「次の動画」は再生キューを優先します。

## 配布

必要な時に`Tools > Common Assets > Release Builder`でUnityPackageとZIPを生成します。ツールのソースは`Assets/aki_lua87/Editor/CommonAssetsReleaseBuilder.cs`です。プレイヤー本体と開発用デモワールドは収録しません。

C#／UdonSharpコンパイルと編集時の構成を確認しています。VRChat内の操作・複数人同期・途中参加・Quest/Android、新規環境への導入は別途手動確認が必要です。

## ライセンス

独自コード・Prefab・アイコン等は[MIT License](LICENSE)です。Copyright (c) 2026 aki_lua87。

同梱のZen Maru Gothic Regularは**SIL Open Font License 1.1**です。帰属表示は`Assets/aki_lua87/AVEC/Core/THIRD_PARTY_NOTICES.txt`、全文はCoreとRemoteの`Fonts/OFL.txt`にあります。外部プレイヤー・SDK等には各配布元の利用条件が適用されます。
