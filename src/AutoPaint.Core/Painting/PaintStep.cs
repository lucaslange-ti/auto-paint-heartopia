using AutoPaint.Core.Calibration;

namespace AutoPaint.Core.Painting;

public readonly record struct CellPos(int Column, int Row);

/// <summary>
/// Uma unidade da ordem de pintura: um tom já selecionado e as células a clicar
/// em sequência. No modo linha, cada step é um trecho horizontal contíguo; no
/// modo cor, é um grupo inteiro do mesmo tom.
/// </summary>
public sealed record PaintStep(ShadeRef Shade, IReadOnlyList<CellPos> Cells);
