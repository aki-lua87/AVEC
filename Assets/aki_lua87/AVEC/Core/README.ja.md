# AVEC — Aki's Video Enhancement Components

既存のVRChat動画プレイヤーへ、プレイリストやジェスチャーリモコンを追加するコンポーネント群です。プレイヤー本体・VRChat SDK・UdonSharpは別途導入します。

## 導入

Unity 2022.3.22f1 / VRChat Worlds SDK 3.10.5を対象としています。

1. `AVEC-Core`を導入します。
2. 必要な機能の`AVEC-Playlist`または`AVEC-Remote`を導入します。
3. 接続先プレイヤー本体と、対応する`AVEC-Connector-プレイヤー名-機能名`を導入します。
4. 必要なExtensionを追加します。動的プレイリストには`AVEC-Extension-DynamicPlaylist`、プレイリストをリモコンから操作するには`AVEC-Extension-PlaylistRemote`を使います。

未導入のプレイヤー・機能用Connectorは導入しないでください。各パッケージの依存先はRelease Builderと出力manifestに表示します。Coreや機能からExtensionを自動ダウンロードすることはありません。

PlaylistのPrefabは`Features/Playlist/Prefabs/CommonVideoPlaylist.prefab`、Remoteは`Features/Remote/Prefabs/CommonGestureRemote.prefab`です。空のDefaultPlaylistは`Features/Playlist/Samples/Playlists/DefaultPlaylist.asset`です。

動的プレイリストのExtensionを導入すると、本体の編集画面とInspectorに「動的プレイリストを開く」が表示されます。専用画面で挿入先PlaylistDataを選択し、チャンネル／YouTubeプレイリストを取得してプレビューした後、タブを挿入して保存します。DefaultPlaylistへ挿入する場合は参照中のすべてのUIが共有するため、通常は専用のデータを作成して指定します。

配布物は必要な時だけ`Tools > Common Assets > Release Builder`から生成します。通常の編集時にパッケージは生成しません。

- [アーキテクチャ](ARCHITECTURE.ja.md)
- [旧版からの移行](MIGRATION.ja.md)
- [Playlist操作](../Features/Playlist/README.ja.md)
- [Remote操作](../Features/Remote/README.ja.md)
- [素材の帰属](THIRD_PARTY_NOTICES.md)

独自部分の正式なオープンソースライセンスはまだ採用前です。同梱フォントはOFL 1.1です。
