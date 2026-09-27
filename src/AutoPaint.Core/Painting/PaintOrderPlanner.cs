using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;

namespace AutoPaint.Core.Painting;

/// <summary>
/// Gera a ordem de pintura a partir do plano. Puro e testável. Espelha os dois
/// modos do projeto de referência.
/// </summary>
public sealed class PaintOrderPlanner
{
    public IReadOnlyList<PaintStep> Plan(PaintPlan plan, PaintOptions options)
        => options.Mode == PaintMode.Color
            ? PlanByColor(plan, options.SkipBackground)
            : PlanByRow(plan, options.SkipBackground);

    private static IReadOnlyList<PaintStep> PlanByRow(PaintPlan plan, ShadeRef? skip)
    {
        var steps = new List<PaintStep>();

        for (int row = 0; row < plan.Size.Rows; row++)
        {
            bool leftToRight = row % 2 == 0; // serpentina
            int start = leftToRight ? 0 : plan.Size.Columns - 1;
            int end = leftToRight ? plan.Size.Columns : -1;
            int dir = leftToRight ? 1 : -1;

            List<CellPos>? run = null;
            ShadeRef runShade = default;

            for (int col = start; col != end; col += dir)
            {
                var shade = plan.Get(col, row);
                if (skip is { } bg && shade == bg)
                {
                    Flush(steps, ref run, runShade);
                    continue;
                }

                if (run is null || shade != runShade)
                {
                    Flush(steps, ref run, runShade);
                    run = new List<CellPos>();
                    runShade = shade;
                }

                run.Add(new CellPos(col, row));
            }

            Flush(steps, ref run, runShade);
        }

        return steps;

        static void Flush(List<PaintStep> steps, ref List<CellPos>? run, ShadeRef shade)
        {
            if (run is { Count: > 0 })
                steps.Add(new PaintStep(shade, run));
            run = null;
        }
    }

    private static IReadOnlyList<PaintStep> PlanByColor(PaintPlan plan, ShadeRef? skip)
    {
        var groups = new Dictionary<ShadeRef, List<CellPos>>();

        for (int row = 0; row < plan.Size.Rows; row++)
        for (int col = 0; col < plan.Size.Columns; col++)
        {
            var shade = plan.Get(col, row);
            if (skip is { } bg && shade == bg)
                continue;

            if (!groups.TryGetValue(shade, out var list))
                groups[shade] = list = new List<CellPos>();
            list.Add(new CellPos(col, row));
        }

        return groups
            .OrderByDescending(g => g.Value.Count)
            .Select(g => new PaintStep(g.Key, g.Value))
            .ToArray();
    }
}
