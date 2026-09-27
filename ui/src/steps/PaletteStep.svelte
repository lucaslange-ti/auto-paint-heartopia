<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import { bridge } from '../lib/bridge';
  import { Requests } from '../lib/contracts';
  import type { PointDto, Rgb } from '../lib/contracts';
  import { GAME_PALETTE, MAIN_COUNT, MAX_SHADES, buildMainColors, shadePositions } from '../lib/gamePalette';
  import HelpTip from '../lib/HelpTip.svelte';
  import imgPanel from '../assets/paleta.png';
  import imgBack from '../assets/botao-voltar.png';
  import imgBrush from '../assets/pincel.png';
  import imgColors from '../assets/cores.png';
  import imgShades from '../assets/tons.png';

  type GlobalKey = 'shadesPanel' | 'back' | 'paintTool';

  // Só a posição importa: a cor é fixa (lib/gamePalette.ts).
  async function capturePoint(label: string): Promise<PointDto | null> {
    const r = await bridge.request<{ cancelled?: boolean; x: number; y: number }>(
      Requests.CalibrationCapturePoint,
      { target: 'seq', label }
    );
    return r.cancelled ? null : { x: r.x, y: r.y };
  }

  async function captureSeq(count: number, label: (i: number) => string): Promise<PointDto[] | null> {
    const points: PointDto[] = [];
    for (let i = 0; i < count; i++) {
      const p = await capturePoint(label(i));
      if (!p) {
        app.flash(t('calib.cancelledSeq'));
        return null;
      }
      points.push(p);
    }
    return points;
  }

  // Cada captura já grava no config: a etapa é obrigatória e não pode se perder
  // se o usuário avançar sem um "salvar" explícito.
  async function run(action: () => Promise<void>) {
    app.busy = true;
    try {
      await action();
    } catch (e) {
      app.flash((e as Error).message);
    } finally {
      app.busy = false;
    }
  }

  const captureGlobal = (which: GlobalKey) =>
    run(async () => {
      const p = await capturePoint(t(`calib.${which}`));
      if (!p || !app.config) return;
      if (which === 'shadesPanel') app.config.calibration.shadesPanelButtonPos = p;
      if (which === 'back') app.config.calibration.backButtonPos = p;
      if (which === 'paintTool') app.config.calibration.paintToolButtonPos = p;
      await app.saveConfig();
    });

  const captureColors = () =>
    run(async () => {
      if (!app.config) return;
      const pos = await captureSeq(MAIN_COUNT, (i) => t('calib.colorProgress', { i: i + 1 }));
      if (!pos) return;
      const prev = app.config.calibration.mainColors;
      app.config.calibration.mainColors = buildMainColors(pos, shadePositions(prev), prev);
      await app.saveConfig();
    });

  const captureShades = () =>
    run(async () => {
      if (!app.config) return;
      const pos = await captureSeq(MAX_SHADES, (i) => t('calib.shadeProgress', { i: i + 1, n: MAX_SHADES }));
      if (!pos) return;
      const prev = app.config.calibration.mainColors;
      app.config.calibration.mainColors = buildMainColors(
        prev.map((c) => c.pos),
        pos,
        prev
      );
      await app.saveConfig();
    });

  function toggleColor(ci: number) {
    if (!app.config) return;
    app.config.calibration.mainColors[ci].enabled = !app.config.calibration.mainColors[ci].enabled;
    app.saveConfig();
  }

  function toggleShade(ci: number, si: number) {
    const shade = app.config?.calibration.mainColors[ci]?.shades[si];
    if (!shade) return;
    shade.enabled = !shade.enabled;
    app.saveConfig();
  }

  const colors = $derived(app.config?.calibration.mainColors ?? []);
  const hasColors = $derived(colors.length === MAIN_COUNT);
  const shadePos = $derived(shadePositions(colors));
  const hex = (c: Rgb) => '#' + [c.r, c.g, c.b].map((v) => v.toString(16).padStart(2, '0')).join('').toUpperCase();
  const css = (c: Rgb) => `rgb(${c.r},${c.g},${c.b})`;
  const globalPos = (which: GlobalKey) =>
    which === 'shadesPanel'
      ? app.config?.calibration.shadesPanelButtonPos
      : which === 'back'
        ? app.config?.calibration.backButtonPos
        : app.config?.calibration.paintToolButtonPos;
  const globals: GlobalKey[] = ['shadesPanel', 'back', 'paintTool'];
  const globalImage: Record<GlobalKey, string> = { shadesPanel: imgPanel, back: imgBack, paintTool: imgBrush };

  // Checklist da etapa: 3 botões + posição das cores + posição dos tons.
  const checks = $derived([...globals.map((k) => !!globalPos(k)), hasColors, shadePos.length > 0]);
  const doneCount = $derived(checks.filter(Boolean).length);
  const activeShades = (ci: number) => colors[ci]?.shades.filter((s) => s.enabled).length ?? 0;
</script>

{#snippet check(ok: boolean)}
  <span class="status" class:ok>
    {#if ok}
      <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 6 9 17l-5-5" /></svg>
    {/if}
  </span>
{/snippet}

<div class="stack page">
  <header class="head">
    <div>
      <h1>{t('step.palette')}</h1>
      <p class="muted">{t('palette.lead')}</p>
    </div>
    <div class="progress" class:complete={doneCount === checks.length}>
      <strong>{doneCount}/{checks.length}</strong>
      <span>{t('palette.progress')}</span>
      <span class="bar"><span style="width: {(doneCount / checks.length) * 100}%"></span></span>
    </div>
  </header>

  <!-- Antes de começar -->
  <div class="heads-up" role="note">
    <span class="hu-icon" aria-hidden="true">
      <svg viewBox="0 0 24 24"><rect x="2" y="4" width="20" height="14" rx="2" /><path d="M8 21h8M12 18v3" /><path d="m9 11 2 2 4-4" /></svg>
    </span>
    <div>
      <strong>{t('palette.headsUpTitle')}</strong>
      <span>{t('palette.headsUpText')}</span>
    </div>
  </div>

  <!-- 1. Botões do jogo -->
  <section class="section">
    <h2><span class="n">1</span>{t('calib.globals')}</h2>
    <div class="buttons">
      {#each globals as key}
        {@const pos = globalPos(key)}
        <div class="tile btn-tile" class:ok={!!pos}>
          <img class="thumb" src={globalImage[key]} alt="" />
          <div class="info center">
            <HelpTip text={t('calib.tipButton')} image={globalImage[key]}><strong>{t(`calib.${key}`)}</strong></HelpTip>
            <span class="muted small">{pos ? `${pos.x}, ${pos.y}` : t('palette.pending')}</span>
          </div>
          <div class="actions">
            {@render check(!!pos)}
            <button class:primary={!pos} onclick={() => captureGlobal(key)} disabled={app.busy}>
              {pos ? t('calib.recapture') : t('calib.capture')}
            </button>
          </div>
        </div>
      {/each}
    </div>
  </section>

  <!-- 2. Posição das cores e dos tons -->
  <section class="section">
    <h2><span class="n">2</span>{t('calib.palette')}</h2>
    <div class="captures">
      <div class="tile big" class:ok={hasColors}>
        <div class="info">
          <HelpTip text={t('calib.tipColors')} image={imgColors}><strong>{t('calib.colorsPos')}</strong></HelpTip>
          <span class="muted small">{t('calib.colorsHint')}</span>
        </div>
        <div class="actions">
          {@render check(hasColors)}
          <button class:primary={!hasColors} onclick={captureColors} disabled={app.busy}>
            {hasColors ? t('calib.recapture') : t('calib.capture')}
          </button>
        </div>
      </div>

      <div class="tile big" class:ok={shadePos.length > 0} class:locked={!hasColors}>
        <div class="info">
          <HelpTip text={t('calib.tipShades')} image={imgShades}><strong>{t('calib.shadesPos')}</strong></HelpTip>
          <span class="muted small">{hasColors ? t('calib.shadesHint') : t('calib.needColors')}</span>
        </div>
        <div class="actions">
          {@render check(shadePos.length > 0)}
          <button class:primary={hasColors && !shadePos.length} onclick={captureShades} disabled={app.busy || !hasColors}>
            {shadePos.length ? t('calib.recapture') : t('calib.capture')}
          </button>
        </div>
      </div>
    </div>
  </section>

  <!-- 3. Paleta fixa do jogo -->
  <section class="section">
    <h2><span class="n">3</span>{t('palette.gridTitle')}</h2>
    <p class="muted small">{t('calib.toggleHint')}</p>
    <div class="palette">
      {#each GAME_PALETTE as fixed, ci}
        {@const color = colors[ci]}
        <div class="color" class:off={color && !color.enabled}>
          <div class="chead">
            <span class="big-swatch" style="background: {css(fixed.rgb)}"></span>
            <div class="cname">
              <strong>{fixed.name}</strong>
              <span class="muted small">{hex(fixed.rgb)}{color ? ` · ${activeShades(ci)}/${fixed.shades.length}` : ''}</span>
            </div>
            {#if color}
              <button
                class="switch"
                role="switch"
                aria-checked={color.enabled}
                aria-label={t('calib.use')}
                title={t('calib.use')}
                onclick={() => toggleColor(ci)}
              ><span></span></button>
            {/if}
          </div>
          <div class="shades">
            {#each fixed.shades as shade, si}
              {@const saved = color?.shades[si]}
              <button
                class="chip"
                class:off={saved && !saved.enabled}
                disabled={!saved}
                title="{shade.name} · {hex(shade.rgb)}{saved ? ` · ${saved.enabled ? t('calib.shadeOn') : t('calib.shadeOff')}` : ''}"
                aria-pressed={saved?.enabled}
                onclick={() => toggleShade(ci, si)}
              >
                <span class="chip-swatch" style="background: {css(shade.rgb)}"></span>
                <span class="chip-label">{shade.name}</span>
              </button>
            {/each}
          </div>
        </div>
      {/each}
    </div>
  </section>

  {#if !app.paletteReady}
    <div class="notice">{t('palette.required')}</div>
  {/if}
</div>

<style>
  .page { gap: 1.2rem; }
  .small { font-size: .8rem; line-height: 1.4; }

  /* ---------- cabeçalho + progresso ---------- */
  .head { display: flex; gap: 1rem; align-items: flex-end; justify-content: space-between; flex-wrap: wrap; }
  .head h1 { margin: 0 0 .25rem; }
  .head p { margin: 0; max-width: 52ch; }
  .progress {
    display: grid; grid-template-columns: auto 1fr; gap: .1rem .5rem; align-items: baseline; min-width: 190px;
    padding: .6rem .8rem; border-radius: 12px; border: 1px solid var(--border); background: var(--surface);
  }
  .progress strong { font-size: 1.25rem; }
  .progress > span:not(.bar) { font-size: .78rem; color: var(--muted); }
  .bar { grid-column: 1 / -1; height: 6px; border-radius: 999px; background: var(--surface-2); overflow: hidden; margin-top: .3rem; }
  .bar span { display: block; height: 100%; border-radius: inherit; background: var(--accent); transition: width .3s ease; }
  .progress.complete .bar span { background: var(--ok); }
  .progress.complete strong { color: var(--ok); }

  /* ---------- seções ---------- */
  /* ---------- aviso antes do 1º passo ---------- */
  .heads-up {
    display: flex; align-items: center; gap: .9rem; padding: .9rem 1.1rem; border-radius: 14px;
    border: 1.5px solid color-mix(in srgb, var(--accent) 55%, var(--border));
    background:
      linear-gradient(90deg, color-mix(in srgb, var(--accent) 14%, transparent), transparent 70%),
      var(--surface);
    box-shadow: 0 6px 18px color-mix(in srgb, var(--accent) 12%, transparent);
  }
  .heads-up > div { display: flex; flex-direction: column; gap: .15rem; line-height: 1.45; }
  .heads-up span:not(.hu-icon) { font-size: .88rem; }
  .hu-icon {
    width: 42px; height: 42px; border-radius: 12px; display: grid; place-items: center; flex-shrink: 0;
    background: var(--accent); color: var(--accent-fg); box-shadow: 0 4px 12px color-mix(in srgb, var(--accent) 35%, transparent);
  }
  .hu-icon svg { width: 22px; height: 22px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }

  .section { display: flex; flex-direction: column; gap: .6rem; }
  .section h2 { display: flex; align-items: center; gap: .5rem; margin: 0; font-size: 1.02rem; }
  .section > p { margin: -.2rem 0 0; }
  .n {
    width: 24px; height: 24px; border-radius: 8px; display: grid; place-items: center; font-size: .8rem; font-weight: 800;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }

  .tile {
    display: flex; align-items: center; gap: .75rem; padding: .75rem .85rem; border-radius: 14px;
    border: 1px solid var(--border); background: var(--surface); transition: border-color .15s ease, background .15s ease;
  }
  .tile.ok { border-color: color-mix(in srgb, var(--ok) 45%, var(--border)); background: color-mix(in srgb, var(--ok) 5%, var(--surface)); }
  .tile.locked { opacity: .6; }
  .info { display: flex; flex-direction: column; gap: .15rem; min-width: 0; flex: 1; }
  .tile button { flex-shrink: 0; }

  .buttons { display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: .6rem; }
  /* Cartão em pé: ícone em cima, nome + dica no meio, botão largo embaixo. */
  .btn-tile { flex-direction: column; align-items: stretch; gap: .6rem; padding: 1rem .85rem .85rem; text-align: center; }
  .btn-tile .thumb { align-self: center; }
  .btn-tile .actions button { flex: 1; }
  .info.center { align-items: center; flex: none; }
  .thumb { width: 56px; height: 56px; object-fit: contain; border-radius: 14px; background: var(--surface-2); padding: 4px; flex-shrink: 0; }

  .captures { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: .6rem; }
  .tile.big { align-items: flex-start; flex-direction: column; gap: .7rem; }
  .actions { display: flex; align-items: center; gap: .6rem; margin-top: auto; }

  .status {
    width: 22px; height: 22px; border-radius: 999px; flex-shrink: 0; display: grid; place-items: center;
    border: 2px dashed var(--border);
  }
  .status.ok { border: none; background: var(--ok); color: #fff; }
  .status svg { width: 14px; height: 14px; fill: none; stroke: currentColor; stroke-width: 3; stroke-linecap: round; stroke-linejoin: round; }

  /* ---------- paleta ---------- */
  .palette { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: .6rem; }
  .color {
    display: flex; flex-direction: column; gap: .6rem; padding: .75rem; border-radius: 14px;
    border: 1px solid var(--border); background: var(--surface); transition: opacity .15s ease;
  }
  .color.off { opacity: .45; }
  .chead { display: flex; align-items: center; gap: .6rem; }
  .big-swatch { width: 34px; height: 34px; border-radius: 10px; flex-shrink: 0; box-shadow: inset 0 0 0 1px rgba(0, 0, 0, .12); }
  .cname { display: flex; flex-direction: column; flex: 1; min-width: 0; }

  .switch {
    position: relative; width: 38px; height: 22px; padding: 0; border-radius: 999px; flex-shrink: 0;
    border: none; background: var(--surface-2); cursor: pointer; transition: background .15s ease;
  }
  .switch span {
    position: absolute; top: 3px; left: 3px; width: 16px; height: 16px; border-radius: 999px;
    background: #fff; box-shadow: 0 1px 3px rgba(0, 0, 0, .3); transition: transform .15s ease;
  }
  .switch[aria-checked='true'] { background: var(--ok); }
  .switch[aria-checked='true'] span { transform: translateX(16px); }
  .switch[aria-checked='true']:hover:not(:disabled) { background: var(--ok); filter: brightness(1.06); }

  .shades { display: grid; grid-template-columns: repeat(5, 1fr); gap: .35rem; }
  .chip {
    display: flex; flex-direction: column; align-items: center; gap: .15rem; padding: .25rem 0 .2rem;
    border-radius: 9px; border: 1px solid transparent; background: transparent; font: inherit; cursor: pointer;
  }
  .chip:hover:not(:disabled) { border-color: var(--border); background: var(--surface-2); }
  .chip:disabled { cursor: default; opacity: 1; }
  .chip-swatch { position: relative; width: 26px; height: 26px; border-radius: 7px; box-shadow: inset 0 0 0 1px rgba(0, 0, 0, .12); overflow: hidden; }
  .chip-label { font-size: .68rem; color: var(--muted); }
  .chip.off .chip-swatch { opacity: .35; }
  .chip.off .chip-swatch::after {
    content: ''; position: absolute; inset: 0;
    background: linear-gradient(to top right, transparent calc(50% - 1.5px), var(--danger) calc(50% - 1.5px), var(--danger) calc(50% + 1.5px), transparent calc(50% + 1.5px));
  }
  .chip.off .chip-label { text-decoration: line-through; }

  /* ---------- aviso ---------- */
  .notice {
    padding: .75rem 1rem; border-radius: 12px; font-size: .86rem;
    border: 1px solid color-mix(in srgb, #f5a524 40%, transparent); background: color-mix(in srgb, #f5a524 9%, transparent);
  }
</style>
