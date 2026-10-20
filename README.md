# AVEC — Aki's Video Enhancement Components

VRChatの動画プレイヤーに、共通プレイリストUIとジェスチャーリモコンを追加するアセットです。iwaSync3 / KineL / VizVid / YamaPlayer / ProTV 3に対応します。

## 主な機能

- タブ付きプレイリスト、順番・シャッフル・単曲リピート再生。
- 再生キュー・再生履歴（各最大32件）。同期再生では入力者名と動画タイトルも共有。
- ユーザーリスト（最大32件）のURL追加・選択再生・削除と、ユーザー／ワールドごとの永続化。任意のYTTL連携で追加時にタイトルを取得・保存。
- Inspectorからキュー・履歴・ユーザーリストを個別に有効化。
- 再生／一時停止、音量調整、スピーカーアイコンによるミュート。
- ジェスチャーリモコン、操作ガイド、手元への召喚。
- 任意のExtensionによるYouTube取り込み・動的タイトル更新、プレイリストとリモコンの連携。

## 導入

確認環境は **Unity 2022.3.22f1 / VRChat Worlds SDK 3.10.5**。UdonSharpはSDK同梱版を使用します。プレイヤー本体・SDK・AudioLinkは同梱しません。

コンパイル・編集時の接続確認版: iwaSync3 3.8.0 / KineL 2.5.5 / VizVid 1.8.2 / YamaPlayer 2.0.0-beta.7 / ProTV 3.0.0-beta.31。

1. SDK入りのWorldプロジェクトに、使用する動画プレイヤー本体を導入します。
2. `AVEC-Core`と、必要な`AVEC-Playlist`／`AVEC-Remote`を導入します。
3. 使用するプレイヤー・機能に対応した`AVEC-Connector-プレイヤー名-機能名`を導入します。未導入プレイヤー用のConnectorは入れないでください。
4. `Assets/aki_lua87/AVEC/Prefabs/`から`CommonVideoPlaylist.prefab`または`CommonGestureRemote.prefab`を配置し、ルートのSetup Inspectorで接続先を選択します。

GitHubのソースを利用する場合は、`Assets/aki_lua87/AVEC`のCore・必要な機能・Connector・Extensionと、必要なPrefabを`.meta`ごと同じパスへ配置してください。Cloneだけで開けるUnityプロジェクトや、UPMのGit URLから導入するパッケージではありません。全Connectorを配置する場合は5種類すべてのプレイヤーが必要です。

ProTVは`Packages/dev.architech.protv`のTVManagerへ接続します。ProTV 2は対象外です。音量を各ユーザーだけで調整するため、ProTVのSync Volume ControlはOFFで使用してください。ONの場合、共通UI／リモコンからの音量変更は拒否します。ProTVのネイティブな認可・ロック設定に従い、連続再生はTVPluginのイベントで処理します。本体やネイティブPrefabの変更は不要です。

開発用デモはエントランスと廊下、5つのプレイヤー個室＋入口を閉じた追加用の空き部屋から構成します。QvPen・ミラー・共通ガイドはエントランス、RTABはYamaPlayer室に配置します。各室の`Local room audio`（DemoRoomAudio）はローカルプレイヤーの部屋内外を判定してAudioSourceのenabledを切り替え、ネイティブの音量・ミュート設定を保ったまま室外の動画音声を遮断します。ワープ・リスポーンにも位置判定で対応します。新しいスピーカーを追加した際はsourcesとsourceEnabledも更新してください。各リモコンの呼び出し範囲は対象プレイヤーの部屋内に限定し、廊下・エントランスからは呼び出せません。

## プレイリスト

手持ち版は`CommonVideoPlaylistTablet.prefab`を使用します。既存UIのInspectorの「現在のUIから手持ちタブレット版を作成」で、手動調整と接続先を保ったコピーも作成できます。幅32cmの画面、Pickup、レイ操作、VRの人差し指タップ・スクロールに対応します。[設定と手動確認](Assets/aki_lua87/AVEC/Features/Playlist/Tablet.md)を参照してください。

Inspectorの「新規プレイリストを作成」から専用データを作り、「プレイリストを編集する」でタブ・動画を編集して保存します。同じデータを複数のUIで共有できます。DefaultPlaylistはサンプル動画入りです。

「タブのアイコンを表示」で種類別アイコンをON／OFFできます。キューは再生マーク付きリスト、履歴は時計、ユーザーリストは人物、静的プレイリストは通常のリスト、動的プレイリストは更新マーク付きリストを表示します。動的／静的はタイトル取得元URLの有無で自動判別し、OFFにすると名前の左余白も戻ります。タブ名や保存済み動画データは変更しません。

動画行の「＋」またはURL入力で再生キューへ追加します。キューは追加順に再生し、プレイリストより優先します。▶はキューの再生開始・再開、「クリア」は再生待ちの項目を削除します。再生履歴は行を選ぶと再度再生できます。音量・ミュートは自分だけに適用されます。

「ユーザーリスト」は自分のURLを保存するリストです。行を選ぶと再生し、「削除モード」中は行を選ぶと削除します。同じワールド内の複数UIで共通のリストを表示し、次回訪問時に復元します。PlaylistSetupの「特殊リスト」のチェック変更はその場で反映されます。VizVid用Playlist ConnectorとYTTL Managerを任意で接続すると、再生せずに追加時のタイトル取得を要求します。[保存方式・設定・手動確認](Assets/aki_lua87/AVEC/Features/Playlist/UserList.md)を参照してください。

YouTubeから取り込む場合は`AVEC-Extension-DynamicPlaylist`を導入し、「aki_lua87's Playlist Generator を開く」でID／URLを入力してプレビュー・保存します。取り込みと動的タイトル更新は外部サービス`https://vrc.akakitune87.net`へアクセスします。動的URLはサービスの稼働とAllow Untrusted URLsに依存し、最大20枠です。バックエンドはこのリポジトリに含みません。

チャンネルの指定には`UC…`形式のチャンネルID、`@ハンドル`、`/channel/ID`・`/@handle`形式のYouTube URLを使用できます。動的・固定のどちらも登録APIでチャンネルを解決し、応答の`channel_id`をタイトル取得・再生・固定一覧取得のURLに使用します。登録APIが有効なチャンネルIDを返さない場合は取り込みを中止します。既存タブのURLや手動設定は自動で書き換えません。

YouTubeプレイリストのタブ名はサーバー応答の`playlist_title`を初期値として使用します。旧サーバーや名前を取得できない場合はIDを表示し、プレビューで変更できます。名前を取得するためのYouTubeページのHTML取得は行いません。既存タブ名は取り込み処理で上書きしません。

固定キーワードで更新されるタブは、取り込み元を「YouTube検索」にしてキーワードを入力し、「取得してプレビュー」から追加します。検索タブは動的専用で、新着順の検索結果を最大20枠で表示します。タブ名は挿入前に変更できます。キーワードはURLエンコードされ、動画は検索順位に対応するURLへ接続されます。タブ選択時に動画タイトルを取得し、検索結果が少ない時の余剰枠は取得後に非表示になります。現在0件でも将来の結果用に20枠を確保します。検索結果はサーバーの時刻単位キャッシュに従い、ワールドを更新せずに入れ替わります。VRChat内でキーワードを変更する機能ではありません。動的URLをキュー・ユーザーリスト等へ保存した場合も、その順位の動画が後から変わることがあります。

背景を透過して枠線だけを表示する場合は、PlaylistSetupのテーマに`Samples/Themes/Wireframe.asset`を指定します。枠線の太さと色をテーマで調整できます。左メニューはスクロールバーとの間に余白を確保し、重複するコンテナの枠を省きます。元のテーマへ切り替えると元のスプライトと余白に戻ります。ルートの位置・サイズ、フォント、動画データ、接続先は変更しません。

RTAB上で使う場合は`AVEC-Extension-RTabPlaylist`を導入してください。[導入手順](Assets/aki_lua87/AVEC/Extensions/RTabPlaylist/README.md)に従って、RTABのApps配下へ`RTabPlaylist.prefab`を配置します。RTab本体・YamaPlayerと、Core・Playlist・YamaPlayer用Playlist Connector・DynamicPlaylistが必要です。

## リモコンとガイド

操作の割り当てはワールド内のガイドを参照してください。手元ガイドはリモコンを持って右スティックを下へ長押し（初期値約1.33秒）、DesktopではHキー長押しで表示します。常設の操作一覧には`CommonGestureGuide.prefab`を配置し、リモコンと同じGestureDataを指定します。

持っていないリモコンは右スティックの下長押し（初期値3秒）で呼び出せます。各Setup Inspectorの「呼び出しを初期状態で有効にする」でON／OFFを設定します。「呼び出し可能範囲（任意）」にBoxColliderを指定すると、プレイヤーの足元が範囲内にいる時だけ呼び出せます。未設定なら従来どおりどこからでも呼び出せます。範囲外へ出ると長押しの進行はキャンセルされ、ガイド表示や通常のPickup操作には影響しません。

「呼び出し範囲を作成・接続」で、通行を妨げないTrigger設定済みの範囲を作成できます。選択されたBoxColliderのCenter／SizeとTransformで範囲を調整してください。配布用の`Features/Remote/Samples/RemoteSummonArea.prefab`も同梱します。範囲はリモコンとは別の固定オブジェクトとして配置してください。リモコン自身やその子にある範囲、無効・非アクティブな範囲では呼び出せません。複数台の範囲を重ねたり範囲なしで有効にすると、その場所では複数台が呼び出し対象になります。

プレイリストも操作する場合は`AVEC-Extension-PlaylistRemote`を導入し、同じプレイヤーへ接続したPlaylistViewを指定します。「次の動画」は再生キューを優先します。

## 配布

必要な時に`Tools > Common Assets > Release Builder`でUnityPackageとZIPを生成します。ツールのソースは`Assets/aki_lua87/Editor/CommonAssetsReleaseBuilder.cs`です。プレイヤー本体と開発用デモワールドは収録しません。

C#／UdonSharpコンパイルと編集時の構成を確認しています。VRChat内の操作・複数人同期・途中参加・Quest/Android、新規環境への導入は別途手動確認が必要です。

## ライセンス

独自コード・Prefab・アイコン等は[MIT License](LICENSE)です。Copyright (c) 2026 aki_lua87。

同梱のZen Maru Gothic Regularは**SIL Open Font License 1.1**です。全文はCoreとRemoteの`Fonts/OFL.txt`にあります。

外部プレイヤー・RTab・SDK等の帰属表示、公式配布先、利用条件とProTV／ArchiTech.SDK等のライセンス全文は[THIRD_PARTY_NOTICES.txt](Assets/aki_lua87/AVEC/Core/THIRD_PARTY_NOTICES.txt)にまとめています。本体のソース・ネイティブPrefab・導入パッケージは同梱しません。一部のコンパイル済みUdonに含まれる外部基底クラス等の処理には元の利用条件が適用され、AVECのMITライセンスの対象にはなりません。通知ファイルはRelease Builderで各UnityPackageとZIPへ収録します。RTAB拡張の配布・販売説明には[RTabのBOOTH URL](https://rasta.booth.pm/items/7311462)も記載してください。
