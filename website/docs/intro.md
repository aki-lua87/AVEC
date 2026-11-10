---
title: AVECについて
description: VRChatの動画プレイヤーに共通プレイリストUIとジェスチャーリモコンを追加するAVECの紹介。
---

AVEC（Aki's Video Enhancement Components）は、VRChatの動画プレイヤーに**共通プレイリストUIとジェスチャーリモコン**を追加する無料アセットです。

既存のプレイヤーを接続先として使いながら、動画を選ぶ・順番を決める・手元で操作するためのUIを追加できます。独自の動画プレイヤー **AVEC-SimpleScreen** もCoreに含まれています。

## できること

| できること | 機能・使い方 |
| --- | --- |
| 動画を分類して並べる | [タブ付きプレイリスト](guides/playlists.md)、順番・シャッフル・単曲リピート再生 |
| 次に見たい動画を追加する | [再生キュー](guides/queue-history-saved.md)（最大32件） |
| 前に見た動画を探す | [再生履歴](guides/queue-history-saved.md)（最大32件） |
| 利用者が自分だけのプレイリストを作る | [保存リスト](guides/queue-history-saved.md)（ユーザー・ワールドごとに最大32件） |
| 手元で操作する | [手持ちタブレット](guides/tablet.md)、[ジェスチャーリモコン・操作ガイド](guides/remote.md) |
| 動画一覧を取り込む | [YouTubeチャンネル／プレイリストの取り込み](extensions/youtube-import.md)（任意拡張） |
| ワールドをアップロードし直さずに動画一覧を更新する | [YouTubeの動画一覧の動的更新](extensions/dynamic-playlist.md)（任意拡張） |

音量とミュートは各ユーザーのローカル設定です。動画の再生操作には接続先プレイヤーの権限・ロックが適用されます。

## 対応するプレイヤー

iwaSync3、KineL V2・V3、VizVid、YamaPlayer、ProTV 3、およびAVEC-SimpleScreenに接続できます。外部プレイヤー本体は別途導入してください。

プレイヤーごとの注意点と更新手順は[対応環境と更新](reference/compatibility.md)にまとめています。

## はじめかた

1. [インストール](getting-started/installation.md)で、必要なパッケージを選びます。
2. [最初のプレイリスト](getting-started/first-playlist.md)で、配置・接続・動画登録を行います。
3. 必要に応じて[リモコン](guides/remote.md)や[YouTube取り込み](extensions/youtube-import.md)を追加します。

使い方が分からない場合や接続できない場合は、[困ったときは](troubleshooting.md)を参照してください。
