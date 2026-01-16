using System.Net.WebSockets;
using System.Text;
using ECommons.DalamudServices;
using ECommons.Logging;
using Newtonsoft.Json.Linq;

namespace RotationSolver.Helpers;

/// <summary>
/// Connects to OverlayPlugin's WS server and consumes cactbot broadcast messages to drive RotationSolver states.
/// Expected broadcast payload example from cactbot overlay:
/// callOverlayHandler({ call: 'broadcast', msg: { key: 'rotationsolver', payload: { type: 'raidwide', duration: 6 }}})
/// </summary>
internal sealed class CactbotTimelineBridge : IDisposable
{
    private readonly CancellationTokenSource? _cts;
    private readonly Task? _loopTask;

    public CactbotTimelineBridge()
    {
        if (!Service.Config.EnableCactbotTimeline)
            return;

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => ConnectLoop(_cts.Token));
        PluginLog.Information("CactbotTimelineBridge: started");
    }

    private static async Task ConnectLoop(CancellationToken ct)
    {
        var url = "ws://127.0.0.1:10501/ws";

        var backoffMs = 1000;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListenAsync(new Uri(url), ct);
                backoffMs = 1000; // reset after a successful session ends
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"CactbotTimelineBridge: connection error: {ex.Message}");
            }

            try
            {
                await Task.Delay(backoffMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            backoffMs = Math.Min(backoffMs * 2, 30000);
        }
    }

    private static async Task ConnectAndListenAsync(Uri uri, CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

        await ws.ConnectAsync(uri, ct);
        PluginLog.Information($"CactbotTimelineBridge: connected to {uri}");
        if (Service.Config.InDebug)
            Svc.Toasts.ShowNormal($"RSR cactbot: connected {uri}");

        // Try to subscribe to BroadcastMessage events (OverlayPlugin convention)
        var subscribe = Encoding.UTF8.GetBytes("{\"call\":\"subscribe\",\"events\":[\"BroadcastMessage\"]}");
        await ws.SendAsync(new ArraySegment<byte>(subscribe), WebSocketMessageType.Text, true, ct);
        if (Service.Config.InDebug)
            PluginLog.Debug("CactbotTimelineBridge: subscribed to BroadcastMessage");

        var buffer = new byte[16 * 1024];
        var sb = new StringBuilder();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage) continue;

            var json = sb.ToString();
            sb.Clear();
            if (Service.Config.InDebug)
                //PluginLog.Debug($"CactbotTimelineBridge: recv: {json}");
            HandleMessage(json);
        }
    }

    private static void HandleMessage(string json)
    {
        try
        {
            var jobj = JObject.Parse(json);
            // Common shapes: { type: 'BroadcastMessage', msg: {...} } or { event: 'broadcast', message: {...} }
            var eventType = (string?)jobj["type"] ?? (string?)jobj["event"];
            if (eventType == null)
                return;

            if (!eventType.Equals("BroadcastMessage", StringComparison.OrdinalIgnoreCase)
                && !eventType.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var msg = jobj["msg"] ?? jobj["message"] ?? jobj["data"];
            if (msg is not JObject m)
                return;

            var payload = m["payload"] ?? m["data"] ?? m["detail"] ?? m["message"] ?? m;
            if (payload is not JObject p)
                return;

            var kind = ((string?)p["type"] ?? (string?)p["event"])?.Trim();
            var duration = p["duration"]?.Value<double>() ?? p["durationSeconds"]?.Value<double>() ?? 0.0;
            if (string.IsNullOrEmpty(kind))
                return;

            if (Service.Config.InDebug)
                PluginLog.Debug($"CactbotTimelineBridge: kind='{kind}', duration={duration}");

            ProcessEvent(kind!, duration);
        }
        catch (Exception ex)
        {
            if (Service.Config.InDebug)
                PluginLog.Debug($"CactbotTimelineBridge: parse error: {ex.Message}\n{json}");
        }
    }

    private static void ProcessEvent(string kind, double durationSeconds)
    {
        SpecialCommandType? special = kind.ToLowerInvariant() switch
        {
            "raidwide" or "aoe" or "raidwide_aoe" => SpecialCommandType.DefenseArea,
            "tankbuster" or "buster" => SpecialCommandType.DefenseSingle,
            "knockback" => SpecialCommandType.AntiKnockback,
            "untargetable_soon" or "immune_soon" or "downtime_soon" => SpecialCommandType.NoCasting,
            "clear" or "end" or "targetable" => SpecialCommandType.EndSpecial,
            _ => null,
        };

        if (special == null)
            return;

        if (special == SpecialCommandType.EndSpecial)
        {
            DataCenter.SpecialType = SpecialCommandType.EndSpecial;
            if (Service.Config.InDebug)
                PluginLog.Information($"CactbotTimelineBridge: clear special (from '{kind}')");
            if (Service.Config.ShowCactbotToasts)
                Svc.Toasts.ShowNormal("RSR: cactbot clear");
            return;
        }

        DataCenter.SpecialType = special.Value;
        PluginLog.Information($"CactbotTimelineBridge: '{kind}' -> {special} ({durationSeconds:F1}s)");
        if (Service.Config.ShowCactbotToasts)
            Svc.Toasts.ShowNormal($"RSR cactbot: {kind} → {special} ({durationSeconds:F1}s)");

        if (durationSeconds > 0)
        {
            var delay = TimeSpan.FromSeconds(durationSeconds);
            _ = Svc.Framework.RunOnTick(() =>
            {
                if (DataCenter.SpecialType == special)
                {
                    DataCenter.SpecialType = SpecialCommandType.EndSpecial;
                    if (Service.Config.InDebug)
                        PluginLog.Information($"CactbotTimelineBridge: auto-clear {special} after {durationSeconds:F1}s");
                }
            }, delay);
        }
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
    }
}
