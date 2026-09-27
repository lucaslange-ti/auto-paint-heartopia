<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import { copyText } from '../lib/clipboard';
  import { LINKS } from '../lib/links';
  import { BRAND_ICONS } from '../lib/brandIcons';
  import { SUPPORTERS } from '../lib/supporters';
  import qrPix from '../assets/qr-code-pix.png';

  let copied = $state(false);

  async function copyPix() {
    const ok = await copyText(t('about.pixKey'));
    app.flash(t(ok ? 'about.pixCopied' : 'about.pixCopyFail'));
    if (ok) {
      copied = true;
      setTimeout(() => (copied = false), 2000);
    }
  }

  async function copyNick() {
    const ok = await copyText(LINKS.discordNick);
    app.flash(t(ok ? 'help.nickCopied' : 'about.pixCopyFail'));
  }

  const perks = [
    { icon: 'discord', key: 'role' },
    { icon: 'star', key: 'credits' },
    { icon: 'rocket', key: 'updates' }
  ] as const;
</script>

{#snippet glyph(name: string)}
  {#if name === 'discord'}
    <svg viewBox="0 0 24 24" class="fill" aria-hidden="true"><path d={BRAND_ICONS.discord} /></svg>
  {:else}
    <svg viewBox="0 0 24 24" aria-hidden="true">
      {#if name === 'heart'}
        <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z" />
      {:else if name === 'star'}
        <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01z" />
      {:else if name === 'rocket'}
        <path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z" />
        <path d="m12 15-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z" />
        <path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5" />
      {:else if name === 'copy'}
        <rect x="9" y="9" width="13" height="13" rx="2" /><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
      {:else if name === 'check'}
        <path d="M20 6 9 17l-5-5" />
      {/if}
    </svg>
  {/if}
{/snippet}

<div class="stack page">
  <!-- Chamada -->
  <section class="hero">
    <span class="hero-icon">{@render glyph('heart')}</span>
    <div>
      <h1>{t('sup.title')}</h1>
      <p>{t('sup.lead')}</p>
    </div>
  </section>

  <!-- O que o apoiador ganha -->
  <section class="section">
    <h2>{t('sup.perksTitle')}</h2>
    <div class="perks">
      {#each perks as p}
        <div class="perk {p.icon}">
          <span class="perk-icon">{@render glyph(p.icon)}</span>
          <strong>{t(`sup.perk.${p.key}`)}</strong>
          <span class="muted">{t(`sup.perk.${p.key}.text`)}</span>
        </div>
      {/each}
    </div>
  </section>

  <!-- Pix -->
  <section class="section">
    <h2>{t('sup.howTitle')}</h2>
    <div class="pix">
      <div class="qr-box">
        <img class="qr" src={qrPix} alt="QR Code Pix" width="170" height="170" />
        <span class="muted small">{t('about.qrHint')}</span>
      </div>

      <div class="pix-side">
        <span class="step"><b>1</b>{t('sup.step1')}</span>
        <button type="button" class="pixcode" title={t('about.copy')} onclick={copyPix}>
          <code>{t('about.pixKey')}</code>
        </button>
        <button type="button" class="primary copy" onclick={copyPix}>
          {@render glyph(copied ? 'check' : 'copy')}
          {copied ? t('sup.copied') : t('sup.copyPix')}
        </button>

        <span class="step"><b>2</b>{t('sup.step2')}</span>
        <div class="claim">
          <button type="button" class="discord-btn" onclick={copyNick} title={t('help.copyNick')}>
            {@render glyph('discord')}
            <span>{LINKS.discordNick}</span>
          </button>
          <a class="discord-btn ghosty" href={LINKS.discordServer} target="_blank" rel="noreferrer">
            {t('help.server')}
          </a>
        </div>
      </div>
    </div>
  </section>

  <!-- Mural -->
  <section class="section">
    <h2>{t('sup.wallTitle')}</h2>
    {#if SUPPORTERS.length}
      <div class="wall">
        {#each SUPPORTERS as name}
          <span class="name">{@render glyph('star')}{name}</span>
        {/each}
      </div>
    {:else}
      <div class="empty">{t('sup.wallEmpty')}</div>
    {/if}
  </section>
</div>

<style>
  .page { gap: 1.2rem; }
  .small { font-size: .78rem; }
  svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; flex-shrink: 0; }
  svg.fill { fill: currentColor; stroke: none; }

  .section { display: flex; flex-direction: column; gap: .6rem; }
  .section h2 { margin: 0; font-size: 1.02rem; }

  /* ---------- chamada ---------- */
  .hero {
    display: flex; gap: 1rem; align-items: center; padding: 1.4rem; border-radius: 18px; border: 1px solid var(--border);
    background:
      radial-gradient(90% 120% at 0% 0%, color-mix(in srgb, #e0245e 20%, transparent), transparent 60%),
      radial-gradient(70% 90% at 100% 100%, color-mix(in srgb, var(--accent) 18%, transparent), transparent 60%),
      var(--surface);
  }
  .hero h1 { margin: 0 0 .3rem; font-size: 1.5rem; letter-spacing: -.02em; }
  .hero p { margin: 0; color: var(--muted); line-height: 1.5; max-width: 60ch; }
  .hero-icon {
    width: 56px; height: 56px; border-radius: 16px; display: grid; place-items: center; flex-shrink: 0;
    background: #e0245e; color: #fff; box-shadow: 0 8px 20px color-mix(in srgb, #e0245e 40%, transparent);
    animation: beat 1.8s ease-in-out infinite;
  }
  .hero-icon svg { width: 28px; height: 28px; fill: currentColor; stroke: none; }
  @keyframes beat { 0%, 100% { transform: scale(1); } 12% { transform: scale(1.1); } 24% { transform: scale(1); } 36% { transform: scale(1.06); } }
  @media (prefers-reduced-motion: reduce) { .hero-icon { animation: none; } }

  /* ---------- vantagens ---------- */
  .perks { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: .6rem; }
  .perk {
    --c: var(--accent);
    display: flex; flex-direction: column; gap: .3rem; padding: 1rem; border-radius: 14px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .perk.discord { --c: #5865f2; }
  .perk.star { --c: #f5a524; }
  .perk.rocket { --c: #e0245e; }
  .perk .muted { font-size: .82rem; line-height: 1.4; }
  .perk-icon {
    width: 40px; height: 40px; border-radius: 11px; display: grid; place-items: center; margin-bottom: .3rem;
    color: var(--c); background: color-mix(in srgb, var(--c) 15%, transparent);
  }
  .perk.star .perk-icon svg { fill: currentColor; }

  /* ---------- pix ---------- */
  .pix {
    display: grid; grid-template-columns: auto 1fr; gap: 1.2rem; padding: 1.1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .qr-box { display: flex; flex-direction: column; align-items: center; gap: .4rem; text-align: center; max-width: 190px; }
  .qr { background: #fff; padding: 8px; border-radius: 12px; box-shadow: 0 4px 14px rgba(0, 0, 0, .12); }
  .pix-side { display: flex; flex-direction: column; gap: .55rem; min-width: 0; }
  .step { display: flex; align-items: center; gap: .5rem; font-weight: 600; font-size: .9rem; }
  .step:not(:first-child) { margin-top: .5rem; }
  .step b {
    width: 22px; height: 22px; border-radius: 7px; display: grid; place-items: center; font-size: .75rem;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  .pixcode {
    text-align: left; padding: .55rem .7rem; background: var(--surface-2); border: 1px dashed var(--border);
    border-radius: 10px; cursor: pointer;
  }
  .pixcode:hover:not(:disabled) { border-color: var(--accent); background: var(--surface-2); }
  .pixcode code { font-size: .7rem; line-height: 1.45; word-break: break-all; white-space: normal; user-select: all; color: var(--muted); }
  .copy { display: inline-flex; align-items: center; justify-content: center; gap: .45rem; font-weight: 600; padding: .6rem 1rem; }
  .copy svg { width: 18px; height: 18px; }

  .claim { display: flex; flex-wrap: wrap; gap: .5rem; }
  .discord-btn {
    display: inline-flex; align-items: center; gap: .45rem; padding: .5rem .9rem; border-radius: 10px; font: inherit; font-weight: 600;
    border: 1px solid #5865f2; background: #5865f2; color: #fff; text-decoration: none; cursor: pointer;
  }
  .discord-btn:hover:not(:disabled) { background: #4752c4; }
  .discord-btn.ghosty { background: transparent; color: #5865f2; }
  .discord-btn.ghosty:hover { background: color-mix(in srgb, #5865f2 12%, transparent); }

  /* ---------- mural ---------- */
  .wall { display: flex; flex-wrap: wrap; gap: .45rem; }
  .name {
    display: inline-flex; align-items: center; gap: .35rem; padding: .35rem .75rem; border-radius: 999px; font-weight: 600; font-size: .86rem;
    background: color-mix(in srgb, #f5a524 14%, var(--surface)); border: 1px solid color-mix(in srgb, #f5a524 40%, transparent);
  }
  .name svg { width: 14px; height: 14px; color: #f5a524; fill: currentColor; }
  .empty {
    padding: 1.2rem; border-radius: 14px; text-align: center; color: var(--muted);
    border: 2px dashed var(--border);
  }

  @media (max-width: 640px) { .pix { grid-template-columns: 1fr; } .qr-box { max-width: none; } }
</style>
