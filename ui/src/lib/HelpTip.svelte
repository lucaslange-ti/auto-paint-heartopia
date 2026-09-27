<script lang="ts">
  import type { Snippet } from 'svelte';

  // Rótulo com dica ao passar o mouse: texto + imagem
  // mostrando onde clicar no jogo.
  let { text, image, children }: { text: string; image: string; children: Snippet } = $props();
</script>

<span class="tip">
  <span class="label">{@render children()}</span>
  <span class="mark" aria-hidden="true">?</span>
  <span class="pop">
    <span class="text">{text}</span>
    <img src={image} alt="" />
  </span>
</span>

<style>
  .tip { position: relative; display: inline-flex; align-items: center; gap: .35rem; max-width: 100%; cursor: help; }
  .label { min-width: 0; }
  .mark {
    width: 22px; height: 22px; border-radius: 999px; display: grid; place-items: center; flex-shrink: 0;
    font-size: .85rem; font-weight: 800; background: var(--accent); color: var(--accent-fg);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 25%, transparent);
    transition: transform .12s ease;
  }
  .tip:hover .mark { transform: scale(1.12); }
  .pop {
    display: none; position: absolute; z-index: 20; top: calc(100% + .35rem); left: 0;
    width: max-content; max-width: 300px; padding: .6rem; flex-direction: column; gap: .5rem;
    background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius);
    box-shadow: 0 6px 20px rgba(0, 0, 0, .25); font-size: .82rem; font-weight: 400; color: var(--fg);
    line-height: 1.35; cursor: default;
  }
  .tip:hover .pop { display: flex; }
  .pop img { max-width: 100%; max-height: 340px; object-fit: contain; align-self: center; border-radius: 6px; }
</style>
