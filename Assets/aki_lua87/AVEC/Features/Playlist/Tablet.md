# 手持ちタブレット版

シーン内の壁設置版のPlaylistSetupで「現在のUIから手持ちタブレット版を作成」を押すと、現在の手動調整・動画データ・接続先を保ったコピーを追加します。Tools > AVEC > Playlist > Create Handheld Copy of Selected Playlistからも作成できます。元のUIの再生成・テーマ適用は行いません。

配布用はPrefabs/CommonVideoPlaylistTablet.prefabです。配置後、子のPlaylist ScreenにあるPlaylistSetupで接続先を設定してください。保存済みPrefabから新規に作成する場合はTools > AVEC > Playlist > Create Handheld Prefabを使用します。既存Prefabは上書きしません。

- 画面幅32cm、高さは元UIの縦横比を維持します。標準UIでは約20cmです。外枠を含む幅は34.2cm、厚さは約6mmです。
- 枠や背面をつかんで持ち運べます。Auto Holdは無効で、VRでは握りを離すと手から放し、その位置に留まります。位置は各ユーザーのローカル扱いで、ObjectSyncは使用しません。
- 再生・キュー・履歴は既存の接続アダプターを共有します。元の壁設置版も引き続き操作できます。
- 通常のレイ操作に加え、VRでは持っていない側の人差し指を画面に押し込み、手前へ離すとボタンが反応します。押したまま上下に動かすとリストをスクロールし、音量スライダーとスクロールバーも指で動かせます。
- 指ボーンのないアバターとDesktopではレイ操作を使用します。URL入力欄の選択・文字入力も標準のレイ操作を使用してください。
- 指先は人差し指の中節・末節ボーンから推定します。必要に応じてルートのPlaylistTabletTouchでfingertipExtension、releaseDistance、maximumPenetration、dragDistanceを調整してください。距離設定の単位はメートルです。
- 指タップはButtonの永続SendCustomEvent(string)を使用します。プレイリスト編集による行更新時に参照も更新されます。OnClickを手動変更した場合はPlaylistTabletTouchのコンポーネントメニューからRefresh Touch Bindingsを実行してください。独自のUnityEventを追加する場合は標準レイ操作を使用するか、SendCustomEventへ接続してください。

## 手動確認

VRChatで左右それぞれの手で持ち、レイ操作、反対側の人差し指でのタップ、スワイプ、音量操作を確認してください。持ち替え、指の押しっぱなしでの連打防止、非表示の行やスクロール外の行への誤操作、壁UIとのキュー共有、Desktopでの持ち運び・レイ操作も確認してください。

実装時の確認範囲はC# / UdonSharpコンパイルと編集時の構成確認までです。実機の指位置やPickup時の操作感は手動確認が必要です。
