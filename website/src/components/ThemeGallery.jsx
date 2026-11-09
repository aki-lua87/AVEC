import React from 'react';
import useBaseUrl from '@docusaurus/useBaseUrl';

const themes = [
  ['DefaultTheme', '濃いグレーとシアンの標準配色'],
  ['Mint', '濃紺とミント色。設定画像で使用しているテーマ'],
  ['Amber', '濃紺とオレンジ'],
  ['Orchid', '濃紺とピンク'],
  ['Periwinkle', '濃紺と淡い青紫'],
  ['RedBlack', '黒と赤、白い文字'],
  ['WhiteBlue', '白と青、濃紺の文字'],
  ['Monochrome', '白と黒の無彩色'],
  ['DefaultThemeGlass', '標準配色の半透明版'],
];

export default function ThemeGallery() {
  const base = useBaseUrl('/img/themes/');
  return <div className="avec-theme-gallery">
    {themes.map(([name, description]) => <figure key={name}>
      <a href={`${base}${name}.svg`} aria-label={`${name}の配色見本を大きく表示`}>
        <img src={`${base}${name}.svg`} alt={`${name}：${description}`} width="540" height="326" loading="lazy" />
      </a>
      <figcaption><strong>{name}</strong><span>{description}</span></figcaption>
    </figure>)}
  </div>;
}
