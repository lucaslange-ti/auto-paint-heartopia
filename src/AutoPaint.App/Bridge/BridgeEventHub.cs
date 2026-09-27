using AutoPaint.Core.Bridge;

namespace AutoPaint.App.Bridge;

/// <summary>
/// Canal único de eventos C# -&gt; UI. Existe para quebrar o ciclo de DI entre
/// <see cref="BridgeRouter"/> (roteia comandos) e <see cref="Painting.PaintSession"/>
/// (emite progresso): ambos dependem do hub, não um do outro.
/// </summary>
public sealed class BridgeEventHub
{
    public event Action<BridgeEvent>? EventRaised;

    public void Emit(BridgeEvent ev) => EventRaised?.Invoke(ev);
}
