# 旧構成からAVECへの移行

この開発プロジェクトではUnityのMoveAssetで移動済みです。GUID、Prefab内部のfileID、既存のクラス名・名前空間・機能Runtimeのアセンブリ名を保持しています。シーンやPrefabの作り直しは不要です。

既存の利用者には、AVECへ更新する前にUnityのProjectウィンドウで次のフォルダーを移動する手順を案内してください。同じGUIDのアセットを含むUnityPackageを先にインポートすると、旧配置へ更新されることがあります。旧フォルダーを削除して入れ直さないでください。

|旧配置|新配置|
|---|---|
|CommonVideoPlaylist/Core|AVEC/Features/Playlist|
|CommonGestureRemote/Core|AVEC/Features/Remote|
|CommonVideoPlaylist/Extensions/各プレイヤー|AVEC/Connectors/各プレイヤー/Playlist|
|CommonGestureRemote/Extensions/各プレイヤー|AVEC/Connectors/各プレイヤー/Remote|
|CommonGestureRemote/Extensions/CommonVideoPlaylist|AVEC/Extensions/PlaylistRemote|
|移動後のFeatures/Playlist/Fonts|AVEC/Core/Fonts|
|AVEC/Features/Playlist/Prefabs/CommonVideoPlaylist.prefab|AVEC/Prefabs/CommonVideoPlaylist.prefab|
|AVEC/Features/Remote/Prefabs/CommonGestureRemote.prefab|AVEC/Prefabs/CommonGestureRemote.prefab|
|AVEC/Features/Remote/Prefabs/CommonGestureGuide.prefab|AVEC/Prefabs/CommonGestureGuide.prefab|

表のすべてのパスは`Assets/aki_lua87/`からの相対パスです。移動してからCore・必要な機能・Connector・Extensionを更新します。元は本体に含まれていた動的取り込み・タイトル取得を使う場合はDynamicPlaylistのExtensionも導入してください。

PlaylistDataのGUIDと既存の動的設定は保持されます。新しいDefaultPlaylistの場所は`AVEC/Features/Playlist/Samples/Playlists/DefaultPlaylist.asset`です。自作データは移動不要です。

既存シーンの動的設定に必要なRuntimeフックは、データを保存・反映するとき、または既存のPlay／Build前データ反映で配線します。この変更確認のためにPlayや全体Bakeを自動実行しません。

シーンの手動編集・未保存変更は閉じたり読み直したりせず保持します。パッケージ更新後のシーン保存、VRChat内での再生・キュー・リモコン・途中参加確認は利用者が行います。
