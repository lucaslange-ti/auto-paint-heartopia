import type { TimingConfig } from './contracts';

// Perfis de velocidade da pintura (etapa Pintura). "normal" = padrão do C#.
export const SPEED_PRESETS = {
  safe: { moveDurationMs: 45, mouseDownMs: 30, afterClickMs: 90, panelOpenMs: 180, shadeSelectMs: 90, rowDelayMs: 150 },
  normal: { moveDurationMs: 30, mouseDownMs: 20, afterClickMs: 60, panelOpenMs: 120, shadeSelectMs: 60, rowDelayMs: 100 },
  fast: { moveDurationMs: 12, mouseDownMs: 12, afterClickMs: 30, panelOpenMs: 90, shadeSelectMs: 40, rowDelayMs: 60 }
} satisfies Record<string, TimingConfig>;

export type SpeedPreset = keyof typeof SPEED_PRESETS;

/** Qual perfil bate exatamente com o timing salvo (null = ajuste manual). */
export function matchPreset(timing: TimingConfig | undefined): SpeedPreset | null {
  if (!timing) return null;
  const keys = Object.keys(SPEED_PRESETS.normal) as (keyof TimingConfig)[];
  for (const [name, preset] of Object.entries(SPEED_PRESETS)) {
    if (keys.every((k) => preset[k] === timing[k])) return name as SpeedPreset;
  }
  return null;
}
