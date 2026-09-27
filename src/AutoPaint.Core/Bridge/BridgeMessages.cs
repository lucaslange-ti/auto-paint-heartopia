using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoPaint.Core.Bridge;

/// <summary>
/// Protocolo da ponte WebView2 &lt;-&gt; C#. Transporte: WebMessageReceived +
/// PostWebMessageAsJson. Ver docs/ponte-protocolo.md.
/// </summary>
public sealed class BridgeRequest
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("payload")] public JsonElement? Payload { get; set; }
}

public sealed class BridgeResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("result")] public object? Result { get; set; }
    [JsonPropertyName("error")] public BridgeError? Error { get; set; }

    public static BridgeResponse Success(string id, object? result = null) => new() { Id = id, Ok = true, Result = result };
    public static BridgeResponse Failure(string id, string code, string message) =>
        new() { Id = id, Ok = false, Error = new BridgeError { Code = code, Message = message } };
}

public sealed class BridgeError
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

/// <summary>Mensagem empurrada do C# para a UI (sem request correspondente).</summary>
public sealed class BridgeEvent
{
    [JsonPropertyName("type")] public string Type { get; } = "event";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("payload")] public object? Payload { get; set; }

    public static BridgeEvent Of(string name, object? payload = null) => new() { Name = name, Payload = payload };
}

/// <summary>Nomes de request (UI -> C#).</summary>
public static class BridgeRequests
{
    public const string AppPing = "app/ping";
    public const string AppInfo = "app/info";
    public const string ConfigGet = "config/get";
    public const string ConfigSet = "config/set";
    public const string ConfigReset = "config/reset";
    public const string ImageConvert = "image/convert";
    public const string CalibrationStartRect = "calibration/start-rect";
    public const string CalibrationStartGrid = "calibration/start-grid";
    public const string CalibrationDetectRect = "calibration/detect-rect";
    public const string CalibrationCapturePoint = "calibration/capture-point";
    public const string PaintStart = "paint/start";
    public const string PaintPause = "paint/pause";
    public const string PaintResume = "paint/resume";
    public const string PaintAbort = "paint/abort";
    public const string PaintSeek = "paint/seek";
    public const string PaintRepair = "paint/repair";
}

/// <summary>Nomes de evento (C# -> UI).</summary>
public static class BridgeEvents
{
    public const string PaintProgress = "paint/progress";
    public const string PaintStatus = "paint/status";
    public const string CalibrationPointCaptured = "calibration/point-captured";
    public const string CalibrationRectCaptured = "calibration/rect-captured";
    public const string Toast = "app/toast";
}
