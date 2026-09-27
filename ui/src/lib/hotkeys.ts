import type { HotkeyDto } from './contracts';

// Tradução entre KeyboardEvent.code (navegador) e virtual-key code do Windows,
// que é o que o hook global do C# compara. Só teclas que fazem sentido como atalho.
const CODE_TO_VK: Record<string, number> = {
  Space: 0x20, Pause: 0x13, Insert: 0x2d, Delete: 0x2e, Home: 0x24, End: 0x23,
  PageUp: 0x21, PageDown: 0x22, ArrowLeft: 0x25, ArrowUp: 0x26, ArrowRight: 0x27, ArrowDown: 0x28,
  ScrollLock: 0x91, Backquote: 0xc0, Minus: 0xbd, Equal: 0xbb, BracketLeft: 0xdb, BracketRight: 0xdd,
  Backslash: 0xdc, Semicolon: 0xba, Quote: 0xde, Comma: 0xbc, Period: 0xbe, Slash: 0xbf
};
for (let i = 0; i < 26; i++) CODE_TO_VK[`Key${String.fromCharCode(65 + i)}`] = 0x41 + i;
for (let i = 0; i <= 9; i++) {
  CODE_TO_VK[`Digit${i}`] = 0x30 + i;
  CODE_TO_VK[`Numpad${i}`] = 0x60 + i;
}
for (let i = 1; i <= 12; i++) CODE_TO_VK[`F${i}`] = 0x6f + i;

const VK_LABEL: Record<number, string> = {
  0x20: 'Espaço', 0x13: 'Pause', 0x2d: 'Insert', 0x2e: 'Delete', 0x24: 'Home', 0x23: 'End',
  0x21: 'PageUp', 0x22: 'PageDown', 0x25: '←', 0x26: '↑', 0x27: '→', 0x28: '↓', 0x91: 'ScrollLock',
  0xc0: "'", 0xbd: '-', 0xbb: '=', 0xdb: '[', 0xdd: ']', 0xdc: '\\', 0xba: 'Ç', 0xde: '~', 0xbc: ',', 0xbe: '.', 0xbf: ';'
};

export function vkFromCode(code: string): number | null {
  return CODE_TO_VK[code] ?? null;
}

export function keyLabel(vk: number, spaceLabel = 'Espaço'): string {
  if (vk === 0x20) return spaceLabel;
  if (vk >= 0x41 && vk <= 0x5a) return String.fromCharCode(vk);
  if (vk >= 0x30 && vk <= 0x39) return String.fromCharCode(vk);
  if (vk >= 0x60 && vk <= 0x69) return `Num ${vk - 0x60}`;
  if (vk >= 0x70 && vk <= 0x7b) return `F${vk - 0x6f}`;
  return VK_LABEL[vk] ?? `#${vk}`;
}

/** Partes do atalho para exibir como teclas: ["Ctrl", "Alt", "X"]. */
export function hotkeyParts(h: HotkeyDto | undefined, spaceLabel?: string): string[] {
  if (!h || !h.key) return [];
  return [...(h.ctrl ? ['Ctrl'] : []), ...(h.alt ? ['Alt'] : []), ...(h.shift ? ['Shift'] : []), keyLabel(h.key, spaceLabel)];
}

export const isFunctionKey = (vk: number) => vk >= 0x70 && vk <= 0x7b;

export const sameHotkey = (a: HotkeyDto, b: HotkeyDto) =>
  a.key === b.key && a.ctrl === b.ctrl && a.alt === b.alt && a.shift === b.shift;

export const DEFAULT_HOTKEYS = {
  abort: { ctrl: true, alt: true, shift: false, key: 0x58 },
  pauseResume: { ctrl: true, alt: true, shift: false, key: 0x20 }
} satisfies Record<string, HotkeyDto>;
