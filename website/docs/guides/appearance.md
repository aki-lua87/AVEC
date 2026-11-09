---
title: 見た目の設定
description: テーマ、半透明表示、枠線表示、固定背景画像の設定方法。
---

PlaylistSetupの **Theme Asset** にテーマを指定し、プレイリストの配色と表示方式を切り替えます。

## テーマを選ぶ

テーマは`Assets/aki_lua87/AVEC/Features/Playlist/Samples/Themes/`にあります。

| アセット | 見た目 |
| --- | --- |
| `RedBlack.asset` | 黒い背景、赤いアクセント、白い文字 |
| `WhiteBlue.asset` | 白い背景、青いアクセント、濃紺の文字 |
| `DefaultThemeGlass.asset` | DefaultThemeの配色を保持した半透明表示 |
| `Monochrome.asset` | 白をベースに黒い文字。選択・ホバー・押下も無彩色 |
| `Wireframe.asset` | 背景を透過し、枠線を表示 |

テーマを選ぶと、Unity上のUIへ反映されます。

## 透明度とボタンの反応

色のアルファ（A）が不透明度です。Glass版の初期値は背景45%、面55%、ボタン80%、選択90%で、文字は不透明です。背景と一覧が重なる部分ではアルファが合成されます。

**Button Highlight Tint** と **Button Pressed Tint** は、ホバー・押下時にボタン色へ乗算する色です。

## 枠線だけを表示する

`Wireframe.asset`を指定します。枠線の太さと色はテーマで調整できます。通常のテーマへ切り替えると元のスプライトと余白に戻ります。

## 固定の背景画像を設定する

1. 背景画像をUnityへ入れ、Texture Typeを **Sprite (2D and UI)** にします。
2. プレイリストの既存の **Background** にあるImageへ、画像を **Source Image** として指定します。
3. **Image Type** を **Simple** にします。
4. 元画像の色で表示したい場合はColorを白にし、Aで透明度を調整します。
5. Prefabまたはシーンを保存します。

:::note テーマを先に設定してください
テーマの再適用では背景色も設定されます。背景画像と色の手動設定は、テーマ適用後に行ってください。Wireframeは枠線用スプライトを使うため、画像背景には通常またはGlassテーマを使用します。
:::

テーマの変更と、Transform・フォント・動画データ・接続先の変更は別の作業です。既に調整したUIは、必要な項目だけを変更してください。
