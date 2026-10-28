# RTAB Playlist Extension

[RTab（らすたの雑貨屋）](https://rasta.booth.pm/items/7311462)でAVECプレイリストを使用する拡張です。RTab本体・YamaPlayer本体は含みません。

## 必要なアセット

- RTab / YamaPlayer
- AVEC-Core（Playlist・リモコンを含む）
- AVEC-Connector-YamaPlayer
- AVEC-Extension-DynamicPlaylist（付属Prefabの動的タイトル更新）

RTab依存のコードとPrefabは、このExtension内だけに置いています。通常のPlaylistにはRTabへの参照を追加しません。

## 配置と接続

シーンにRTabを配置し、`Prefabs/RTabPlaylist.prefab`をRTabのApps直下へ配置してください。子の`Playlist Screen`のPlaylistSetupでYamaPlayerのUIControllerを接続先に指定してください。保存リストを使用する場合は、同Inspectorの特殊リスト設定で永続化参照も更新してください。接続先を変更するときも、このInspectorを使用します。

配布Prefabにはシーン固有のプレイヤー参照を保存していません。既存アプリを二重に配置しないでください。
プレイリスト・キュー・履歴・保存リスト・再生操作・音量操作は既存のAVEC機能を使用します。保存リストはシーン内の既存PlayerObjectに参照を追加し、既存の保存オブジェクトやNetwork IDを作り直しません。URL追加時のタイトル自動取得には、必要に応じて別途YTTLの接続が必要です。

RTABのホームアイコン登録・開閉・タッチ・スクロールはRTABに任せます。独立したCanvas・Collider・Pickupは追加せず、RTABのCanvasを共有します。画面は2400×1800のアプリ領域全体に表示し、下部のホーム操作位置にはボタンを重ねません。ホーム用アイコンは白い角丸背景付きの専用画像を使用し、プレイリスト内のタブアイコンは変更しません。


## 配布

`Tools > AVEC > Release Builder`の`AVEC-Extension-RTabPlaylist`から必要な時に出力します。RTab・YamaPlayer本体、開発シーンは出力対象外です。配布説明には上記RTabのBOOTH URLを記載してください。

## 手動確認

C# / UdonSharpコンパイルと編集時の参照を確認します。Playモードは自動実行しません。VRChatでホームのPlaylistアイコン、アプリ切替、指タッチ／Desktop操作、スクロール、YamaPlayerの再生、キュー・連続再生、複数人同期・途中参加、保存リストの保存と再訪時の復元を確認してください。
