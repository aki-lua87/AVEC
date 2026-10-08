# 検証記録

## 操作・表示改善（2026-10-05）

- UnityMCPでC# / UdonSharpコンパイルを確認。現在のコンパイルエラーなし。
- 4種類のプレイヤー接続先、プレイリストの位置・色、リモコンの接続先を保持し、編集時の参照を確認。
- 各プレイリストの122動画行・進捗バー、6行のキュー・履歴、動的プレイリストの20インデックス枠を確認。履歴タブとパネルの重複なし、ボタンの参照欠落なし。
- 一時停止・再開、3種類の再生モードとネイティブ設定連動、iwaSync音量換算、履歴・キュー保持・動的タイトルによる非表示のコードを更新。
- リモコンはVRで自由角度・AutoHoldなし、既定UD=ミュート切替、DU=1動画リピート切替。最大3方向のInspector設定を追加し、動画ON/OFF操作を削除。
- デモシーンに影なしの補助Directional Lightを追加。作業前から未保存変更があったため、シーン全体は保存していない。必要な自作Prefab・データアセットのみ保存。
- Playモード、VRChat内の動作・ネットワーク同期・VRでの持ち方は未確認。別プロジェクト、配布パッケージ生成、ネイティブ本体の変更は行っていない。


## YouTubeプレイリストの取り込み（2026-10-04）

- 編集画面の取り込み元に「プレイリスト」を追加。YouTube URLのlistパラメーターまたはIDから、固定URL・動的URLの動画行を作成します。既存のタイトル取得形式と互換性があるため、Udon実行コードは変更していません。
- UnityMCPからこのプロジェクトのC#コンパイル正常、UdonSharpの117スクリプトのコンパイル成功を確認。
- 公開リスト `PLBCF2DAC6FFB574DE` のAPI応答を確認し、Unity Editor内で保存しない取り込み結果を確認。固定・動的とも13件、同じタイトル順、固定側に目次URLなし、動的側の位置インデックスとURL、目次URLが動画行に入らないことを確認。
- 開いているCommonAssetsDemoシーンは作業開始時から未保存変更あり。4つのPlaylistSetupのInspector値・Transform・Prefab overridesを作業前後で比較し、一致を確認。データの保存、シーン保存、UI全体のBake、Prefab再生成、別プロジェクト確認、Playモードテスト、配布パッケージ生成は実施していません。
- ワールド内での動的URLの実再生・タイトル更新、編集ウィンドウの手動クリックによる追加保存はユーザーによる確認が必要です。

## コンパクトUI・テーマ・ネイティブ操作・デモ（2026-10-04 / 0.2.0）

- このプロジェクト内でC# / UdonSharpコンパイルを確認しました。別プロジェクトでは検証していません。
- 4方向のデモシーンで、4プレイヤーそれぞれのプレイリスト接続設定、再生順アイコンと音量UIの参照、4リモコンのプレイヤー／プレイリスト参照、SceneDescriptor・Spawn・PipelineManagerを編集時に確認しました。
- Playモード、自動ランタイムテスト、SDK Build & Test、アップロードは実行していません。再生・停止・音量・ジェスチャー・ネットワーク同期はデモシーンで手動確認する前提です。
- Release Builderのエディタスクリプトを追加しました。今回の変更ではUnityPackageを生成していません。以下の旧版の検証記録は履歴であり、0.2.0の実機動作を示すものではありません。

## 編集欄が無効になる不具合（2026-10-03）

- 編集用コピーの `HideFlags.HideAndDontSave` に `NotEditable` が含まれ、SerializedPropertyの入力欄が無効になる不具合を修正。`HideInHierarchy | DontSave` に変更し、ドメイン再読み込みで復元した既存の編集用コピーも修復します。
- 分離Unity環境で18項目のEditor検証成功。以前のフラグで `SerializedProperty.editable` がfalseになることを再現し、新規コピーと復元コピーの入力欄が編集可能になることを確認。保存、共有UI反映、破棄、Undoの検証も成功。証跡：`Release/playlist-editor-editable-report.txt`。

## ランタイムの再生順切り替え（2026-10-03）

- ワールド内のUIに「再生順: 順番／ランダム」ボタンを追加。既存UIも保存／Play／Build時に追加し、再ベイクで重複しないようにしています。
- 再生中の動画を読み直さず次の動画から変更。終了通知後の待機も維持し、接続先の操作権限を確認します。実行時のモードはプレイヤー単位で共有・同期し、停止／タブ変更後も保持します。
- 分離プロジェクトでUdonSharpコンパイルと53項目のC#・接続検証成功。追加検証はボタンの永続Udonイベント参照、再ベイク、再読み込みなしの切り替え、表示ラベル、順番再生への復帰、権限拒否、終了待機中の切り替え、停止／タブ変更後の維持、OnDeserializationでのモード復元。
- Coreと各Extensionの5構成でC#参照限定コンパイル成功。証跡：`Release/runtime-mode-report.txt`。VRChat内のボタン操作・複数人同期は実機未確認です。UnityPackageは再エクスポートしていません。

## タブごとのランダム再生（2026-10-03）

- 編集ウィンドウにタブごとの「ランダム再生」を追加。保存時に設定をベイクします。初期値はオフで、既存の順番再生を維持します。
- ランダム再生は手動選択した行から開始し、同じタブの有効な行を重複なく一巡して停止。同期するシードと開始行から同じ順番を再構成します。
- `.Validation/AutoAdvance` で110スクリプトのUdonSharpコンパイルと、連続再生・接続を含む44項目のC#検証に成功。32種類のシードで全行を一度ずつ再生して停止すること、シードによる順番の変化、末尾行からの開始、1件だけのタブ、手動選択での再開始、同期フィールドをOnDeserializationで受け取った場合の順序復元、通常再生への復帰を確認しました。
- 証跡：`Release/shuffle-report.txt`。検証用コードは既存の `SequenceCheck.cs.txt` に追加しました。VRChat実機での映像再生・所有者交代・複数人同期は未検証です。UnityPackageは再エクスポートしていません。

## 動画終了後の連続再生（2026-10-03）

- iwaSync3、KineL、VizVid、YamaPlayerの終了イベントから、再生元タブの次の動画を要求する処理を追加。末尾で停止し、Join時の自動再生は行いません。
- 分離プロジェクト `.Validation/AutoAdvance`（Unity 2022.3.22f1 / SDK 3.10.5）で、4プレイヤーと検証用コードを含む110スクリプトのUdonSharpコンパイル成功。
- 共通処理のC#コールバック検証と、各製品Prefabを使った接続検証の36項目成功。空URLスキップ、タブ末尾、表示タブの変更、同じURLの連続行、終了通知の重複、手動停止、エラー／再試行、外部URLへの変更、YamaPlayerのStop→End順序、次動画待機中の手動選択、読み込み間隔の待機、権限拒否での停止、複数UIの共有、所有者と同じGameObjectへの配置、リスナーのプログラム参照を確認。
- Coreのみ・Core＋各Extensionの5構成でC#参照限定コンパイル成功。分離プロジェクトと本体のC#ソース一致を確認し、生成済みUdonプログラムを本体へ反映。
- 検証ソース：`Assets/CommonVideoPlaylistDevelopment/SequenceCheck.cs.txt` と `SequenceTestAdapter.cs.txt`。分離プロジェクトでそれぞれ `Assets/Editor/SequenceCheck.cs` と `Assets/SequenceTestAdapter.cs` に配置し、`-executeMethod SequenceCheck.Run` で実行。結果は `Release/sequence-report.txt`。
- 初回インポート直後は第三者ライブラリの拡張メソッド重複でコンパイルに失敗。検証用Unityを再起動して解消し、第三者コードの変更は行っていません。
- これはEditorでのロジック・接続検証です。今回の連続再生をVRChat内の実動画、複数クライアント、途中参加、所有者交代で確認する作業は未実施です。配布用UnityPackageの再エクスポートも未実施です。

実機確認手順：接続した共通UIから短い動画を選択 → 自然終了で同じタブの次の行が再生されることを確認 → 別タブ表示中も元の順序を保つことを確認 → 末尾停止・手動停止・読み込みエラーを確認。単曲リピートをオフにし、キュー有効／無効、2クライアントでの重複要求の有無、再生中の途中参加・所有者退出も確認してください。

## プレイリスト編集ウィンドウ（2026-10-02）

- VizVidとYamaPlayerの導入済みEditorコードを参考に、タブ／動画の2ペイン、並べ替え、保存操作を持つ専用ウィンドウを追加。
- 分離したUnity 2022.3.22f1プロジェクトで15項目成功：ウィンドウのLayout/Repaint、同一アセットのウィンドウ再利用、保存前のデータ分離、タブ・動画の追加初期値、保存、共有UIへの反映（非アクティブを含む）、無関係なUIの保持、シーンdirty、アセットのディスク保存、未保存変更の破棄、空データ保存、データ／UIをまとめたUndo、参照不備時の保存前チェック。
- Coreと4種のExtensionを含む5構成のC#参照限定コンパイル成功。今回の変更はEditor側のみ。
- 検証ソース：`Assets/CommonVideoPlaylistDevelopment/PlaylistEditorCheck.cs.txt`。分離プロジェクトのEditorフォルダーへコピーし、`-executeMethod PlaylistEditorCheck.Run` で実行。
- 記録：`Release/playlist-editor-report.txt`。Layout/Repaintはバッチ実行によるエラー検証で、実画面の目視や手動ドラッグ操作の検証ではありません。
- 保存でプレイリストのアセットをディスクへ保存し、ロード済みの対象UIを更新。シーン自体の保存は通常のUnity操作に委ね、他の未保存作業を一緒に保存しません。閉じたシーンは次回のPlay/Build時に反映します。

## YouTubeチャンネル追加・動的タイトル更新（2026-10-02）

- Coreと各Extensionの5構成でC#コンパイル成功。
- `.Validation/NamespacedCore` のUnity 2022.3.22f1 / SDK 3.10.5でUdonSharpコンパイル成功。StringLoaderのreceiver変換に必要な `VRC.Udon.Serialization.OdinSerializer` をCore.Runtimeの参照に追加。
- 21項目の検証成功：ID/URL入力、無効入力、目次URL・インデックス・Text参照のベイク、Udonプログラム参照、JSONの日本語・引用符・カンマ、並べ替えた動画行、固定行の保持、別タブ応答の逆順到着、不正JSON/型/通信失敗時の保持、短いリスト・空リストで古いタイトルを初期値へ戻す処理。
- 上記コールバック検証はエディタのC#実行。VRChat内のUdonVMによるStringLoader通信や実機再生の成功を示すものではありません。
- 提示されたチャンネルIDで実際にEditorの取得処理を実行し、動的URL20件と固定URL21件の作成を確認。固定URLの件数はサーバー応答に依存します。
- 検証ソース：`Assets/CommonVideoPlaylistDevelopment/ChannelCheck.cs.txt`。分離プロジェクトの `Assets/Editor/ChannelCheck.cs` にコピーし、`-executeMethod ChannelCheck.Run`（コンパイル・ロジック）または `ChannelCheck.RunImport`（実API）で実行。
- 記録：`Release/channel-check-report.txt`、`Release/channel-import-report.txt`。今回のコード変更を含むUnityPackageの再エクスポートは未実施。

実機確認手順：PlaylistDataに動的チャンネルを追加 → Play/Buildでベイク → VRChatでAllow Untrusted URLsを有効化 → タブを選択して番号がタイトルへ変わることを確認 → 取得中に別タブへ移動して誤更新がないことを確認 → 動画を選択して接続先の再生を確認。通信失敗時は表示を維持し、5秒以上空けたタブ再選択で再試行します。

実施日: 2026-10-02。Unity操作はUnityMCPを使用。コードと検証補助ファイルはファイル操作で作成しました。

## 実施済み

配置・名前空間の変更後にUnity/Udonコンパイル、エディタ63項目、参照を限定したC#の5構成を再確認しました。ClientSimとカメラ画像の記録は変更前のものです。エディタ検証の破棄処理後にUdonSharpの遅延コールバックからMissingReferenceExceptionが出るため、これは検証補助処理の既知の制限として記録しています。

- Unity 2022.3.22f1 / VRChat Worlds SDK 3.10.5同梱UdonSharpでUnity C#およびUdonSharpコンパイル成功。全導入環境で108本のUdonスクリプトをコンパイルし、本実装の6つの具象プログラムを生成しました。
- エディタ検証63項目成功: 候補0／1／複数件、4プレイヤーの接続アダプター生成、手動変更、再検出で手動参照保持、種類不一致、参照切れ、空タブ、nullデータ、不正インデックス、空URL、1つのデータを2つのUIへ反映、Prefabアセットでのシーン接続禁止、16個のボタンの永続Udonイベント参照。
- ClientSimのUdonVMで共通UIを検証: 実際のSendCustomEventからタブ切り替え、空タブ表示、ScrollRectの末端移動、空URL拒否、未接続表示を確認。
- 下表の各個別シーンで、ボタン操作から接続先の実行中Udon変数へ選択URLが届くことを確認。単にアダプターの戻り値だけを判定していません。
- Coreのみ、Core＋各Extensionの5構成で、無関係なプレイヤーのDLL参照を除外したC#コンパイル成功。KineLは製品アセンブリのautoReferenced=falseに対応した明示参照を使用。
- 別の新規Unityプロジェクト（動画プレイヤー0件、SDK 3.10.5のみ）にCore UnityPackageを実際にインポート。Unity C#とUdonSharpコンパイル、Prefab、Udonプログラム、ボタン参照の保持を確認。
- UnityPackage内のアセットを列挙し、第三者プレイヤー本体が入らないこと、GUID参照がCore／Extension／SDK等の依存関係で解決することを確認。
- World Space CanvasのUnityカメラ画像を確認。左右一覧、スクロールバー、選択色、状態表示に目立つ重なりがないことを確認。

| プレイヤー | バージョン | 全導入環境でUdonコンパイル | ClientSimのURL受け渡し |
|---|---|---|---|
| iwaSync3 | 3.8.0 | 成功 | VideoCoreの_urlで確認 |
| KineL | 2.5.5 | 成功 | KinelVideoPlayerの_syncedUrlで確認 |
| VizVid | 1.8.2 | 成功 | CoreのlocalUrlで確認 |
| YamaPlayer | 2.0.0-beta.7 | 成功 | Controllerの_trackのURLで確認 |

## 未確認・制限

- VRChat実機での映像／音声の再生成功、URLごとの対応サービス、ライブ配信、Android／Questは未確認です。ClientSimのURL受け渡し成功は映像再生成功の保証ではありません。
- 複数クライアント間の同期、同時操作の競合、途中参加、所有者退出、Master／非MasterやYamaPlayer権限拡張の全組み合わせは実機未確認です。実装は各製品の権限確認と正式再生経路に従っています。
- VRヘッドセットでのレーザー操作、スクロールバーの掴みやすさ、アバタースケールごとの可読性は実機未確認です。
- 各プレイヤーだけを導入した別Unityプロジェクト4種でのUdonSharp再コンパイルとExtensionインポートは未実施です。参照を限定したC#検証、全導入環境のUdonコンパイル、Coreだけの実インポートは区別しています。
- Undo登録とPrefab override保存は実装していますが、複雑なPrefab Variantや複数段階Undo/Redoの網羅的な操作検証は未実施です。
- 対応確認は上表の導入済みバージョンに限定します。YamaPlayerはbeta版であり、以後のAPI変更時には再検証が必要です。

## 再検証の手順

1. Coreと利用するExtensionを導入し、プレイヤーを0台→1台→2台と配置してInspectorの候補と選択を確認します。
2. 手動で接続先を切り替え、再検出しても維持されることを確認します。プレイヤーを削除し、参照切れを確認します。Undo/Redoも確認します。
3. サンプルのFeatured、Music、Empty tabを切り替え、縦スクロール、空URL表示を確認します。
4. 有効URLを選択し、実際のプレイヤーで再生を確認します。キュー有効時は標準のキュー挙動も確認します。
5. VRChatで2クライアントを使い、ロック／権限拒否、操作権移動、途中参加、所有者退出、同時選択を確認します。
6. PC VRと必要なAndroid環境でUIサイズ、文字、ドラッグ操作を確認します。

## 開発環境に残した証跡

`Release/editor-checks.txt`、`Release/live-ui-*.txt`、`Release/compile-matrix/results.txt`、`Release/core-import-check.txt`、`Release/package-audit.json`に結果を保存しています。開発専用メニューと各プレイヤー入り検証シーンは`Assets/CommonVideoPlaylistDevelopment`にあり、UnityPackageには含めていません。

元シーンのファイルを上書きせず、作業時のシーン状態は`Assets/CommonVideoPlaylistDevelopment/Snapshots/OriginalWorkingCopy.unity`にも退避しました。検証中、既存のVPM取得スレッドを伴うドメイン再読み込みでUnityMCPが切断したため、保存・退避後にOSからUnityを再起動し復旧しました。その後はUnityMCPで検証を継続しました。VPMの外部取得／Thread abortedメッセージは本実装のコンパイルエラーとは区別しています。

## キュー・Samples整理・公開レビュー（2026-10-04）

- SamplesをPlaylists / Themes / Scenesに分割。GUIDを保持して移動。
- 通常Inspectorから複製・コンパクト初期化ボタンを削除。接続先表示は種類名だけに変更。
- 4プレイヤー共通の最大32件FIFOキュー、6件ページ、追加／削除／全消去／再生を追加。
- C#とUdonSharpコンパイル、Prefabとロード済み4インスタンスの参照・イベント配線を確認。
- 変更前の既存Transform値を照合し、位置・回転・サイズの変化なし。開いているシーンは未保存のまま保持。
- Play/VRChat試験・別プロジェクトへのインポート・配布パッケージ生成は未実施。

手動確認: 短い動画2本を追加 → 順番と自然終了 → 7件以上でページ移動・削除 → 停止と再開 → 権限がない利用者の拒否 → 2クライアントの同期・途中参加・所有者退出 → iwaSync3/VizVid本体キューとの併用で順番・重複要求を確認。単曲リピートは無効にします。

公開前の残件と帰属はルートOPEN_SOURCE_REVIEW.ja.mdとCore/THIRD_PARTY_NOTICES.mdを参照。独自部分のLICENSEは採用決定後に追加します。

## キュータブ・URL入力・デモジャンプ（2026-10-04）

- キューを左側タブの先頭へ移動。右側上部の「動画を追加」からVRCUrlInputFieldでURLを登録できる構成へ変更。
- データの再ベイク時もキュータブを保持し、起動時はキューを選択。直接入力URLとプレイリスト由来の項目を同じFIFOで同期。
- デモVRCWorldにDemoJumpSettingsを追加し、ローカルプレイヤーのJumpImpulseだけを3に設定。歩行速度・重力は変更しない。
- C#／UdonSharpコンパイル: 新規エラー0。4インスタンスのキュータブ・URL入力・クリックイベント参照を確認。
- 編集時の描画をキュー一覧・URL入力フォームで確認。既存配置・接続・色に無関係な変更なし。
- シーンはインメモリで変更し、未保存のまま保持。Playモード・VRChat試験は未実施。

手動確認: キューと通常タブの切り替え → URL入力から追加 → 既存動画の＋から追加 → 混在キューの自然終了・削除・同期・途中参加 → デモでジャンプを確認。
