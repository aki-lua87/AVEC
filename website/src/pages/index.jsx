import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import playlistOverview from '../../docs/assets/playlist-overview.png';

export default function Home() {
  return (
    <Layout title="VRChatの動画プレイヤーに、使いやすい操作を" description="AVECはVRChat向けの共通プレイリストUIとジェスチャーリモコンです。導入手順、使い方、設定と拡張機能を紹介します。">
      <main>
        <section className="avec-hero">
          <div className="container avec-hero-inner">
            <div>
              <p className="avec-eyebrow">Aki's Video Enhancement Components</p>
              <h1><span className="avec-heading-line">いつもの動画プレイヤーに、</span><span className="avec-heading-line">使いやすい操作を。</span></h1>
              <p className="avec-lead">プレイリストも、再生キューも、手元のリモコンも。<br />AVECはVRChatの動画視聴を支える共通UIです。</p>
              <div className="avec-actions">
                <Link className="button button--primary button--lg" to="/docs/getting-started/installation">導入する →</Link>
                <Link className="button button--secondary button--lg" to="/docs/intro">AVECについて</Link>
              </div>
              <p className="avec-support">iwaSync3 / KineL V2・V3 / VizVid / YamaPlayer / ProTV 3<br />Core同梱のAVEC-SimpleScreenにも対応</p>
            </div>
            <figure className="avec-product-shot">
              <Link to="/docs/guides/playlists" aria-label="プレイリストの実画面と使い方を見る">
                <img src={playlistOverview} alt="AVECの実画面。撮影用のサンプル動画一覧と再生・音量操作を表示" width="892" height="556" />
              </Link>
              <figcaption>Unity編集時の表示例 · 名前・URLは撮影用のダミー</figcaption>
            </figure>
          </div>
        </section>
        <section className="container avec-start">
          <p className="avec-eyebrow">DOCUMENTATION</p>
          <h2>やりたいことから、はじめる。</h2>
          <div className="avec-guide-grid">
            <Link to="/docs/getting-started/first-playlist"><span>01 / SETUP</span><h3>最初のプレイリストを作る</h3><p>Prefabを配置してプレイヤーへ接続し、動画を登録するまで。</p><b>セットアップを読む →</b></Link>
            <Link to="/docs/guides/remote"><span>02 / CONTROLS</span><h3>手元から操作する</h3><p>ジェスチャーリモコン、操作ガイド、呼び出し範囲の設定。</p><b>リモコンの使い方 →</b></Link>
            <Link to="/docs/extensions/dynamic-playlist"><span>03 / EXTENSIONS</span><h3>YouTubeから取り込む</h3><p>動画一覧の取り込みと、外部サービスを使う動的なリスト。</p><b>拡張機能を読む →</b></Link>
          </div>
        </section>
        <section className="container avec-note"><h2>設定を詳しく知りたいときは</h2><p><Link to="/docs/reference/components">Inspectorの設定項目</Link>、<Link to="/docs/reference/compatibility">対応環境と更新</Link>、<Link to="/docs/troubleshooting">困ったときは</Link>を参照してください。</p></section>
      </main>
    </Layout>
  );
}
