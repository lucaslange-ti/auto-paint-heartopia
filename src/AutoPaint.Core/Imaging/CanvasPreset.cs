namespace AutoPaint.Core.Imaging;

public enum CanvasAspect { OneToOne, SixteenNine, NineSixteen, FourThree, ThreeFour }

public enum CanvasPrecision { Small, Medium, Big, SuperLarge }

/// <summary>Dimensão de grade (colunas x linhas) para uma proporção + precisão.</summary>
public readonly record struct CanvasSize(int Columns, int Rows)
{
    public int CellCount => Columns * Rows;
}

/// <summary>
/// Tabela fixa de grades suportadas pelo Heartopia, espelhando o projeto de
/// referência (PckyDev/Heartopia-Image-Painter). T-Shirt fica para a v2.
/// </summary>
public static class CanvasPresets
{
    private static readonly Dictionary<(CanvasAspect, CanvasPrecision), CanvasSize> Map = new()
    {
        [(CanvasAspect.OneToOne, CanvasPrecision.Small)]      = new(30, 30),
        [(CanvasAspect.OneToOne, CanvasPrecision.Medium)]     = new(50, 50),
        [(CanvasAspect.OneToOne, CanvasPrecision.Big)]        = new(100, 100),
        [(CanvasAspect.OneToOne, CanvasPrecision.SuperLarge)] = new(150, 150),

        [(CanvasAspect.SixteenNine, CanvasPrecision.Small)]      = new(30, 18),
        [(CanvasAspect.SixteenNine, CanvasPrecision.Medium)]     = new(50, 28),
        [(CanvasAspect.SixteenNine, CanvasPrecision.Big)]        = new(100, 56),
        [(CanvasAspect.SixteenNine, CanvasPrecision.SuperLarge)] = new(150, 84),

        [(CanvasAspect.NineSixteen, CanvasPrecision.Small)]      = new(18, 30),
        [(CanvasAspect.NineSixteen, CanvasPrecision.Medium)]     = new(28, 50),
        [(CanvasAspect.NineSixteen, CanvasPrecision.Big)]        = new(56, 100),
        [(CanvasAspect.NineSixteen, CanvasPrecision.SuperLarge)] = new(84, 150),

        [(CanvasAspect.FourThree, CanvasPrecision.Small)]      = new(30, 24),
        [(CanvasAspect.FourThree, CanvasPrecision.Medium)]     = new(50, 38),
        [(CanvasAspect.FourThree, CanvasPrecision.Big)]        = new(100, 76),
        [(CanvasAspect.FourThree, CanvasPrecision.SuperLarge)] = new(150, 114),

        [(CanvasAspect.ThreeFour, CanvasPrecision.Small)]      = new(24, 30),
        [(CanvasAspect.ThreeFour, CanvasPrecision.Medium)]     = new(38, 50),
        [(CanvasAspect.ThreeFour, CanvasPrecision.Big)]        = new(76, 100),
        [(CanvasAspect.ThreeFour, CanvasPrecision.SuperLarge)] = new(114, 150),
    };

    public static CanvasSize Resolve(CanvasAspect aspect, CanvasPrecision precision) => Map[(aspect, precision)];

    public static IReadOnlyCollection<(CanvasAspect Aspect, CanvasPrecision Precision, CanvasSize Size)> All()
        => Map.Select(kv => (kv.Key.Item1, kv.Key.Item2, kv.Value)).ToArray();
}
