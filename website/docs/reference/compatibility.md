---
title: 対応環境と更新
description: 確認したUnity・SDK・プレイヤーのバージョン、更新時の扱いと確認範囲。
---

## 確認環境

| 項目 | 確認バージョン |
| --- | --- |
| Unity | 2022.3.22f1 |
| VRChat Worlds SDK | 3.10.5（同梱UdonSharp） |
| iwaSync3 | 3.8.0 |
| KineL V2 | 2.5.5 |
| KineL V3 | `la.niri.videoplayer` 3.4.3 |
| VizVid | 1.8.2 |
| YamaPlayer | 2.0.0-beta.7 |
| ProTV 3 | 3.0.0-beta.31 |

Unityで設定とコンパイルを確認したバージョンです。全バージョン・全端末での動作を保証するものではありません。使用するワールドでも動作を確認してください。

KineLは使用するプレイヤーがV2かV3かを確認し、それに対応したConnectorを選んでください。

ProTV 2は対象外です。ProTV 3では **Sync Volume ControlをOFF** にしてください。ONの場合、AVECから音量を変更できません。

## 更新の前に

1. シーン、Prefab overrides、プレイリストデータを含めてバックアップします。
2. 使用中の外部プレイヤーとConnectorの組み合わせを確認します。
3. 必要なCore・Connector・Extensionを更新します。
4. コンパイルと各Setupの参照を確認し、対象ワールドで手動確認します。

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

## 確認範囲

現在の確認範囲はUnityでのコンパイルと設定です。ワールド公開前に、次の動作を確認してください。

- VRChat内での操作と再生
- 複数人同期、途中参加、所有者退出
- 保存リストの保存と再訪時の復元
- Quest／Androidでの再生・操作・URL互換性
- 新規プロジェクトへの導入
