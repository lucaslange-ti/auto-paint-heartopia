using AutoPaint.Core.Imaging;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Platform;

namespace AutoPaint.Core.Config;

/// <summary>
/// Modelo persistido em %AppData%\AutoPaintHeartopia\config.json.
/// Tipos simples (listas, arrays) para serializar direto com System.Text.Json.
/// A migração acontece no <see cref="IConfigStore"/> pelo <see cref="SchemaVersion"/>.
/// </summary>
public sealed class AppConfig
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string Language { get; set; } = "pt-BR";
    public string Theme { get; set; } = "system";

    /// <summary>Tamanho do texto da interface: "small", "medium", "large" ou "xlarge".</summary>
    public string FontSize { get; set; } = "medium";

    public CanvasAspect CanvasAspect { get; set; } = CanvasAspect.OneToOne;
    public Dictionary<string, CanvasPrecision> PrecisionByAspect { get; set; } = new();

    public TimingConfig Timing { get; set; } = new();
    public PaintMode PaintMode { get; set; } = PaintMode.Color;
    public bool EnableDragStrokes { get; set; }

    // Passe automático de verificação ao final da pintura. Sem opção na UI: o
    // reparo agora é só sob demanda, pelo botão "Reparo de pintura" na etapa
    // Pintura (usa VerifyToleranceRgb / VerifyMaxPasses abaixo).
    public bool VerifyAndRepair { get; set; } = false;
    public int VerifyToleranceRgb { get; set; } = 35;
    public int VerifyMaxPasses { get; set; } = 10;

    /// <summary>Atalhos globais durante a pintura (editáveis em Configurações).</summary>
    public HotkeysDto Hotkeys { get; set; } = new();

    public bool CheckForUpdates { get; set; } = true;
    /// <summary>Card de progresso sobreposto ao jogo durante a pintura.</summary>
    public bool StatusOverlayEnabled { get; set; } = true;

    /// <summary>Canto da tela do card: "TopLeft", "TopRight", "BottomLeft" ou "BottomRight".</summary>
    public string StatusOverlayCorner { get; set; } = "TopLeft";

    public Dictionary<string, string> LastImagePathByKey { get; set; } = new();
    public Dictionary<string, RectDto> LastCanvasRectByKey { get; set; } = new();

    public CalibrationDto Calibration { get; set; } = new();
    public WindowStateDto Window { get; set; } = new();
}

public sealed class TimingConfig
{
    public int MoveDurationMs { get; set; } = 30;
    public int MouseDownMs { get; set; } = 20;
    public int AfterClickMs { get; set; } = 60;
    public int PanelOpenMs { get; set; } = 120;
    public int ShadeSelectMs { get; set; } = 60;
    public int RowDelayMs { get; set; } = 100;

    public PaintTiming ToTiming() => new()
    {
        MoveDurationMs = MoveDurationMs,
        MouseDownMs = MouseDownMs,
        AfterClickMs = AfterClickMs,
        PanelOpenMs = PanelOpenMs,
        ShadeSelectMs = ShadeSelectMs,
        RowDelayMs = RowDelayMs
    };
}

public sealed class RectDto { public int X { get; set; } public int Y { get; set; } public int Width { get; set; } public int Height { get; set; } }
public sealed class PointDto { public int X { get; set; } public int Y { get; set; } }
public sealed class RgbDto { public byte R { get; set; } public byte G { get; set; } public byte B { get; set; } }

/// <summary>Grade do canvas por origem + passo (sub-pixel). Preferido sobre <see cref="RectDto"/>.</summary>
public sealed class CanvasGridDto
{
    public double OriginX { get; set; }
    public double OriginY { get; set; }
    public double PitchX { get; set; }
    public double PitchY { get; set; }
    public int Columns { get; set; }
    public int Rows { get; set; }
}

public sealed class CalibrationDto
{
    public RectDto? CanvasRect { get; set; }
    public CanvasGridDto? CanvasGrid { get; set; }
    public PointDto? ShadesPanelButtonPos { get; set; }
    public PointDto? BackButtonPos { get; set; }
    public PointDto? PaintToolButtonPos { get; set; }
    public List<MainColorDto> MainColors { get; set; } = new();
}

public sealed class MainColorDto
{
    public string Name { get; set; } = "";
    public PointDto Pos { get; set; } = new();
    public RgbDto Rgb { get; set; } = new();
    public List<ShadeDto> Shades { get; set; } = new();

    /// <summary>Cores desativadas não entram no matching nem na pintura.</summary>
    public bool Enabled { get; set; } = true;
}

public sealed class ShadeDto
{
    public string Name { get; set; } = "";
    public PointDto Pos { get; set; } = new();
    public RgbDto Rgb { get; set; } = new();

    /// <summary>Tons desativados não entram no matching nem na pintura.</summary>
    public bool Enabled { get; set; } = true;
}

public sealed class HotkeyDto
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    /// <summary>Virtual-key code do Windows (ex.: 0x58 = X, 0x20 = Espaço).</summary>
    public int Key { get; set; }

    public static HotkeyDto From(HotkeyBinding b) => new() { Ctrl = b.Ctrl, Alt = b.Alt, Shift = b.Shift, Key = b.Key };

    public HotkeyBinding ToBinding() => new(Ctrl, Alt, Shift, Key);
}

public sealed class HotkeysDto
{
    public HotkeyDto Abort { get; set; } = HotkeyDto.From(HotkeyBinding.DefaultAbort);
    public HotkeyDto PauseResume { get; set; } = HotkeyDto.From(HotkeyBinding.DefaultPauseResume);
}

public sealed class WindowStateDto
{
    public double Width { get; set; } = 1100;
    public double Height { get; set; } = 720;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Maximized { get; set; }
}
