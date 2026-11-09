---
title: インストール
description: AVEC-Core、プレイヤー別Connector、任意のExtensionの選び方と導入順。
---

AVECは、共通機能の **Core**、外部プレイヤーとの接続を行う **Connector**、任意機能の **Extension** に分かれています。

## 用意するもの

- VRChat Worlds SDKを導入したWorldプロジェクト
- AVEC-Core
- 外部プレイヤーを使う場合は、その本体と対応するConnector

確認環境はUnity **2022.3.22f1**、VRChat Worlds SDK **3.10.5**です。UdonSharpはSDK同梱版を使用します。SDK、外部プレイヤー、AudioLinkはAVECに同梱しません。

:::note 配布先について
AVECは[BOOTHの商品ページ](https://aki-lua87.booth.pm/items/9029664)から入手できます。このドキュメントではパッケージ名で案内します。

:::

## 1. プレイヤー本体を導入する

利用するプレイヤーを、その公式手順に従って導入します。AVEC-SimpleScreenを使う場合、この手順は不要です。

## 2. AVEC-Coreを導入する

Coreにはプレイリスト、リモコン、両者の連携、AVEC-SimpleScreenと配布用Prefabを含みます。

## 3. 対応するConnectorを導入する

| 接続先 | パッケージ |
| --- | --- |
| iwaSync3 | `AVEC-Connector-IwaSync` |
| KineL V2 | `AVEC-Connector-KineL` |
| KineL V3 | `AVEC-Connector-KineLV3` |
| VizVid | `AVEC-Connector-VizVid` |
| YamaPlayer | `AVEC-Connector-YamaPlayer` |
| ProTV 3 | `AVEC-Connector-ProTV` |
| AVEC-SimpleScreen | 追加不要（Coreに同梱） |

各Connectorには、プレイリストとリモコンの両方の接続が含まれます。**本体を導入していないプレイヤー用のConnectorは追加しないでください。**

KineL V2とV3は別のConnectorです。ProTV 2には対応していません。ProTV 3ではローカル音量操作のため、プレイヤー側の **Sync Volume ControlをOFF** にします。

## 4. Prefabを配置する

`Assets/aki_lua87/AVEC/Prefabs/`から使用するPrefabをシーンへ配置します。

| Prefab | 用途 |
| --- | --- |
| `AVEC-VideoPlaylist.prefab` | 通常の壁設置用プレイリスト |
| `AVEC-VideoPlaylistCompact.prefab` | 横幅を抑えたプレイリスト |
| `AVEC-VideoPlaylistTablet.prefab` | 手持ちタブレット |
| `AVEC-GestureRemote.prefab` | ジェスチャーリモコン |
| `AVEC-GestureGuide.prefab` | 常設の操作ガイド |
| `AVEC-SimpleScreen.prefab` | Core同梱の動画プレイヤー |

次は[最初のプレイリスト](first-playlist.md)で接続先と動画を設定します。

## 拡張機能を追加する場合

| パッケージ | 追加される機能 |
| --- | --- |
| `AVEC-Extension-DynamicPlaylist` | [YouTube取り込みと動的更新](../extensions/dynamic-playlist.md) |
| `AVEC-Extension-YttlTitles` | [保存リストへ追加したURLのタイトル取得](../extensions/yttl-titles.md) |
| `AVEC-Extension-RTabPlaylist` | [RTabのアプリとしてプレイリストを配置](../extensions/rtab.md) |

基本のプレイリストや保存リストを使うだけなら、Extensionは不要です。

更新時は、先に[対応環境と更新](../reference/compatibility.md)を確認してください。
