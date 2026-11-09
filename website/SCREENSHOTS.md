# スクリーンショットの記録

公開用画像は、撮影専用シーン`Assets/aki_lua87/Documentation/DocumentationCapture.unity`と、専用データ`DocumentationPlaylist.asset`から撮影します。通常のデモや利用者のプレイリストからは撮影しません。

## 撮影用データ

- 6タブ・18件。名前は「サンプル動画」「サンプルチャンネルA」などの架空名。
- URLは`https://example.com/videos/sample-01.mp4`などの説明用アドレス。動画は再生できません。
- 動的タイトル取得元URLは空欄。実在する動画・チャンネル・外部APIへアクセスしません。
- 接続先はCore同梱のAVEC-SimpleScreen。外部プレイヤー本体は撮影シーンに含めません。

## 掲載画像

| 画像（`docs/assets/`） | 対象 | 掲載先 |
| --- | --- | --- |
| `playlist-overview.png` | 「サンプル動画」タブをSceneビューで正面から表示 | トップ、プレイリストの使い方 |
| `playlist-setup.png` | Documentation PlaylistのPlaylistSetup | 最初のプレイリスト、Inspectorの設定項目 |
| `playlist-editor.png` | DocumentationPlaylistを開いた動画編集ウィンドウ | 最初のプレイリスト |
| `playlist-special-lists.png` | Inspector画像から特殊リスト設定部分を切り出したもの | キュー・履歴・保存リスト |

上記4枚は実際のUnity編集画面を撮影・切り出した画像です。VRChat内の動作確認画像ではありません。

### Prefabの表示例

`AVEC-VideoPlaylistTablet.png`、`AVEC-GestureRemote.png`、`AVEC-GestureGuide.png`、`AVEC-SimpleScreen.png`は、UnityMCPから一時的なPreviewSceneにPrefabの複製を置き、Camera.Renderで取得した画像です。リモコンはModel部分を撮影しています。動画の再生や外部通信は行っていません。タブレットは接続先・動画の設定前、SimpleScreenは動画読み込み前、ガイドはスクロール一覧の先頭です。

既存シーン・Prefabには編集を加えず、撮影用のPreviewSceneは取得後に破棄しました。シーンの保存・Playモードでの動作確認は行っていません。

## 配色見本・操作説明図

- `static/img/themes/*.svg`：全9テーマの配色データから生成する見本。実UIのスクリーンショットではありません。`scripts/generate-doc-visuals.cjs`で再生成します。
- `static/img/guides/*.svg`：ジェスチャー入力、操作ガイド、タブレット、SimpleScreenの説明図。スクリーンショットと混同しないよう、各掲載箇所に説明図であることを明記します。
- タブレット説明図の画面部分のみ、上記の`playlist-overview.png`を使います。
- `static/img/social-card.png`：OGP用の1200×630画像。`scripts/render-social-card.ps1`で再生成します。

ワールド内操作の撮影は手動確認時に追加します。図やPrefabの表示例を動作確認済みの証拠として扱いません。

## 撮り直すとき

1. 現在の作業シーン・未保存状態を確認し、撮影シーンをAdditiveで開きます。作業中のシーンを置き換えないでください。
2. Documentation PlaylistとDocumentationPlaylistを使います。撮影シーンのSimpleScreen以外へ接続しないでください。
3. 画面内の名前・URLがダミーだけになっていることと、動画行の旧`Add to queue`ボタンが非表示であることを確認して撮影します。現行UIでは動画詳細からキューへ追加します。
4. `docs/assets/`の画像を差し替え、`mise run docs:build`で検証します。

撮影シーンのUIは既存UIの複製を基にし、データ更新は複製側だけに適用しています。UI全体のBake・テーマ再適用・Prefab再生成は行っていません。撮影シーンは編集用で、Build Settings・配布パッケージ・ワールドアップロードの対象には含めません。

既存デモには互換用の旧ボタンが残っています。`PlaylistBaker.ApplyData`で追加された行と`PlaylistSpecialLists.Apply`は旧ボタンを再表示する場合があるため、これらを撮影準備で使った後は表示状態を確認してください。撮影シーンの旧ボタンは非表示で保存しています。配布Prefabに旧ボタンはなく、ランタイムの`PlaylistView._ApplySpecialLists`でも旧ボタンを非表示にします。

Unity内の操作はUnityMCP、ウィンドウ画像の取得はcomputer-useを使用します。元の全ウィンドウ画像はローカルの`work/docs-screenshots/dummy/`にあり、公開対象には含めません。
