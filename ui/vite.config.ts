import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { resolve } from 'node:path';

// A UI é embutida no app: build gera arquivos estáticos em App/wwwroot, que o
// WPF serve via SetVirtualHostNameToFolderMapping (https://app.local).
export default defineConfig({
  plugins: [svelte()],
  base: './',
  build: {
    outDir: resolve(__dirname, '../src/AutoPaint.App/wwwroot'),
    emptyOutDir: true,
    target: 'chrome120',
    assetsDir: 'assets'
  },
  server: { port: 5599 }
});
