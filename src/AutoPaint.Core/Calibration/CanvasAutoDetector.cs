using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Calibration;

/// <summary>
/// Resultado da detecção automática do canvas. <see cref="Confidence"/> 0 = não achou
/// (<see cref="Reason"/> diz por quê); &gt; 0 = retângulo utilizável, em pixels físicos.
/// </summary>
public sealed record CanvasDetection(PhysicalRect Rect, double Confidence, string? Reason)
{
    public bool Found => Confidence > 0 && !Rect.IsEmpty;
    public static CanvasDetection Fail(string reason) => new(default, 0, reason);
}

/// <summary>
/// Acha a superfície de desenho do Heartopia numa captura de tela, sem nenhum clique.
/// A superfície é a faixa branca hachurada: pixels quase brancos (canais altos e
/// próximos) cercados pelo creme amarelado do painel.
///
/// Heurística: classifica "branco de pintura", pega o maior componente conectado,
/// valida forma / tamanho / borda de creme. Puro e testável — a captura vem de fora.
/// v1: assume o jogo no monitor primário, modo pintura aberto e desobstruído.
/// </summary>
public static class CanvasAutoDetector
{
    // "Branco de pintura": todos os canais altos e pouco espalhados (aceita o leve
    // hachurado, recusa o creme #EFE3C9 do painel).
    private const int WhiteMinChannel = 210;
    private const int WhiteMaxSpread = 24;

    // Área branca mínima (em amostras da grade) e preenchimento mínimo do bounding box.
    private const int MinSamples = 400;
    private const double MinFillRatio = 0.80;

    public static CanvasDetection Detect(ScreenCapture capture)
    {
        int w = capture.Bounds.Width, h = capture.Bounds.Height;
        if (w < 64 || h < 64) return CanvasDetection.Fail("capture_too_small");

        int step = Math.Max(1, Math.Min(w, h) / 500);
        int gw = (w + step - 1) / step;
        int gh = (h + step - 1) / step;

        var paint = new bool[gw * gh];
        for (int gy = 0; gy < gh; gy++)
        {
            int py = Math.Min(gy * step, h - 1);
            for (int gx = 0; gx < gw; gx++)
            {
                int px = Math.Min(gx * step, w - 1);
                paint[gy * gw + gx] = IsPaintWhite(capture.Pixels[py * w + px]);
            }
        }

        // Maior componente conectado (4-vizinhos) de "branco de pintura".
        var seen = new bool[gw * gh];
        var stack = new Stack<int>();
        int bestCount = 0, bMinX = 0, bMinY = 0, bMaxX = 0, bMaxY = 0;

        bool Take(int i)
        {
            if (!paint[i] || seen[i]) return false;
            seen[i] = true;
            return true;
        }

        for (int origin = 0; origin < paint.Length; origin++)
        {
            if (!Take(origin)) continue;

            int count = 0, minX = gw, minY = gh, maxX = -1, maxY = -1;
            stack.Push(origin);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % gw, y = i / gw;
                count++;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;

                if (x > 0 && Take(i - 1)) stack.Push(i - 1);
                if (x < gw - 1 && Take(i + 1)) stack.Push(i + 1);
                if (y > 0 && Take(i - gw)) stack.Push(i - gw);
                if (y < gh - 1 && Take(i + gw)) stack.Push(i + gw);
            }

            if (count > bestCount)
            {
                bestCount = count;
                bMinX = minX; bMinY = minY; bMaxX = maxX; bMaxY = maxY;
            }
        }

        if (bestCount < MinSamples) return CanvasDetection.Fail("no_white_area");

        int boxGW = bMaxX - bMinX + 1;
        int boxGH = bMaxY - bMinY + 1;

        double fill = bestCount / (double)(boxGW * boxGH);
        if (fill < MinFillRatio) return CanvasDetection.Fail("irregular_shape");

        double areaFrac = boxGW * boxGH / (double)(gw * gh);
        if (areaFrac is < 0.02 or > 0.80) return CanvasDetection.Fail("implausible_size");

        double aspect = boxGW / (double)boxGH;
        if (aspect is < 0.15 or > 6.0) return CanvasDetection.Fail("implausible_aspect");

        // Pelo menos um lado (esq/dir) cercado de creme -> descarta diálogo branco.
        bool leftCream = ColumnMostlyCream(capture, w, h, step, bMinX - 2, bMinY, bMaxY);
        bool rightCream = ColumnMostlyCream(capture, w, h, step, bMaxX + 2, bMinY, bMaxY);
        if (!leftCream && !rightCream) return CanvasDetection.Fail("no_cream_border");

        // Encosta em 3+ bordas da captura -> provavelmente não é o painel.
        int touching = (bMinX <= 0 ? 1 : 0) + (bMinY <= 0 ? 1 : 0)
                     + (bMaxX >= gw - 1 ? 1 : 0) + (bMaxY >= gh - 1 ? 1 : 0);
        if (touching >= 3) return CanvasDetection.Fail("fills_screen");

        // Bordas grosseiras (quantizadas ao passo de amostragem). O refino abaixo
        // varre pixel a pixel a transição creme<->branco, tirando o erro de ~step px
        // — crítico porque a grade divide a largura por dezenas de células.
        int coarseL = bMinX * step, coarseR = Math.Min(w - 1, (bMaxX + 1) * step - 1);
        int coarseT = bMinY * step, coarseB = Math.Min(h - 1, (bMaxY + 1) * step - 1);

        // Faixa central de cada borda (evita os cantos arredondados do painel).
        int yA = coarseT + (coarseB - coarseT) / 4, yB = coarseB - (coarseB - coarseT) / 4;
        int xA = coarseL + (coarseR - coarseL) / 4, xB = coarseR - (coarseR - coarseL) / 4;

        double left = RefineVerticalEdge(capture, w, coarseL, step, yA, yB, +1);
        double right = RefineVerticalEdge(capture, w, coarseR, step, yA, yB, -1);
        double top = RefineHorizontalEdge(capture, w, h, coarseT, step, xA, xB, +1);
        double bottom = RefineHorizontalEdge(capture, w, h, coarseB, step, xA, xB, -1);

        // left/right/top/bottom são as fronteiras fracionárias creme<->branco, então
        // a largura é a distância entre elas (sem o +1 do índice inteiro).
        int rx = capture.Bounds.X + (int)Math.Round(left);
        int ry = capture.Bounds.Y + (int)Math.Round(top);
        int rw = Math.Max(1, (int)Math.Round(right - left));
        int rh = Math.Max(1, (int)Math.Round(bottom - top));
        var rect = new PhysicalRect(rx, ry, rw, rh);

        double confidence = 0.55
            + 0.20 * Math.Clamp((fill - MinFillRatio) / (1 - MinFillRatio), 0, 1)
            + 0.15 * (((leftCream ? 1 : 0) + (rightCream ? 1 : 0)) / 2.0)
            + 0.10 * (touching == 0 ? 1 : 0);

        return new CanvasDetection(rect, Math.Clamp(confidence, 0, 1), null);
    }

    /// <summary>
    /// Acha, em torno de <paramref name="approxX"/>, a coluna onde a fração de
    /// "branco de pintura" na faixa <paramref name="y0"/>..<paramref name="y1"/>
    /// cruza 0,5, varrendo de fora para dentro (<paramref name="dir"/> = +1 borda
    /// esquerda, -1 direita). Interpola entre as duas colunas vizinhas -> sub-pixel.
    /// </summary>
    private static double RefineVerticalEdge(ScreenCapture cap, int w, int approxX, int step, int y0, int y1, int dir)
    {
        int lo = Math.Clamp(approxX - step - 2, 0, w - 1);
        int hi = Math.Clamp(approxX + step + 2, 0, w - 1);
        int from = dir > 0 ? lo : hi;
        int to = dir > 0 ? hi : lo;

        double prevFrac = dir > 0 ? 0.0 : 1.0;
        int prevX = from;
        for (int x = from; dir > 0 ? x <= to : x >= to; x += dir)
        {
            double frac = ColumnPaintFraction(cap, w, x, y0, y1);
            if (frac >= 0.5)
            {
                if (x != prevX && Math.Abs(frac - prevFrac) > 1e-6)
                    return prevX + (0.5 - prevFrac) / (frac - prevFrac) * (x - prevX);
                return x;
            }
            prevFrac = frac;
            prevX = x;
        }
        return approxX;
    }

    private static double RefineHorizontalEdge(ScreenCapture cap, int w, int h, int approxY, int step, int x0, int x1, int dir)
    {
        int lo = Math.Clamp(approxY - step - 2, 0, h - 1);
        int hi = Math.Clamp(approxY + step + 2, 0, h - 1);
        int from = dir > 0 ? lo : hi;
        int to = dir > 0 ? hi : lo;

        double prevFrac = dir > 0 ? 0.0 : 1.0;
        int prevY = from;
        for (int y = from; dir > 0 ? y <= to : y >= to; y += dir)
        {
            double frac = RowPaintFraction(cap, w, y, x0, x1);
            if (frac >= 0.5)
            {
                if (y != prevY && Math.Abs(frac - prevFrac) > 1e-6)
                    return prevY + (0.5 - prevFrac) / (frac - prevFrac) * (y - prevY);
                return y;
            }
            prevFrac = frac;
            prevY = y;
        }
        return approxY;
    }

    private static double ColumnPaintFraction(ScreenCapture cap, int w, int x, int y0, int y1)
    {
        if (y1 < y0) return 0;
        int hit = 0, n = 0;
        for (int y = y0; y <= y1; y++, n++)
            if (IsPaintWhite(cap.Pixels[y * w + x])) hit++;
        return n == 0 ? 0 : hit / (double)n;
    }

    private static double RowPaintFraction(ScreenCapture cap, int w, int y, int x0, int x1)
    {
        if (x1 < x0) return 0;
        int hit = 0, n = 0;
        for (int x = x0; x <= x1; x++, n++)
            if (IsPaintWhite(cap.Pixels[y * w + x])) hit++;
        return n == 0 ? 0 : hit / (double)n;
    }

    private static bool IsPaintWhite(Rgb c)
    {
        int min = Math.Min(c.R, Math.Min(c.G, c.B));
        int max = Math.Max(c.R, Math.Max(c.G, c.B));
        return min >= WhiteMinChannel && max - min <= WhiteMaxSpread;
    }

    private static bool IsCream(Rgb c)
        => c.R > 205 && c.G > 190 && c.B > 140 && c.B < c.R && c.R - c.B >= 12 && !IsPaintWhite(c);

    private static bool ColumnMostlyCream(ScreenCapture cap, int w, int h, int step, int gx, int gyFrom, int gyTo)
    {
        int px = gx * step;
        if (px < 0 || px >= w) return false;

        int cream = 0, total = 0;
        for (int gy = gyFrom; gy <= gyTo; gy++)
        {
            int py = Math.Min(gy * step, h - 1);
            if (py < 0) continue;
            total++;
            if (IsCream(cap.Pixels[py * w + px])) cream++;
        }
        return total > 0 && cream / (double)total >= 0.55;
    }
}
