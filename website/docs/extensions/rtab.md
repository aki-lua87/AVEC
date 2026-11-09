---
title: RTabで使う
description: RTabのAppsにAVECプレイリストを追加する手順と依存アセット。
---

`AVEC-Extension-RTabPlaylist`は、[RTab（らすたの雑貨屋）](https://rasta.booth.pm/items/7311462)のアプリとしてAVECプレイリストを使う拡張です。

RTab本体・YamaPlayer本体は含まれていません。

## 必要なアセット

- RTab本体、YamaPlayer本体
- AVEC-Core
- AVEC-Connector-YamaPlayer
- AVEC-Extension-DynamicPlaylist（付属Prefabの動的タイトル更新用）

## 配置と接続

1. シーンにRTabを配置します。
2. `Assets/aki_lua87/AVEC/Extensions/RTabPlaylist/Prefabs/AVEC-RTabPlaylist.prefab`を、RTabの **Apps直下** へ配置します。
3. 子の **Playlist Screen** のプレイリスト設定で、YamaPlayerの **UIController** を接続先に指定します。
4. 保存リストを使う場合は、同じInspectorの特殊リスト設定で永続化参照も更新します。

既存アプリを二重に配置しないでください。配布Prefabにはシーン固有のプレイヤー参照を保存していません。接続先の変更もプレイリスト設定から行います。

## 操作と保存リスト

ホームへのアイコン登録・開閉・タッチ・スクロールはRTab側の仕組みを使います。独立したCanvas・Collider・Pickupを追加する必要はありません。

再生・キュー・履歴・保存リスト・音量はAVECの機能を使います。保存リストはシーン内の既存PlayerObjectを共有します。公開済みの保存オブジェクトやNetwork IDを作り直さないでください。

URL追加時のタイトル取得は[別途YTTL拡張](yttl-titles.md)が必要です。

## 公開前に確認すること

VRChatでホームアイコン、アプリ切り替え、VR／Desktop操作、スクロール、YamaPlayerの再生、複数人同期・途中参加、保存リストの復元を確認してください。

配布・販売説明には[RTabのBOOTHページ](https://rasta.booth.pm/items/7311462)を記載してください。
