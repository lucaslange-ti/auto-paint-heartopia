<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import { bridge } from '../lib/bridge';
  import { Requests } from '../lib/contracts';
  import type { CanvasAspect, CanvasPrecision, FitMode, ResampleMode, PlanConversion } from '../lib/contracts';

  // Formato desenhado de cada proporção (largura x altura em px do ícone).
  const aspects: { value: CanvasAspect; label: string; w: number; h: number }[] = [
    { value: 'OneToOne', label: '1:1', w: 22, h: 22 },
    { value: 'SixteenNine', label: '16:9', w: 32, h: 18 },
    { value: 'NineSixteen', label: '9:16', w: 18, h: 32 },
    { value: 'FourThree', label: '4:3', w: 28, h: 21 },
    { value: 'ThreeFour', label: '3:4', w: 21, h: 28 }
  ];
  const sizes: CanvasPrecision[] = ['Small', 'Medium', 'Big', 'SuperLarge'];
  const fits: FitMode[] = ['Contain', 'Fill'];
  const resamples: ResampleMode[] = ['Lanczos', 'NearestNeighbor'];

  let fit = $state<FitMode>('Contain');
  let resample = $state<ResampleMode>('Lanczos');
  let error = $state<string | null>(null);
  let fileName = $state<string | null>(null);
  let dragging = $state(false);
  let previewCanvas: HTMLCanvasElement | undefined = $state();

  const aspect = $derived(app.config?.canvasAspect ?? 'OneToOne');
  const precision = $derived(app.config?.precisionByAspect?.[aspect] ?? 'Small');
  const usage = $derived([...(app.conversion?.usage ?? [])].sort((a, b) => b.count - a.count));

  function setAspect(value: CanvasAspect) {
    if (!app.config) return;
    app.config.canvasAspect = value;
    app.saveConfig();
  }
  function setPrecision(value: CanvasPrecision) {
    if (!app.config) return;
    app.config.precisionByAspect = { ...app.config.precisionByAspect, [aspect]: value };
    app.saveConfig();
  }

  function loadFile(file: File | undefined) {
    if (!file || !file.type.startsWith('image/')) return;
    fileName = file.name;
    const reader = new FileReader();
    reader.onload = () => {
      app.imageDataUrl = reader.result as string;
      app.conversion = null; // imagem nova invalida a conversão anterior
    };
    reader.readAsDataURL(file);
  }

  function onFile(e: Event) {
    const input = e.target as HTMLInputElement;
    loadFile(input.files?.[0]);
    input.value = ''; // permite escolher o mesmo arquivo de novo
  }

  function onDrop(e: DragEvent) {
    e.preventDefault();
    dragging = false;
    loadFile(e.dataTransfer?.files?.[0]);
  }

  async function convert() {
    if (!app.imageDataUrl) return;
    error = null;
    app.busy = true;
    try {
      app.conversion = await bridge.request<PlanConversion>(Requests.ImageConvert, {
        aspect,
        precision,
        fit,
        resample,
        imageBase64: app.imageDataUrl
      });
    } catch (e) {
      error = (e as Error).message;
    } finally {
      app.busy = false;
    }
  }

  $effect(() => {
    const conv = app.conversion;
    const cv = previewCanvas;
    if (!conv || !cv) return;
    cv.width = conv.columns;
    cv.height = conv.rows;
    const ctx = cv.getContext('2d');
    if (!ctx) return;
    const img = ctx.createImageData(conv.columns, conv.rows);
    for (let i = 0; i < conv.cells.length; i++) {
      const u = conv.usage[conv.cells[i]];
      img.data[i * 4 + 0] = u.rgb.r;
      img.data[i * 4 + 1] = u.rgb.g;
      img.data[i * 4 + 2] = u.rgb.b;
      img.data[i * 4 + 3] = 255;
    }
    ctx.putImageData(img, 0, 0);
  });
</script>

<div class="stack page">
  <header class="head">
    <h1>{t('step.image')}</h1>
    <p class="muted">{t('image.lead')}</p>
  </header>

  {#if !app.hasPalette}
    <div class="notice">{t('image.needPalette')}</div>
  {/if}

  <!-- 1. Quadro do jogo -->
  <section class="section">
    <h2><span class="n">1</span>{t('image.frameTitle')}</h2>
    <div class="panel">
      <div class="field">
        <div class="field-head">
          <strong>{t('image.aspect')}</strong>
          <span class="muted small">{t('image.aspectHelp')}</span>
        </div>
        <div class="options">
          {#each aspects as a}
            <button class="opt aspect" class:on={aspect === a.value} onclick={() => setAspect(a.value)} aria-pressed={aspect === a.value}>
              <span class="shape" style="width: {a.w}px; height: {a.h}px"></span>
              <span>{a.label}</span>
            </button>
          {/each}
        </div>
      </div>

      <div class="field">
        <div class="field-head">
          <strong>{t('image.size')}</strong>
          <span class="muted small">{t('image.sizeHelp')}</span>
        </div>
        <div class="segmented">
          {#each sizes as s}
            <button class:on={precision === s} onclick={() => setPrecision(s)} aria-pressed={precision === s}>
              {t(`size.${s}`)}
            </button>
          {/each}
        </div>
      </div>
    </div>
  </section>

  <!-- 2. Imagem -->
  <section class="section">
    <h2><span class="n">2</span>{t('image.pickTitle')}</h2>
    <label
      class="drop"
      class:dragging
      class:has={!!app.imageDataUrl}
      ondragover={(e) => { e.preventDefault(); dragging = true; }}
      ondragleave={() => (dragging = false)}
      ondrop={onDrop}
    >
      <input type="file" accept="image/*" onchange={onFile} />
      {#if app.imageDataUrl}
        <img class="thumb" src={app.imageDataUrl} alt="" />
        <span class="drop-text">
          <strong>{fileName ?? t('image.chosen')}</strong>
          <span class="muted small">{t('image.change')}</span>
        </span>
      {:else}
        <span class="drop-icon">
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12" />
          </svg>
        </span>
        <span class="drop-text">
          <strong>{t('image.dropTitle')}</strong>
          <span class="muted small">{t('image.dropHint')}</span>
        </span>
      {/if}
    </label>
  </section>

  <!-- 3. Ajustes -->
  <section class="section">
    <h2><span class="n">3</span>{t('image.adjustTitle')}</h2>
    <div class="panel two">
      <div class="field">
        <strong>{t('image.fit')}</strong>
        <div class="cards">
          {#each fits as f}
            <button class="opt card-opt" class:on={fit === f} onclick={() => (fit = f)} aria-pressed={fit === f}>
              <strong>{t(`fit.${f}`)}</strong>
              <span class="muted small">{t(`fit.${f}.help`)}</span>
            </button>
          {/each}
        </div>
      </div>
      <div class="field">
        <strong>{t('image.resample')}</strong>
        <div class="cards">
          {#each resamples as r}
            <button class="opt card-opt" class:on={resample === r} onclick={() => (resample = r)} aria-pressed={resample === r}>
              <strong>{t(`resample.${r}`)}</strong>
              <span class="muted small">{t(`resample.${r}.help`)}</span>
            </button>
          {/each}
        </div>
      </div>
    </div>
  </section>

  <div class="convert-row">
    <button class="primary convert" onclick={convert} disabled={!app.imageDataUrl || !app.hasPalette || app.busy}>
      <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 3-1.9 5.8a2 2 0 0 1-1.3 1.3L3 12l5.8 1.9a2 2 0 0 1 1.3 1.3L12 21l1.9-5.8a2 2 0 0 1 1.3-1.3L21 12l-5.8-1.9a2 2 0 0 1-1.3-1.3z" /></svg>
      {app.conversion ? t('image.reconvert') : t('image.convert')}
    </button>
    {#if !app.imageDataUrl}<span class="muted small">{t('image.needImage')}</span>{/if}
  </div>

  {#if error}<div class="notice danger">{error}</div>{/if}

  <!-- Resultado -->
  {#if app.conversion}
    <section class="section">
      <h2>{t('image.resultTitle')}</h2>
      <div class="result">
        <div class="compare">
          {#if app.imageDataUrl}
            <figure><img src={app.imageDataUrl} alt="original" /><figcaption>{t('image.original')}</figcaption></figure>
          {/if}
          <span class="arrow" aria-hidden="true">→</span>
          <figure>
            <canvas bind:this={previewCanvas} class="pixelated"></canvas>
            <figcaption>{t('image.inGame')}</figcaption>
          </figure>
        </div>

        <div class="stats">
          <div><span class="muted small">{t('image.grid')}</span><strong>{app.conversion.columns}×{app.conversion.rows}</strong></div>
          <div><span class="muted small">{t('image.cells')}</span><strong>{app.conversion.cellCount.toLocaleString()}</strong></div>
          <div><span class="muted small">{t('image.colors')}</span><strong>{app.conversion.usage.length}</strong></div>
        </div>

        <div class="usage">
          {#each usage as u}
            <span class="chip" title="{u.name} · {u.count}">
              <span class="sw" style="background: rgb({u.rgb.r},{u.rgb.g},{u.rgb.b})"></span>
              <span>{u.name}</span>
              <span class="muted">{u.count}</span>
            </span>
          {/each}
        </div>
      </div>
    </section>
  {/if}
</div>

<style>
  .page { gap: 1.2rem; }
  .small { font-size: .8rem; line-height: 1.4; }
  .head h1 { margin: 0 0 .25rem; }
  .head p { margin: 0; }

  .section { display: flex; flex-direction: column; gap: .6rem; }
  .section h2 { display: flex; align-items: center; gap: .5rem; margin: 0; font-size: 1.02rem; }
  .n {
    width: 24px; height: 24px; border-radius: 8px; display: grid; place-items: center; font-size: .8rem; font-weight: 800;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  .panel {
    display: flex; flex-direction: column; gap: 1.1rem; padding: 1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .panel.two { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); }
  .field { display: flex; flex-direction: column; gap: .5rem; }
  .field-head { display: flex; flex-direction: column; gap: .1rem; }

  /* opções selecionáveis */
  .opt {
    border: 1.5px solid var(--border); background: var(--surface); border-radius: 12px; font: inherit; cursor: pointer;
    transition: border-color .15s ease, background .15s ease, box-shadow .15s ease;
  }
  .opt:hover:not(:disabled) { border-color: color-mix(in srgb, var(--accent) 50%, var(--border)); background: var(--surface); }
  .opt.on {
    border-color: var(--accent); background: color-mix(in srgb, var(--accent) 9%, var(--surface));
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 16%, transparent);
  }

  .options { display: flex; flex-wrap: wrap; gap: .5rem; }
  .aspect { display: flex; flex-direction: column; align-items: center; justify-content: flex-end; gap: .4rem; width: 72px; height: 72px; padding: .5rem; font-weight: 600; font-size: .82rem; }
  .shape { border-radius: 4px; border: 2px solid var(--muted); }
  .aspect.on .shape { border-color: var(--accent); background: color-mix(in srgb, var(--accent) 22%, transparent); }

  .segmented { display: inline-flex; flex-wrap: wrap; gap: .25rem; padding: .25rem; border-radius: 12px; background: var(--surface-2); align-self: flex-start; }
  .segmented button { border: none; background: transparent; border-radius: 9px; padding: .45rem .9rem; font-weight: 500; color: var(--muted); }
  .segmented button:hover:not(:disabled) { background: color-mix(in srgb, var(--surface) 70%, transparent); color: var(--fg); }
  .segmented button.on { background: var(--surface); color: var(--fg); font-weight: 600; box-shadow: 0 1px 4px rgba(0, 0, 0, .12); }

  .cards { display: grid; grid-template-columns: 1fr 1fr; gap: .5rem; }
  .card-opt { display: flex; flex-direction: column; align-items: flex-start; gap: .2rem; padding: .7rem .8rem; text-align: left; }

  /* área de soltar a imagem */
  .drop {
    position: relative; display: flex; align-items: center; justify-content: center; gap: 1rem; min-height: 120px; padding: 1.2rem;
    border: 2px dashed var(--border); border-radius: 16px; background: var(--surface); cursor: pointer; margin: 0;
    transition: border-color .15s ease, background .15s ease;
  }
  .drop:hover, .drop.dragging { border-color: var(--accent); background: color-mix(in srgb, var(--accent) 6%, var(--surface)); }
  .drop.has { justify-content: flex-start; border-style: solid; }
  .drop input { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
  .drop-icon {
    width: 52px; height: 52px; border-radius: 14px; display: grid; place-items: center;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  .drop-icon svg { width: 26px; height: 26px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
  .drop-text { display: flex; flex-direction: column; gap: .15rem; }
  .thumb { width: 84px; height: 84px; object-fit: cover; border-radius: 12px; border: 1px solid var(--border); }

  /* converter */
  .convert-row { display: flex; align-items: center; gap: .8rem; flex-wrap: wrap; }
  .convert {
    display: inline-flex; align-items: center; gap: .5rem; padding: .7rem 1.4rem; font-weight: 600; border-radius: 12px;
    box-shadow: 0 6px 18px color-mix(in srgb, var(--accent) 35%, transparent);
  }
  .convert:disabled { box-shadow: none; }
  .convert svg { width: 18px; height: 18px; fill: currentColor; }

  /* resultado */
  .result {
    display: flex; flex-direction: column; gap: 1rem; padding: 1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .compare { display: flex; align-items: center; justify-content: center; gap: 1rem; flex-wrap: wrap; }
  .arrow { font-size: 1.4rem; color: var(--muted); }
  figure { margin: 0; display: flex; flex-direction: column; align-items: center; gap: .4rem; }
  figure img, figure canvas {
    max-width: 300px; max-height: 300px; width: auto; border: 1px solid var(--border); border-radius: 12px; background: #fff;
  }
  figcaption { font-size: .78rem; font-weight: 600; color: var(--muted); text-transform: uppercase; letter-spacing: .06em; }
  .pixelated { image-rendering: pixelated; min-width: 200px; }

  .stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: .5rem; }
  .stats div { display: flex; flex-direction: column; gap: .1rem; padding: .6rem .8rem; border-radius: 12px; background: var(--surface-2); }
  .stats strong { font-size: 1.1rem; }

  .usage { display: flex; flex-wrap: wrap; gap: .35rem; }
  .chip {
    display: inline-flex; align-items: center; gap: .35rem; padding: .25rem .6rem .25rem .3rem; border-radius: 999px;
    background: var(--surface-2); font-size: .8rem;
  }
  .sw { width: 18px; height: 18px; border-radius: 999px; box-shadow: inset 0 0 0 1px rgba(0, 0, 0, .15); }

  .notice {
    padding: .75rem 1rem; border-radius: 12px; font-size: .86rem;
    border: 1px solid color-mix(in srgb, #f5a524 40%, transparent); background: color-mix(in srgb, #f5a524 9%, transparent);
  }
  .notice.danger { border-color: color-mix(in srgb, var(--danger) 45%, transparent); background: color-mix(in srgb, var(--danger) 8%, transparent); color: var(--danger); }
</style>
