namespace AutoPaint.Core.Painting;

/// <summary>
/// Tempos de automação, em milissegundos. Defaults idênticos ao projeto de
/// referência (valores em segundos lá: 0.03 / 0.02 / 0.06 / 0.12 / 0.06 / 0.10).
/// </summary>
public sealed record PaintTiming
{
    public int MoveDurationMs { get; init; } = 30;
    public int MouseDownMs { get; init; } = 20;
    public int AfterClickMs { get; init; } = 60;
    public int PanelOpenMs { get; init; } = 120;
    public int ShadeSelectMs { get; init; } = 60;
    public int RowDelayMs { get; init; } = 100;

    public static PaintTiming Safe   => new() { MoveDurationMs = 45, MouseDownMs = 30, AfterClickMs = 90,  PanelOpenMs = 180, ShadeSelectMs = 90, RowDelayMs = 150 };
    public static PaintTiming Normal => new();
    public static PaintTiming Fast   => new() { MoveDurationMs = 12, MouseDownMs = 12, AfterClickMs = 30,  PanelOpenMs = 90,  ShadeSelectMs = 40, RowDelayMs = 60 };
}
