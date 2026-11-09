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
const themes = ['DefaultTheme', 'Mint', 'Amber', 'Orchid', 'Periwinkle', 'RedBlack', 'WhiteBlue', 'Monochrome', 'DefaultThemeGlass'];
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
  body += panel(18, 16, 28, 4, 'accent') + label(18, 43, 'AVEC-PLAYLIST', 18) + label(18, 66, '接続先: AVEC-SimpleScreen', 12, true);
  body += panel(18, 82, 145, 176, 'surface') + panel(175, 82, 347, 176, 'surface');
  ['再生キュー', '再生履歴', '保存リスト', 'サンプル動画'].forEach((t, i) => { body += panel(24, 88 + i * 40, 133, 34, i === 3 ? 'selected' : 'button') + label(32, 110 + i * 40, t); });
  ['サンプル動画 01', 'サンプル動画 02', 'サンプル動画 03'].forEach((t, i) => { body += panel(182, 90 + i * 49, 326, 41, 'button') + label(194, 116 + i * 49, t); });
  body += panel(512, 88, 5, 164, 'accent') + panel(18, 276, 38, 32, 'button') + label(31, 299, '▶', 16);
  body += label(77, 298, '音量', 13) + panel(119, 289, 286, 7, 'surface') + panel(119, 289, 162, 7, 'accent') + label(425, 298, '50%', 14, true);
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
  box(52, 205, 856, 70, '#24515b', 8) + box(70, 224, 317, 9, '#80d8ca', 4) + text(70, 257, '01:23 / 04:56', 14, '#fff') +
  text(412, 240, '♪', 23, '#fff') + box(453, 224, 100, 9, '#80d8ca', 4) + text(572, 237, '50%', 15, '#fff') +
  box(627, 219, 184, 34, '#dbe8eb', 5) + text(641, 242, '動画URL', 16) + box(824, 219, 64, 34, '#80d8ca', 5) + text(833, 242, 'LOAD', 16) +
  text(68, 333, '① シーク：再生位置を変える', 19) + text(402, 333, '② 自分の音量', 19) + text(627, 333, '③ URLを入力して再生', 19));
console.log(`Generated ${themes.length} theme previews and 4 instructional figures.`);
