namespace AutoPaint.Core.Imaging;

/// <summary>
/// Decodifica e redimensiona uma imagem para uma <see cref="PixelGrid"/> CxR.
/// Única parte da conversão que depende de biblioteca de imagem — implementada
/// fora do Core (Platform.Windows / System.Drawing). Alpha é compositado sobre
/// branco, igual ao projeto de referência.
/// </summary>
public interface IImageRasterizer
{
    PixelGrid Rasterize(Stream imageData, CanvasSize target, FitMode fit, ResampleMode resample);
}
