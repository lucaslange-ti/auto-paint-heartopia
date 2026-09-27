# UI — Auto Paint Heartopia

Interface em **Svelte 5 + TypeScript + Vite**. Buildada, vira arquivos estáticos
em `../src/AutoPaint.App/wwwroot`, servidos pelo WPF via
`SetVirtualHostNameToFolderMapping` (`https://app.local`).

## Comandos

```bash
npm install
npm run dev      # http://localhost:5599  (bridge cai em modo mock fora do app)
npm run build    # gera ../src/AutoPaint.App/wwwroot  (substitui o placeholder)
npm run check    # svelte-check
```

## Ponte

`src/lib/bridge.ts` — cliente tipado (`bridge.request` / `bridge.on`).
`src/lib/contracts.ts` — espelho dos DTOs C#. Protocolo em `../docs/ponte-protocolo.md`.

> Enquanto a UI real não é buildada, o app carrega o placeholder
> `../src/AutoPaint.App/wwwroot/index.html` + `bridge.js`. O primeiro
> `npm run build` sobrescreve esses arquivos.
