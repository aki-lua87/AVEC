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
