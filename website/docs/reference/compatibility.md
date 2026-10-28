---
title: 対応環境と更新
description: 確認したUnity・SDK・プレイヤーのバージョン、更新時の扱いと確認範囲。
---

## 確認環境

| 項目 | 記録された確認版 |
| --- | --- |
| Unity | 2022.3.22f1 |
| VRChat Worlds SDK | 3.10.5（同梱UdonSharp） |
| iwaSync3 | 3.8.0 |
| KineL V2 | 2.5.5としての確認記録あり |
| KineL V3 | `la.niri.videoplayer` 3.4.3 |
| VizVid | 1.8.2 |
| YamaPlayer | 2.0.0-beta.7 |
| ProTV 3 | 3.0.0-beta.31 |

これは最新推奨版や全バージョンでの動作保証を示す表ではありません。リポジトリ内に記録された**コンパイルと編集時接続の確認版**です。

KineLのパッケージ3.4.3には旧V2とV3の実装が含まれます。パッケージ番号だけで接続先APIを判断せず、V2／V3に対応したConnectorを選んでください。

ProTVは`Packages/dev.architech.protv`のTVManagerへ接続します。ProTV 2は対象外です。**Sync Volume ControlをOFF** にしてください。ONの場合、AVECからの音量変更を拒否します。

## 更新の前に

### Prefab名の統一

配布Prefab名を`AVEC-`プレフィックスへ統一しました。例: `CommonVideoPlaylist.prefab` → `AVEC-VideoPlaylist.prefab`、`CommonGestureRemote.prefab` → `AVEC-GestureRemote.prefab`。GUIDは維持しています。スクリプトの型名・名前空間は変更していません。

このリポジトリ内の既存シーンは、GUIDを使ったPrefab参照を維持します。既存シーンのオブジェクト名は、個別の名前を維持するため一括変更しません。

### 更新手順

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

アセットのGUIDは維持するため、更新時に既存アセットを一括削除しないでください。UnityPackageの上書きでは不要になった旧ファイルは自動削除されません。

YTTLの分離に関する移行は[YTTLタイトル取得](../extensions/yttl-titles.md)を参照してください。

## 確認範囲

記録されている確認は、C#／UdonSharpコンパイルと編集時の構成確認です。次は別途手動で確認してください。

- VRChat内での操作と再生
- 複数人同期、途中参加、所有者退出
- 保存リストの保存と再訪時の復元
- Quest／Androidでの再生・操作・URL互換性
- 新規プロジェクトへの導入

自動テスト済み・全端末対応済みとしては案内していません。
