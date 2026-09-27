<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import { bridge } from '../lib/bridge';
  import { Requests } from '../lib/contracts';
  import type { CanvasGridDto } from '../lib/contracts';

  // Grade atual (mesma lógica da etapa Imagem): o C# resolve colunas x linhas.
  const aspect = $derived(app.config?.canvasAspect ?? 'OneToOne');
  const precision = $derived(app.config?.precisionByAspect?.[aspect] ?? 'Small');
  const gridArgs = $derived({ aspect, precision });

  const aspectLabel: Record<string, string> = {
    OneToOne: '1:1',
    SixteenNine: '16:9',
    NineSixteen: '9:16',
    FourThree: '4:3',
    ThreeFour: '3:4'
  };

  async function calibrateGrid() {
    app.busy = true;
    try {
      const r = await bridge.request<{ cancelled?: boolean } & CanvasGridDto>(
        Requests.CalibrationStartGrid,
        gridArgs
      );
      if (!r.cancelled && app.config) {
        app.config.calibration.canvasGrid = {
          originX: r.originX,
          originY: r.originY,
          pitchX: r.pitchX,
          pitchY: r.pitchY,
          columns: r.columns,
          rows: r.rows
        };
        app.flash(t('calib.gridOk'));
      }
    } catch (e) {
      app.flash((e as Error).message);
    } finally {
      app.busy = false;
    }
  }

  async function detectCanvas() {
    app.busy = true;
    try {
      const r = await bridge.request<{
        found: boolean;
        reason?: string;
        x: number;
        y: number;
        width: number;
        height: number;
        grid?: CanvasGridDto | null;
      }>(Requests.CalibrationDetectRect, gridArgs);
      if (r.found && app.config) {
        app.config.calibration.canvasRect = { x: r.x, y: r.y, width: r.width, height: r.height };
        app.config.calibration.canvasGrid = r.grid ?? null;
        app.flash(t('calib.detectOk'));
      } else {
        app.flash(t('calib.detectFail'));
      }
    } catch (e) {
      app.flash((e as Error).message);
    } finally {
      app.busy = false;
    }
  }

  // Estado da calibração frente à grade convertida na etapa Imagem.
  const grid = $derived(app.config?.calibration.canvasGrid ?? null);
  const rect = $derived(app.config?.calibration.canvasRect ?? null);
  const conv = $derived(app.conversion);
  const mismatch = $derived(!!(grid && conv && (grid.columns !== conv.columns || grid.rows !== conv.rows)));
  const canvasState = $derived<'none' | 'ok' | 'mismatch'>(!app.hasCanvas ? 'none' : mismatch ? 'mismatch' : 'ok');
  const detail = $derived(
    grid
      ? t('canvas.gridDetail', { c: grid.columns, r: grid.rows, px: grid.pitchX.toFixed(1), py: grid.pitchY.toFixed(1) })
      : rect
        ? t('canvas.rectDetail', { w: rect.width, h: rect.height, x: rect.x, y: rect.y })
        : t('canvas.noneDetail')
  );
</script>

{#snippet glyph(name: string)}
  <svg viewBox="0 0 24 24" aria-hidden="true">
    {#if name === 'scan'}
      <path d="M3 7V5a2 2 0 0 1 2-2h2M17 3h2a2 2 0 0 1 2 2v2M21 17v2a2 2 0 0 1-2 2h-2M7 21H5a2 2 0 0 1-2-2v-2" />
      <path d="M7 12h10" />
    {:else if name === 'grid'}
      <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M3 9h18M3 15h18M9 3v18M15 3v18" />
    {:else if name === 'check'}
      <path d="M20 6 9 17l-5-5" />
    {:else if name === 'alert'}
      <path d="M12 9v4M12 17h.01" /><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
    {:else if name === 'dash'}
      <circle cx="12" cy="12" r="9" stroke-dasharray="3 3" />
    {/if}
  </svg>
{/snippet}

<div class="stack page">
  <header class="head">
    <div>
      <h1>{t('step.canvas')}</h1>
      <p class="muted">{t('canvas.lead')}</p>
    </div>
    <button class="frame" onclick={() => app.goTo('image')} title={t('canvas.changeFrame')}>
      <span class="muted small">{t('canvas.frame')}</span>
      <strong>{aspectLabel[aspect]} · {t(`size.${precision}`)}</strong>
    </button>
  </header>

  <!-- Situação atual -->
  <div class="status {canvasState}">
    <span class="status-icon">{@render glyph(canvasState === 'ok' ? 'check' : canvasState === 'mismatch' ? 'alert' : 'dash')}</span>
    <div>
      <strong>{t(`canvas.state.${canvasState}`)}</strong>
      <span class="small">{canvasState === 'mismatch' ? t('canvas.mismatchHint') : detail}</span>
    </div>
  </div>

  <!-- Opção principal -->
  <section class="option main">
    <span class="tag">{t('canvas.recommended')}</span>
    <div class="option-head">
      <span class="icon">{@render glyph('scan')}</span>
      <div>
        <h2>{t('calib.detectCanvas')}</h2>
        <p class="muted">{t('canvas.detectLead')}</p>
      </div>
    </div>
    <ol class="how">
      <li><b>1</b>{t('canvas.detect1')}</li>
      <li><b>2</b>{t('canvas.detect2')}</li>
      <li><b>3</b>{t('canvas.detect3')}</li>
    </ol>
    <button class="primary big" onclick={detectCanvas} disabled={app.busy}>
      {@render glyph('scan')}
      {t('calib.detectCanvas')}
    </button>
  </section>

  <!-- Plano B -->
  <section class="option alt">
    <div class="option-head">
      <span class="icon soft">{@render glyph('grid')}</span>
      <div>
        <h2>{t('calib.gridCanvas')}</h2>
        <p class="muted">{t('canvas.gridLead')}</p>
      </div>
      <button onclick={calibrateGrid} disabled={app.busy}>{t('calib.gridCanvas')}</button>
    </div>
    <p class="muted small">{t('calib.gridHint')}</p>
  </section>
</div>

<style>
  .page { gap: 1.1rem; }
  .small { font-size: .8rem; line-height: 1.4; }
  svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; flex-shrink: 0; }

  .head { display: flex; gap: 1rem; align-items: flex-end; justify-content: space-between; flex-wrap: wrap; }
  .head h1 { margin: 0 0 .25rem; }
  .head p { margin: 0; max-width: 55ch; }
  .frame {
    display: flex; flex-direction: column; align-items: flex-start; gap: .1rem; padding: .55rem .85rem;
    border-radius: 12px; background: var(--surface);
  }

  /* situação */
  .status {
    --c: var(--muted);
    display: flex; align-items: center; gap: .8rem; padding: .85rem 1rem; border-radius: 14px;
    border: 1px solid color-mix(in srgb, var(--c) 40%, var(--border)); background: color-mix(in srgb, var(--c) 7%, var(--surface));
  }
  .status > div { display: flex; flex-direction: column; gap: .1rem; }
  .status.ok { --c: var(--ok); }
  .status.mismatch { --c: #f5a524; }
  .status-icon {
    width: 36px; height: 36px; border-radius: 999px; display: grid; place-items: center; flex-shrink: 0;
    color: var(--c); background: color-mix(in srgb, var(--c) 16%, transparent);
  }
  .status.ok .status-icon { background: var(--ok); color: #fff; }
  .status.ok .status-icon svg { stroke-width: 3; }

  /* opções */
  .option {
    position: relative; display: flex; flex-direction: column; gap: .8rem; padding: 1.1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .option.main {
    border: 1.5px solid color-mix(in srgb, var(--accent) 55%, var(--border));
    background:
      radial-gradient(90% 120% at 100% 0%, color-mix(in srgb, var(--accent) 12%, transparent), transparent 60%),
      var(--surface);
    box-shadow: 0 8px 24px color-mix(in srgb, var(--accent) 12%, transparent);
  }
  .tag {
    position: absolute; top: -10px; left: 1rem; font-size: .7rem; font-weight: 700; letter-spacing: .06em; text-transform: uppercase;
    padding: .2rem .6rem; border-radius: 999px; background: var(--accent); color: var(--accent-fg);
  }
  .option-head { display: flex; align-items: center; gap: .8rem; }
  .option-head > div { flex: 1; min-width: 0; }
  .option-head h2 { margin: 0 0 .15rem; font-size: 1.05rem; }
  .option-head p { margin: 0; line-height: 1.45; }
  .option-head button { flex-shrink: 0; }
  .icon {
    width: 46px; height: 46px; border-radius: 13px; display: grid; place-items: center; flex-shrink: 0;
    background: var(--accent); color: var(--accent-fg); box-shadow: 0 6px 16px color-mix(in srgb, var(--accent) 35%, transparent);
  }
  .icon svg { width: 24px; height: 24px; }
  .icon.soft { background: var(--surface-2); color: var(--muted); box-shadow: none; }

  .how { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: .5rem; }
  .how li {
    display: flex; align-items: flex-start; gap: .55rem; padding: .65rem .75rem; border-radius: 12px;
    background: var(--surface-2); font-size: .85rem; line-height: 1.4;
  }
  .how b {
    width: 22px; height: 22px; border-radius: 7px; display: grid; place-items: center; flex-shrink: 0; font-size: .75rem;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 16%, var(--surface));
  }
  .big {
    align-self: flex-start; display: inline-flex; align-items: center; gap: .5rem; padding: .7rem 1.4rem; font-weight: 600;
    border-radius: 12px; box-shadow: 0 6px 18px color-mix(in srgb, var(--accent) 35%, transparent);
  }
  .big:disabled { box-shadow: none; }
  .option.alt p.small { margin: 0; }
</style>
