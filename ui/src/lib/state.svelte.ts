import { bridge } from './bridge';
import { i18n } from './i18n.svelte';
import { Events, Requests } from './contracts';
import type { AppConfig, AppInfo, FontSize, PaintProgress, PaintStatus, PlanConversion } from './contracts';
import { applyGamePalette } from './gamePalette';

export const STEPS = ['onboarding', 'palette', 'image', 'canvas', 'paint', 'summary'] as const;
export type StepName = (typeof STEPS)[number];
export type Route = 'wizard' | 'settings' | 'supporters' | 'about';

class AppState {
  route = $state<Route>('wizard');
  step = $state(0);

  info = $state<AppInfo | null>(null);
  config = $state<AppConfig | null>(null);

  imageDataUrl = $state<string | null>(null);
  conversion = $state<PlanConversion | null>(null);

  progress = $state<PaintProgress | null>(null);
  status = $state<PaintStatus | null>(null);
  lastRunRepair = $state(false);
  paused = $state(false);

  busy = $state(false);
  toast = $state<string | null>(null);

  async init() {
    bridge.on<PaintProgress>(Events.PaintProgress, (p) => (this.progress = p));
    bridge.on<PaintStatus>(Events.PaintStatus, (s) => {
      this.status = s;
      // Pausa pode vir do atalho global: o app volta para a frente na etapa Pintura.
      if (s.phase === 'Paused') {
        this.paused = true;
        this.goTo('paint');
      } else {
        this.paused = false;
      }
      if (['Finished', 'Aborted', 'Failed'].includes(s.phase)) this.goTo('summary');
    });
    bridge.on<{ message: string }>(Events.Toast, (p) => this.flash(p?.message ?? ''));

    try {
      this.info = await bridge.request<AppInfo>(Requests.AppInfo);
      this.config = await bridge.request<AppConfig>(Requests.ConfigGet);
      if (this.config && applyGamePalette(this.config.calibration.mainColors)) await this.saveConfig();
      this.applyFontSize();
      i18n.set(this.config?.language ?? this.info?.culture ?? 'pt-BR');
    } catch {
      i18n.set('pt-BR');
    }
  }

  // Tamanho base do texto: as telas usam rem, então tudo escala junto.
  applyFontSize() {
    const px: Record<FontSize, number> = { small: 12.5, medium: 14, large: 16, xlarge: 18 };
    document.documentElement.style.fontSize = `${px[this.config?.fontSize ?? 'medium'] ?? 14}px`;
  }

  async saveConfig() {
    if (!this.config) return;
    await bridge.request(Requests.ConfigSet, this.config);
  }

  async startRepair() {
    this.progress = null;
    this.status = null;
    this.lastRunRepair = true;
    await bridge.request(Requests.PaintRepair);
    this.goTo('paint'); // etapa Pintura mostra o progresso do reparo
  }

  goStep(n: number) {
    this.step = Math.max(0, Math.min(STEPS.length - 1, n));
    this.route = 'wizard';
  }

  goTo(name: StepName) {
    this.goStep(STEPS.indexOf(name));
  }

  flash(msg: string) {
    this.toast = msg;
    setTimeout(() => (this.toast = null), 3500);
  }

  get hasPalette() {
    return (this.config?.calibration?.mainColors ?? []).some((c) => c.enabled && c.shades.some((s) => s.enabled));
  }

  get hasButtons() {
    const c = this.config?.calibration;
    return !!(c?.shadesPanelButtonPos && c?.backButtonPos && c?.paintToolButtonPos);
  }

  // Etapa "Cores e botões" completa: libera o avanço para a Imagem.
  get paletteReady() {
    return this.hasPalette && this.hasButtons;
  }

  get hasCanvas() {
    const c = this.config?.calibration;
    return !!(c?.canvasGrid || c?.canvasRect);
  }
}

export const app = new AppState();
