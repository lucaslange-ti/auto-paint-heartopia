using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Calibration;

/// <summary>Botão de um tom específico dentro do painel de tons de uma cor principal.</summary>
public sealed record ShadeButton(string Name, PhysicalPoint Pos, Rgb Rgb);

/// <summary>
/// Cor principal da paleta do jogo: botão que abre o painel de tons, mais a lista
/// de tons capturados pelo assistente.
/// </summary>
public sealed record MainColor(string Name, PhysicalPoint Pos, Rgb Rgb, IReadOnlyList<ShadeButton> Shades);

/// <summary>
/// Paleta configurada pelo usuário no assistente de calibração. Hierárquica:
/// cor principal -> painel de tons -> tom. Espelha o modelo do projeto de referência.
/// </summary>
public sealed record PaletteConfig(
    IReadOnlyList<MainColor> MainColors,
    PhysicalPoint? ShadesPanelButton,
    PhysicalPoint? BackButton,
    PhysicalPoint? PaintToolButton)
{
    public static PaletteConfig Empty { get; } = new(Array.Empty<MainColor>(), null, null, null);

    /// <summary>Total de tons cadastrados em todas as cores principais.</summary>
    public int ShadeCount => MainColors.Sum(c => c.Shades.Count);
}

/// <summary>Referência estável a um tom dentro de uma <see cref="PaletteConfig"/>.</summary>
public readonly record struct ShadeRef(int MainColorIndex, int ShadeIndex);
