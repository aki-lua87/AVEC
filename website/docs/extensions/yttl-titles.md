---
title: 保存リストのタイトル取得
description: VizVid同梱YTTLを使う任意拡張の導入と接続方法。
---

`AVEC-Extension-YttlTitles`は、保存リストへURLを追加した時、**動画を再生せずにタイトルを取得する**任意拡張です。

基本のURL追加・保存・選択再生・削除には不要です。

## 必要なもの

VizVid付属の **YTTL Manager** が必要です。YTTL本体は同梱しません。

この拡張はVizVid同梱版のYTTLに対応しています。

再生するプレイヤーはVizVidに限定しません。この拡張のためにAVEC-Connector-VizVidを入れる必要もありません。

## 接続する

1. AVEC-Coreと、再生に使うプレイヤー用Connectorを導入します。
2. VizVid同梱YTTLが使える環境に`AVEC-Extension-YttlTitles`を追加します。
3. シーンへVizVid付属の **YTTL Manager.prefab** を配置します。
4. プレイリスト設定の **YTTLタイトル取得（拡張）** でManagerを指定します。シーン内に1個だけなら **シーン内のYTTLを接続** も使えます。
5. 既存設定の確認・更新には **YTTL接続を適用** を使います。UI全体の再生成は不要です。

VizVidのプレイヤーを配置しただけでは、編集時にManagerが実体化していない場合があります。

## 取得できない場合

未接続・未対応URL・通信失敗時は、URLを表示してそのまま利用できます。取得したタイトルは最大256文字で保存し、次回訪問時にも表示します。

対応サイトとAllow Untrusted URLsなどの条件は、YTTLとVRChatの設定に従います。DynamicPlaylistのサーバーからの取得には、この拡張は不要です。

## タイトル取得をやめる

タイトル取得をやめる場合は、各プレイリスト設定の **YTTL Manager** の指定を解除します。URLの追加・保存・再生・削除は引き続き使えます。
