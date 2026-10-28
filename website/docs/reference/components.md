---
title: Inspectorの設定項目
description: PlaylistSetup、RemoteSetup、保存用オブジェクトの役割と設定場所。
---

設定は、シーン上のPrefabにあるSetup Inspectorから行います。通常の接続変更や設定確認のために、UI全体やPrefabを作り直す必要はありません。

## PlaylistSetup

通常版・コンパクト版はルート、タブレット版・RTab版は子の **Playlist Screen** にあります。

![PlaylistSetupのカスタムInspectorの表示例](../assets/playlist-setup.png)

*撮影専用シーンでAVEC-SimpleScreenへ接続した表示例です。プレイリストは撮影用のダミーデータです。接続候補と拡張の項目は、導入したアセットによって変わります。*

| 項目・操作 | 役割 |
| --- | --- |
| プレイリストデータ（`data`） | タブと登録動画を保持するPlaylistData |
| Theme Asset（`theme`） | 編集時に反映するPlaylistTheme |
| プレイヤー種別・接続先 | 使用するConnectorとシーン上のプレイヤー |
| 新規プレイリストを作成 | 専用のPlaylistDataを作成 |
| プレイリストを編集する | タブ・動画の編集画面を開く |
| タブのアイコンを表示 | タブの種類別アイコンを切り替え |
| 特殊リスト | キュー・履歴・保存リストを個別に有効化 |
| YTTLタイトル取得（拡張） | 拡張導入時のみ表示するManager接続 |

`view`、テンプレート、一覧のコンテナなどは内部UIの参照です。通常の動画登録では変更しません。

## PlaylistData

ワールド作者が登録するタブと動画のデータです。複数UIで共有できます。[利用者の保存リスト](../guides/queue-history-saved.md)とは別です。

ユーザーデータは`Assets/AVECUserData/`へ保存できます。Release Builderはプレイリストデータを配布対象に含めません。

## RemoteSetupとGestureData

| 設定 | 役割 |
| --- | --- |
| 接続先プレイヤー（`player`） | リモコンが操作するプレイヤー |
| プレイリスト（`playlist`） | 同じ接続先のPlaylistViewとの連携 |
| GestureData（`data`） | 操作の割り当てとガイドで使う共通データ |
| モデル設定 | リモコンの見た目の選択 |
| 呼び出しの初期状態・範囲 | 手元への呼び出し設定 |

常設ガイドにはリモコンと同じGestureDataを指定します。呼び出し範囲は[リモコンと操作ガイド](../guides/remote.md)を参照してください。

## 保存用オブジェクト

`CommonVideoPlaylist User Data`には、VRCPlayerObject、VRCEnablePersistence、PlaylistUserDataが付いています。同じシーンのUIはこのオブジェクトを共有します。

公開後の削除・再作成・Network ID変更は、保存データとの対応を失う可能性があります。詳細は[保存リスト](../guides/queue-history-saved.md)を参照してください。

## SimpleScreenと指タッチ

プレイヤー固有の調整は[AVEC-SimpleScreen](../guides/simple-screen.md)、指先判定は[手持ちタブレット](../guides/tablet.md)にまとめています。
