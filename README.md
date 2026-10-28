# AVEC — Aki's Video Enhancement Components

VRChatの動画プレイヤーに、共通プレイリストUIとジェスチャーリモコンを追加するアセットです。iwaSync3 / KineL / VizVid / YamaPlayer / ProTV 3に対応し、Coreに独自の小型プレイヤーAVEC-SimpleScreenも含みます。

## 主な機能

- タブ付きプレイリスト、順番・シャッフル・単曲リピート再生。
- 再生キュー・再生履歴（各最大32件）。同期再生では入力者名と動画タイトルも共有。
- 保存リスト（最大32件）のURL追加・選択再生・削除と、ユーザー／ワールドごとの永続化。任意のYTTL連携で追加時にタイトルを取得・保存。
- Inspectorからキュー・履歴・保存リストを個別に有効化。
- 再生／一時停止、音量調整、スピーカーアイコンによるミュート。
- ジェスチャーリモコン、操作ガイド、手元への召喚。
- 任意のExtensionによるYouTube取り込み・動的タイトル更新、プレイリストとリモコンの連携。

## 導入

確認環境は **Unity 2022.3.22f1 / VRChat Worlds SDK 3.10.5**。UdonSharpはSDK同梱版を使用します。外部プレイヤー本体・SDK・AudioLinkは同梱しません。

コンパイル・編集時の接続確認版: iwaSync3 3.8.0 / KineL 2.5.5 / VizVid 1.8.2 / YamaPlayer 2.0.0-beta.7 / ProTV 3.0.0-beta.31。

1. SDK入りのWorldプロジェクトに、使用する動画プレイヤー本体を導入します。
2. `AVEC-Core`を導入します。Core・Playlist・リモコン・PlaylistRemote連携と、それぞれの配布用Prefabを含みます。
3. 使用するプレイヤーに対応した`AVEC-Connector-プレイヤー名`を導入します。各ConnectorはPlaylist・リモコン両方の接続を含みます。未導入プレイヤー用のConnectorは入れないでください。KineL V2は`AVEC-Connector-KineL`、V3は`AVEC-Connector-KineLV3`を使用します。
4. `Assets/aki_lua87/AVEC/Prefabs/`から`CommonVideoPlaylist.prefab`または`CommonGestureRemote.prefab`を配置し、ルートのSetup Inspectorで接続先を選択します。

AVEC-SimpleScreenを使用する場合は外部プレイヤーと追加Connectorは不要です。Core同梱の`AVEC-SimpleScreen.prefab`を配置し、Playlist／RemoteのSetup Inspectorでその`SimpleScreenPlayer`を選択してください。モニターにレイを当てるとシーク・ローカル音量／ミュート・URL入力を表示します。[設定と手動確認](Assets/aki_lua87/AVEC/Features/SimpleScreen/README.md)を参照してください。

GitHubのソースを利用する場合は、`Assets/aki_lua87/AVEC`のCore・必要な機能・Connector・Extensionと、必要なPrefabを`.meta`ごと同じパスへ配置してください。Cloneだけで開けるUnityプロジェクトや、UPMのGit URLから導入するパッケージではありません。全Connectorを配置する場合は5種類すべてのプレイヤーが必要です。

ProTVは`Packages/dev.architech.protv`のTVManagerへ接続します。ProTV 2は対象外です。音量を各ユーザーだけで調整するため、ProTVのSync Volume ControlはOFFで使用してください。ONの場合、共通UI／リモコンからの音量変更は拒否します。ProTVのネイティブな認可・ロック設定に従い、連続再生はTVPluginのイベントで処理します。本体やネイティブPrefabの変更は不要です。

開発用デモはエントランスと廊下、5つのプレイヤー個室＋入口を閉じた追加用の空き部屋から構成します。QvPen・ミラー・共通ガイドはエントランス、RTABはYamaPlayer室に配置します。各室の`Local room audio`（DemoRoomAudio）はローカルプレイヤーの部屋内外を判定してAudioSourceのenabledを切り替え、ネイティブの音量・ミュート設定を保ったまま室外の動画音声を遮断します。ワープ・リスポーンにも位置判定で対応します。新しいスピーカーを追加した際はsourcesとsourceEnabledも更新してください。各リモコンの呼び出し範囲は対象プレイヤーの部屋内に限定し、廊下・エントランスからは呼び出せません。

## プレイリスト

手持ち版は`CommonVideoPlaylistTablet.prefab`を使用します。配置後、PlaylistSetupでプレイリスト・テーマ・接続先を設定します。幅32cmの画面、Pickup、レイ操作、VRの人差し指タップ・スクロールに対応します。[設定と手動確認](Assets/aki_lua87/AVEC/Features/Playlist/Tablet.md)を参照してください。

横幅を抑える場合は`CommonVideoPlaylistCompact.prefab`を使用します。画面は440×560で、通常版と同じ高さです。上部のリスト選択を開くとキュー・履歴・保存リスト・通常リストを選べます。選択後、または外側を押すと閉じます。設定するプレイリスト・テーマ・接続先と操作は通常版と共通です。配布用Prefabは未接続・動画データなしです。

各タブの説明は1行表示です。入力欄も単一行になり、既存データの改行は表示時のみ空白に置き換えます。長い説明は枠の右端で切り、元の説明データは保持します。

Inspectorの「新規プレイリストを作成」から専用データを作り、「プレイリストを編集する」でタブ・動画を編集して保存します。同じデータを複数のUIで共有できます。DefaultPlaylistはサンプル動画入りです。

「タブのアイコンを表示」で種類別アイコンをON／OFFできます。キューは再生マーク付きリスト、履歴は時計、保存リストは人物、静的プレイリストは通常のリスト、動的プレイリストは更新マーク付きリストを表示します。動的／静的はタイトル取得元URLの有無で自動判別し、OFFにすると名前の左余白も戻ります。タブ名や保存済み動画データは変更しません。

動画詳細の「キューへ追加」またはキューのURL入力で再生キューへ追加します。キューは追加順に再生し、プレイリストより優先します。「次の動画へ」で次へ進み、各行の「×」で削除します。再生履歴は行から動画詳細を開けます。音量・ミュートは自分だけに適用されます。

「保存リスト」は自分のURLを保存するリストです。行を選ぶと再生し、「削除モード」中は行を選ぶと削除します。同じワールド内の複数UIで共通のリストを表示し、次回訪問時に復元します。PlaylistSetupの「特殊リスト」のチェック変更はその場で反映されます。基本機能はYTTLなしで利用できます。追加時のタイトル取得だけは、VizVid同梱YTTLを導入済みの環境向けの別パッケージ`AVEC-Extension-YttlTitles`で提供します。[拡張の導入・更新](Assets/aki_lua87/AVEC/Extensions/YttlTitles/README.md)と[保存方式・設定・手動確認](Assets/aki_lua87/AVEC/Features/Playlist/UserList.md)を参照してください。

配布Prefabはプレイリストデータを含みません。シーンへ配置した未設定のUIには、既存の`Assets/AVECUserData/DefaultPlaylist.asset`、旧版の`DefaultPlaylist.asset`の順で接続します。どちらもなければ`Assets/AVECUserData/DefaultPlaylist.asset`を作成します。設定済みの参照と登録内容は変更しません。壁設置版・タブレット版・RTAB版は初期状態では同じデータを共有し、別の内容にする場合はInspectorの「新規プレイリストを作成」で分けられます。プレイリストデータはRelease Builderの配布対象外です。

YouTubeから取り込む場合は`AVEC-Extension-DynamicPlaylist`を導入し、「AVEC-PlaylistGenerator を開く」でID／URLを入力してプレビュー・保存します。取り込みと動的タイトル更新は外部サービス`https://vrc.akakitune87.net`へアクセスします。自動更新URLはサービスの稼働とAllow Untrusted URLsに依存し、最大50枠です。バックエンドはこのリポジトリに含みません。

チャンネルの指定には`UC…`形式のチャンネルID、`@ハンドル`、`/channel/ID`・`/@handle`形式のYouTube URLを使用できます。動的・固定のどちらも登録APIでチャンネルを解決し、応答の`channel_id`をタイトル取得・再生・固定一覧取得のURLに使用します。登録APIが有効なチャンネルIDを返さない場合は取り込みを中止します。既存タブのURLや手動設定は自動で書き換えません。

YouTubeプレイリストのタブ名はサーバー応答の`playlist_title`を初期値として使用します。旧サーバーや名前を取得できない場合はIDを表示し、プレビューで変更できます。名前を取得するためのYouTubeページのHTML取得は行いません。既存タブ名は取り込み処理で上書きしません。


背景を透過して枠線だけを表示する場合は、PlaylistSetupのテーマに`Samples/Themes/Wireframe.asset`を指定します。枠線の太さと色をテーマで調整できます。左メニューはスクロールバーとの間に余白を確保し、重複するコンテナの枠を省きます。元のテーマへ切り替えると元のスプライトと余白に戻ります。ルートの位置・サイズ、フォント、動画データ、接続先は変更しません。

RTAB上で使う場合は`AVEC-Extension-RTabPlaylist`を導入してください。[導入手順](Assets/aki_lua87/AVEC/Extensions/RTabPlaylist/README.md)に従って、RTABのApps配下へ`RTabPlaylist.prefab`を配置します。RTab本体・YamaPlayerと、AVEC-Core・AVEC-Connector-YamaPlayer・DynamicPlaylistが必要です。

追加の配色は、PlaylistSetupの「Theme Asset」に`Assets/aki_lua87/AVEC/Features/Playlist/Samples/Themes/`内のアセットを指定して切り替えます。

| アセット | 配色 |
| --- | --- |
| `RedBlack.asset` | 黒い背景に鮮やかな赤いボタン・アクセント、白い文字（旧GamingRedBlack、GUID維持） |
| `WhiteBlue.asset` | 白い背景に鮮やかな青いボタン・アクセント、濃紺の文字 |
| `DefaultThemeGlass.asset` | DefaultThemeの配色を保持した半透明版 |
| `Monochrome.asset` | 白をベースに黒い文字。選択色・ホバー・押下も無彩色 |

テーマの`Button Highlight Tint`と`Button Pressed Tint`は、ホバー・押下時にボタン色へ乗算する色です。

各テーマの色のアルファ（A）が実装側の不透明度です。Glass版は背景45%、面55%、ボタン80%、選択90%で、文字は不透明です。背景と一覧などが重なる部分ではアルファが合成されます。テーマを適用すると編集時に各UIへ反映され、ランタイムにテーマを読み込む必要はありません。

固定の背景画像は既存の`Background`のImageへ設定できます。画像を`Sprite (2D and UI)`としてインポートし、Imageの`Source Image`へ指定、`Image Type`を`Simple`にします。画像の色をそのまま表示する場合はImageのColorを白にし、Aで透明度を調整します。テーマを後から適用すると背景色も再設定されるため、画像と色の設定はテーマ適用後に行ってください。Wireframeは枠線用スプライトへ切り替えるため、画像背景には通常／Glassテーマを使用します。画像の手動設定を行ったPrefab／シーンを保存しておけば実行時も固定表示です。

## リモコンとガイド

同じプレイヤーに接続したプレイリストとリモコンがある場合、プレイリストの右下に、再生ボタンと同じサイズのリモコンON／OFFボタンが表示されます。各ユーザーのローカル切り替えで、OFFではリモコン本体が非表示になり、手元への呼び出しもできません。ONで元の位置に再表示します。OFF時はリモコンアイコンに斜線が付きます。同じ接続先の壁面・タブレットUIは同じリモコンを切り替えます。リモコンがない場合はボタンを表示しません。呼び出しだけのON／OFF設定は変更しません。

再生・再生順・リモコン・ミュートの各アイコンにレイを合わせると機能名のTipsを表示し、レイを外すと消えます。Tipsはローカル表示で、クリックやレイ判定を遮りません。

操作の割り当てはワールド内のガイドを参照してください。手元ガイドはリモコンを持って右スティックを下へ長押し（初期値約1.33秒）、DesktopではHキー長押しで表示します。常設の操作一覧には`CommonGestureGuide.prefab`を配置し、リモコンと同じGestureDataを指定します。

持っていないリモコンは右スティックの下長押し（初期値3秒）で呼び出せます。各Setup Inspectorの「呼び出しを初期状態で有効にする」でON／OFFを設定します。「呼び出し可能範囲（任意）」にBoxColliderを指定すると、プレイヤーの足元が範囲内にいる時だけ呼び出せます。未設定なら従来どおりどこからでも呼び出せます。範囲外へ出ると長押しの進行はキャンセルされ、ガイド表示や通常のPickup操作には影響しません。

「呼び出し範囲を作成・接続」で、通行を妨げないTrigger設定済みの範囲を作成できます。選択されたBoxColliderのCenter／SizeとTransformで範囲を調整してください。配布用の`Features/Remote/Samples/RemoteSummonArea.prefab`も同梱します。範囲はリモコンとは別の固定オブジェクトとして配置してください。リモコン自身やその子にある範囲、無効・非アクティブな範囲では呼び出せません。複数台の範囲を重ねたり範囲なしで有効にすると、その場所では複数台が呼び出し対象になります。

プレイリストも操作する場合は、Core同梱のPlaylistRemote連携を使用し、同じプレイヤーへ接続したPlaylistViewを指定します。追加パッケージは不要です。「次の動画」は再生キューを優先します。

## 配布

必要な時に`Tools > AVEC > Release Builder`でUnityPackageとZIPを生成します。ツールのソースは`Assets/aki_lua87/Editor/CommonAssetsReleaseBuilder.cs`です。AVEC-SimpleScreen本体とコネクターはCoreに収録し、外部プレイヤー本体と開発用デモワールドは収録しません。

配布は計10パッケージです。`AVEC-Core`が1つ、Connectorが6つ（IwaSync / KineL / VizVid / YamaPlayer / ProTV / KineLV3）、Extensionが3つ（DynamicPlaylist / RTabPlaylist / YttlTitles）です。Extensionは引き続き個別導入します。

旧版の`AVEC-Playlist`／`AVEC-Remote`／`AVEC-Extension-PlaylistRemote`は`AVEC-Core`へ、機能別Connectorは同じプレイヤーの統合Connectorへ置き換わります。アセットのパスとGUIDは維持するため、更新時に既存アセットを削除する必要はありません。Release Builderの保存済み選択は、統合前のいずれかが選択されていれば統合後も選択する形で移行します。

C#／UdonSharpコンパイルと編集時の構成を確認しています。VRChat内の操作・複数人同期・途中参加・Quest/Android、新規環境への導入は別途手動確認が必要です。

## ライセンス

独自コード・Prefab・アイコン等は[MIT License](LICENSE)です。Copyright (c) 2026 aki_lua87。

同梱のZen Maru Gothic Regularは**SIL Open Font License 1.1**です。全文はCoreとRemoteの`Fonts/OFL.txt`にあります。

外部プレイヤー・RTab・SDK等の帰属表示、公式配布先、利用条件とProTV／ArchiTech.SDK等のライセンス全文は[THIRD_PARTY_NOTICES.txt](Assets/aki_lua87/AVEC/Core/THIRD_PARTY_NOTICES.txt)にまとめています。本体のソース・ネイティブPrefab・導入パッケージは同梱しません。一部のコンパイル済みUdonに含まれる外部基底クラス等の処理には元の利用条件が適用され、AVECのMITライセンスの対象にはなりません。通知ファイルはRelease Builderで各UnityPackageとZIPへ収録します。RTAB拡張の配布・販売説明には[RTabのBOOTH URL](https://rasta.booth.pm/items/7311462)も記載してください。
