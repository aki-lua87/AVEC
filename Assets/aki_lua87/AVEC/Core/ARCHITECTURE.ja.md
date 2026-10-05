# AVECの構成

```text
Assets/aki_lua87/AVEC/
  Core/                         共通の登録・検出・実行時フック・フォント
  Features/
    Playlist/                   データ編集、UI、連続再生、共通キュー
    Remote/                     ジェスチャー入力と操作
  Connectors/
    Shared/                     配布済みConnectorの編集時配線・検出処理
    IwaSync/{Editor,Playlist,Remote}/
    KineL/{Editor,Playlist,Remote}/
    VizVid/{Editor,Playlist,Remote}/
    YamaPlayer/{Editor,Playlist,Remote}/
  Extensions/
    DynamicPlaylist/            YouTube取り込みと実行時タイトル取得
    PlaylistRemote/             PlaylistとRemoteの連携
```

Coreは特定プレイヤーや機能を参照しません。機能はCoreへ依存し、任意のConnector／Extensionの具象クラスを参照しません。ConnectorはCore・対応する機能・接続先プレイヤーへ依存します。ExtensionはCoreと対象機能へ依存します。PlaylistRemoteだけはPlaylistとRemoteの両方が必要です。

## Connectorの追加

Editorの`ConnectorRegistry.Register`へ`ConnectorDefinition`を登録します。プレイヤー・PlaylistAdapter・RemoteTargetの型名と、種類の表示名、必要なら権限オブジェクトの参照フィールドを指定します。複雑な権限オブジェクトの解決はResolveAuthorityで提供できます。既存の型名は互換のため維持しています。未導入の型は検出対象になりません。

プレイヤー固有の追加配線はConfigurePlaylist／ConfigureRemote、画面の初期検出はDiscoverRemoteOutputs、AudioLink表示用の音源検出はAudioSources、追加の検証はValidatePlaylistで提供します。新規プレイヤーの追加に、機能側の固定配列や分岐の変更は不要です。

各プレイヤーに対するRuntimeアダプターは従来のPlaylistAdapter／RemoteTarget契約を実装します。権限・再生・終了通知はネイティブプレイヤーのAPIに従います。共有カーソル・キューの所有者をネイティブ再生権限と同じGameObjectへ置く既存の設計は維持しています。

Connectorの配布はプレイヤー別・機能別です。両機能を使う場合はPlaylist用とRemote用を導入します。共通のEditor定義とShared配線は両パッケージへ同じGUIDで収録するため、二重のスクリプトにはなりません。

## Extensionの追加

Editorの`FeatureExtensionRegistry.Register`へID・対象機能・開く処理・データ反映時の処理を登録します。本体は導入済みExtensionのボタンだけを描画し、データ反映時に登録されたBakeを呼びます。Extensionが未導入なら取り込み画面も実行時通信も存在しません。

実行時の任意処理はCoreの`FeatureExtensionBehaviour`を実装します。PlaylistViewはタブ選択時に登録されたフックを呼びます。DynamicPlaylistTitlesだけがタイトルのHTTP取得とJSON処理を持ちます。通常のPlaylistViewにダウンローダーや外部サービスのURLはありません。

旧データのtitleSourceUrl／dynamicTitleIndexとViewのベイク済み配列は互換メタデータとして保持します。本体の通常編集画面には取得設定を表示しません。Extension未導入時もデータを消さず、固定表示名で再生できます。動的動画URL自体は外部サービスへの依存が残るため、独立して使う場合は固定URLで取り込みます。

動的取得結果は先にプレビューし、明示的な挿入操作で元のPlaylistDataを保存します。既存タブは保持して末尾へ追加し、挿入前に本体編集ウィンドウの未保存変更を確認します。保存と開いているUIへの反映は既存のUndo処理を使います。

配布定義はRelease BuilderのPackagesです。CollectPathsで出力範囲を確認でき、IncludeDependenciesを使わず第三者プレイヤー・シーンを除外します。manifestは各パッケージの依存先を記録します。
