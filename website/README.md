# AVEC 公開ドキュメント

Docusaurus製の日本語ドキュメントサイトです。ソースと公開設定をこのリポジトリで管理し、GitHub Pagesへ配信します。既存のルート`docs/`は開発メモのまま保持し、**公開対象は`website/docs/`だけ**に分けています。

## Windowsとmiseでローカル確認する

npm関連の操作はWindowsのPowerShell上で行います。Node.jsはリポジトリルートの`mise.toml`で固定し、CIでも同じバージョンを使います。

PowerShellでmiseが使えることを`mise --version`で確認してください。未導入の場合は[mise公式のWindows導入手順](https://mise.jdx.dev/installing-mise.html#windows)に従って導入します。

初回は**WindowsのPowerShell**で次を実行します。

```powershell
cd M:\Unity\CommonVideoPlaylistDev
mise trust
mise install
mise run docs:install
mise run docs:dev
```

`mise trust`は、このリポジトリのツール・タスク設定を確認して信頼する初回操作です。`mise run`がNode.jsのPATHを設定するので、シェルのactivate設定がなくてもタスクを実行できます。

ブラウザで`http://localhost:3000/AVEC/`を開きます。終了はPowerShell側で **Ctrl+C** です。次回からはルートで`mise run docs:dev`だけで起動できます。依存関係の更新時は`mise run docs:install`も実行します。

本番と同じ静的サイト・検索を確認する場合は、開発サーバーを停止してから次を実行します。

```powershell
mise run docs:build
mise run docs:serve
```

検索インデックスは本番ビルド時に作られます。本文・CSSを変更した後の検索確認には、もう一度buildを実行してください。

### ファイルと依存関係の扱い

ソースの編集・npmの実行ともに`M:\Unity\CommonVideoPlaylistDev`で行います。開発サーバーは通常のファイル監視を使用します。

`node_modules`はWindowsとWSLで使い回さず、Windowsの`docs:install`で管理します。WSLから切り替える際も、このタスクでWindows用に入れ直してください。

### タスク一覧

| コマンド | 内容 |
| --- | --- |
| `mise run docs:install` | lockfileに従って依存関係をインストール |
| `mise run docs:dev` | ライブ更新付きプレビュー |
| `mise run docs:build` | 本番ビルドと検索インデックス作成 |
| `mise run docs:serve` | 作成済みの本番ビルドを表示 |

追加のnpm操作もWindowsから行います。例: `cd website`の後に`mise exec -- npm outdated`。Node.jsを更新する場合は`mise.toml`の固定バージョンを変更し、`mise install`とビルド確認を行います。

## 編集する場所

| 場所 | 内容 |
| --- | --- |
| `docs/` | 公開本文（Markdown） |
| `sidebars.js` | 左サイドバーの順序と分類 |
| `src/pages/index.jsx` | トップページ |
| `src/css/custom.css` | 配色、文字組み、レスポンシブ表示 |
| `static/img/` | ロゴ、画像、スクリーンショット |
| `docusaurus.config.js` | サイト名、公開URL、検索、ナビゲーション |
| `../mise.toml` | Node.jsの固定バージョンとドキュメント用タスク |
| `../.github/workflows/docs.yml` | PRビルドとPages公開 |

公開サイトは、AVECをワールドへ導入・設定する人と、ワールド内で操作する人のための案内です。ページは目的・前提・手順・結果・注意点の順で書き、詳細なInspector設定は参照ページへ分けます。ソースからの導入、API仕様、内部構成、パッケージ生成、CI/CDは公開本文に混ぜず、開発者向けの`DEVELOPMENT.md`・このREADME・ルート`docs/`へ記載します。

Markdown内のページリンクは相対`.md`パスを使います。画像は同じフォルダの相対参照、またはDocusaurusのベースパス対応機能を使い、リポジトリ名のハードコードを避けます。内部リンク切れはビルドを失敗させます。

## GitHub Pagesへ公開する

現在のoriginとブランチに合わせて、`aki-lua87/AVEC`・`master`を設定しています。

1. 変更内容をレビューしてリポジトリへcommit／pushします。
2. GitHubの **Settings > Pages > Build and deployment > Source** を **GitHub Actions** にします。
3. `master`へpushすると、Actionsの **Documentation** がビルドと公開を自動実行します。再公開したい場合は **Run workflow** で`master`を選び、`deploy`をONにして実行することもできます。
4. `github-pages`環境に承認設定がある場合は、その設定に従ってデプロイします。

公開先は`https://aki-lua87.github.io/AVEC/`です（公開完了を示すURLではありません）。サイト本文はアセットの公開を前提として案内し、先行公開・配布準備中の表示は設けません。BOOTHの商品リンクも掲載した状態を維持します。

**`master`へのpushではビルド後に自動公開**します。Webサイト以外の変更でも実行します。PRはサイト・ツール設定・workflowに変更がある場合にビルドだけを行い、公開しません。手動実行では`master`かつ`deploy`がONの場合に公開し、OFFの場合はビルドだけを行います。

リポジトリ名や独自ドメインを変更した場合は、configの`url`・`baseUrl`を更新します。環境変数`SITE_URL`・`SITE_BASE_URL`でも上書きできます。ワークフローはGitHubのPagesメタデータを使用します。

## 公開前に残る編集

- [BOOTHの商品ページ](https://aki-lua87.booth.pm/items/9029664)とダウンロード導線を公開時に確認する。商品リンクは公開本文に掲載する。
- プレイリスト全体・接続Inspector・動画編集画面は、撮影専用シーンとダミーデータで撮影済み。[撮影記録](SCREENSHOTS.md)を参照。タブレット・リモコン・各拡張の説明画像は順次追加する。
- VRChatでの手動確認結果に応じて、対応環境・端末・同期の記述を更新する。
- 外部サービスと各配布先の案内を公開時点で確認する。

## 構成の参考

- [TexTransTool チュートリアル](https://ttt.rs64.net/docs/Tutorial)
- [Modular Avatar ドキュメント](https://modular-avatar.nadena.dev/ja/docs/experimental-features/portable-avatar-components)
- [Docusaurus: Deployment](https://docusaurus.io/docs/deployment)
- [GitHub Pages: Custom workflows](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)

導入と機能別詳細を分ける情報設計、サイドバー・ページ内目次・前後ページ導線を参考にしています。本文や画像は転載していません。
