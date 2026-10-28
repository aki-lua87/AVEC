---
title: YouTube取り込みと動的更新
description: DynamicPlaylist拡張による動画一覧の取り込みと外部サービスへの依存。
---

`AVEC-Extension-DynamicPlaylist`は、YouTubeの一覧を取り込み、動的なタイトル更新を行う任意拡張です。

## 導入する

AVEC-Coreと必要なConnectorを導入した環境へ、`AVEC-Extension-DynamicPlaylist`を追加します。

## 一覧を取り込む

1. PlaylistSetupから **AVEC-PlaylistGenerator を開く** を選びます。
2. 対象のIDまたはURLを入力します。
3. プレビューでタブ名と動画一覧を確認します。
4. 内容を確認して保存します。

チャンネルの指定には次の形式を使用できます。

- `UC…`形式のチャンネルID
- `@ハンドル`
- `/channel/ID`または`/@handle`形式のYouTube URL

動的・固定のどちらも登録APIでチャンネルを解決します。有効なチャンネルIDが返らない場合は取り込みを中止します。既存タブのURLや手動設定は自動で書き換えません。

## タブ名

YouTubeプレイリストのタブ名は、サーバー応答の`playlist_title`を初期値にします。名前を取得できない場合はIDを表示し、プレビューで変更できます。既存タブ名は取り込み処理で上書きしません。

## 外部サービスと制限

取り込みと動的タイトル更新は、外部サービス **`https://vrc.akakitune87.net`** へアクセスします。バックエンドはこのリポジトリに含まれていません。

:::info 動的なリストを使うときは
自動更新URLは外部サービスの稼働と **Allow Untrusted URLs** に依存します。動的リストは最大50枠です。公開ワールドでは対象URLと利用者側の設定を確認してください。
:::

保存リストへのURL追加時のタイトル取得は別機能です。[YTTLタイトル取得](yttl-titles.md)を参照してください。
