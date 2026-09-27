namespace AutoPaint.Core.Licensing;

/// <summary>
/// Porteira única de licenciamento. A v1 é grátis: <see cref="AlwaysAllowedLicenseGate"/>
/// sempre libera. Se um dia houver plano pago, só esta peça muda (ver
/// docs/stack-dotnet-e-licenciamento.md).
/// </summary>
public interface ILicenseGate
{
    bool CanUse(out string? reason);
}

public sealed class AlwaysAllowedLicenseGate : ILicenseGate
{
    public bool CanUse(out string? reason)
    {
        reason = null;
        return true;
    }
}
