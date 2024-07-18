using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using RotationSolver.Updaters;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightHotbar;

internal static class HotbarHighlight
{
    internal const string _name = "RSR - Overlay (Hotbat Highlight)";
    private static HotbarHighlightDrawing? _highLight;
    public static HashSet<HotbarID> HotbarIDs => _highLight?.HotbarIDs ?? [];
    internal static readonly List<HotbarHighlightDrawing> _drawingElements = [];
    internal static IDrawing2D[] _drawingElements2D = [];
    //private static IDalamudPluginInterface? _pluginInterface;

    private static WindowSystem? windowSystem;

    #region Config
    public static bool Enable { get; set; } = false;
    #endregion

    private static bool _initiated = false;
    public static void Init()
    {
        if (_initiated) return;
        _initiated = true;

        //_pluginInterface = Svc.PluginInterface;
        //_pluginInterface.Create<Service>();
        _highLight = new();
        UpdateSettings();
        windowSystem = new WindowSystem(_name);
        windowSystem.AddWindow(new OverlayWindow());

        Svc.PluginInterface.UiBuilder.Draw += OnDraw;
    }

    public static void Dispose()
    {
        if (!_initiated) return;
        _initiated = false;

        foreach (var item in new List<HotbarHighlightDrawing>(_drawingElements))
        {
            item.Dispose();
        }

        Svc.PluginInterface.UiBuilder.Draw -= OnDraw;
    }

    public static void UpdateSettings()
    {
        Enable = !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] && Service.Config.TeachingMode && MajorUpdater.IsValid;

        if (_highLight != null)
        {
            _highLight.Color = Service.Config.TeachingModeColor;
        }
        else
        {
            Svc.Log.Warning("_highLight is null!");
        }
    }

    private static void OnDraw()
    {
        if (Svc.GameGui.GameUiHidden) return;
        if (!Service.Config.TeachingMode) return;
        windowSystem?.Draw();
    }

    internal static async Task<IDrawing2D[]> To2DAsync()
    {
        List<Task<IEnumerable<IDrawing2D>>> drawing2Ds = [];

        if (_drawingElements != null)
        {
            drawing2Ds.AddRange(_drawingElements.Select(item => System.Threading.Tasks.Task.Run(() =>
            {
                return item.To2DMain();
            })));
        }

        await System.Threading.Tasks.Task.WhenAll([.. drawing2Ds]);
        return drawing2Ds.SelectMany(i => i.Result).ToArray();
    }
}

/// <summary>
/// 2D drawing element.
/// </summary>
public interface IDrawing2D
{
    /// <summary>
    /// Draw on the <seealso cref="ImGui"/>
    /// </summary>
    void Draw();
}

public readonly struct ImageDrawing(IDalamudTextureWrap texture, Vector2 pt1, Vector2 pt2, uint col = uint.MaxValue) : IDrawing2D
{
    private readonly IDalamudTextureWrap _texture = texture;
    private readonly Vector2 _pt1 = pt1, _pt2 = pt2, _uv1 = default, _uv2 = Vector2.One;
    private readonly uint _col = col;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="pt1"></param>
    /// <param name="pt2"></param>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="col"></param>
    public ImageDrawing(IDalamudTextureWrap texture, Vector2 pt1, Vector2 pt2,
        Vector2 uv1, Vector2 uv2, uint col = uint.MaxValue)
        : this(texture, pt1, pt2, col)
    {
        _uv1 = uv1;
        _uv2 = uv2;
    }

    /// <summary>
    /// Draw on the <seealso cref="ImGui"/>
    /// </summary>
    public void Draw()
    {
        ImGui.GetWindowDrawList().AddImage(_texture.ImGuiHandle, _pt1, _pt2, _uv1, _uv2, _col);
    }
}

public readonly struct PolylineDrawing(Vector2[] pts, uint color, float thickness) : IDrawing2D
{
    private readonly Vector2[] _pts = pts;
    private readonly uint _color = color;
    internal readonly float _thickness = thickness;

    /// <summary>
    /// Draw on the <seealso cref="ImGui"/>
    /// </summary>
    public void Draw()
    {
        if (_pts == null || _pts.Length < 2) return;

        foreach (var pt in _pts)
        {
            ImGui.GetWindowDrawList().PathLineTo(pt);
        }

        if (_thickness == 0)
        {
            ImGui.GetWindowDrawList().PathFillConvex(_color);
        }
        else if (_thickness < 0)
        {
            ImGui.GetWindowDrawList().PathStroke(_color, ImDrawFlags.RoundCornersAll, -_thickness);
        }
        else
        {
            ImGui.GetWindowDrawList().PathStroke(_color, ImDrawFlags.Closed | ImDrawFlags.RoundCornersAll, _thickness);
        }
    }
}

/// <summary>
/// The Hot bar ID
/// </summary>
public readonly record struct HotbarID(HotbarSlotType SlotType, uint Id) { }