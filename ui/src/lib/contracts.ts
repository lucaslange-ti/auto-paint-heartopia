// Espelho tipado dos DTOs C# da ponte. Manter em sincronia com
// AutoPaint.Core/Bridge/BridgeMessages.cs, .../Config/AppConfig.cs e .../Imaging/*.
// (gerador automático fica para depois)

export interface BridgeResponse<R = unknown> {
  id: string;
  ok: boolean;
  result?: R;
  error?: { code: string; message: string };
}

export interface BridgeEvent<P = unknown> {
  type: 'event';
  name: string;
  payload: P;
}

export const Requests = {
  AppPing: 'app/ping',
  AppInfo: 'app/info',
  ConfigGet: 'config/get',
  ConfigSet: 'config/set',
  ConfigReset: 'config/reset',
  ImageConvert: 'image/convert',
  CalibrationStartRect: 'calibration/start-rect',
  CalibrationStartGrid: 'calibration/start-grid',
  CalibrationDetectRect: 'calibration/detect-rect',
  CalibrationCapturePoint: 'calibration/capture-point',
  PaintStart: 'paint/start',
  PaintPause: 'paint/pause',
  PaintResume: 'paint/resume',
  PaintAbort: 'paint/abort',
  PaintSeek: 'paint/seek',
  PaintRepair: 'paint/repair'
} as const;

export const Events = {
  PaintProgress: 'paint/progress',
  PaintStatus: 'paint/status',
  CalibrationPointCaptured: 'calibration/point-captured',
  CalibrationRectCaptured: 'calibration/rect-captured',
  Toast: 'app/toast'
} as const;

export interface AppInfo {
  version: string;
  os: string;
  language: string;
  culture: string;
  licensed: boolean;
}

export type CanvasAspect = 'OneToOne' | 'SixteenNine' | 'NineSixteen' | 'FourThree' | 'ThreeFour';
export type CanvasPrecision = 'Small' | 'Medium' | 'Big' | 'SuperLarge';
export type FitMode = 'Contain' | 'Fill';
export type ResampleMode = 'NearestNeighbor' | 'Lanczos';
export type PaintMode = 'Row' | 'Color';

export interface Rgb { r: number; g: number; b: number; }
export interface PointDto { x: number; y: number; }
export interface RectDto { x: number; y: number; width: number; height: number; }
export interface CanvasGridDto {
  originX: number;
  originY: number;
  pitchX: number;
  pitchY: number;
  columns: number;
  rows: number;
}

export interface ShadeUsage {
  mainColorIndex: number;
  shadeIndex: number;
  name: string;
  rgb: Rgb;
  count: number;
}

export interface PlanConversion {
  columns: number;
  rows: number;
  cellCount: number;
  hash: string;
  usage: ShadeUsage[];
  cells: number[]; // row-major; cada valor é índice em `usage`
}

export interface ShadeDto { name: string; pos: PointDto; rgb: Rgb; enabled: boolean; }
export interface MainColorDto { name: string; pos: PointDto; rgb: Rgb; shades: ShadeDto[]; enabled: boolean; }

export interface CalibrationDto {
  canvasRect?: RectDto | null;
  canvasGrid?: CanvasGridDto | null;
  shadesPanelButtonPos?: PointDto | null;
  backButtonPos?: PointDto | null;
  paintToolButtonPos?: PointDto | null;
  mainColors: MainColorDto[];
}

export interface TimingConfig {
  moveDurationMs: number;
  mouseDownMs: number;
  afterClickMs: number;
  panelOpenMs: number;
  shadeSelectMs: number;
  rowDelayMs: number;
}

export type FontSize = 'small' | 'medium' | 'large' | 'xlarge';
export type OverlayCorner = 'TopLeft' | 'TopRight' | 'BottomLeft' | 'BottomRight';
export interface HotkeyDto { ctrl: boolean; alt: boolean; shift: boolean; key: number; }

export interface AppConfig {
  schemaVersion: number;
  language: string;
  theme: string;
  fontSize: FontSize;
  hotkeys: { abort: HotkeyDto; pauseResume: HotkeyDto };
  statusOverlayEnabled: boolean;
  statusOverlayCorner: OverlayCorner;
  canvasAspect: CanvasAspect;
  precisionByAspect: Record<string, CanvasPrecision>;
  timing: TimingConfig;
  paintMode: PaintMode;
  enableDragStrokes: boolean;
  verifyAndRepair: boolean;
  verifyToleranceRgb: number;
  verifyMaxPasses: number;
  checkForUpdates: boolean;
  lastImagePathByKey: Record<string, string>;
  lastCanvasRectByKey: Record<string, RectDto>;
  calibration: CalibrationDto;
  window: { width: number; height: number; left?: number | null; top?: number | null; maximized: boolean };
}

export type PaintPhase =
  | 'Preparing' | 'Painting' | 'Verifying' | 'Repairing' | 'Finished' | 'Aborted' | 'Failed';

export interface PaintProgress {
  phase: PaintPhase;
  cellsDone: number;
  cellsTotal: number;
  cellsRepaired: number;
  currentShadeName?: string | null;
  elapsed: string;
  eta?: string | null;
  message?: string | null;
  fraction: number;
}

export interface PaintStatus {
  phase: string;
  canResume?: boolean;
  message?: string | null;
}
