using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;

namespace AutoPaint.Core.Painting;

public sealed record PaintRequest(
    PaintPlan Plan,
    CalibrationProfile Calibration,
    PaintOptions Options,
    ResumeState? Resume = null)
{
    /// <summary>
    /// Roda só o passe de verificação/reparo: amostra cada célula, pinta as que não
    /// bateram (inclui as em branco) e deixa em paz as que já estão certas.
    /// </summary>
    public bool RepairOnly { get; init; }
}

/// <summary>
/// Motor de pintura. Roda numa thread dedicada. Reporta progresso por
/// <paramref name="progress"/>; abortar via <paramref name="ct"/>; pausar via
/// <see cref="Pause"/>/<see cref="Resume"/>.
/// </summary>
public interface IPaintEngine
{
    Task RunAsync(PaintRequest request, IProgress<PaintProgress> progress, CancellationToken ct);

    void Pause();
    void Resume();

    /// <summary>
    /// Reposiciona o cursor de pintura para um índice de célula global (0..total,
    /// na ordem de pintura). Para frente pula células; para trás repinta a partir
    /// dali. Aplicado no próximo ponto seguro do laço — use com a pintura pausada.
    /// </summary>
    void SeekTo(int globalCellIndex);

    /// <summary>Último estado de retomada (após pausa/abort). Null se concluiu.</summary>
    ResumeState? LastResumeState { get; }
}
