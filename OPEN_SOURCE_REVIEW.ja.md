# AVEC 公開前レビュー

確認日: 2026-10-04。対象は現在の `Assets/aki_lua87/AVEC`（Core・Playlist・Remote・Connector・Extension）と
`Assets/aki_lua87/Editor/CommonAssetsReleaseBuilder.cs`。
既存Git履歴はありません。外部プレイヤーを再配布する権利の確認や、
別プロジェクトへのインポート、Play/VRChat実機試験は今回の確認範囲外です。

## 結論

独自コード・Prefab・テーマ・幾何学アイコンを公開する資産リポジトリとして
整理できます。開発用Unityプロジェクト全体をそのまま公開する構成ではありません。
独自部分のライセンスは **MITを推奨**します。まだ採用を確定しておらず、
独自部分のLICENSEは未追加です。公開前に著作権者名・年を決めて正式なLICENSEを
追加してください。フォントには別途OFL 1.1が継続して適用されます。

MITは改変・再配布・商用利用を認め、著作権表示と許諾文の保持を求めるため、
VRChatワールドに組み込みやすい条件です。明示的な特許許諾を重視するなら
Apache-2.0も候補ですが、本アセットではMITが簡潔です。

## 素材

|対象|確認結果|公開時の扱い|
|---|---|---|
|ZenMaruGothic-Regular.ttf|公式TTFとSHA-256一致。加工・再命名なし|OFL.txtと著作権表示を同梱。フォント単体の販売は不可|
|play / stop / ordered / shuffle / speaker PNG|PlaylistIcons.csの幾何学描画。外部アイコン集を参照しない|独自部分のライセンスへ含める|
|Prefab / Theme / C# / Authoring data|製品フォルダー内の独自実装。外部プレイヤー本体は含まない|独自部分のライセンスを明記|
|SDK・UdonSharp・各VideoPlayer・AudioLink|開発環境にインストール済み／別途導入する依存物|このリポジトリから配布しない|

詳細の帰属表示は製品Coreの `THIRD_PARTY_NOTICES.md` と `Fonts/OFL.txt` にあります。

## ソースと構成の確認

- Coreと機能は任意プレイヤーのコンパイル参照を持たず、Connectorが対応APIへ接続します。
  ネイティブのソース・Prefabは変更していません。
- Release Builderは指定した製品フォルダーを `ExportPackageOptions.Default` で出力し、
  外部依存をまとめて取り込む `IncludeDependencies` は使用しません。シーンは出力対象外です。
  OFL.txtとTHIRD_PARTY_NOTICES.mdはCoreフォルダー内なので出力対象になります。
- 対象のC#・JSON・Markdown・Prefab・Assetに対する秘密鍵・トークン・認証情報・
  絶対マシンパスの文字列検索では、公開対象に該当する埋め込みは見つかりませんでした。
  この検索は機械的な検出であり、全ての由来・個人情報を保証するものではありません。
- 配布用DefaultPlaylistは空です。製品Prefabのベイク済み動画URLも空です。
- フォントは静的TTF、アイコンは小さな透過PNGです。Unityの `.meta` を一緒に保持すれば
  参照GUIDを維持できます。これらの形式自体に公開を妨げる事情は確認できませんでした。
- 任意導入のDynamicPlaylist ExtensionのYouTube取り込みは外部のホスト済みサービスに依存します。
  ユーザーが取り込みボタンを押すとIDを送信し、チャンネル登録のPOSTや目次取得を行います。
  動的タイトルは設定済み目次URLへ実行中にアクセスします。バックエンドはこの配布物に
  含まれず、サービス停止時にも手入力の固定URL・タイトルは使えます。
- 各Connectorは対応プレイヤーの型を直接参照します。未導入のプレイヤー用Connectorを
  Unityへまとめて入れるとコンパイルエラーになります。Coreと必要な機能・Connector・Extensionだけを導入してください。
- Assets配下には製品外の `test.cs`、個別iwaSyncエディタ、デモシーン、外部アセットもあります。
  これらは今回の製品公開対象に含めません。

## Gitで追跡する範囲

ルート `.gitignore` を製品用に整理しました。AVECとRelease Builder以外の
Assets、Packages、ProjectSettings、検証用プロジェクト、ローカル設定、ログ、生成成果物を
除外します。Libraryなど既存のUnityキャッシュ除外も維持しています。
この設定は未追跡ファイルの追加を抑えるもので、既にコミット済みの内容は除去しません。
現在この作業ディレクトリーはGitリポジトリではありません。

これはアセットのソース配布構成です。利用者はVRChat Worlds SDK入りのUnityプロジェクトを
作り、Coreと必要な機能・Connector・Extensionを導入します。GitHubからCloneしたフォルダーをそのまま
開発用Unityプロジェクトとして開く構成や、Package ManagerのGit URLで導入するUPMパッケージ
にはしていません。

## 公開前に残る項目

1. MITなど独自部分のライセンスを採用し、著作権者名と年を記載したLICENSEを追加する。
   UnityPackageにも独自LICENSEを同梱する。
2. 自分に権利がある範囲であることを確認する。今後の外部コード・画像の取り込みには
   帰属表示とそのライセンスを追加する。
3. ユーザーがVRChatでキュー・権限・複数人同期・途中参加・ネイティブキューとの併用を確認する。
   AudioLink本体導入後の接続状況表示も手動確認する。
4. 公開する内容とGitの追加対象を確認する。公開・Push・配布パッケージ生成は今回実施していない。

## 根拠

- [Zen Maru Gothic / OFL](https://github.com/googlefonts/zen-marugothic/blob/main/OFL.txt)
- [SIL OFL公式本文](https://openfontlicense.org/open-font-license-official-text/)
- [MIT本文 / OSI](https://opensource.org/license/mit)
- [GitHub: repository licensing](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)
