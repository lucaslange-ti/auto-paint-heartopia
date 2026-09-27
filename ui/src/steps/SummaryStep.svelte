<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import { GAME_PALETTE } from '../lib/gamePalette';
  import { LINKS } from '../lib/links';
  import { BRAND_ICONS } from '../lib/brandIcons';

  const phase = $derived(app.status?.phase ?? app.progress?.phase ?? 'Finished');
  const outcome = $derived<'ok' | 'stopped' | 'failed'>(
    phase === 'Aborted' ? 'stopped' : phase === 'Failed' ? 'failed' : 'ok'
  );
  const title = $derived(
    outcome === 'stopped'
      ? t('summary.aborted')
      : outcome === 'failed'
        ? t('summary.failed')
        : app.lastRunRepair
          ? t('summary.repairDone')
          : t('summary.done')
  );
  const subtitle = $derived(
    outcome === 'ok'
      ? app.lastRunRepair ? t('summary.repairSub') : t('summary.doneSub')
      : outcome === 'stopped' ? t('summary.abortedSub') : t('summary.failedSub')
  );

  // TimeSpan do C# chega como "hh:mm:ss.fffffff" (ou "d.hh:mm:ss…").
  function seconds(span: string | undefined): number {
    if (!span) return 0;
    const m = span.match(/(?:(\d+)\.)?(\d+):(\d+):(\d+(?:\.\d+)?)/);
    if (!m) return 0;
    return (+(m[1] ?? 0)) * 86400 + +m[2] * 3600 + +m[3] * 60 + +m[4];
  }
  function formatDuration(total: number): string {
    const s = Math.round(total);
    const h = Math.floor(s / 3600);
    const min = Math.floor((s % 3600) / 60);
    const sec = s % 60;
    return h ? `${h}h ${min}min` : min ? `${min}min ${sec}s` : `${sec}s`;
  }

  const p = $derived(app.progress);
  const elapsedSec = $derived(seconds(p?.elapsed));
  const pct = $derived(p && p.cellsTotal ? Math.round((p.cellsDone / p.cellsTotal) * 100) : 0);
  const perMinute = $derived(elapsedSec > 0 && p ? Math.round((p.cellsDone / elapsedSec) * 60) : 0);

  // Anel de progresso.
  const R = 52;
  const C = 2 * Math.PI * R;

  // Confete de "pixels" nas cores do jogo quando dá tudo certo.
  const confetti = Array.from({ length: 22 }, (_, i) => {
    const color = GAME_PALETTE[(i % 12) + 1].rgb;
    return {
      left: (i * 37) % 100,
      delay: (i % 7) * 0.18,
      dur: 2.2 + (i % 5) * 0.35,
      size: 6 + (i % 3) * 3,
      rot: (i * 47) % 360,
      color: `rgb(${color.r},${color.g},${color.b})`
    };
  });

  // Miniatura do que foi pintado (mesma lógica da etapa Imagem).
  let preview: HTMLCanvasElement | undefined = $state();
  $effect(() => {
    const conv = app.conversion;
    const cv = preview;
    if (!conv || !cv) return;
    cv.width = conv.columns;
    cv.height = conv.rows;
    const ctx = cv.getContext('2d');
    if (!ctx) return;
    const img = ctx.createImageData(conv.columns, conv.rows);
    for (let i = 0; i < conv.cells.length; i++) {
      const u = conv.usage[conv.cells[i]];
      img.data.set([u.rgb.r, u.rgb.g, u.rgb.b, 255], i * 4);
    }
    ctx.putImageData(img, 0, 0);
  });
</script>

{#snippet glyph(name: string)}
  <svg viewBox="0 0 24 24" aria-hidden="true">
    {#if name === 'ok'}
      <path d="M20 6 9 17l-5-5" />
    {:else if name === 'stopped'}
      <rect x="6" y="6" width="12" height="12" rx="2" />
    {:else if name === 'failed'}
      <path d="M18 6 6 18M6 6l12 12" />
    {:else if name === 'image'}
      <rect x="3" y="3" width="18" height="18" rx="2" /><circle cx="9" cy="9" r="2" /><path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21" />
    {:else if name === 'play'}
      <path d="M6 3 20 12 6 21z" />
    {:else if name === 'wrench'}
      <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z" />
    {:else if name === 'heart'}
      <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z" />
    {/if}
  </svg>
{/snippet}

<div class="stack page">
  <!-- Resultado -->
  <section class="hero {outcome}">
    {#if outcome === 'ok'}
      <div class="confetti" aria-hidden="true">
        {#each confetti as c}
          <span
            style="left: {c.left}%; width: {c.size}px; height: {c.size}px; background: {c.color};
                   animation-delay: {c.delay}s; animation-duration: {c.dur}s; --rot: {c.rot}deg"
          ></span>
        {/each}
      </div>
    {/if}

    <span class="badge">{@render glyph(outcome)}</span>
    <h1>{title}</h1>
    <p>{subtitle}</p>
    {#if app.status?.message}<p class="message">{app.status.message}</p>{/if}
  </section>

  {#if p}
    <section class="numbers">
      <!-- Anel com o % pintado -->
      <div class="ring-card">
        <svg class="ring" viewBox="0 0 120 120" aria-hidden="true">
          <circle cx="60" cy="60" r={R} class="track" />
          <circle
            cx="60" cy="60" r={R} class="value {outcome}"
            stroke-dasharray={C} stroke-dashoffset={C * (1 - pct / 100)}
          />
        </svg>
        <div class="ring-label">
          <strong>{pct}%</strong>
          <span class="muted">{t('summary.completed')}</span>
        </div>
      </div>

      <div class="stats">
        <div class="stat">
          <span class="muted">{t('summary.painted')}</span>
          <strong>{p.cellsDone.toLocaleString()}<small> / {p.cellsTotal.toLocaleString()}</small></strong>
        </div>
        <div class="stat">
          <span class="muted">{t('summary.elapsed')}</span>
          <strong>{formatDuration(elapsedSec)}</strong>
        </div>
        <div class="stat">
          <span class="muted">{t('summary.speed')}</span>
          <strong>{perMinute.toLocaleString()}<small> {t('summary.perMinute')}</small></strong>
        </div>
        <div class="stat">
          <span class="muted">{t('summary.repaired')}</span>
          <strong>{p.cellsRepaired.toLocaleString()}</strong>
        </div>
      </div>
    </section>
  {/if}

  {#if app.conversion}
    <section class="painted">
      <canvas bind:this={preview} class="pixelated"></canvas>
      <div>
        <strong>{t('summary.whatPainted')}</strong>
        <span class="muted small">
          {app.conversion.columns}×{app.conversion.rows} · {app.conversion.usage.length} {t('summary.shades')}
        </span>
        {#if outcome === 'ok' && !app.lastRunRepair}
          <span class="muted small tip">{t('summary.repairTip')}</span>
        {/if}
      </div>
    </section>
  {/if}

  <!-- Próximos passos -->
  <section class="actions">
    {#if app.status?.canResume}
      <button class="primary big" onclick={() => app.goTo('paint')}>{@render glyph('play')}{t('paint.resume')}</button>
      <button class="big" onclick={() => app.goTo('image')}>{@render glyph('image')}{t('summary.again')}</button>
    {:else}
      <button class="primary big" onclick={() => app.goTo('image')}>{@render glyph('image')}{t('summary.again')}</button>
      <button class="big" onclick={() => app.goTo('paint')}>{@render glyph('wrench')}{t('summary.toRepair')}</button>
    {/if}
  </section>

  <!-- Apoio e sugestões -->
  <section class="cta">
    <span class="cta-icon">{@render glyph('heart')}</span>
    <div class="cta-text">
      <strong>{outcome === 'ok' ? t('summary.ctaTitleOk') : t('summary.ctaTitle')}</strong>
      <span class="muted small">{t('summary.ctaText')}</span>
    </div>
    <div class="cta-actions">
      <button class="love" onclick={() => (app.route = 'supporters')}>{@render glyph('heart')}{t('home.support')}</button>
      <a class="discord" href={LINKS.discordServer} target="_blank" rel="noreferrer">
        <svg viewBox="0 0 24 24" aria-hidden="true" class="brand"><path d={BRAND_ICONS.discord} /></svg>
        {t('summary.suggest')}
      </a>
    </div>
  </section>
</div>

<style>
  .page { gap: 1.1rem; }
  .small { font-size: .8rem; line-height: 1.45; }
  svg { width: 18px; height: 18px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; flex-shrink: 0; }

  /* ---------- resultado ---------- */
  .hero {
    --c: var(--ok);
    position: relative; overflow: hidden; display: flex; flex-direction: column; align-items: center; text-align: center;
    padding: 2rem 1.4rem 1.6rem; border-radius: 18px; border: 1px solid color-mix(in srgb, var(--c) 35%, var(--border));
    background:
      radial-gradient(80% 110% at 50% 0%, color-mix(in srgb, var(--c) 20%, transparent), transparent 65%),
      var(--surface);
  }
  .hero.stopped { --c: #f5a524; }
  .hero.failed { --c: var(--danger); }
  .badge {
    position: relative; width: 64px; height: 64px; border-radius: 999px; display: grid; place-items: center; margin-bottom: .8rem;
    background: var(--c); color: #fff; box-shadow: 0 0 0 8px color-mix(in srgb, var(--c) 18%, transparent), 0 10px 24px color-mix(in srgb, var(--c) 35%, transparent);
    animation: pop .45s cubic-bezier(.2, 1.4, .4, 1) both;
  }
  .badge svg { width: 32px; height: 32px; stroke-width: 3; }
  .hero.stopped .badge svg { fill: currentColor; stroke: none; }
  .hero h1 { position: relative; margin: 0 0 .35rem; font-size: 1.6rem; letter-spacing: -.02em; }
  .hero p { position: relative; margin: 0; color: var(--muted); max-width: 52ch; line-height: 1.5; }
  .hero .message { margin-top: .6rem; color: var(--danger); font-size: .86rem; }
  @keyframes pop { from { transform: scale(.4); opacity: 0; } to { transform: scale(1); opacity: 1; } }

  .confetti { position: absolute; inset: 0; pointer-events: none; }
  .confetti span {
    position: absolute; top: -12px; border-radius: 2px; opacity: 0;
    animation-name: fall; animation-timing-function: ease-in; animation-iteration-count: 1; animation-fill-mode: forwards;
  }
  @keyframes fall {
    0% { transform: translateY(0) rotate(0); opacity: 1; }
    100% { transform: translateY(260px) rotate(var(--rot)); opacity: 0; }
  }
  @media (prefers-reduced-motion: reduce) { .confetti { display: none; } .badge { animation: none; } }

  /* ---------- números ---------- */
  .numbers { display: grid; grid-template-columns: auto 1fr; gap: .8rem; align-items: stretch; }
  .ring-card {
    position: relative; display: grid; place-items: center; padding: .8rem 1.2rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .ring { width: 130px; height: 130px; transform: rotate(-90deg); }
  .ring circle { fill: none; stroke-width: 10; }
  .ring .track { stroke: var(--surface-2); }
  .ring .value { stroke: var(--ok); stroke-linecap: round; transition: stroke-dashoffset .8s ease; }
  .ring .value.stopped { stroke: #f5a524; }
  .ring .value.failed { stroke: var(--danger); }
  .ring-label { position: absolute; display: flex; flex-direction: column; align-items: center; }
  .ring-label strong { font-size: 1.7rem; letter-spacing: -.03em; line-height: 1; }
  .ring-label .muted { font-size: .72rem; text-transform: uppercase; letter-spacing: .06em; margin-top: .2rem; }

  .stats { display: grid; grid-template-columns: repeat(2, 1fr); gap: .6rem; }
  .stat {
    display: flex; flex-direction: column; justify-content: center; gap: .2rem; padding: .8rem 1rem; border-radius: 14px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .stat .muted { font-size: .72rem; text-transform: uppercase; letter-spacing: .06em; }
  .stat strong { font-size: 1.35rem; letter-spacing: -.02em; }
  .stat small { font-size: .8rem; font-weight: 500; color: var(--muted); }

  /* ---------- o que foi pintado ---------- */
  .painted {
    display: flex; align-items: center; gap: 1rem; padding: .9rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .painted > div { display: flex; flex-direction: column; gap: .2rem; }
  .painted canvas { width: 96px; height: auto; max-height: 120px; object-fit: contain; border-radius: 10px; border: 1px solid var(--border); background: #fff; }
  .pixelated { image-rendering: pixelated; }
  .tip { margin-top: .2rem; }

  /* ---------- ações ---------- */
  .actions { display: flex; flex-wrap: wrap; gap: .6rem; }
  .big { display: inline-flex; align-items: center; gap: .5rem; padding: .7rem 1.2rem; font-weight: 600; border-radius: 12px; }
  .big.primary { box-shadow: 0 6px 18px color-mix(in srgb, var(--accent) 35%, transparent); }
  .big.primary svg { fill: none; }
  .love { color: #e0245e; border-color: color-mix(in srgb, #e0245e 45%, transparent); background: color-mix(in srgb, #e0245e 8%, transparent); }
  .love:hover:not(:disabled) { background: color-mix(in srgb, #e0245e 16%, transparent); }
  .love svg { fill: currentColor; stroke: none; }

  /* ---------- apoio e sugestões ---------- */
  .cta {
    display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; padding: 1rem 1.1rem; border-radius: 16px;
    border: 1px solid color-mix(in srgb, #e0245e 30%, var(--border));
    background:
      radial-gradient(90% 140% at 0% 50%, color-mix(in srgb, #e0245e 12%, transparent), transparent 60%),
      radial-gradient(70% 120% at 100% 50%, color-mix(in srgb, #5865f2 12%, transparent), transparent 60%),
      var(--surface);
  }
  .cta-icon {
    width: 44px; height: 44px; border-radius: 13px; display: grid; place-items: center; flex-shrink: 0;
    background: #e0245e; color: #fff; box-shadow: 0 6px 16px color-mix(in srgb, #e0245e 35%, transparent);
  }
  .cta-icon svg { width: 22px; height: 22px; fill: currentColor; stroke: none; }
  .cta-text { display: flex; flex-direction: column; gap: .15rem; flex: 1; min-width: 220px; }
  .cta-actions { display: flex; gap: .5rem; flex-wrap: wrap; }
  .cta-actions > * { display: inline-flex; align-items: center; gap: .45rem; font-weight: 600; }
  .discord {
    padding: .5rem .9rem; border-radius: var(--radius); text-decoration: none; font-size: inherit;
    color: #fff; background: #5865f2; border: 1px solid #5865f2;
  }
  .discord:hover { background: #4752c4; }
  .brand { fill: currentColor; stroke: none; }

  @media (max-width: 620px) { .numbers { grid-template-columns: 1fr; } }
</style>
