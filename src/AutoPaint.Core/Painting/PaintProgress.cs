namespace AutoPaint.Core.Painting;

public enum PaintPhase { Preparing, Painting, Verifying, Repairing, Finished, Aborted, Failed }

/// <summary>Evento de progresso empurrado para a ponte -> UI (WebView2).</summary>
public sealed record PaintProgress
{
    public PaintPhase Phase { get; init; }
    public int CellsDone { get; init; }
    public int CellsTotal { get; init; }
    public int CellsRepaired { get; init; }
    public string? CurrentShadeName { get; init; }
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? Eta { get; init; }
    public string? Message { get; init; }

    public double Fraction => CellsTotal == 0 ? 0 : (double)CellsDone / CellsTotal;
}
