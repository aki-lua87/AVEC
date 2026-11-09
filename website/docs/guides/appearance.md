---
title: 見た目の設定
description: テーマ、半透明表示、固定背景画像の設定方法。
---

import ThemeGallery from '@site/src/components/ThemeGallery';

プレイリスト設定の **Theme Asset** にテーマを指定し、プレイリストの配色と透明度を切り替えます。

## テーマを選ぶ

テーマは`Assets/aki_lua87/AVEC/Features/Playlist/Samples/Themes/`にあります。

**DefaultTheme・Mint・Amber・Orchid・Periwinkle・RedBlack・WhiteBlue・Monochrome・DefaultThemeGlass** の9種類から選べます。ファイル名は、それぞれの名前に`.asset`を付けたものです。

<ThemeGallery />

*実際のテーマデータから作成した配色見本です。Unity画面のスクリーンショットではありません。Glass版の格子模様は透明度を比較するための背景で、テーマに格子模様が含まれるわけではありません。見本を押すと拡大できます。*

テーマを選ぶと、Unity上のUIへ反映されます。

## 透明度とボタンの反応

色のアルファ（A）が不透明度です。Glass版の初期値は背景45%、面55%、ボタン80%、選択90%で、文字は不透明です。背景と一覧が重なる部分ではアルファが合成されます。

**Button Highlight Tint** と **Button Pressed Tint** は、ホバー・押下時にボタン色へ乗算する色です。

## 固定の背景画像を設定する

1. 背景画像をUnityへ入れ、Texture Typeを **Sprite (2D and UI)** にします。
2. プレイリストの既存の **Background** にあるImageへ、画像を **Source Image** として指定します。
3. **Image Type** を **Simple** にします。
4. 元画像の色で表示したい場合はColorを白にし、Aで透明度を調整します。
5. Prefabまたはシーンを保存します。

:::note テーマを先に設定してください
テーマの再適用では背景色も設定されます。背景画像と色の手動設定は、テーマ適用後に行ってください。
:::

テーマの変更と、Transform・フォント・動画データ・接続先の変更は別の作業です。既に調整したUIは、必要な項目だけを変更してください。
