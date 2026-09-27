using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using AutoPaint.App.Overlays;
using AutoPaint.App.Painting;
using AutoPaint.Core.Bridge;
using AutoPaint.Core.Calibration;
using AutoPaint.Core.Config;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Licensing;
using AutoPaint.Core.Primitives;
using Serilog;

namespace AutoPaint.App.Bridge;

/// <summary>
/// Roteia um <see cref="BridgeRequest"/> para o serviço certo do Core e devolve
/// <see cref="BridgeResponse"/>. Também expõe <see cref="EventRaised"/> para
/// empurrar eventos (progresso etc.) para a UI.
/// </summary>
public sealed class BridgeRouter
{
    private readonly IConfigStore _config;
    private readonly ILicenseGate _license;
    private readonly ImageToPlanService _imageToPlan;
    private readonly PaintSession _paint;
    private readonly CalibrationOverlayService _calibration;
    private readonly BridgeEventHub _events;

    public BridgeRouter(
        IConfigStore config,
        ILicenseGate license,
        ImageToPlanService imageToPlan,
        PaintSession paint,
        CalibrationOverlayService calibration,
        BridgeEventHub events)
    {
        _config = config;
        _license = license;
        _imageToPlan = imageToPlan;
        _paint = paint;
        _calibration = calibration;
        _events = events;
    }

    public void Emit(BridgeEvent ev) => _events.Emit(ev);

    public async Task<BridgeResponse> HandleAsync(BridgeRequest req)
    {
        try
        {
            return req.Type switch
            {
                BridgeRequests.AppPing => BridgeResponse.Success(req.Id, new { pong = true, at = DateTimeOffset.UtcNow }),
                BridgeRequests.AppInfo => BridgeResponse.Success(req.Id, AppInfo()),
                BridgeRequests.ConfigGet => BridgeResponse.Success(req.Id, _config.Current),
                BridgeRequests.ConfigSet => ConfigSet(req),
                BridgeRequests.ConfigReset => Do(req, () => { _config.Reset(); _config.Flush(); }),
                BridgeRequests.ImageConvert => ImageConvert(req),
                BridgeRequests.PaintStart => PaintStart(req),
                BridgeRequests.PaintPause => Do(req, _paint.Pause),
                BridgeRequests.PaintResume => Do(req, _paint.Resume),
                BridgeRequests.PaintAbort => Do(req, _paint.Abort),
                BridgeRequests.PaintSeek => PaintSeek(req),
                BridgeRequests.PaintRepair => PaintRepair(req),
                BridgeRequests.CalibrationStartRect => await CalibrationStartRect(req),
                BridgeRequests.CalibrationStartGrid => await CalibrationStartGrid(req),
                BridgeRequests.CalibrationDetectRect => await CalibrationDetectRect(req),
                BridgeRequests.CalibrationCapturePoint => await CalibrationCapturePoint(req),
                _ => NotImplemented(req)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha tratando {Type}", req.Type);
            return BridgeResponse.Failure(req.Id, "handler_error", ex.Message);
        }
    }

    private async Task<BridgeResponse> CalibrationStartRect(BridgeRequest req)
    {
        var rect = await _calibration.SelectCanvasRectAsync();
        if (rect is not { } r)
            return BridgeResponse.Success(req.Id, new { cancelled = true });

        _config.Current.Calibration.CanvasRect = RectDtoOf(r);
        _config.Current.Calibration.CanvasGrid = null; // volta ao modo retângulo
        _config.Save();

        var payload = RectPayload(r);
        Emit(BridgeEvent.Of(BridgeEvents.CalibrationRectCaptured, payload));
        return BridgeResponse.Success(req.Id, payload);
    }

    private async Task<BridgeResponse> CalibrationStartGrid(BridgeRequest req)
    {
        if (ResolveGrid(req.Payload) is not { } size)
            return BridgeResponse.Failure(req.Id, "bad_request", "aspecto/precisão ausentes ou inválidos");

        var grid = await _calibration.SelectCanvasGridAsync(
            size.Columns, size.Rows,
            "Centro da célula do canto SUPERIOR-ESQUERDO (1 de 2)",
            "Centro da célula do canto INFERIOR-DIREITO (2 de 2)");

        if (grid is null)
            return BridgeResponse.Success(req.Id, new { cancelled = true });

        PersistGrid(grid);
        Emit(BridgeEvent.Of(BridgeEvents.CalibrationRectCaptured, RectPayload(grid.BoundingRect)));
        return BridgeResponse.Success(req.Id, GridPayload(grid));
    }

    private async Task<BridgeResponse> CalibrationDetectRect(BridgeRequest req)
    {
        var det = await _calibration.DetectCanvasRectAsync();
        if (!det.Found)
        {
            Log.Information("Auto-detecção do canvas falhou: {Reason}", det.Reason);
            return BridgeResponse.Success(req.Id, new { found = false, reason = det.Reason ?? "not_found" });
        }

        var r = det.Rect;
        var grid = ResolveGrid(req.Payload) is { } size
            ? CanvasGrid.FromRect(r, size.Columns, size.Rows)
            : null;

        _config.Current.Calibration.CanvasRect = RectDtoOf(r);
        _config.Current.Calibration.CanvasGrid = grid is null ? null : GridDtoOf(grid);
        _config.Save();

        Emit(BridgeEvent.Of(BridgeEvents.CalibrationRectCaptured, RectPayload(r)));
        return BridgeResponse.Success(req.Id, new
        {
            found = true,
            confidence = det.Confidence,
            x = r.X, y = r.Y, width = r.Width, height = r.Height,
            grid = grid is null ? null : GridPayload(grid)
        });
    }

    private void PersistGrid(CanvasGrid grid)
    {
        _config.Current.Calibration.CanvasGrid = GridDtoOf(grid);
        _config.Current.Calibration.CanvasRect = RectDtoOf(grid.BoundingRect);
        _config.Save();
    }

    /// <summary>(colunas, linhas) do preset a partir de aspect+precision no payload.</summary>
    private static (int Columns, int Rows)? ResolveGrid(JsonElement? payload)
    {
        if (payload is not { ValueKind: JsonValueKind.Object } pl) return null;
        if (!pl.TryGetProperty("aspect", out var a) || !pl.TryGetProperty("precision", out var p)) return null;
        if (!Enum.TryParse<CanvasAspect>(a.GetString(), ignoreCase: true, out var aspect)) return null;
        if (!Enum.TryParse<CanvasPrecision>(p.GetString(), ignoreCase: true, out var precision)) return null;
        var s = CanvasPresets.Resolve(aspect, precision);
        return (s.Columns, s.Rows);
    }

    private static RectDto RectDtoOf(PhysicalRect r) => new() { X = r.X, Y = r.Y, Width = r.Width, Height = r.Height };

    private static CanvasGridDto GridDtoOf(CanvasGrid g) => new()
    {
        OriginX = g.OriginX, OriginY = g.OriginY, PitchX = g.PitchX, PitchY = g.PitchY,
        Columns = g.Columns, Rows = g.Rows
    };

    private static object RectPayload(PhysicalRect r) => new { x = r.X, y = r.Y, width = r.Width, height = r.Height };

    private static object GridPayload(CanvasGrid g) => new
    {
        originX = g.OriginX, originY = g.OriginY, pitchX = g.PitchX, pitchY = g.PitchY,
        columns = g.Columns, rows = g.Rows
    };

    private async Task<BridgeResponse> CalibrationCapturePoint(BridgeRequest req)
    {
        string target = "";
        string? customLabel = null;
        if (req.Payload is { ValueKind: JsonValueKind.Object } pl)
        {
            if (pl.TryGetProperty("target", out var t)) target = t.GetString() ?? "";
            if (pl.TryGetProperty("label", out var l)) customLabel = l.GetString();
        }

        var label = customLabel ?? target switch
        {
            "shadesPanel" => "Botão: abrir painel de tons",
            "back" => "Botão: voltar",
            "paintTool" => "Botão: ferramenta de pintura",
            _ => "Aponte o botão"
        };

        var hit = await _calibration.CapturePointAsync(label);
        if (hit is not { } h)
            return BridgeResponse.Success(req.Id, new { cancelled = true });

        var pointDto = new PointDto { X = h.Point.X, Y = h.Point.Y };
        switch (target)
        {
            case "shadesPanel": _config.Current.Calibration.ShadesPanelButtonPos = pointDto; break;
            case "back": _config.Current.Calibration.BackButtonPos = pointDto; break;
            case "paintTool": _config.Current.Calibration.PaintToolButtonPos = pointDto; break;
        }
        _config.Save();

        var payload = new { target, x = h.Point.X, y = h.Point.Y, rgb = new { r = h.Rgb.R, g = h.Rgb.G, b = h.Rgb.B } };
        Emit(BridgeEvent.Of(BridgeEvents.CalibrationPointCaptured, payload));
        return BridgeResponse.Success(req.Id, payload);
    }

    private BridgeResponse ConfigSet(BridgeRequest req)
    {
        if (req.Payload is not { } payload)
            return BridgeResponse.Failure(req.Id, "bad_request", "payload ausente");

        var incoming = payload.Deserialize<AppConfig>(JsonOpts);
        if (incoming is null)
            return BridgeResponse.Failure(req.Id, "bad_request", "config inválida");

        _config.Apply(incoming);
        _config.Save();
        return BridgeResponse.Success(req.Id);
    }

    private BridgeResponse ImageConvert(BridgeRequest req)
    {
        if (req.Payload is not { } payload)
            return BridgeResponse.Failure(req.Id, "bad_request", "payload ausente");

        var p = payload.Deserialize<ConvertPayload>(JsonOpts);
        if (p is null || string.IsNullOrWhiteSpace(p.ImageBase64))
            return BridgeResponse.Failure(req.Id, "bad_request", "imagem ausente");

        var palette = _config.Current.Calibration.ToDomain().Palette;
        if (palette.ShadeCount == 0)
            return BridgeResponse.Failure(req.Id, "not_calibrated", "Cadastre a paleta antes de converter.");

        byte[] bytes;
        try
        {
            var b64 = p.ImageBase64;
            int comma = b64.IndexOf(',');
            if (b64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                b64 = b64[(comma + 1)..];
            bytes = Convert.FromBase64String(b64);
        }
        catch (FormatException)
        {
            return BridgeResponse.Failure(req.Id, "bad_request", "base64 inválido");
        }

        using var stream = new MemoryStream(bytes);
        var plan = _imageToPlan.Convert(stream, p.Aspect, p.Precision, p.Fit, p.Resample, palette);
        _paint.SetPlan(plan);

        return BridgeResponse.Success(req.Id, ImageToPlanService.Describe(plan));
    }

    private BridgeResponse PaintSeek(BridgeRequest req)
    {
        if (req.Payload is not { ValueKind: JsonValueKind.Object } pl
            || !pl.TryGetProperty("cell", out var c)
            || !c.TryGetInt32(out int cell))
            return BridgeResponse.Failure(req.Id, "bad_request", "campo 'cell' ausente ou inválido");

        _paint.Seek(cell);
        return BridgeResponse.Success(req.Id, new { cell });
    }

    private BridgeResponse PaintStart(BridgeRequest req)
    {
        if (!_license.CanUse(out var reason))
            return BridgeResponse.Failure(req.Id, "not_licensed", reason ?? "indisponível");

        bool resume = false;
        if (req.Payload is { ValueKind: JsonValueKind.Object } pl && pl.TryGetProperty("resume", out var r))
            resume = r.ValueKind == JsonValueKind.True;

        _paint.Start(resume);
        return BridgeResponse.Success(req.Id, new { started = true });
    }

    private BridgeResponse PaintRepair(BridgeRequest req)
    {
        if (!_license.CanUse(out var reason))
            return BridgeResponse.Failure(req.Id, "not_licensed", reason ?? "indisponível");

        _paint.StartRepair();
        return BridgeResponse.Success(req.Id, new { started = true, repair = true });
    }

    private static BridgeResponse Do(BridgeRequest req, Action action)
    {
        action();
        return BridgeResponse.Success(req.Id);
    }

    private object AppInfo() => new
    {
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
        os = Environment.OSVersion.VersionString,
        language = _config.Current.Language,
        culture = CultureInfo.CurrentUICulture.Name,
        licensed = _license.CanUse(out _)
    };

    private static BridgeResponse NotImplemented(BridgeRequest req) =>
        BridgeResponse.Failure(req.Id, "not_implemented", $"'{req.Type}' ainda não implementado.");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private sealed record ConvertPayload(
        CanvasAspect Aspect,
        CanvasPrecision Precision,
        FitMode Fit,
        ResampleMode Resample,
        string ImageBase64);
}
