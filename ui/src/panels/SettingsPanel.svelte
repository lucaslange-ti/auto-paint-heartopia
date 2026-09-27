<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { i18n, t } from '../lib/i18n.svelte';
  import { bridge } from '../lib/bridge';
  import { Requests } from '../lib/contracts';
  import type { FontSize, HotkeyDto, OverlayCorner } from '../lib/contracts';
  import { DEFAULT_HOTKEYS, hotkeyParts, isFunctionKey, sameHotkey, vkFromCode } from '../lib/hotkeys';

  type HotkeyName = 'abort' | 'pauseResume';

  // Captura de atalho: enquanto `capturing` está setado, a próxima combinação
  // pressionada vira o novo atalho (Esc cancela).
  let capturing = $state<HotkeyName | null>(null);
  let preview = $state<string[]>([]);
  let captureError = $state<string | null>(null);

  function startCapture(which: HotkeyName) {
    capturing = which;
    preview = [];
    captureError = null;
  }

  function onCaptureKey(e: KeyboardEvent) {
    if (!capturing || !app.config) return;
    e.preventDefault();
    e.stopPropagation();

    if (e.code === 'Escape') {
      capturing = null;
      return;
    }

    const mods = [...(e.ctrlKey ? ['Ctrl'] : []), ...(e.altKey ? ['Alt'] : []), ...(e.shiftKey ? ['Shift'] : [])];
    const vk = vkFromCode(e.code);
    if (vk === null) {
      // Só modificador (ou tecla não suportada): mostra o que já está pressionado.
      preview = [...mods, '…'];
      return;
    }

    const next: HotkeyDto = { ctrl: e.ctrlKey, alt: e.altKey, shift: e.shiftKey, key: vk };
    const other = app.config.hotkeys[capturing === 'abort' ? 'pauseResume' : 'abort'];
    if (!next.ctrl && !next.alt && !next.shift && !isFunctionKey(vk)) {
      captureError = t('hotkeys.needModifier');
      preview = hotkeyParts(next, t('paint.keySpace'));
      return;
    }
    if (sameHotkey(next, other)) {
      captureError = t('hotkeys.duplicate');
      preview = hotkeyParts(next, t('paint.keySpace'));
      return;
    }

    app.config.hotkeys[capturing] = next;
    app.saveConfig();
    capturing = null;
    app.flash(t('hotkeys.saved'));
  }

  function resetHotkeys() {
    if (!app.config) return;
    app.config.hotkeys = { abort: { ...DEFAULT_HOTKEYS.abort }, pauseResume: { ...DEFAULT_HOTKEYS.pauseResume } };
    app.saveConfig();
    capturing = null;
  }

  const corners: OverlayCorner[] = ['TopLeft', 'TopRight', 'BottomLeft', 'BottomRight'];

  function toggleOverlay() {
    if (!app.config) return;
    app.config.statusOverlayEnabled = !app.config.statusOverlayEnabled;
    app.saveConfig();
  }

  function setCorner(c: OverlayCorner) {
    if (!app.config) return;
    app.config.statusOverlayCorner = c;
    app.saveConfig();
  }

  const hotkeyNames: HotkeyName[] = ['abort', 'pauseResume'];
  const isDefaultHotkeys = $derived(
    !!app.config &&
      sameHotkey(app.config.hotkeys.abort, DEFAULT_HOTKEYS.abort) &&
      sameHotkey(app.config.hotkeys.pauseResume, DEFAULT_HOTKEYS.pauseResume)
  );

  const langNames: Record<string, { name: string; sub: string }> = {
    'pt-BR': { name: 'Português', sub: 'Brasil' },
    'en-US': { name: 'English', sub: 'United States' }
  };

  const fontSizes: { value: FontSize; px: number }[] = [
    { value: 'small', px: 13 },
    { value: 'medium', px: 17 },
    { value: 'large', px: 21 },
    { value: 'xlarge', px: 25 }
  ];

  let confirming = $state(false);

  function setLang(lang: string) {
    i18n.set(lang);
    if (app.config) {
      app.config.language = lang;
      app.saveConfig();
    }
  }

  function setFontSize(size: FontSize) {
    if (!app.config) return;
    app.config.fontSize = size;
    app.applyFontSize();
    app.saveConfig();
  }

  function toggleUpdates() {
    if (!app.config) return;
    app.config.checkForUpdates = !app.config.checkForUpdates;
    app.saveConfig();
  }

  async function reset() {
    confirming = false;
    await bridge.request(Requests.ConfigReset);
    app.config = await bridge.request(Requests.ConfigGet);
    i18n.set(app.config?.language ?? 'pt-BR');
    app.applyFontSize();
    app.flash(t('settings.resetDone'));
  }
</script>

{#snippet glyph(name: string)}
  <svg viewBox="0 0 24 24" aria-hidden="true">
    {#if name === 'globe'}
      <circle cx="12" cy="12" r="10" /><path d="M2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
    {:else if name === 'text'}
      <path d="M4 7V4h16v3M9 20h6M12 4v16" />
    {:else if name === 'overlay'}
      <rect x="2" y="4" width="20" height="14" rx="2" /><rect x="4.5" y="6.5" width="7" height="4" rx="1" /><path d="M8 21h8" />
    {:else if name === 'keyboard'}
      <rect x="2" y="5" width="20" height="14" rx="2" /><path d="M6 9h.01M10 9h.01M14 9h.01M18 9h.01M6 13h.01M18 13h.01M9 15h6" />
    {:else if name === 'refresh'}
      <path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8" />
      <path d="M21 3v5h-5M3 21v-5h5" />
    {:else if name === 'trash'}
      <path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
    {:else if name === 'check'}
      <path d="M20 6 9 17l-5-5" />
    {/if}
  </svg>
{/snippet}

<svelte:window onkeydown={onCaptureKey} />

<div class="stack page">
  <header>
    <h1>{t('nav.settings')}</h1>
    <p class="muted">{t('settings.lead')}</p>
  </header>

  {#if app.config}
    <!-- Idioma -->
    <section class="group">
      <div class="group-head">
        <span class="icon">{@render glyph('globe')}</span>
        <div>
          <strong>{t('settings.language')}</strong>
          <span class="muted small">{t('settings.languageHelp')}</span>
        </div>
      </div>
      <div class="langs">
        {#each i18n.languages as l}
          <button class="lang" class:on={i18n.lang === l} onclick={() => setLang(l)} aria-pressed={i18n.lang === l}>
            <span class="code">{l.slice(0, 2).toUpperCase()}</span>
            <span class="lang-text">
              <strong>{langNames[l]?.name ?? l}</strong>
              <span class="muted small">{langNames[l]?.sub ?? ''}</span>
            </span>
            {#if i18n.lang === l}<span class="tick">{@render glyph('check')}</span>{/if}
          </button>
        {/each}
      </div>
    </section>

    <!-- Tamanho do texto -->
    <section class="group">
      <div class="group-head">
        <span class="icon">{@render glyph('text')}</span>
        <div>
          <strong>{t('settings.fontSize')}</strong>
          <span class="muted small">{t('settings.fontSizeHelp')}</span>
        </div>
      </div>
      <div class="sizes">
        {#each fontSizes as f}
          {@const on = (app.config.fontSize ?? 'medium') === f.value}
          <button class="size" class:on onclick={() => setFontSize(f.value)} aria-pressed={on}>
            <span class="aa" style="font-size: {f.px}px">Aa</span>
            <span>{t(`settings.fontSize.${f.value}`)}</span>
          </button>
        {/each}
      </div>
    </section>

    <!-- Card sobreposto ao jogo -->
    <section class="group">
      <div class="group-head spread">
        <span class="icon">{@render glyph('overlay')}</span>
        <div>
          <strong>{t('overlay.title')}</strong>
          <span class="muted small">{t('overlay.help')}</span>
        </div>
        <button
          class="switch"
          role="switch"
          aria-checked={app.config.statusOverlayEnabled}
          aria-label={t('overlay.title')}
          onclick={toggleOverlay}
        ><span></span></button>
      </div>

      <div class="corner-pick" class:disabled={!app.config.statusOverlayEnabled}>
        <span class="muted small">{t('overlay.corner')}</span>
        <div class="screen">
          {#each corners as c}
            <button
              class="corner {c}"
              class:on={app.config.statusOverlayCorner === c}
              onclick={() => setCorner(c)}
              disabled={!app.config.statusOverlayEnabled}
              aria-pressed={app.config.statusOverlayCorner === c}
              title={t(`overlay.${c}`)}
            >
              <span class="mini">
                <span class="mini-bar"></span>
                <span class="mini-line"></span>
              </span>
            </button>
          {/each}
          <span class="screen-label">{t(`overlay.${app.config.statusOverlayCorner}`)}</span>
        </div>
      </div>
    </section>

    <!-- Atalhos -->
    <section class="group">
      <div class="group-head">
        <span class="icon">{@render glyph('keyboard')}</span>
        <div>
          <strong>{t('hotkeys.title')}</strong>
          <span class="muted small">{t('hotkeys.help')}</span>
        </div>
      </div>
      <div class="hotkeys">
        {#each hotkeyNames as name}
          <div class="hk" class:listening={capturing === name}>
            <span class="hk-name">{t(`hotkeys.${name}`)}</span>
            <span class="keys">
              {#if capturing === name}
                {#if preview.length}
                  {#each preview as k, i}{#if i > 0}<span class="plus">+</span>{/if}<kbd>{k}</kbd>{/each}
                {:else}
                  <span class="listen">{t('hotkeys.pressNow')}</span>
                {/if}
              {:else}
                {#each hotkeyParts(app.config.hotkeys[name], t('paint.keySpace')) as k, i}{#if i > 0}<span class="plus">+</span>{/if}<kbd>{k}</kbd>{/each}
              {/if}
            </span>
            {#if capturing === name}
              <button onclick={() => (capturing = null)}>{t('settings.cancel')}</button>
            {:else}
              <button onclick={() => startCapture(name)} disabled={capturing !== null}>{t('hotkeys.change')}</button>
            {/if}
          </div>
          {#if capturing === name}
            <p class="hk-hint small" class:err={!!captureError}>{captureError ?? t('hotkeys.captureHint')}</p>
          {/if}
        {/each}
      </div>
      {#if !isDefaultHotkeys}
        <button class="linkish" onclick={resetHotkeys}>{t('hotkeys.reset')}</button>
      {/if}
    </section>

    <!-- Atualizações -->
    <section class="group row-group">
      <div class="group-head">
        <span class="icon">{@render glyph('refresh')}</span>
        <div>
          <strong>{t('settings.updates')}</strong>
          <span class="muted small">{t('settings.updatesHelp')}</span>
        </div>
      </div>
      <button
        class="switch"
        role="switch"
        aria-checked={app.config.checkForUpdates}
        aria-label={t('settings.updates')}
        onclick={toggleUpdates}
      ><span></span></button>
    </section>

    <!-- Zona de perigo -->
    <section class="group danger-zone">
      <div class="group-head">
        <span class="icon red">{@render glyph('trash')}</span>
        <div>
          <strong>{t('settings.reset')}</strong>
          <span class="muted small">{t('settings.resetHelp')}</span>
        </div>
      </div>
      {#if confirming}
        <div class="confirm">
          <span>{t('settings.resetConfirm')}</span>
          <div class="confirm-actions">
            <button onclick={() => (confirming = false)}>{t('settings.cancel')}</button>
            <button class="danger-solid" onclick={reset}>{t('settings.resetYes')}</button>
          </div>
        </div>
      {:else}
        <button class="danger" onclick={() => (confirming = true)}>{t('settings.reset')}</button>
      {/if}
    </section>
  {/if}
</div>

<style>
  .page { gap: 1rem; }
  .small { font-size: .8rem; line-height: 1.45; }
  header h1 { margin: 0 0 .25rem; }
  header p { margin: 0; }
  svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; flex-shrink: 0; }

  .group {
    display: flex; flex-direction: column; gap: .9rem; padding: 1.1rem; border-radius: 16px;
    border: 1px solid var(--border); background: var(--surface);
  }
  .row-group { flex-direction: row; align-items: center; justify-content: space-between; gap: 1rem; }
  .group-head { display: flex; align-items: center; gap: .8rem; }
  .group-head > div { display: flex; flex-direction: column; gap: .1rem; }
  .icon {
    width: 40px; height: 40px; border-radius: 11px; display: grid; place-items: center; flex-shrink: 0;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  .icon.red { color: var(--danger); background: color-mix(in srgb, var(--danger) 12%, transparent); }

  /* idioma */
  .langs { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: .5rem; }
  .lang {
    display: flex; align-items: center; gap: .7rem; padding: .7rem .8rem; text-align: left; font: inherit;
    border-radius: 12px; border: 1.5px solid var(--border); background: var(--surface); cursor: pointer;
    transition: border-color .15s ease, background .15s ease, box-shadow .15s ease;
  }
  .lang:hover:not(:disabled) { border-color: color-mix(in srgb, var(--accent) 50%, var(--border)); background: var(--surface); }
  .lang.on {
    border-color: var(--accent); background: color-mix(in srgb, var(--accent) 8%, var(--surface));
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 15%, transparent);
  }
  .code {
    width: 36px; height: 36px; border-radius: 10px; display: grid; place-items: center; flex-shrink: 0;
    font-size: .8rem; font-weight: 800; letter-spacing: .04em; background: var(--surface-2);
  }
  .lang.on .code { background: var(--accent); color: var(--accent-fg); }
  .lang-text { display: flex; flex-direction: column; flex: 1; }
  .tick { width: 22px; height: 22px; border-radius: 999px; display: grid; place-items: center; background: var(--accent); color: var(--accent-fg); }
  .tick svg { width: 13px; height: 13px; stroke-width: 3.5; }

  /* tamanho do texto */
  .sizes { display: grid; grid-template-columns: repeat(auto-fit, minmax(110px, 1fr)); gap: .5rem; }
  .size {
    display: flex; flex-direction: column; align-items: center; justify-content: flex-end; gap: .35rem; padding: .8rem .5rem .6rem;
    font: inherit; border-radius: 12px; border: 1.5px solid var(--border); background: var(--surface); cursor: pointer;
    transition: border-color .15s ease, background .15s ease, box-shadow .15s ease;
  }
  .size:hover:not(:disabled) { border-color: color-mix(in srgb, var(--accent) 50%, var(--border)); background: var(--surface); }
  .size.on {
    border-color: var(--accent); background: color-mix(in srgb, var(--accent) 8%, var(--surface));
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 15%, transparent);
  }
  .aa { font-weight: 700; line-height: 1; height: 28px; display: flex; align-items: flex-end; }
  .size.on .aa { color: var(--accent); }

  /* card sobreposto */
  .group-head.spread > div { flex: 1; }
  .corner-pick { display: flex; flex-direction: column; gap: .5rem; transition: opacity .15s ease; }
  .corner-pick.disabled { opacity: .45; }
  .screen {
    position: relative; width: min(100%, 340px); aspect-ratio: 16 / 9; border-radius: 12px; padding: 8px;
    display: grid; grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr; gap: 8px;
    background:
      linear-gradient(135deg, color-mix(in srgb, #41b97b 22%, transparent), color-mix(in srgb, #2b83c1 22%, transparent)),
      var(--surface-2);
    border: 6px solid color-mix(in srgb, var(--fg) 75%, transparent);
  }
  .corner {
    display: flex; padding: 4px; border-radius: 8px; border: 1.5px dashed transparent; background: transparent; cursor: pointer;
  }
  .corner:hover:not(:disabled) { border-color: color-mix(in srgb, var(--accent) 60%, transparent); background: color-mix(in srgb, var(--accent) 8%, transparent); }
  .TopLeft { justify-content: flex-start; align-items: flex-start; }
  .TopRight { justify-content: flex-end; align-items: flex-start; }
  .BottomLeft { justify-content: flex-start; align-items: flex-end; }
  .BottomRight { justify-content: flex-end; align-items: flex-end; }
  .mini {
    display: flex; flex-direction: column; gap: 4px; width: 56px; padding: 6px; border-radius: 6px;
    background: color-mix(in srgb, var(--fg) 18%, transparent); transition: all .15s ease;
  }
  .mini-bar { height: 4px; border-radius: 2px; background: color-mix(in srgb, var(--fg) 30%, transparent); }
  .mini-line { height: 3px; width: 70%; border-radius: 2px; background: color-mix(in srgb, var(--fg) 22%, transparent); }
  .corner.on .mini { background: #16161e; box-shadow: 0 4px 12px rgba(0, 0, 0, .35); outline: 2px solid var(--accent); }
  .corner.on .mini-bar { background: linear-gradient(90deg, var(--accent), #e0245e); }
  .corner.on .mini-line { background: rgba(255, 255, 255, .5); }
  .screen-label {
    position: absolute; left: 50%; top: 50%; transform: translate(-50%, -50%); pointer-events: none;
    font-size: .78rem; font-weight: 600; padding: .2rem .6rem; border-radius: 999px;
    background: var(--surface); color: var(--fg); box-shadow: 0 2px 8px rgba(0, 0, 0, .15); white-space: nowrap;
  }

  /* atalhos */
  .hotkeys { display: flex; flex-direction: column; gap: .5rem; }
  .hk {
    display: flex; align-items: center; gap: .8rem; padding: .6rem .8rem; border-radius: 12px;
    background: var(--surface-2); border: 1.5px solid transparent; transition: border-color .15s ease, background .15s ease;
  }
  .hk.listening {
    border-color: var(--accent); background: color-mix(in srgb, var(--accent) 8%, var(--surface));
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 15%, transparent);
  }
  .hk-name { min-width: 130px; font-weight: 600; }
  .keys { flex: 1; display: flex; align-items: center; flex-wrap: wrap; gap: .25rem; min-height: 28px; }
  .plus { color: var(--muted); font-size: .8rem; }
  kbd {
    display: inline-block; min-width: 1.9em; padding: .12rem .5rem; border-radius: 7px; text-align: center;
    font: 700 .8rem/1.5 inherit; color: var(--fg); background: var(--surface); border: 1px solid var(--border);
    box-shadow: 0 2px 0 var(--border);
  }
  .listen { color: var(--accent); font-weight: 600; font-size: .86rem; animation: blink 1.2s ease-in-out infinite; }
  @keyframes blink { 50% { opacity: .45; } }
  @media (prefers-reduced-motion: reduce) { .listen { animation: none; } }
  .hk-hint { margin: -.2rem 0 0 .8rem; color: var(--muted); }
  .hk-hint.err { color: var(--danger); }
  .linkish {
    align-self: flex-start; border: none; background: transparent; padding: 0; color: var(--accent);
    font-size: .84rem; font-weight: 600; cursor: pointer;
  }
  .linkish:hover:not(:disabled) { background: transparent; text-decoration: underline; }

  /* interruptor */
  .switch {
    position: relative; width: 46px; height: 26px; padding: 0; border-radius: 999px; flex-shrink: 0;
    border: none; background: var(--surface-2); cursor: pointer; transition: background .15s ease;
    box-shadow: inset 0 0 0 1px var(--border);
  }
  .switch span {
    position: absolute; top: 3px; left: 3px; width: 20px; height: 20px; border-radius: 999px;
    background: #fff; box-shadow: 0 1px 3px rgba(0, 0, 0, .3); transition: transform .18s ease;
  }
  .switch[aria-checked='true'] { background: var(--ok); box-shadow: none; }
  .switch[aria-checked='true'] span { transform: translateX(20px); }
  .switch[aria-checked='true']:hover:not(:disabled) { background: var(--ok); filter: brightness(1.06); }

  /* zona de perigo */
  .danger-zone { border-color: color-mix(in srgb, var(--danger) 30%, var(--border)); }
  .danger-zone > button { align-self: flex-start; }
  .confirm {
    display: flex; align-items: center; justify-content: space-between; gap: .8rem; flex-wrap: wrap; padding: .75rem .9rem;
    border-radius: 12px; font-size: .88rem; background: color-mix(in srgb, var(--danger) 8%, var(--surface));
    border: 1px solid color-mix(in srgb, var(--danger) 35%, transparent);
  }
  .confirm-actions { display: flex; gap: .5rem; }
  .danger-solid { background: var(--danger); border-color: var(--danger); color: #fff; font-weight: 600; }
  .danger-solid:hover:not(:disabled) { background: var(--danger); filter: brightness(1.08); }
</style>
