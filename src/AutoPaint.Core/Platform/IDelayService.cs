namespace AutoPaint.Core.Platform;

/// <summary>
/// Espera fatiada em blocos curtos para reagir rápido a abortar/pausar.
/// </summary>
public interface IDelayService
{
    /// <summary>
    /// Espera <paramref name="duration"/> em blocos de ~<see cref="SliceMs"/> ms.
    /// Lança <see cref="OperationCanceledException"/> se <paramref name="ct"/> disparar.
    /// Se <paramref name="pause"/> estiver setado, bloqueia (respeitando ct) até liberar.
    /// </summary>
    void Wait(TimeSpan duration, CancellationToken ct, ManualResetEventSlim? pause = null);

    int SliceMs { get; }
}
