---
title: 対応環境と更新
description: 対応するプレイヤー、更新手順と導入後の確認事項。
---

## 対応するプレイヤー

iwaSync3、KineL V2・V3、VizVid、YamaPlayer、ProTV 3、およびCore同梱のAVEC-SimpleScreenに対応します。外部プレイヤーを使う場合は、本体と対応するConnectorを導入してください。

KineLは使用するプレイヤーがV2かV3かを確認し、それに対応したConnectorを選んでください。

ProTV 2は対象外です。ProTV 3では **Sync Volume ControlをOFF** にしてください。ONの場合、AVECから音量を変更できません。

## 更新の前に

1. シーン、Prefab overrides、プレイリストデータを含めてバックアップします。
2. 使用中の外部プレイヤーとConnectorの組み合わせを確認します。
3. 必要なCore・Connector・Extensionを更新します。
4. コンパイルとプレイリスト設定・リモコン設定の接続先を確認し、対象ワールドで動作を確認します。

更新のために既存UIや保存用オブジェクトを削除・再作成する必要はありません。パスと`.meta`を保持してください。

## 旧パッケージからの移行

| 旧パッケージ | 現在の構成 |
| --- | --- |
| `AVEC-Playlist` | `AVEC-Core` |
| `AVEC-Remote` | `AVEC-Core` |
| `AVEC-Extension-PlaylistRemote` | `AVEC-Core` |
| プレイリスト用／リモコン用の機能別Connector | 同じプレイヤーの統合Connector |

更新時に既存アセットを一括削除しないでください。UnityPackageの上書きでは不要になった旧ファイルは自動削除されません。

YTTLの分離に関する移行は[YTTLタイトル取得](../extensions/yttl-titles.md)を参照してください。

## 導入・更新後に確認すること

ワールドの設定や接続先プレイヤーによって挙動が変わるため、公開前に使用する機能を確認してください。

- VRChat内での操作と再生
- 複数人同期、途中参加、所有者退出
- 保存リストの保存と再訪時の復元
- Quest／Androidでの再生・操作・URL互換性
