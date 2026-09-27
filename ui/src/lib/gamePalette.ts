import type { MainColorDto, PointDto, Rgb } from './contracts';

// Paleta fixa do Heartopia: 13 cores principais (P1..P13), cada uma com seus tons
// (T1..T10; o P1 só tem T1..T5). As cores não mudam — o usuário só calibra onde
// ficam os botões na tela. Os tons ficam no mesmo lugar em todo painel, então as
// posições dos tons são capturadas uma vez e replicadas para todas as cores.
const TABLE: { main: string; shades: string[] }[] = [
  { main: '#051616', shades: ['#051616', '#414545', '#808282', '#BEBFBF', '#FEFFFF'] },
  { main: '#EE6F72', shades: ['#CF354D', '#EE6F72', '#A6263D', '#F5ACA6', '#C98483', '#A35D5E', '#69313B', '#E7D5D5', '#C0ACAB', '#755E5E'] },
  { main: '#F98358', shades: ['#E95E2B', '#F98358', '#AB4226', '#FEBA9F', '#D9937C', '#AF6C58', '#753B31', '#E9D5D0', '#C1ACA6', '#755E59'] },
  { main: '#FEAE3B', shades: ['#F49E16', '#FEAE3B', '#B16F16', '#FECE92', '#DAA76D', '#B3814B', '#795126', '#F5E4CE', '#CDBCA9', '#806F5E'] },
  { main: '#F9D838', shades: ['#EDCA16', '#F9D838', '#B39416', '#FAE791', '#D3BE6F', '#AB954B', '#756326', '#EEE7C7', '#C6BFA2', '#787259'] },
  { main: '#B6C931', shades: ['#A8BC16', '#B6C931', '#758616', '#D8DF93', '#ADB76D', '#85914B', '#535E2B', '#E6E9C7', '#BCC2A3', '#6E745D'] },
  { main: '#41B97B', shades: ['#05A25D', '#41B97B', '#057447', '#9CDAAD', '#76B28B', '#4F8969', '#245640', '#C3E0CC', '#9DB7A6', '#53695D'] },
  { main: '#05ABA0', shades: ['#058781', '#05ABA0', '#056966', '#7ECDC2', '#55A49C', '#2B7E78', '#054B4B', '#BEE0DA', '#98B7B2', '#4E6B66'] },
  { main: '#0599BA', shades: ['#05729C', '#0599BA', '#055878', '#79BBCA', '#5193A5', '#246D7F', '#05495B', '#C6DDE2', '#9EB5BA', '#4F676F'] },
  { main: '#2B83C1', shades: ['#055EA6', '#2B83C1', '#054782', '#83A8C9', '#5D80A1', '#365B7F', '#193B56', '#C1CDD5', '#9BA6B0', '#4C5967'] },
  { main: '#7577BD', shades: ['#534DA1', '#7577BD', '#3E387E', '#A2A0C7', '#787AA1', '#55567E', '#333555', '#C9CAD5', '#A2A3B0', '#565869'] },
  { main: '#A167A9', shades: ['#813D8B', '#A167A9', '#602B6C', '#B89BB9', '#907395', '#6C4D73', '#432E4B', '#CFC9D1', '#ABA1AC', '#605665'] },
  { main: '#CF6B8F', shades: ['#AD356F', '#CF6B8F', '#862658', '#D9A1B4', '#B47A8C', '#8B5367', '#60354B', '#E4D5DA', '#BCADB1', '#725E66'] }
];

const toRgb = (hex: string): Rgb => ({
  r: parseInt(hex.slice(1, 3), 16),
  g: parseInt(hex.slice(3, 5), 16),
  b: parseInt(hex.slice(5, 7), 16)
});

export const GAME_PALETTE = TABLE.map((c, i) => ({
  name: `P${i + 1}`,
  rgb: toRgb(c.main),
  shades: c.shades.map((h, j) => ({ name: `T${j + 1}`, rgb: toRgb(h) }))
}));

export const MAIN_COUNT = GAME_PALETTE.length;
export const MAX_SHADES = Math.max(...GAME_PALETTE.map((c) => c.shades.length));

/**
 * Monta a paleta completa do config a partir das posições capturadas. Cores sem
 * posição de tom ficam com `shades` vazio (não pintáveis). Preserva `enabled` das cores e dos tons.
 */
export function buildMainColors(
  colorPos: PointDto[],
  shadePos: PointDto[],
  previous: MainColorDto[] = []
): MainColorDto[] {
  return GAME_PALETTE.map((c, i) => ({
    name: c.name,
    pos: colorPos[i],
    rgb: c.rgb,
    enabled: previous[i]?.enabled ?? true,
    shades:
      shadePos.length >= c.shades.length
        ? c.shades.map((s, j) => ({
            name: s.name,
            pos: shadePos[j],
            rgb: s.rgb,
            enabled: previous[i]?.shades[j]?.enabled ?? true
          }))
        : []
  }));
}

/**
 * Posições dos tons já calibradas, tiradas da primeira cor com o painel completo
 * (T1..T10). Vazio se ainda não foram capturadas.
 */
export function shadePositions(colors: MainColorDto[]): PointDto[] {
  const full = colors.find((c) => c.shades.length === MAX_SHADES);
  return full ? full.shades.map((s) => s.pos) : [];
}

/**
 * Força as cores fixas numa paleta já salva (configs antigos guardavam a cor lida
 * da tela). Devolve true se algo mudou.
 */
export function applyGamePalette(colors: MainColorDto[]): boolean {
  if (colors.length !== MAIN_COUNT) return false;
  let changed = false;
  const same = (a: Rgb, b: Rgb) => a.r === b.r && a.g === b.g && a.b === b.b;
  colors.forEach((c, i) => {
    const fixed = GAME_PALETTE[i];
    if (c.name !== fixed.name || !same(c.rgb, fixed.rgb)) {
      c.name = fixed.name;
      c.rgb = fixed.rgb;
      changed = true;
    }
    c.shades.forEach((s, j) => {
      const fs = fixed.shades[j];
      if (fs && (s.name !== fs.name || !same(s.rgb, fs.rgb))) {
        s.name = fs.name;
        s.rgb = fs.rgb;
        changed = true;
      }
    });
  });
  return changed;
}
