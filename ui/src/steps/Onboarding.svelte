<script lang="ts">
  import { app } from '../lib/state.svelte';
  import { t } from '../lib/i18n.svelte';
  import HelpCard from '../lib/HelpCard.svelte';
  import { GAME_PALETTE } from '../lib/gamePalette';

  // Coração em pixel art pintado célula a célula com os tons do P2 (rosa do jogo):
  // mostra em 2 segundos o que o app faz. 1 = tom base, 2 = sombra, 3 = brilho.
  const HEART = [
    '.11...11.',
    '1311.1111',
    '131111112',
    '111111112',
    '.1111112.',
    '..11112..',
    '...112...',
    '....2....'
  ];
  const pink = GAME_PALETTE[1].shades;
  const tone: Record<string, (typeof pink)[number]> = { '1': pink[1], '2': pink[0], '3': pink[3] };
  const cells = HEART.flatMap((row, y) =>
    [...row].map((ch, x) => ({ x, y, rgb: tone[ch]?.rgb ?? null }))
  );
  let painted = 0;
  const heart = cells.map((c) => ({ ...c, order: c.rgb ? painted++ : 0 }));

  const steps = [
    { key: 'palette', icon: 'palette' },
    { key: 'image', icon: 'image' },
    { key: 'canvas', icon: 'frame' },
    { key: 'paint', icon: 'play' }
  ] as const;
</script>

{#snippet glyph(name: string)}
  <svg viewBox="0 0 24 24" aria-hidden="true">
    {#if name === 'palette'}
      <path d="M12 22a1 1 0 0 1 0-20 10 9 0 0 1 10 9 5 5 0 0 1-5 5h-2.25a1.75 1.75 0 0 0-1.4 2.8l.3.4a1.75 1.75 0 0 1-1.4 2.8z" />
      <circle cx="13.5" cy="6.5" r="1" class="dot" /><circle cx="17.5" cy="10.5" r="1" class="dot" />
      <circle cx="6.5" cy="12.5" r="1" class="dot" /><circle cx="8.5" cy="7.5" r="1" class="dot" />
    {:else if name === 'image'}
      <rect x="3" y="3" width="18" height="18" rx="2" /><circle cx="9" cy="9" r="2" />
      <path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21" />
    {:else if name === 'frame'}
      <path d="M3 7V5a2 2 0 0 1 2-2h2M17 3h2a2 2 0 0 1 2 2v2M21 17v2a2 2 0 0 1-2 2h-2M7 21H5a2 2 0 0 1-2-2v-2" />
      <rect x="7" y="7" width="10" height="10" rx="1" />
    {:else if name === 'play'}
      <path d="M6 3 20 12 6 21z" />
    {:else if name === 'heart'}
      <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z" />
    {:else if name === 'arrow'}
      <path d="M5 12h14M12 5l7 7-7 7" />
    {:else if name === 'shield'}
      <path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" />
      <path d="m9 12 2 2 4-4" />
    {/if}
  </svg>
{/snippet}

<div class="stack home">
  <section class="hero">
    <div class="pitch">
      <span class="badge">{t('home.badge')}</span>
      <h1>{t('home.title')}</h1>
      <p class="lead">{t('home.lead')}</p>
      <div class="ctas">
        <button class="primary cta" onclick={() => app.goTo('palette')}>
          {t('onboarding.start')}
          {@render glyph('arrow')}
        </button>
        <button class="support" onclick={() => (app.route = 'supporters')}>
          {@render glyph('heart')}
          {t('home.support')}
        </button>
      </div>
    </div>

    <div class="art" aria-hidden="true">
      <div class="pixels" style="--cols: {HEART[0].length}">
        {#each heart as c}
          {#if c.rgb}
            <span class="px" style="--c: rgb({c.rgb.r},{c.rgb.g},{c.rgb.b}); --i: {c.order}"></span>
          {:else}
            <span></span>
          {/if}
        {/each}
      </div>
    </div>
  </section>

  <section class="steps">
    <h2>{t('home.howTitle')}</h2>
    <ol>
      {#each steps as s, i}
        <li>
          <span class="icon">{@render glyph(s.icon)}</span>
          <span class="num">{i + 1}</span>
          <strong>{t(`home.step.${s.key}`)}</strong>
          <span class="muted">{t(`home.step.${s.key}.text`)}</span>
        </li>
      {/each}
    </ol>
  </section>

  <HelpCard />

  <div class="notice">
    <span class="nicon">{@render glyph('shield')}</span>
    <p>{t('onboarding.warn')}</p>
  </div>
</div>

<style>
  .home { gap: 1.1rem; }

  /* ---------- hero ---------- */
  .hero {
    position: relative; overflow: hidden; display: grid; grid-template-columns: 1fr auto; gap: 1.5rem;
    align-items: center; padding: 1.6rem; border-radius: 18px; border: 1px solid var(--border);
    background:
      radial-gradient(90% 120% at 0% 0%, color-mix(in srgb, var(--accent) 22%, transparent), transparent 60%),
      radial-gradient(70% 90% at 100% 100%, color-mix(in srgb, #ee6f72 20%, transparent), transparent 60%),
      var(--surface);
  }
  .badge {
    display: inline-block; font-size: .72rem; font-weight: 700; letter-spacing: .06em; text-transform: uppercase;
    padding: .25rem .6rem; border-radius: 999px; color: var(--accent);
    background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  h1 { margin: .7rem 0 .5rem; font-size: 1.75rem; line-height: 1.15; letter-spacing: -.02em; }
  .lead { margin: 0; color: var(--muted); font-size: .98rem; line-height: 1.5; max-width: 44ch; }
  .ctas { display: flex; flex-wrap: wrap; gap: .6rem; margin-top: 1.2rem; }
  .ctas button { display: inline-flex; align-items: center; gap: .45rem; padding: .65rem 1.1rem; font-weight: 600; border-radius: 10px; }
  .ctas svg { width: 18px; height: 18px; fill: none; stroke: currentColor; stroke-width: 2.2; stroke-linecap: round; stroke-linejoin: round; }
  .cta { box-shadow: 0 6px 18px color-mix(in srgb, var(--accent) 40%, transparent); }
  .cta:hover svg { transform: translateX(3px); }
  .cta svg { transition: transform .15s ease; }
  .support { color: #e0245e; border-color: color-mix(in srgb, #e0245e 45%, transparent); background: color-mix(in srgb, #e0245e 8%, transparent); }
  .support:hover:not(:disabled) { background: color-mix(in srgb, #e0245e 16%, transparent); }
  .support svg { fill: currentColor; stroke: none; }

  /* coração sendo "pintado" */
  .art { display: grid; place-items: center; padding: .6rem; }
  .pixels { display: grid; grid-template-columns: repeat(var(--cols), 14px); grid-auto-rows: 14px; gap: 2px; }
  .px {
    border-radius: 3px; background: var(--c); opacity: 0; transform: scale(.3);
    animation: paint .28s ease-out forwards; animation-delay: calc(var(--i) * 32ms + .2s);
  }
  @keyframes paint { to { opacity: 1; transform: scale(1); } }
  @media (prefers-reduced-motion: reduce) { .px { animation: none; opacity: 1; transform: none; } }

  /* ---------- passo a passo ---------- */
  .steps h2 { margin: 0 0 .6rem; font-size: 1.05rem; }
  .steps ol { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: .6rem; }
  .steps li {
    position: relative; display: flex; flex-direction: column; gap: .3rem; padding: .9rem;
    border-radius: 14px; border: 1px solid var(--border); background: var(--surface);
  }
  .steps li strong { font-size: .95rem; }
  .steps li .muted { font-size: .82rem; line-height: 1.4; }
  .icon {
    width: 38px; height: 38px; border-radius: 10px; display: grid; place-items: center; margin-bottom: .3rem;
    color: var(--accent); background: color-mix(in srgb, var(--accent) 14%, transparent);
  }
  .icon svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
  .icon svg :global(.dot) { fill: currentColor; stroke: none; }
  .num {
    position: absolute; top: .7rem; right: .8rem; font-size: 1.6rem; font-weight: 800; line-height: 1;
    color: color-mix(in srgb, var(--fg) 10%, transparent);
  }

  /* ---------- aviso ---------- */
  .notice {
    display: flex; gap: .7rem; align-items: flex-start; padding: .8rem 1rem; border-radius: 12px;
    border: 1px solid color-mix(in srgb, #f5a524 40%, transparent); background: color-mix(in srgb, #f5a524 9%, transparent);
  }
  .notice p { margin: 0; font-size: .84rem; line-height: 1.45; }
  .nicon { flex-shrink: 0; color: #d98a00; }
  .nicon svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }

  @media (max-width: 640px) {
    .hero { grid-template-columns: 1fr; }
    .art { display: none; }
  }
</style>
