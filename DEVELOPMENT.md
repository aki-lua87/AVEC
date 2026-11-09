# 開発・配布

利用方法は[公開ドキュメント](https://aki-lua87.github.io/AVEC/)を参照してください。この文書はソースを編集・配布する方向けです。

## ソースの構成

- `Assets/aki_lua87/AVEC/`：Core、各機能、Connector、Extension、配布Prefab
- `Assets/aki_lua87/Editor/`：Release Builder
- `website/`：公開ドキュメントサイト

SDK・外部プレイヤー・開発用デモは配布ソースに含みません。必要な依存を導入したWorldプロジェクトで、使用する機能とConnectorを`.meta`ごと同じパスへ配置します。UPMのGit URLから直接導入するパッケージではありません。

YTTLの内部構成・旧ファイルの整理手順は[拡張のREADME](Assets/aki_lua87/AVEC/Extensions/YttlTitles/README.md)を参照してください。公開サイトでは、配布パッケージを使った導入・操作を案内します。

## 配布パッケージを作る

Unityの **Tools > AVEC > Release Builder** を使います。

1. **配布準備**：必要なコンパイルと配布対象の保存を行います。
2. **配布検証**：依存関係・Prefab・Program配置などを確認します。
3. **UnityPackage と ZIP を作成**：対象Prefabにバージョン表示を埋め込み、パッケージを出力します。

開いている開発用シーンのバージョン表示が更新された場合は、シーンも保存してください。通常の編集ごとにパッケージを作る必要はありません。外部プレイヤー本体や動画データは配布対象に含めません。

## ドキュメントサイト

ローカル確認・ビルド・公開手順は[website/README.md](website/README.md)にまとめています。`master`へのpushでGitHub Pagesへ自動デプロイします。
