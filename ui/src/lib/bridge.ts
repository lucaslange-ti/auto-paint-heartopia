import type { BridgeEvent, BridgeResponse } from './contracts';

// Cliente tipado da ponte WebView2 <-> C#. Fora do WebView2 (ex.: `vite dev` no
// navegador) cai num modo mock que só loga, para a UI não quebrar.

type Pending = { resolve: (v: unknown) => void; reject: (e: Error) => void };

class Bridge {
  private pending = new Map<string, Pending>();
  private listeners = new Map<string, Set<(payload: unknown) => void>>();
  private seq = 0;
  private readonly wv = window.chrome?.webview;

  constructor() {
    this.wv?.addEventListener('message', (e: MessageEvent) => this.onMessage(e.data));
  }

  get isHosted(): boolean {
    return !!this.wv;
  }

  request<R = unknown, P = unknown>(type: string, payload?: P): Promise<R> {
    const id = `r${++this.seq}`;
    if (!this.wv) {
      console.warn('[bridge] sem WebView2; request ignorada:', type, payload);
      return Promise.reject(new Error('bridge indisponível fora do app'));
    }
    return new Promise<R>((resolve, reject) => {
      this.pending.set(id, { resolve: resolve as (v: unknown) => void, reject });
      this.wv!.postMessage({ id, type, payload: payload ?? null });
    });
  }

  on<P = unknown>(name: string, fn: (payload: P) => void): () => void {
    let set = this.listeners.get(name);
    if (!set) this.listeners.set(name, (set = new Set()));
    set.add(fn as (p: unknown) => void);
    return () => this.listeners.get(name)?.delete(fn as (p: unknown) => void);
  }

  private onMessage(msg: unknown): void {
    if (!msg || typeof msg !== 'object') return;

    if ((msg as BridgeEvent).type === 'event') {
      const ev = msg as BridgeEvent;
      this.listeners.get(ev.name)?.forEach((fn) => {
        try {
          fn(ev.payload);
        } catch (err) {
          console.error(err);
        }
      });
      return;
    }

    const res = msg as BridgeResponse;
    const p = res.id ? this.pending.get(res.id) : undefined;
    if (!p) return;
    this.pending.delete(res.id);
    res.ok ? p.resolve(res.result) : p.reject(new Error(res.error?.message ?? 'erro'));
  }
}

export const bridge = new Bridge();
