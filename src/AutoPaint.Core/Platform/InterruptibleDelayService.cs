namespace AutoPaint.Core.Platform;

/// <summary>
/// Implementação pura de <see cref="IDelayService"/>: dorme em fatias de 20 ms,
/// checando cancelamento e pausa entre elas.
/// </summary>
public sealed class InterruptibleDelayService : IDelayService
{
    public int SliceMs => 20;

    public void Wait(TimeSpan duration, CancellationToken ct, ManualResetEventSlim? pause = null)
    {
        var deadline = DateTime.UtcNow + duration;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (pause is { IsSet: false })
            {
                // Espera a liberação da pausa sem estender o tempo restante.
                while (!pause.Wait(SliceMs))
                    ct.ThrowIfCancellationRequested();
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return;

            int slice = (int)Math.Min(SliceMs, remaining.TotalMilliseconds);
            if (ct.WaitHandle.WaitOne(slice))
                ct.ThrowIfCancellationRequested();
        }
    }
}
