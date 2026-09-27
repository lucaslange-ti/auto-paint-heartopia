using System.Text.Json;
using System.Windows.Threading;
using AutoPaint.Core.Bridge;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace AutoPaint.App.Bridge;

/// <summary>
/// Cola o <see cref="CoreWebView2"/> ao <see cref="BridgeRouter"/>:
/// WebMessageReceived -> router -> PostWebMessageAsJson; e eventos do router -> UI.
/// </summary>
public sealed class WebViewBridge : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        // A UI (TypeScript) fala camelCase; enums como string com o nome do membro.
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly CoreWebView2 _core;
    private readonly BridgeRouter _router;
    private readonly BridgeEventHub _events;
    private readonly Dispatcher _dispatcher;

    public WebViewBridge(CoreWebView2 core, BridgeRouter router, BridgeEventHub events, Dispatcher dispatcher)
    {
        _core = core;
        _router = router;
        _events = events;
        _dispatcher = dispatcher;

        _core.WebMessageReceived += OnWebMessageReceived;
        _events.EventRaised += OnRouterEvent;
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        BridgeRequest? req = null;
        try
        {
            req = JsonSerializer.Deserialize<BridgeRequest>(e.WebMessageAsJson, JsonOpts);
            if (req is null || string.IsNullOrEmpty(req.Id))
            {
                Log.Warning("Mensagem da UI ignorada (sem id/type): {Raw}", e.WebMessageAsJson);
                return;
            }

            var res = await _router.HandleAsync(req).ConfigureAwait(true);
            Post(res);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro processando mensagem da UI");
            if (req is not null)
                Post(BridgeResponse.Failure(req.Id, "bridge_error", ex.Message));
        }
    }

    private void OnRouterEvent(BridgeEvent ev)
    {
        if (_dispatcher.CheckAccess()) Post(ev);
        else _dispatcher.BeginInvoke(() => Post(ev));
    }

    private void Post(object message)
    {
        try
        {
            _core.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOpts));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha ao postar mensagem para a UI");
        }
    }

    public void Dispose()
    {
        _core.WebMessageReceived -= OnWebMessageReceived;
        _events.EventRaised -= OnRouterEvent;
    }
}
