module.exports = {
  docs: [
    'intro',
    { type: 'category', label: 'はじめに', collapsed: false, items: [
      'getting-started/installation', 'getting-started/first-playlist',
    ] },
    { type: 'category', label: '使い方', items: [
      'guides/playlists', 'guides/queue-history-saved', 'guides/tablet',
      'guides/remote', 'guides/appearance', 'guides/simple-screen',
    ] },
    { type: 'category', label: '拡張機能', items: [
      'extensions/youtube-import', 'extensions/dynamic-playlist', 'extensions/yttl-titles', 'extensions/rtab',
    ] },
    { type: 'category', label: '設定と仕様', items: [
      'reference/components', 'reference/compatibility', 'reference/licenses',
    ] },
    'troubleshooting',
  ],
};
