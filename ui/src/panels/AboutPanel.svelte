<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t, i18n } from '../lib/i18n.svelte';
  import { LINKS } from '../lib/links';
  import { BRAND_ICONS } from '../lib/brandIcons';
  import logo from '../assets/logo.png';

  const facts = $derived([
    { label: t('about.version'), value: app.info?.version ? `v${app.info.version}` : '—' },
    { label: t('about.system'), value: app.info?.os ?? '—' },
    { label: t('about.language'), value: i18n.lang }
  ]);
</script>

<div class="stack page">
  <section class="hero">
    <img src={logo} alt="" />
    <div>
      <h1>{t('app.title')}</h1>
      <p>{t('about.tagline')}</p>
      {#if app.info?.version}<span class="badge">v{app.info.version}</span>{/if}
    </div>
  </section>

  <div class="facts">
    {#each facts as f}
      <div class="fact">
        <span class="muted">{f.label}</span>
        <strong>{f.value}</strong>
      </div>
    {/each}
  </div>

  <section class="card-x">
    <h2>{t('about.madeBy')}</h2>
    <div class="author">
      <div class="avatar">L</div>
      <div class="who">
        <strong>{LINKS.discordNick}</strong>
        <span class="muted">{t('about.authorRole')}</span>
      </div>
      <div class="links">
        <a class="icon-link discord" href={LINKS.discordServer} target="_blank" rel="noreferrer" title="Discord">
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d={BRAND_ICONS.discord} /></svg>
        </a>
        {#if LINKS.facebook}
          <a class="icon-link facebook" href={LINKS.facebook} target="_blank" rel="noreferrer" title="Facebook">
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d={BRAND_ICONS.facebook} /></svg>
          </a>
        {/if}
        {#if LINKS.youtube}
          <a class="icon-link youtube" href={LINKS.youtube} target="_blank" rel="noreferrer" title="YouTube">
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d={BRAND_ICONS.youtube} /></svg>
          </a>
        {/if}
      </div>
    </div>
  </section>

  <section class="card-x">
    <h2>{t('about.thanks')}</h2>
    <p class="muted">{t('about.credits')}</p>
    <button class="support" onclick={() => (app.route = 'supporters')}>
      <svg viewBox="0 0 24 24" aria-hidden="true" class="heart"><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z" /></svg>
      {t('home.support')}
    </button>
  </section>
</div>

<style>
  .page { gap: 1rem; }

  .hero {
    display: flex; gap: 1.2rem; align-items: center; padding: 1.5rem; border-radius: 18px; border: 1px solid var(--border);
    background:
      radial-gradient(90% 120% at 0% 0%, color-mix(in srgb, var(--accent) 22%, transparent), transparent 60%),
      radial-gradient(70% 90% at 100% 100%, color-mix(in srgb, #ee6f72 18%, transparent), transparent 60%),
      var(--surface);
  }
  .hero img { width: 88px; height: 88px; border-radius: 22px; box-shadow: 0 10px 26px rgba(0, 0, 0, .22); flex-shrink: 0; }
  .hero h1 { margin: 0 0 .25rem; font-size: 1.55rem; letter-spacing: -.02em; }
  .hero p { margin: 0 0 .6rem; color: var(--muted); line-height: 1.5; }
  .badge {
    display: inline-block; font-size: .72rem; font-weight: 700; padding: .2rem .6rem; border-radius: 999px;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }

  .facts { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: .6rem; }
  .fact {
    display: flex; flex-direction: column; gap: .2rem; padding: .8rem 1rem; border-radius: 14px;
    border: 1px solid var(--border); background: var(--surface); min-width: 0;
  }
  .fact .muted { font-size: .72rem; text-transform: uppercase; letter-spacing: .06em; }
  .fact strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

  .card-x {
    display: flex; flex-direction: column; gap: .5rem; padding: 1.1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .card-x h2 { margin: 0; font-size: 1rem; }
  .card-x p { margin: 0; line-height: 1.5; }

  .author { display: flex; align-items: center; gap: .8rem; flex-wrap: wrap; }
  .avatar {
    width: 44px; height: 44px; border-radius: 999px; display: grid; place-items: center; font-weight: 800; font-size: 1.1rem;
    color: #fff; background: linear-gradient(135deg, var(--accent), #e0245e);
  }
  .who { display: flex; flex-direction: column; flex: 1; min-width: 0; }
  .who .muted { font-size: .82rem; }
  .links { display: flex; gap: .4rem; }
  .icon-link {
    --c: var(--accent);
    width: 38px; height: 38px; border-radius: 11px; display: grid; place-items: center;
    color: var(--c); background: color-mix(in srgb, var(--c) 13%, transparent); transition: transform .15s ease, background .15s ease;
  }
  .icon-link:hover { transform: translateY(-2px); background: var(--c); color: #fff; }
  .icon-link svg { width: 19px; height: 19px; fill: currentColor; }
  .discord { --c: #5865f2; }
  .facebook { --c: #1877f2; }
  .youtube { --c: #ff0033; }

  .support {
    align-self: flex-start; display: inline-flex; align-items: center; gap: .45rem; margin-top: .3rem; font-weight: 600;
    color: #e0245e; border-color: color-mix(in srgb, #e0245e 45%, transparent); background: color-mix(in srgb, #e0245e 8%, transparent);
  }
  .support:hover:not(:disabled) { background: color-mix(in srgb, #e0245e 16%, transparent); }
  .heart { width: 17px; height: 17px; fill: currentColor; }
</style>
