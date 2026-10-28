---
title: 保存リストのタイトル取得
description: VizVid同梱YTTLを使う任意拡張の接続と更新方法。
---

`AVEC-Extension-YttlTitles`は、保存リストへURLを追加した時、**動画を再生せずにタイトルを取得する**任意拡張です。

基本のURL追加・保存・選択再生・削除には不要です。

## 必要なもの

VizVid同梱の **`VVMW.ThirdParties.Yttl.YttlManager`** が使えるプロジェクトを対象とします。YTTL本体は同梱しません。単体版YTTLやiwaSync3用YTTL Connectorを追加する必要はありません。

再生するプレイヤーはVizVidに限定しません。この拡張のためにAVEC-Connector-VizVidを入れる必要もありません。

## 接続する

1. AVEC-Coreと、再生に使うプレイヤー用Connectorを導入します。
2. VizVid同梱YTTLが使える環境に`AVEC-Extension-YttlTitles`を追加します。
3. シーンへVizVid付属の **YTTL Manager.prefab** を配置します。
4. PlaylistSetupの **YTTLタイトル取得（拡張）** でManagerを指定します。シーン内に1個だけなら **シーン内のYTTLを接続** も使えます。
5. 既存設定の確認・更新には **YTTL接続を適用** を使います。UI全体の再生成は不要です。

VizVidのプレイヤーを配置しただけでは、編集時にManagerが実体化していない場合があります。

## 取得できない場合

未接続・未対応URL・通信失敗時は、URLを表示してそのまま利用できます。取得したタイトルは最大256文字で保存し、次回訪問時にも表示します。

対応サイトとAllow Untrusted URLsなどの条件は、YTTLとVRChatの設定に従います。DynamicPlaylistのサーバーからの取得には、この拡張は不要です。

## 旧版から更新する

旧VizVid Connectorの`VizVidPlaylistTitles`は、この拡張へ移動しました。GUIDと型名を保持しているため、拡張も導入する更新では既存の接続を引き継ぎます。

旧版から更新し、YTTL連携を外す場合は、**旧版のうちに**各SetupのManagerを空にして接続を解除します。その後、シーン上の`VizVidPlaylistTitles`と対応するBacking UdonBehaviour、旧`Connectors/VizVid/Playlist/VizVidPlaylistTitles.cs`・`.asset`と専用コンパイル済みProgramを除去します。

UnityPackageの更新だけでは旧ファイルは削除されません。ネイティブプレイヤーやYTTL本体は削除対象に含めないでください。

詳しい移行記録は[ソース内のREADME](https://github.com/aki-lua87/VRC_MultiVideoPlayerPlaylist/blob/master/Assets/aki_lua87/AVEC/Extensions/YttlTitles/README.md)を参照してください。
