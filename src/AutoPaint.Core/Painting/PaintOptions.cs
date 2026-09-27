using AutoPaint.Core.Calibration;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Painting;

public enum PaintMode
{
    /// <summary>Padrão: varre linha a linha, agrupando trechos horizontais do mesmo tom.</summary>
    Row,
    /// <summary>Agrupa por tom; pinta os maiores grupos primeiro (menos troca de paleta).</summary>
    Color
}

/// <summary>Opções de uma execução de pintura.</summary>
public sealed record PaintOptions
{
    public PaintMode Mode { get; init; } = PaintMode.Color;
    public PaintTiming Timing { get; init; } = PaintTiming.Normal;

    /// <summary>Tom tratado como fundo e não pintado. Null = pinta tudo.</summary>
    public ShadeRef? SkipBackground { get; init; }

    /// <summary>Strokes de arraste para trechos >= 2 células (desligado por padrão).</summary>
    public bool EnableDragStrokes { get; init; }

    /// <summary>Passe de verificação/reparo ao final.</summary>
    public bool VerifyAndRepair { get; init; } = true;
    public int VerifyToleranceRgb { get; init; } = 35;
    public int VerifyMaxPasses { get; init; } = 10;

    /// <summary>
    /// Cores da tela crua do canvas (branco/creme do Heartopia). No verify, uma
    /// célula que deveria ter cor mas é amostrada como uma destas conta como não
    /// pintada e entra no reparo — pega "linha em branco" mesmo dentro da
    /// tolerância normal. Vazio = desliga essa checagem.
    /// </summary>
    public IReadOnlyList<Rgb> BareCanvasColors { get; init; } = Array.Empty<Rgb>();
    public int BareCanvasToleranceRgb { get; init; } = 22;
}
