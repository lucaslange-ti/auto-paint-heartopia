<script lang="ts">
  import { onMount } from 'svelte';
  import { app, STEPS } from './lib/state.svelte';
  import type { Route } from './lib/state.svelte';
  import { i18n, t } from './lib/i18n.svelte';
  import Onboarding from './steps/Onboarding.svelte';
  import PaletteStep from './steps/PaletteStep.svelte';
  import ImageStep from './steps/ImageStep.svelte';
  import CanvasStep from './steps/CanvasStep.svelte';
  import PaintStep from './steps/PaintStep.svelte';
  import SummaryStep from './steps/SummaryStep.svelte';
  import SettingsPanel from './panels/SettingsPanel.svelte';
  import SupportersPanel from './panels/SupportersPanel.svelte';
  import AboutPanel from './panels/AboutPanel.svelte';
  import logo from './assets/logo.png';

  onMount(() => app.init());

  const steps = [Onboarding, PaletteStep, ImageStep, CanvasStep, PaintStep, SummaryStep];
  const Current = $derived(steps[app.step]);

  // Abas clicáveis: as etapas depois de "Cores e botões" exigem ela completa
  // (mesma regra do botão Próximo); o Resumo só existe depois de uma pintura.
  function canVisit(i: number) {
    if (i > STEPS.indexOf('palette') && !app.paletteReady) return false;
    if (STEPS[i] === 'summary' && !app.status) return false;
    return true;
  }

  const nextBlocked = $derived(STEPS[app.step] === 'palette' && !app.paletteReady);

  const nav: { route: Route; key: string; icon: string }[] = [
    { route: 'wizard', key: 'nav.home', icon: 'home' },
    { route: 'settings', key: 'nav.settings', icon: 'gear' },
    { route: 'supporters', key: 'nav.supporters', icon: 'heart' },
    { route: 'about', key: 'nav.about', icon: 'info' }
  ];

  function setLang(e: Event) {
    const lang = (e.target as HTMLSelectElement).value;
    i18n.set(lang);
    if (app.config) {
      app.config.language = lang;
      app.saveConfig();
    }
  }
</script>

{#snippet glyph(name: string)}
  <svg viewBox="0 0 24 24" aria-hidden="true">
    {#if name === 'home'}
      <path d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z" />
    {:else if name === 'gear'}
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
    {:else if name === 'heart'}
      <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z" />
    {:else if name === 'info'}
      <circle cx="12" cy="12" r="10" /><path d="M12 16v-4M12 8h.01" />
    {:else if name === 'globe'}
      <circle cx="12" cy="12" r="10" /><path d="M2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
    {:else if name === 'check'}
      <path d="M20 6 9 17l-5-5" />
    {:else if name === 'left'}
      <path d="M19 12H5M12 19l-7-7 7-7" />
    {:else if name === 'right'}
      <path d="M5 12h14M12 5l7 7-7 7" />
    {/if}
  </svg>
{/snippet}

<div class="shell">
  <header class="topbar">
    <button class="brand" onclick={() => (app.route = 'wizard')} title={t('nav.home')}>
      <img src={logo} alt="" />
      <span class="brand-text">
        <strong>{t('app.title')}</strong>
        {#if app.info?.version}<span class="version">v{app.info.version}</span>{/if}
      </span>
    </button>

    <div class="right">
      <nav class="tabs" aria-label="menu">
        {#each nav as item}
          <button
            class="tab"
            class:active={app.route === item.route}
            class:love={item.route === 'supporters'}
            onclick={() => (app.route = item.route)}
            aria-current={app.route === item.route ? 'page' : undefined}
          >
            {@render glyph(item.icon)}
            <span>{t(item.key)}</span>
          </button>
        {/each}
      </nav>

      <label class="lang" title="Idioma / Language">
        {@render glyph('globe')}
        <select value={i18n.lang} onchange={setLang} aria-label="idioma">
          {#each i18n.languages as l}<option value={l}>{l}</option>{/each}
        </select>
      </label>
    </div>
  </header>

  {#if app.route === 'wizard'}
    <ol class="stepper">
      {#each STEPS as s, i}
        <li class:done={i < app.step} class:active={i === app.step}>
          <button
            onclick={() => app.goStep(i)}
            disabled={!canVisit(i)}
            aria-current={i === app.step ? 'step' : undefined}
          >
            <span class="dot">
              {#if i < app.step}{@render glyph('check')}{:else}{i + 1}{/if}
            </span>
            <span class="label">{t(`step.${s}`)}</span>
          </button>
        </li>
      {/each}
    </ol>
  {/if}

  <div class="scroll">
    <main>
      {#if app.route === 'settings'}
        <SettingsPanel />
      {:else if app.route === 'supporters'}
        <SupportersPanel />
      {:else if app.route === 'about'}
        <AboutPanel />
      {:else}
        <Current />
      {/if}
    </main>
  </div>

  {#if app.route === 'wizard' && app.step > 0 && app.step < STEPS.indexOf('summary')}
    <footer class="bottombar">
      <button class="nav-btn" onclick={() => app.goStep(app.step - 1)}>
        {@render glyph('left')}
        {t('nav.back')}
      </button>

      <div class="where">
        <span class="muted">{t('nav.stepOf', { i: app.step + 1, n: STEPS.length })}</span>
        <strong>{t(`step.${STEPS[app.step]}`)}</strong>
        <span class="track"><span style="width: {((app.step + 1) / STEPS.length) * 100}%"></span></span>
      </div>

      <div class="next-wrap">
        {#if nextBlocked}<span class="blocked">{t('nav.blocked')}</span>{/if}
        <button class="primary nav-btn next" onclick={() => app.goStep(app.step + 1)} disabled={nextBlocked}>
          {t('nav.next')}
          {@render glyph('right')}
        </button>
      </div>
    </footer>
  {/if}

  {#if app.toast}<div class="toast">{app.toast}</div>{/if}
</div>

<style>
  /* Cabeçalho, abas e rodapé fixos; só o conteúdo do meio rola. */
  .shell { display: flex; flex-direction: column; height: 100vh; }
  .topbar, .stepper, .bottombar { flex-shrink: 0; }
  .scroll { flex: 1; min-height: 0; overflow-y: auto; }
  main { padding: 1.2rem; max-width: 860px; width: 100%; margin: 0 auto; }

  svg { width: 17px; height: 17px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; flex-shrink: 0; }

  /* ---------- cabeçalho ---------- */
  .topbar {
    display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap;
    padding: .55rem 1rem; background: var(--surface);
    border-bottom: 1px solid var(--border);
    box-shadow: 0 1px 0 color-mix(in srgb, var(--accent) 10%, transparent);
  }
  .brand {
    display: flex; align-items: center; gap: .6rem; padding: .2rem .4rem .2rem .2rem;
    border: none; background: transparent; border-radius: 12px;
  }
  .brand:hover:not(:disabled) { background: var(--surface-2); }
  .brand img { width: 34px; height: 34px; border-radius: 10px; box-shadow: 0 2px 8px rgba(0, 0, 0, .18); }
  .brand-text { display: flex; flex-direction: column; align-items: flex-start; line-height: 1.15; }
  .brand-text strong { font-size: .98rem; letter-spacing: -.01em; }
  .version { font-size: .7rem; color: var(--muted); }

  .right { display: flex; align-items: center; gap: .6rem; flex-wrap: wrap; }
  .tabs { display: flex; gap: .2rem; padding: .25rem; border-radius: 12px; background: var(--surface-2); }
  .tab {
    display: flex; align-items: center; gap: .4rem; padding: .4rem .75rem; border: none; border-radius: 9px;
    background: transparent; color: var(--muted); font-weight: 500; transition: background .15s ease, color .15s ease;
  }
  .tab:hover:not(:disabled) { background: color-mix(in srgb, var(--surface) 70%, transparent); color: var(--fg); }
  .tab.active { background: var(--surface); color: var(--fg); box-shadow: 0 1px 4px rgba(0, 0, 0, .12); }
  .tab.active svg { color: var(--accent); }
  .tab.love svg { color: #e0245e; }
  .tab.love.active svg, .tab.love:hover svg { fill: #e0245e; }

  .lang {
    display: flex; align-items: center; gap: .3rem; padding: 0 .2rem 0 .55rem; border-radius: 10px;
    border: 1px solid var(--border); color: var(--muted); margin: 0;
  }
  .lang select { border: none; background: transparent; padding: .4rem .3rem; color: var(--fg); cursor: pointer; }

  /* ---------- etapas ---------- */
  .stepper {
    list-style: none; display: flex; margin: 0; padding: .55rem 1rem; gap: 0;
    background: var(--surface); border-bottom: 1px solid var(--border); overflow-x: auto;
  }
  .stepper li { display: flex; align-items: center; flex: 1; min-width: 0; }
  .stepper li:not(:last-child)::after {
    content: ''; flex: 1; height: 2px; min-width: 12px; margin: 0 .35rem; border-radius: 2px; background: var(--border);
  }
  .stepper li.done::after { background: var(--ok); }
  .stepper li:last-child { flex: none; }
  .stepper button {
    display: flex; align-items: center; gap: .45rem; padding: .3rem .5rem; border: none; border-radius: 999px;
    background: transparent; color: var(--muted); font-size: .84rem; white-space: nowrap;
  }
  .stepper button:hover:not(:disabled) { background: var(--surface-2); color: var(--fg); }
  .stepper button:disabled { opacity: .4; }
  .dot {
    width: 24px; height: 24px; border-radius: 999px; display: grid; place-items: center; flex-shrink: 0;
    font-size: .75rem; font-weight: 700; background: var(--surface-2); color: var(--muted);
    transition: background .15s ease, box-shadow .15s ease;
  }
  .dot svg { width: 14px; height: 14px; stroke-width: 3; }
  .stepper li.done button { color: var(--fg); }
  .stepper li.done .dot { background: var(--ok); color: #fff; }
  .stepper li.active button { color: var(--fg); font-weight: 600; }
  .stepper li.active .dot {
    background: var(--accent); color: var(--accent-fg);
    box-shadow: 0 0 0 4px color-mix(in srgb, var(--accent) 22%, transparent);
  }

  /* ---------- rodapé ---------- */
  .bottombar {
    display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; gap: 1rem;
    padding: .7rem 1.2rem; background: var(--surface); border-top: 1px solid var(--border);
    box-shadow: 0 -4px 16px rgba(0, 0, 0, .05);
  }
  .nav-btn { display: inline-flex; align-items: center; gap: .45rem; padding: .55rem 1rem; font-weight: 600; border-radius: 10px; justify-self: start; }
  .nav-btn svg { transition: transform .15s ease; }
  .nav-btn:not(.next):hover:not(:disabled) svg { transform: translateX(-3px); }
  .next { box-shadow: 0 6px 16px color-mix(in srgb, var(--accent) 35%, transparent); }
  .next:hover:not(:disabled) svg { transform: translateX(3px); }
  .next:disabled { box-shadow: none; }

  .where { display: flex; flex-direction: column; align-items: center; gap: .15rem; min-width: 150px; }
  .where span.muted { font-size: .72rem; text-transform: uppercase; letter-spacing: .06em; }
  .where strong { font-size: .92rem; }
  .track { width: 100%; height: 4px; border-radius: 999px; background: var(--surface-2); overflow: hidden; margin-top: .2rem; }
  .track span { display: block; height: 100%; border-radius: inherit; background: var(--accent); transition: width .3s ease; }

  .next-wrap { display: flex; align-items: center; gap: .7rem; justify-self: end; }
  .blocked { font-size: .78rem; color: var(--muted); text-align: right; max-width: 22ch; line-height: 1.3; }

  .toast {
    position: fixed; bottom: 5rem; left: 50%; transform: translateX(-50%); z-index: 50;
    background: var(--fg); color: var(--bg); padding: .6rem 1rem; border-radius: var(--radius);
    font-size: .85rem; box-shadow: 0 4px 16px rgba(0,0,0,.25);
  }

  @media (max-width: 760px) {
    .tab span, .stepper li:not(.active) .label { display: none; }
    .blocked { display: none; }
  }
</style>
