using AutoPaint.Core.Painting;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Platform;

/// <summary>
/// Envio de input de mouse em coordenadas absolutas de tela (pixels físicos).
/// Implementação Windows usa SendInput. Um fake em testes grava as ações (dry-run).
/// </summary>
public interface IInputSender
{
    void MoveTo(PhysicalPoint point);
    void LeftDown();
    void LeftUp();

    /// <summary>
    /// Sequência de 1 clique confiável em jogos: mover -> down -> segurar -> up -> esperar.
    /// Os tempos vêm de <see cref="PaintTiming"/>. Deve honrar <paramref name="ct"/>.
    /// </summary>
    void Tap(PhysicalPoint point, PaintTiming timing, CancellationToken ct);

    /// <summary>Dois <see cref="Tap"/> seguidos (usado na seleção de tom).</summary>
    void DoubleTap(PhysicalPoint point, PaintTiming timing, CancellationToken ct);
}
