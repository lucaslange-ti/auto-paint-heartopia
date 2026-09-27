namespace AutoPaint.Core.Painting;

/// <summary>
/// Ponto de retomada gravado ao pausar/abortar. Só é válido se os hashes do plano
/// e da calibração baterem com os atuais.
/// </summary>
public sealed record ResumeState
{
    public required string PlanHash { get; init; }
    public required string CalibrationHash { get; init; }
    public PaintMode Mode { get; init; }

    /// <summary>Índice do passo na ordem de pintura já gerada (linha ou grupo de cor).</summary>
    public int StepIndex { get; init; }
    /// <summary>Índice da célula dentro do passo atual.</summary>
    public int CellIndexInStep { get; init; }

    public int CellsDone { get; init; }
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
}
