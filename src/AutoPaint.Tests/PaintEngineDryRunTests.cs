using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class PaintEngineDryRunTests
{
    private static readonly PaletteConfig Pal = new(
        new[]
        {
            new MainColor("preto", new PhysicalPoint(10, 10), new Rgb(0, 0, 0), new[]
            {
                new ShadeButton("preto-1", new PhysicalPoint(11, 20), new Rgb(0, 0, 0)),
            })
        },
        ShadesPanelButton: new PhysicalPoint(50, 5),
        BackButton: new PhysicalPoint(5, 5),
        PaintToolButton: null);

    private static readonly CalibrationProfile Calib = new(new PhysicalRect(0, 0, 100, 100), Pal);

    private static PaintPlan AllSameShadePlan(int w, int h)
    {
        var cells = new ShadeRef[w * h];
        Array.Fill(cells, new ShadeRef(0, 0));
        return new PaintPlan(new CanvasSize(w, h), cells, Pal);
    }

    private static PaintEngine NewEngine(RecordingInputSender input) =>
        new(input, new NoDelay(), new FakeGeometry(), new SolidScreenReader());

    [Fact]
    public async Task Paints_every_cell_once_and_selects_the_shade()
    {
        var input = new RecordingInputSender();
        var engine = NewEngine(input);
        var progress = new ListProgress<PaintProgress>();

        var plan = AllSameShadePlan(2, 2);
        var options = new PaintOptions { Mode = PaintMode.Color, VerifyAndRepair = false };

        await engine.RunAsync(new PaintRequest(plan, Calib, options), progress, CancellationToken.None);

        // 4 células nos centros esperados
        var expected = new[]
        {
            new PhysicalPoint(25, 25), new PhysicalPoint(75, 25),
            new PhysicalPoint(25, 75), new PhysicalPoint(75, 75)
        };
        Assert.All(expected, p => Assert.Contains(p, input.Taps));

        Assert.True(input.DoubleTapCount >= 1, "esperava ao menos uma seleção de tom (double-tap)");
        Assert.Equal(PaintPhase.Finished, progress.Items[^1].Phase);
        Assert.Equal(4, progress.Items[^1].CellsDone);
    }

    [Fact]
    public async Task Abort_token_stops_and_reports_aborted()
    {
        var input = new RecordingInputSender();
        var engine = NewEngine(input);
        var progress = new ListProgress<PaintProgress>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var plan = AllSameShadePlan(4, 4);
        var options = new PaintOptions { Mode = PaintMode.Row, VerifyAndRepair = false };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.RunAsync(new PaintRequest(plan, Calib, options), progress, cts.Token));

        Assert.Contains(progress.Items, p => p.Phase == PaintPhase.Aborted);
        Assert.NotNull(engine.LastResumeState);
    }

    // Centros das células de um canvas 100x100 com 4 colunas x 1 linha: y sempre 50.
    // Os cliques da seleção de tom (botões da paleta) ficam em y <= 20, então
    // filtrar por Y == 50 isola só os cliques de célula.
    private static int CellTaps(HookingInputSender input) => input.Taps.Count(p => p.Y == 50);

    [Fact]
    public async Task Seek_forward_skips_the_cells_in_between()
    {
        var input = new HookingInputSender();
        var engine = new PaintEngine(input, new NoDelay(), new FakeGeometry(), new SolidScreenReader());
        var progress = new ListProgress<PaintProgress>();

        var plan = AllSameShadePlan(4, 1); // 1 linha, 4 células, 1 passo
        var options = new PaintOptions { Mode = PaintMode.Row, VerifyAndRepair = false };

        int cells = 0;
        input.BeforeTap = p =>
        {
            if (p.Y != 50) return;      // ignora cliques da paleta
            if (++cells == 1) engine.SeekTo(3); // após a 1ª célula, pula para a última
        };

        await engine.RunAsync(new PaintRequest(plan, Calib, options), progress, CancellationToken.None);

        // Clicou só a célula 0 e a célula 3; a 1 e a 2 ficaram de fora.
        Assert.Equal(
            new[] { new PhysicalPoint(12, 50), new PhysicalPoint(88, 50) },
            input.Taps.Where(p => p.Y == 50).ToArray());
        Assert.Equal(PaintPhase.Finished, progress.Items[^1].Phase);
        Assert.Equal(4, progress.Items[^1].CellsDone);
    }

    [Fact]
    public async Task Seek_backward_repaints_from_the_target_cell()
    {
        var input = new HookingInputSender();
        var engine = new PaintEngine(input, new NoDelay(), new FakeGeometry(), new SolidScreenReader());
        var progress = new ListProgress<PaintProgress>();

        var plan = AllSameShadePlan(4, 1);
        var options = new PaintOptions { Mode = PaintMode.Row, VerifyAndRepair = false };

        int cells = 0;
        var once = false;
        input.BeforeTap = p =>
        {
            if (p.Y != 50) return;
            if (++cells == 4 && !once) { once = true; engine.SeekTo(1); } // volta para a célula 1
        };

        await engine.RunAsync(new PaintRequest(plan, Calib, options), progress, CancellationToken.None);

        Assert.Equal(7, CellTaps(input)); // 0,1,2,3 + repintadas 1,2,3
        Assert.Equal(PaintPhase.Finished, progress.Items[^1].Phase);
        Assert.Equal(4, progress.Items[^1].CellsDone);
    }

    [Fact]
    public async Task RepairOnly_skips_the_paint_pass_and_repaints_mismatches()
    {
        var input = new RecordingInputSender();
        // Alvo preto, tela toda branca -> as 3 células divergem.
        var engine = new PaintEngine(input, new NoDelay(), new FakeGeometry(), new SolidScreenReader(new Rgb(255, 255, 255)));
        var progress = new ListProgress<PaintProgress>();

        var plan = AllSameShadePlan(3, 1);
        var options = new PaintOptions { Mode = PaintMode.Color, VerifyAndRepair = false, VerifyMaxPasses = 1 };

        await engine.RunAsync(
            new PaintRequest(plan, Calib, options) { RepairOnly = true }, progress, CancellationToken.None);

        Assert.DoesNotContain(progress.Items, p => p.Phase == PaintPhase.Painting);
        Assert.Contains(progress.Items, p => p.Phase == PaintPhase.Repairing);

        var last = progress.Items[^1];
        Assert.Equal(PaintPhase.Finished, last.Phase);
        Assert.Equal(3, last.CellsRepaired);
        Assert.Equal(3, input.Taps.Count(p => p.Y == 50)); // 3 centros de célula repintados
    }

    [Fact]
    public async Task Verify_repaints_a_cell_reading_as_bare_canvas_even_within_tolerance()
    {
        var bare = new Rgb(0xF7, 0xF5, 0xF0); // branco da superfície do Heartopia
        var input = new RecordingInputSender();
        var engine = new PaintEngine(input, new NoDelay(), new FakeGeometry(), new SolidScreenReader(bare));
        var progress = new ListProgress<PaintProgress>();

        // Alvo = cinza-claro: perto do branco cru (dist ~49), mas não é a cor do canvas.
        var pal = new PaletteConfig(
            new[]
            {
                new MainColor("cinza", new PhysicalPoint(10, 10), new Rgb(0xD8, 0xD8, 0xD8), new[]
                {
                    new ShadeButton("c1", new PhysicalPoint(11, 20), new Rgb(0xD8, 0xD8, 0xD8)),
                })
            },
            ShadesPanelButton: new PhysicalPoint(50, 5),
            BackButton: new PhysicalPoint(5, 5),
            PaintToolButton: null);
        var calib = new CalibrationProfile(new PhysicalRect(0, 0, 100, 100), pal);
        var plan = new PaintPlan(new CanvasSize(2, 1), new[] { new ShadeRef(0, 0), new ShadeRef(0, 0) }, pal);

        var options = new PaintOptions
        {
            Mode = PaintMode.Color,
            VerifyAndRepair = true,
            VerifyMaxPasses = 1,
            VerifyToleranceRgb = 60,        // 49 < 60 -> a tolerância normal NÃO pegaria
            BareCanvasColors = new[] { bare },
            BareCanvasToleranceRgb = 20
        };

        await engine.RunAsync(new PaintRequest(plan, calib, options), progress, CancellationToken.None);

        var verifying = progress.Items.Where(p => p.Phase == PaintPhase.Verifying).ToList();
        Assert.NotEmpty(verifying);
        Assert.Equal(2, verifying[^1].CellsRepaired);
    }

    [Fact]
    public async Task Verify_pass_repaints_when_screen_does_not_match_target()
    {
        // Alvo é preto; a tela devolve branco -> toda célula diverge -> repinta.
        var input = new RecordingInputSender();
        var engine = new PaintEngine(input, new NoDelay(), new FakeGeometry(), new SolidScreenReader(new Rgb(255, 255, 255)));
        var progress = new ListProgress<PaintProgress>();

        var plan = AllSameShadePlan(2, 1);
        var options = new PaintOptions
        {
            Mode = PaintMode.Color,
            VerifyAndRepair = true,
            VerifyMaxPasses = 1,
            VerifyToleranceRgb = 10
        };

        await engine.RunAsync(new PaintRequest(plan, Calib, options), progress, CancellationToken.None);

        var verifying = progress.Items.Where(p => p.Phase == PaintPhase.Verifying).ToList();
        Assert.NotEmpty(verifying);
        Assert.Equal(2, verifying[^1].CellsRepaired);
    }
}
