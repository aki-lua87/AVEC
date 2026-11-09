const repository = 'AVEC';
const organization = 'aki-lua87';
const booth = 'https://aki-lua87.booth.pm/items/9029664';
const github = `https://github.com/${organization}/${repository}`;

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'AVEC',
  tagline: 'VRChatの動画プレイヤーに、使いやすい操作を。',
  favicon: 'img/avec.svg',
  url: process.env.SITE_URL || `https://${organization}.github.io`,
  baseUrl: process.env.SITE_BASE_URL || `/${repository}/`,
  organizationName: organization,
  projectName: repository,
  trailingSlash: true,
  onBrokenLinks: 'throw',
  markdown: { hooks: { onBrokenMarkdownLinks: 'throw' } },
  i18n: { defaultLocale: 'ja', locales: ['ja'] },
  presets: [['classic', {
    docs: {
      sidebarPath: './sidebars.js',
      showLastUpdateTime: false,
    },
    blog: false,
    theme: { customCss: './src/css/custom.css' },
  }]],
  themes: [['@easyops-cn/docusaurus-search-local', {
    hashed: true,
    language: ['en', 'ja'],
    indexBlog: false,
    indexPages: true,
    docsRouteBasePath: '/docs',
    highlightSearchTermsOnTargetPage: true,
    searchResultLimits: 8,
  }]],
  themeConfig: {
    image: 'img/social-card.png',
    colorMode: { defaultMode: 'light', respectPrefersColorScheme: true },
    navbar: {
      title: 'AVEC',
      logo: { alt: 'AVEC', src: 'img/avec.svg' },
      items: [
        { type: 'docSidebar', sidebarId: 'docs', label: 'ドキュメント', position: 'left' },
        { to: '/docs/getting-started/installation', label: '導入する', position: 'left' },
        { href: booth, label: 'BOOTHで入手', position: 'right' },
        { href: github, label: 'GitHub', position: 'right' },
      ],
    },
    docs: { sidebar: { hideable: true, autoCollapseCategories: false } },
    tableOfContents: { minHeadingLevel: 2, maxHeadingLevel: 3 },
    footer: {
      style: 'dark',
      links: [
        { title: 'はじめる', items: [
          { label: 'AVECについて', to: '/docs/intro' },
          { label: 'インストール', to: '/docs/getting-started/installation' },
          { label: '困ったときは', to: '/docs/troubleshooting' },
        ] },
        { title: '関連情報', items: [
          { label: 'BOOTH', href: booth },
          { label: '対応環境と更新', to: '/docs/reference/compatibility' },
          { label: 'ライセンス', to: '/docs/reference/licenses' },
        ] },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} aki_lua87. Built with Docusaurus.`,
    },
  },
};
module.exports = config;
