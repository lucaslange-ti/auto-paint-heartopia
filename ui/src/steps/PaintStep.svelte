<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import { bridge } from '../lib/bridge';
  import { Requests } from '../lib/contracts';
  import type { PaintMode } from '../lib/contracts';
  import { SPEED_PRESETS, matchPreset } from '../lib/timing';
  import type { SpeedPreset } from '../lib/timing';
  import { hotkeyParts } from '../lib/hotkeys';

  let checks = $state({ a: false, b: false, c: false });
  let seekCell = $state(0);
  let scrubbing = $state(false);
  let repairArmed = $state(false);

  const ready = $derived(checks.a && checks.b && checks.c);
  const canPaint = $derived(!!app.conversion && app.hasCanvas && app.hasPalette);
  const running = $derived(['Preparing', 'Painting', 'Verifying', 'Repairing'].includes(app.progress?.phase ?? ''));
  // A barra só vira controle deslizante com a pintura pausada na fase de cliques.
  const canScrub = $derived(running && app.paused && app.progress?.phase === 'Painting');
  const percent = $derived(Math.round((app.progress?.fraction ?? 0) * 100));

  const mode = $derived(app.config?.paintMode ?? 'Color');
  const speed = $derived(matchPreset(app.config?.timing));
  const modes: PaintMode[] = ['Color', 'Row'];
  const speeds: SpeedPreset[] = ['safe', 'normal', 'fast'];
  const abortKeys = $derived(hotkeyParts(app.config?.hotkeys?.abort, t('paint.keySpace')));
  const pauseKeys = $derived(hotkeyParts(app.config?.hotkeys?.pauseResume, t('paint.keySpace')));

  // Enquanto o usuário não está arrastando, o slider acompanha o progresso real.
  $effect(() => {
    if (!scrubbing && app.progress) seekCell = app.progress.cellsDone;
  });

  function setMode(m: PaintMode) {
    if (!app.config) return;
    app.config.paintMode = m;
    app.saveConfig();
  }

  function setSpeed(s: SpeedPreset) {
    if (!app.config) return;
    app.config.timing = { ...SPEED_PRESETS[s] };
    app.saveConfig();
  }

  async function start(resume = false) {
    app.progress = null;
    app.status = null;
    app.lastRunRepair = false;
    app.paused = false;
    try {
      await bridge.request(Requests.PaintStart, { resume });
    } catch (e) {
      app.flash((e as Error).message);
    }
  }

  async function togglePause() {
    // O estado real chega pelo evento PaintStatus (Paused/Painting); aqui só pede.
    const pausing = !app.paused;
    if (!pausing) scrubbing = false;
    await bridge.request(pausing ? Requests.PaintPause : Requests.PaintResume);
  }

  async function commitSeek() {
    scrubbing = false;
    try {
      await bridge.request(Requests.PaintSeek, { cell: Math.round(seekCell) });
    } catch (e) {
      app.flash((e as Error).message);
    }
  }

  async function abort() {
    await bridge.request(Requests.PaintAbort);
  }

  async function repair() {
    try {
      await app.startRepair();
    } catch (e) {
      app.flash((e as Error).message);
    }
  }
</script>

{#snippet glyph(name: string)}
  <svg viewBox="0 0 24 24" aria-hidden="true">
    {#if name === 'Color'}
      <circle cx="13.5" cy="6.5" r="2.5" /><circle cx="17.5" cy="13.5" r="2.5" /><circle cx="8.5" cy="7.5" r="2.5" /><circle cx="6.5" cy="15.5" r="2.5" />
    {:else if name === 'Row'}
      <path d="M3 6h18M3 12h18M3 18h12" />
    {:else if name === 'safe'}
      <path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" />
    {:else if name === 'normal'}
      <path d="M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z" /><path d="M12 6v6l4 2" />
    {:else if name === 'fast'}
      <path d="M13 2 3 14h9l-1 8 10-12h-9l1-8z" />
    {:else if name === 'play'}
      <path d="M6 3 20 12 6 21z" />
    {:else if name === 'pause'}
      <path d="M8 4v16M16 4v16" />
    {:else if name === 'stop'}
      <rect x="5" y="5" width="14" height="14" rx="2" />
    {:else if name === 'check'}
      <path d="M20 6 9 17l-5-5" />
    {:else if name === 'wrench'}
      <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z" />
    {/if}
  </svg>
{/snippet}

<div class="stack page">
  <header class="head">
    <h1>{t('step.paint')}</h1>
    <p class="muted">{t('paint.lead')}</p>
  </header>

  {#if !canPaint}
    <div class="notice">{!app.conversion ? t('paint.noPlan') : t('paint.notCalibrated')}</div>
  {/if}

  {#if app.status?.canResume && !running}
    <div class="resume">
      <span>{t('paint.resumeCard')}</span>
      <button class="primary" onclick={() => start(true)}>{@render glyph('play')}{t('paint.resume')}</button>
    </div>
  {/if}

  <!-- Progresso -->
  {#if app.progress}
    <section class="live" class:running>
      <div class="live-top">
        <div class="pct">{canScrub ? Math.round((seekCell / app.progress.cellsTotal) * 100) : percent}<small>%</small></div>
        <div class="live-info">
          <strong>{t(`phase.${app.progress.phase}`)}</strong>
          <span class="muted small">
            {canScrub ? seekCell : app.progress.cellsDone} / {app.progress.cellsTotal} {t('image.cells')}
          </span>
        </div>
        {#if running}
          <div class="live-actions">
            <button onclick={togglePause}>
              {@render glyph(app.paused ? 'play' : 'pause')}{app.paused ? t('paint.resume') : t('paint.pause')}
            </button>
            <button class="danger" onclick={abort}>{@render glyph('stop')}{t('paint.abort')}</button>
          </div>
        {/if}
      </div>

      {#if canScrub}
        <input
          class="scrub"
          type="range"
          min="0"
          max={app.progress.cellsTotal}
          bind:value={seekCell}
          oninput={() => (scrubbing = true)}
          onchange={commitSeek}
        />
        <p class="muted small">{t('paint.seekHint')}</p>
      {:else}
        <div class="bar"><div class="fill" style="width: {(app.progress.fraction * 100).toFixed(1)}%"></div></div>
      {/if}

      <div class="chips">
        {#if app.progress.currentShadeName}<span class="chip">{t('paint.shade')}: <b>{app.progress.currentShadeName}</b></span>{/if}
        {#if app.progress.eta}<span class="chip">{t('paint.eta')}: <b>{app.progress.eta.split('.')[0]}</b></span>{/if}
        {#if app.progress.cellsRepaired > 0}<span class="chip">{t('summary.repaired')}: <b>{app.progress.cellsRepaired}</b></span>{/if}
      </div>
    </section>
  {/if}

  {#if !running}
    <!-- 1. Modo -->
    <section class="section">
      <h2><span class="n">1</span>{t('paint.modeTitle')}</h2>
      <div class="choices two">
        {#each modes as m}
          <button class="choice" class:on={mode === m} onclick={() => setMode(m)} aria-pressed={mode === m}>
            {#if m === 'Color'}<span class="tag">{t('canvas.recommended')}</span>{/if}
            <span class="choice-head">
              <span class="icon">{@render glyph(m)}</span>
              <strong>{t(`mode.${m}`)}</strong>
              {#if mode === m}<span class="tick">{@render glyph('check')}</span>{/if}
            </span>
            <span class="muted small">{t(`mode.${m}.text`)}</span>
            <span class="pros">
              <span class="pro">+ {t(`mode.${m}.pro`)}</span>
              <span class="con">− {t(`mode.${m}.con`)}</span>
            </span>
          </button>
        {/each}
      </div>
    </section>

    <!-- 2. Velocidade -->
    <section class="section">
      <h2><span class="n">2</span>{t('settings.timing')}</h2>
      <div class="choices three">
        {#each speeds as s}
          <button class="choice compact" class:on={speed === s} onclick={() => setSpeed(s)} aria-pressed={speed === s}>
            <span class="choice-head">
              <span class="icon">{@render glyph(s)}</span>
              <strong>{t(`settings.timing.${s}`)}</strong>
              {#if speed === s}<span class="tick">{@render glyph('check')}</span>{/if}
            </span>
            <span class="muted small">{t(`speed.${s}.text`)}</span>
          </button>
        {/each}
      </div>
      {#if !speed}<p class="muted small">{t('speed.custom')}</p>{/if}
    </section>

    <!-- 3. Checklist + iniciar -->
    <section class="section">
      <h2><span class="n">3</span>{t('paint.preflight')}</h2>
      <div class="preflight">
        <label class="check-row" class:on={checks.a}>
          <input type="checkbox" bind:checked={checks.a} />
          <span class="box">{@render glyph('check')}</span>
          {t('paint.p1')}
        </label>
        <label class="check-row" class:on={checks.b}>
          <input type="checkbox" bind:checked={checks.b} />
          <span class="box">{@render glyph('check')}</span>
          {t('paint.p2')}
        </label>
        <label class="check-row" class:on={checks.c}>
          <input type="checkbox" bind:checked={checks.c} />
          <span class="box">{@render glyph('check')}</span>
          <span>
            {t('paint.p3lead')}
            <span class="keys">
              <span>{#each abortKeys as k, i}{#if i > 0}+{/if}<kbd>{k}</kbd>{/each} {t('paint.keyAbort')}</span>
              <span>{#each pauseKeys as k, i}{#if i > 0}+{/if}<kbd>{k}</kbd>{/each} {t('paint.keyPause')}</span>
              <span>{t('paint.keyCorner')}</span>
            </span>
          </span>
        </label>

        <button class="primary go" disabled={!ready || !canPaint} onclick={() => start(false)}>
          {@render glyph('play')}
          {t('paint.start')}
        </button>
      </div>
    </section>

    <!-- Reparo -->
    <section class="repair">
      <div class="repair-head">
        <span class="icon soft">{@render glyph('wrench')}</span>
        <div>
          <strong>{t('image.repairTitle')}</strong>
          <span class="muted small">{t('image.repairHelp')}</span>
        </div>
      </div>
      {#if !canPaint}<p class="muted small">{t('image.repairNeed')}</p>{/if}
      <div class="repair-actions">
        <label class="arm"><input type="checkbox" bind:checked={repairArmed} /> {t('image.repairArm')}</label>
        <button disabled={!repairArmed || !canPaint} onclick={repair}>{t('image.repair')}</button>
      </div>
    </section>
  {/if}
</div>

<style>
  .page { gap: 1.2rem; }
  .small { font-size: .8rem; line-height: 1.45; }
  svg { width: 18px; height: 18px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; flex-shrink: 0; }
  .head h1 { margin: 0 0 .25rem; }
  .head p { margin: 0; }

  .section { display: flex; flex-direction: column; gap: .6rem; }
  .section h2 { display: flex; align-items: center; gap: .5rem; margin: 0; font-size: 1.02rem; }
  .section > p { margin: 0; }
  .n {
    width: 24px; height: 24px; border-radius: 8px; display: grid; place-items: center; font-size: .8rem; font-weight: 800;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }

  /* escolhas */
  .choices { display: grid; gap: .6rem; }
  .choices.two { grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); }
  .choices.three { grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); }
  .choice {
    position: relative; display: flex; flex-direction: column; align-items: stretch; gap: .45rem; padding: 1rem;
    text-align: left; font: inherit; border-radius: 14px; border: 1.5px solid var(--border); background: var(--surface); cursor: pointer;
    transition: border-color .15s ease, background .15s ease, box-shadow .15s ease;
  }
  .choice.compact { padding: .8rem .9rem; }
  .choice:hover:not(:disabled) { border-color: color-mix(in srgb, var(--accent) 50%, var(--border)); background: var(--surface); }
  .choice.on {
    border-color: var(--accent); background: color-mix(in srgb, var(--accent) 8%, var(--surface));
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 15%, transparent);
  }
  .choice-head { display: flex; align-items: center; gap: .6rem; }
  .choice-head strong { flex: 1; font-size: .98rem; }
  .icon {
    width: 36px; height: 36px; border-radius: 10px; display: grid; place-items: center; flex-shrink: 0;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  .icon.soft { color: var(--muted); background: var(--surface-2); }
  .tick { width: 22px; height: 22px; border-radius: 999px; display: grid; place-items: center; background: var(--accent); color: var(--accent-fg); }
  .tick svg { width: 13px; height: 13px; stroke-width: 3.5; }
  .tag {
    position: absolute; top: -10px; right: 1rem; font-size: .66rem; font-weight: 700; letter-spacing: .06em; text-transform: uppercase;
    padding: .18rem .55rem; border-radius: 999px; background: var(--accent); color: var(--accent-fg);
  }
  .pros { display: flex; flex-direction: column; gap: .15rem; font-size: .8rem; margin-top: .1rem; }
  .pro { color: var(--ok); }
  .con { color: var(--muted); }

  /* checklist */
  .preflight {
    display: flex; flex-direction: column; gap: .5rem; padding: 1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .check-row {
    position: relative; display: flex; align-items: flex-start; gap: .7rem; padding: .65rem .8rem; border-radius: 12px; margin: 0;
    background: var(--surface-2); color: var(--fg); cursor: pointer; border: 1.5px solid transparent;
    transition: border-color .15s ease, background .15s ease;
  }
  /* O input invisível precisa ficar preso à linha (position: relative acima);
     solto, ele era posicionado em relação à página e criava uma 2ª rolagem. */
  .check-row input { position: absolute; inset: 0; opacity: 0; pointer-events: none; }
  .check-row.on { border-color: color-mix(in srgb, var(--ok) 50%, transparent); background: color-mix(in srgb, var(--ok) 8%, var(--surface)); }
  .box {
    width: 22px; height: 22px; border-radius: 7px; display: grid; place-items: center; flex-shrink: 0;
    border: 2px solid var(--border); background: var(--surface); color: transparent; transition: all .15s ease;
  }
  .box svg { width: 14px; height: 14px; stroke-width: 3.5; }
  .check-row.on .box { background: var(--ok); border-color: var(--ok); color: #fff; }
  .check-row:has(input:focus-visible) { outline: 2px solid var(--accent); outline-offset: 2px; }
  .keys { display: flex; flex-wrap: wrap; gap: .4rem .9rem; margin-top: .35rem; font-size: .8rem; color: var(--muted); }
  kbd {
    display: inline-block; min-width: 1.6em; padding: .05rem .4rem; border-radius: 6px; text-align: center;
    font: 600 .75rem/1.5 inherit; color: var(--fg); background: var(--surface); border: 1px solid var(--border);
    box-shadow: 0 2px 0 var(--border);
  }
  .go {
    align-self: flex-start; display: inline-flex; align-items: center; gap: .5rem; margin-top: .4rem; padding: .75rem 1.6rem;
    font-weight: 700; font-size: 1rem; border-radius: 12px; box-shadow: 0 8px 20px color-mix(in srgb, var(--accent) 38%, transparent);
  }
  .go svg { fill: currentColor; stroke: none; }
  .go:disabled { box-shadow: none; }

  /* progresso ao vivo */
  .live {
    display: flex; flex-direction: column; gap: .8rem; padding: 1.1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .live.running {
    border-color: color-mix(in srgb, var(--accent) 50%, var(--border));
    background: radial-gradient(90% 120% at 0% 0%, color-mix(in srgb, var(--accent) 12%, transparent), transparent 60%), var(--surface);
  }
  .live-top { display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; }
  .pct { font-size: 2.3rem; font-weight: 800; letter-spacing: -.03em; line-height: 1; }
  .pct small { font-size: 1rem; color: var(--muted); margin-left: .1rem; }
  .live-info { display: flex; flex-direction: column; flex: 1; min-width: 0; }
  .live-actions { display: flex; gap: .5rem; }
  .live-actions button { display: inline-flex; align-items: center; gap: .4rem; font-weight: 600; }
  .bar { height: 10px; border-radius: 999px; background: var(--surface-2); overflow: hidden; }
  .fill { height: 100%; border-radius: inherit; background: linear-gradient(90deg, var(--accent), #e0245e); transition: width .2s; }
  .scrub { width: 100%; margin: 0; accent-color: var(--accent); cursor: grab; }
  .scrub:active { cursor: grabbing; }
  .live p { margin: 0; }
  .chips { display: flex; flex-wrap: wrap; gap: .4rem; }
  .chip { font-size: .8rem; padding: .25rem .65rem; border-radius: 999px; background: var(--surface-2); color: var(--muted); }
  .chip b { color: var(--fg); }

  .resume {
    display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap; padding: .85rem 1rem; border-radius: 14px;
    border: 1px solid color-mix(in srgb, var(--accent) 45%, var(--border)); background: color-mix(in srgb, var(--accent) 7%, var(--surface));
  }
  .resume button { display: inline-flex; align-items: center; gap: .4rem; font-weight: 600; }
  .resume svg { fill: currentColor; stroke: none; }

  /* reparo */
  .repair {
    display: flex; flex-direction: column; gap: .6rem; padding: 1rem; border-radius: 16px;
    border: 1px dashed var(--border); background: var(--surface);
  }
  .repair p { margin: 0; }
  .repair-head { display: flex; gap: .7rem; align-items: flex-start; }
  .repair-head > div { display: flex; flex-direction: column; gap: .15rem; }
  .repair-actions { display: flex; align-items: center; justify-content: space-between; gap: .8rem; flex-wrap: wrap; }
  .arm { display: flex; align-items: center; gap: .45rem; margin: 0; font-size: .84rem; color: var(--fg); }

  .notice {
    padding: .75rem 1rem; border-radius: 12px; font-size: .86rem;
    border: 1px solid color-mix(in srgb, #f5a524 40%, transparent); background: color-mix(in srgb, #f5a524 9%, transparent);
  }
</style>
