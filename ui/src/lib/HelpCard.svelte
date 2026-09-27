<script lang="ts">
  import { app } from './state.svelte';
  import { t } from './i18n.svelte';
  import { copyText } from './clipboard';
  import { LINKS } from './links';
  import { BRAND_ICONS } from './brandIcons';

  const ICONS = BRAND_ICONS;

  async function copyNick() {
    const ok = await copyText(LINKS.discordNick);
    app.flash(t(ok ? 'help.nickCopied' : 'about.pixCopyFail'));
  }
</script>

{#snippet icon(path: string)}
  <span class="icon"><svg viewBox="0 0 24 24" aria-hidden="true"><path d={path} /></svg></span>
{/snippet}

{#snippet body(title: string, sub: string, action: string)}
  <span class="body">
    <strong>{title}</strong>
    <span class="sub">{sub}</span>
  </span>
  <span class="action">{action}</span>
{/snippet}

<section class="help">
  <header>
    <h2>{t('help.title')}</h2>
    <p class="muted">{t('help.lead')}</p>
  </header>

  <div class="tiles">
    {#if LINKS.youtube}
      <a class="tile youtube" href={LINKS.youtube} target="_blank" rel="noreferrer">
        {@render icon(ICONS.youtube)}
        {@render body(t('help.video'), t('help.videoSub'), t('help.open'))}
      </a>
    {:else}
      <div class="tile youtube soon" aria-disabled="true">
        {@render icon(ICONS.youtube)}
        {@render body(t('help.video'), t('help.videoSub'), t('help.soon'))}
      </div>
    {/if}

    <a class="tile discord" href={LINKS.discordServer} target="_blank" rel="noreferrer">
      {@render icon(ICONS.discord)}
      {@render body(t('help.server'), t('help.serverSub'), t('help.join'))}
    </a>
  </div>

  <div class="contact">
    <span class="label">{t('help.contact')}</span>
    <div class="tiles">
      <button type="button" class="tile discord" onclick={copyNick} title={t('help.copyNick')}>
        {@render icon(ICONS.discord)}
        {@render body('Discord', LINKS.discordNick, t('about.copy'))}
      </button>

      {#if LINKS.facebook}
        <a class="tile facebook" href={LINKS.facebook} target="_blank" rel="noreferrer">
          {@render icon(ICONS.facebook)}
          {@render body('Facebook', t('help.facebookSub'), t('help.open'))}
        </a>
      {:else}
        <div class="tile facebook soon" aria-disabled="true">
          {@render icon(ICONS.facebook)}
          {@render body('Facebook', t('help.facebookSub'), t('help.soon'))}
        </div>
      {/if}
    </div>
  </div>
</section>

<style>
  .help {
    position: relative; overflow: hidden; display: flex; flex-direction: column; gap: 1rem;
    padding: 1.2rem; border-radius: 14px; border: 1px solid var(--border);
    background:
      radial-gradient(120% 90% at 100% 0%, color-mix(in srgb, var(--accent) 16%, transparent), transparent 60%),
      var(--surface);
  }
  header h2 { margin: 0; font-size: 1.3rem; }
  header p { margin: .25rem 0 0; }

  .tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: .6rem; }

  .tile {
    --brand: var(--accent);
    display: flex; align-items: center; gap: .75rem; padding: .75rem .9rem;
    border-radius: 12px; border: 1px solid var(--border); background: var(--surface-2);
    color: var(--fg); text-decoration: none; text-align: left; font: inherit; cursor: pointer;
    transition: transform .15s ease, border-color .15s ease, box-shadow .15s ease;
  }
  .tile:hover:not(.soon) {
    transform: translateY(-2px); border-color: var(--brand); background: var(--surface-2);
    box-shadow: 0 8px 22px color-mix(in srgb, var(--brand) 28%, transparent);
  }
  .tile.soon { cursor: default; opacity: .6; }
  .youtube { --brand: #ff0033; }
  .discord { --brand: #5865f2; }
  .facebook { --brand: #1877f2; }

  .icon {
    flex-shrink: 0; width: 42px; height: 42px; border-radius: 11px; display: grid; place-items: center;
    background: var(--brand); color: #fff;
  }
  .icon svg { width: 24px; height: 24px; fill: currentColor; }

  .body { display: flex; flex-direction: column; min-width: 0; flex: 1; }
  .body strong { font-size: .95rem; }
  .sub { font-size: .8rem; color: var(--muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

  .action {
    flex-shrink: 0; font-size: .75rem; font-weight: 600; padding: .2rem .55rem; border-radius: 999px;
    color: var(--brand); background: color-mix(in srgb, var(--brand) 14%, transparent);
  }
  .soon .action { color: var(--muted); background: var(--surface); }

  .contact { display: flex; flex-direction: column; gap: .45rem; }
  .label { font-size: .75rem; font-weight: 600; text-transform: uppercase; letter-spacing: .06em; color: var(--muted); }
</style>
