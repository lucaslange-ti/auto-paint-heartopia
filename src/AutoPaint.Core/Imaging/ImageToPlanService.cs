using AutoPaint.Core.Calibration;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Imaging;

/// <summary>Uso de um tom no plano, para o preview e a lista de cores.</summary>
public sealed record ShadeUsage(int MainColorIndex, int ShadeIndex, string Name, Rgb Rgb, int Count);

/// <summary>Resumo enxuto do plano para a UI (não carrega a matriz inteira).</summary>
public sealed record PlanSummary(int Columns, int Rows, int CellCount, string Hash, IReadOnlyList<ShadeUsage> Usage);

/// <summary>
/// Plano completo para a UI desenhar o preview: <see cref="Cells"/> é row-major e
/// cada valor é um índice em <see cref="Usage"/>.
/// </summary>
public sealed record PlanConversion(
    int Columns, int Rows, int CellCount, string Hash,
    IReadOnlyList<ShadeUsage> Usage, int[] Cells);

/// <summary>
/// Orquestra imagem -&gt; <see cref="PaintPlan"/>: redimensiona (rasterizador da
/// plataforma) e casa cada pixel com a paleta (<see cref="PlanBuilder"/>).
/// </summary>
public sealed class ImageToPlanService
{
    private readonly IImageRasterizer _rasterizer;
    private readonly PlanBuilder _builder;

    public ImageToPlanService(IImageRasterizer rasterizer, PlanBuilder builder)
    {
        _rasterizer = rasterizer;
        _builder = builder;
    }

    public PaintPlan Convert(
        Stream image,
        CanvasAspect aspect,
        CanvasPrecision precision,
        FitMode fit,
        ResampleMode resample,
        PaletteConfig palette)
    {
        var size = CanvasPresets.Resolve(aspect, precision);
        var grid = _rasterizer.Rasterize(image, size, fit, resample);
        return _builder.Build(grid, palette);
    }

    public static PlanSummary Summarize(PaintPlan plan)
    {
        var counts = new Dictionary<ShadeRef, int>();
        foreach (var c in plan.Cells)
            counts[c] = counts.GetValueOrDefault(c) + 1;

        var usage = counts
            .OrderByDescending(kv => kv.Value)
            .Select(kv =>
            {
                var main = plan.PaletteSnapshot.MainColors[kv.Key.MainColorIndex];
                var shade = main.Shades[kv.Key.ShadeIndex];
                return new ShadeUsage(kv.Key.MainColorIndex, kv.Key.ShadeIndex, shade.Name, shade.Rgb, kv.Value);
            })
            .ToArray();

        return new PlanSummary(plan.Size.Columns, plan.Size.Rows, plan.Size.CellCount, plan.Hash, usage);
    }

    /// <summary>Resumo + matriz de células (índices em <c>Usage</c>) para o preview.</summary>
    public static PlanConversion Describe(PaintPlan plan)
    {
        var summary = Summarize(plan);

        var indexOf = new Dictionary<ShadeRef, int>();
        for (int i = 0; i < summary.Usage.Count; i++)
            indexOf[new ShadeRef(summary.Usage[i].MainColorIndex, summary.Usage[i].ShadeIndex)] = i;

        var cells = new int[plan.Size.CellCount];
        var span = plan.Cells;
        for (int i = 0; i < span.Length; i++)
            cells[i] = indexOf[span[i]];

        return new PlanConversion(
            summary.Columns, summary.Rows, summary.CellCount, summary.Hash, summary.Usage, cells);
    }
}
