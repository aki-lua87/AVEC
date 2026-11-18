// Code-drawn documentation figures. These are illustrations, not runtime screenshots.
const fs = require('fs');
const path = require('path');
const site = path.resolve(__dirname, '..');
const root = path.dirname(site);
const imageDir = path.join(site, 'static/img/guides');
const themeDir = path.join(site, 'static/img/themes');
fs.mkdirSync(imageDir, {recursive: true});
fs.mkdirSync(themeDir, {recursive: true});
const esc = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const text = (x, y, label, size = 22, color = '#183a43', extra = '') => `<text x="${x}" y="${y}" font-size="${size}" fill="${color}" ${extra}>${esc(label)}</text>`;
const box = (x, y, w, h, fill, radius = 12, extra = '') => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${radius}" fill="${fill}" ${extra}/>`;
const svg = (w, h, title, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" role="img" aria-label="${esc(title)}"><title>${esc(title)}</title><g font-family="'Yu Gothic','Meiryo',sans-serif">${body}</g></svg>\n`;
const write = (name, title, body, height = 380) => fs.writeFileSync(path.join(imageDir, name), svg(960, height, title, box(0, 0, 960, height, '#f1f7f7', 16) + body));
const themes = ['DarkMono', 'DefaultTheme', 'Mint', 'Amber', 'Orchid', 'Periwinkle', 'RedBlack', 'WhiteBlue', 'Monochrome', 'DefaultThemeGlass'];
for (const name of themes) {
  const source = fs.readFileSync(path.join(root, `Assets/aki_lua87/AVEC/Features/Playlist/Samples/Themes/${name}.asset`), 'utf8');
  const palette = {};
  for (const key of ['background', 'surface', 'button', 'selected', 'accent', 'text', 'mutedText']) {
    const match = source.match(new RegExp(`^  ${key}: \\{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\\}`, 'm'));
    if (!match) throw new Error(`Missing theme color: ${name}.${key}`);
    const [r, g, b, a] = match.slice(1).map(Number);
    palette[key] = {color: `rgb(${[r, g, b].map(c => Math.round(c * 255)).join(',')})`, alpha: a};
  }
  const panel = (x, y, w, h, key) => box(x, y, w, h, palette[key].color, 5, `fill-opacity="${palette[key].alpha}"`);
  const label = (x, y, value, size = 14, muted = false) => text(x, y, value, size, palette[muted ? 'mutedText' : 'text'].color);
  let body = `<defs><pattern id="checks" width="24" height="24" patternUnits="userSpaceOnUse"><rect width="24" height="24" fill="#b9c6cb"/><path d="M0 0h12v12H0zM12 12h12v12H12z" fill="#d5dfe2"/></pattern></defs>`;
  body += box(0, 0, 540, 326, 'url(#checks)', 12) + panel(0, 0, 540, 326, 'background');
  body += panel(18, 16, 20, 3, 'accent') + label(18, 43, 'AVEC-PLAYLIST', 18) + label(481, 20, 'v0.0.3', 8, true);
  body += `<path d="M0 58h540" stroke="${palette.mutedText.color}" stroke-opacity=".15"/>`;
  body += panel(175, 78, 347, 180, 'surface');
  ['再生キュー', '再生履歴', '保存リスト'].forEach((t, i) => { body += label(38, 91 + i * 31, t, 12, true); });
  body += `<g fill="none" stroke="${palette.mutedText.color}" stroke-width="1.3"><rect x="19" y="80" width="10" height="8" rx="1"/><path d="m22 82 3 2-3 1zM32 82v8H22m13-6v9H25"/><circle cx="25" cy="117" r="6"/><path d="M25 113v4l3 2M21 143h8v13l-4-3-4 3z"/></g>`;
  body += `<path d="M18 176h140" stroke="${palette.mutedText.color}" stroke-opacity=".2"/>` + label(18, 195, 'PLAYLISTS', 8, true);
  body += panel(15, 204, 140, 29, 'selected') + panel(15, 207, 2, 23, 'accent') + label(27, 223, 'サンプル動画', 12);
  body += label(27, 253, '環境映像', 12, true);
  ['サンプル動画 01', 'サンプル動画 02', 'サンプル動画 03'].forEach((t, i) => { body += label(190, 102 + i * 32, t, 13); });
  body += `<circle cx="34" cy="294" r="16" fill="${palette.text.color}" fill-opacity=".06"/>` + label(29, 299, '▶', 13, true);
  body += label(61, 298, '再生中の動画はありません', 10, true) + panel(292, 291, 162, 4, 'button') + panel(292, 291, 81, 4, 'accent') + label(465, 298, '50%', 10, true);
  body += `<g fill="none" stroke="${palette.mutedText.color}" stroke-width="1.2"><rect x="510" y="281" width="11" height="23" rx="2"/><path d="M513 286h5m-5 5h5m-5 5h3"/></g>`;
  fs.writeFileSync(path.join(themeDir, `${name}.svg`), svg(540, 326, `${name}の配色見本`, body));
}

write('remote-input.svg', 'リモコンのジェスチャー入力手順の説明図',
  text(32, 43, 'ジェスチャー入力：押す → 描く → 離す', 27) +
  [0, 1, 2].map((i) => box(28 + i * 308, 72, 288, 268, '#fff') + text(48 + i * 308, 111, `${i + 1}. ${['押したまま', '矢印の順に動かす', '操作名を確認して離す'][i]}`, 20)).join('') +
  box(124, 145, 75, 129, '#183a43', 28) + box(143, 166, 37, 18, '#69d4c6', 6) + text(65, 309, '使用トリガー／使用ボタン', 17) +
  text(365, 216, '→ ← →', 53, '#087f8c') + text(360, 282, '例：次の動画', 20) +
  box(668, 159, 230, 76, '#e4f5ef') + text(699, 207, '次の動画', 26, '#087f8c') + text(675, 283, '離した時に実行', 20));

write('remote-guide.svg', '手元ガイドと常設ガイドの違いの説明図',
  text(32, 43, '操作ガイドの見方', 27) + box(28, 70, 440, 268, '#fff') + box(492, 70, 440, 268, '#fff') +
  text(52, 110, '手元ガイド', 24) + text(516, 110, '常設ガイド', 24) +
  box(75, 137, 347, 104, '#15343c') + text(100, 173, '→ ← →   次の動画', 20, '#fff') + text(100, 212, '↑ ↓       一時停止／再開', 20, '#fff') +
  text(52, 281, '持っている時に開閉', 20) + text(52, 311, 'VR：スティック下 ／ Desktop：H', 17) +
  box(542, 137, 340, 104, '#15343c') + text(566, 173, '→ ↑       音量を上げる', 20, '#fff') + text(566, 212, '↓ ↑       ミュート／解除', 20, '#fff') +
  text(516, 281, 'ワールド内の決まった場所に設置', 20) + text(516, 311, 'そのワールドの割り当てを確認', 17));

const screen = fs.readFileSync(path.join(site, 'docs/assets/playlist-overview.png')).toString('base64');
write('tablet-touch.svg', 'タブレットの指操作の説明図。画面部分は実UI画像',
  text(32, 43, '手持ちタブレット：画面をタップ・スワイプ', 27) +
  box(40, 83, 550, 259, '#23363f', 22) + `<image x="57" y="98" width="516" height="225" preserveAspectRatio="xMidYMid meet" href="data:image/png;base64,${screen}"/>` +
  `<path d="M530 251l29-44q9-12 18-6l-16 36 17-5q19-1 26 17l16 49-42 22-36-31-22-10q-11-8-1-15l16 1" fill="#e9bb92" stroke="#895b43" stroke-width="3"/>` +
  text(641, 132, 'タップ', 23, '#087f8c') + text(641, 166, '押して、手前へ離す', 20) +
  text(641, 227, 'スクロール', 23, '#087f8c') + text(641, 261, '押したまま上下へ動かす', 20) + text(641, 316, '文字入力はレイ操作', 19));

write('simple-screen-controls.svg', 'SimpleScreenの操作パネルの説明図',
  text(32, 43, 'SimpleScreen：モニターにレイを当てると操作を表示', 27) +
  box(35, 72, 890, 222, '#16353e', 10) + text(391, 160, '動画の表示領域', 23, '#acc9ce') +
  box(52, 205, 856, 70, '#182124', 8) + box(70, 216, 815, 5, '#80d8ca', 4) +
  text(70, 257, '▶  ■', 22, '#fff') + box(163, 234, 410, 30, '#101416', 5) + text(176, 255, '動画URLを入力', 16, '#acc9ce') +
  text(600, 255, '♪', 22, '#fff') + box(640, 245, 100, 6, '#80d8ca', 4) + text(755, 255, '50%', 15, '#fff') +
  box(813, 234, 75, 30, '#203438', 5) + text(828, 255, 'Sync', 16, '#fff') +
  text(68, 333, '① シーク：再生位置を変える', 19) + text(402, 333, '② 自分の音量', 19) + text(627, 333, '③ URLを入力して再生', 19));
console.log(`Generated ${themes.length} theme previews and 4 instructional figures.`);
