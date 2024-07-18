using System.Xml.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Files;
using RotationSolver.Basic.Configuration;
using RotationSolver.Updaters;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightHotbar;

internal static class PainterManager  // TODO: Rename to more fitting name (ex. 'TeachingModeHighlight')
{
    private static DrawingHighlightHotbar? _highLight;
    public static HashSet<HotbarID> HotbarIDs => _highLight?.HotbarIDs ?? [];

    public static Vector4 HighlightColor // TODO: Check if we can directly use the config value
    {
        get => _highLight?.Color ?? Vector4.One;
        set
        {
            if (_highLight == null) return;
            _highLight.Color = value;
        }
    }

    public static void Init() // called by: RotationSolverPlugin during initialization
    {
        RSRMainDrawer.Init(Svc.PluginInterface, RSRMainDrawer._name); //TODO: check if we can merge 'RSRMainDrawer.Init' into 'PainterManager.Init'

        _highLight = new();
        UpdateSettings(); // Updates initial value's, then gets called by the 'UpdateWork' method in MajorUpdater.cs
    }

    public static void UpdateSettings()
    {
        RSRMainDrawer.Enable = !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] && Service.Config.TeachingMode && MajorUpdater.IsValid;
        RSRMainDrawer.ViewPadding = Vector4.One * 50;

        HighlightColor = Service.Config.TeachingModeColor;
    }

    public static void Dispose()
    {
        RSRMainDrawer.Dispose();
    }
}

public class DrawingHighlightHotbar : IDrawing
{
    private static readonly Vector2 _uv1 = new(96 * 5 / 852f, 0),
        _uv2 = new((96 * 5 + 144) / 852f, 0.5f);

    private static IDalamudTextureWrap? _texture = null;

    /// <summary>
    /// The action ids that will be highlighted.
    /// </summary>
    public HashSet<HotbarID> HotbarIDs { get; } = [];

    /// <summary>
    /// The color of highlight.
    /// </summary>
    public Vector4 Color { get; set; } = new Vector4(0.8f, 0.5f, 0.3f, 1);

    /// <summary>
    /// 
    /// </summary>
    public DrawingHighlightHotbar()
    {
        if (_texture != null) return;
        var tex = Svc.Data?.GetFile<TexFile>("ui/uld/icona_frame_hr1.tex");
        if (tex == null) return;
        byte[] imageData = tex.ImageData;
        byte[] array = new byte[imageData.Length];

        for (int i = 0; i < imageData.Length; i += 4)
        {
            array[i] = array[i + 1] = array[i + 2] = byte.MaxValue;
            array[i + 3] = imageData[i + 3];
        }

        _texture = Svc.Texture.CreateFromRaw(RawImageSpecification.Rgba32(tex.Header.Width, tex.Header.Height), array);
    }

    private static unsafe bool IsVisible(AtkUnitBase unit)
    {
        if (!unit.IsVisible) return false;
        if (unit.VisibilityFlags == 1) return false;

        return IsVisible(unit.RootNode);
    }

    private static unsafe bool IsVisible(AtkResNode* node)
    {
        while (node != null)
        {
            if (!node->IsVisible()) return false;
            node = node->ParentNode;
        }

        return true;
    }

    private protected override unsafe IEnumerable<IDrawing2D> To2D()
    {
        if (_texture == null) return [];

        List<IDrawing2D> result = [];

        var hotBarIndex = 0;
        foreach (var intPtr in GetAddons<AddonActionBar>()
            .Union(GetAddons<AddonActionBarX>())
            .Union(GetAddons<AddonActionCross>())
            .Union(GetAddons<AddonActionDoubleCrossBase>()))
        {
            var actionBar = (AddonActionBarBase*)intPtr;
            if (actionBar != null && IsVisible(actionBar->AtkUnitBase))
            {
                var s = actionBar->AtkUnitBase.Scale;

                var isCrossBar = hotBarIndex > 9;
                if (isCrossBar)
                {
                    if (hotBarIndex == 10)
                    {
                        var actBar = (AddonActionCross*)intPtr;
                        hotBarIndex = actBar->RaptureHotbarId;
                    }
                    else
                    {
                        var actBar = (AddonActionDoubleCrossBase*)intPtr;
                        hotBarIndex = actBar->BarTarget;
                    }
                }
                var hotBar = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule()->Hotbars[hotBarIndex];

                var slotIndex = 0;
                foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
                {
                    var iconAddon = slot.Icon;
                    if ((nint)iconAddon != nint.Zero && IsVisible(&iconAddon->AtkResNode))
                    {
                        AtkResNode node = default;
                        HotbarSlot bar = hotBar.Slots[slotIndex];

                        if (isCrossBar)
                        {
                            var manager = slot.Icon->AtkResNode.ParentNode->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager;

                            for (var i = 0; i < manager.NodeListCount; i++)
                            {
                                node = *manager.NodeList[i];
                                if (node.Width == 72) break;
                            }
                        }
                        else
                        {
                            node = *slot.Icon->AtkResNode.ParentNode->ParentNode;
                        }

                        if (IsActionSlotRight(slot, bar))
                        {
                            var pt1 = new Vector2(node.ScreenX, node.ScreenY);
                            var pt2 = pt1 + new Vector2(node.Width * s, node.Height * s);

                            result.Add(new ImageDrawing(_texture, pt1, pt2, _uv1, _uv2, ImGui.ColorConvertFloat4ToU32(Color)));
                        }
                    }

                    slotIndex++;
                }
            }

            hotBarIndex++;
        }

        return result;
    }

    /// <inheritdoc/>
    protected override void UpdateOnFrame() // TODO: Remove redundant jump!
    {
        return;
    }

    private static unsafe IEnumerable<nint> GetAddons<T>() where T : struct
    {
        if (typeof(T).GetCustomAttribute<Addon>() is not Addon on) return [];

        return on.AddonIdentifiers
            .Select(str => Svc.GameGui.GetAddonByName(str, 1))
            .Where(ptr => ptr != nint.Zero);
    }

    private unsafe bool IsActionSlotRight(ActionBarSlot slot, HotbarSlot hot)
    {
        var actionId = ActionManager.Instance()->GetAdjustedActionId((uint)slot.ActionId);
        foreach (var hotbarId in HotbarIDs)
        {
            if (hot.OriginalApparentSlotType != hotbarId.SlotType) continue;
            if (hot.ApparentSlotType != hotbarId.SlotType) continue;
            if (actionId != hotbarId.Id) continue;

            return true;
        }

        return false;
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

internal class OverlayWindow : Window // TODO: un-comment ImGuiWindowFlags NoBackground/NoTitleBar/NoResize
{
    public OverlayWindow() : base(RSRMainDrawer._name, /*ImGuiWindowFlags.NoBackground |*/ ImGuiWindowFlags.NoBringToFrontOnFocus
            /*| ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize*/ | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav, true)
    {
        IsOpen = true;
        BgAlpha = 0.5f; // TODO: remove
        AllowClickthrough = true;
        ForceMainWindow = true;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);

        base.PreDraw();
    }

    public override void Draw()
    {
        if (!RSRMainDrawer.Enable || Svc.ClientState == null || Svc.ClientState.LocalPlayer == null) return;

        ImGui.GetStyle().AntiAliasedFill = false;

        try
        {
            if (!Service.Config.UseWorkTask)
            {
                RSRMainDrawer._drawingElements2D = RSRMainDrawer.To2DAsync().Result;
            }

            foreach (var item in RSRMainDrawer._drawingElements2D.OrderBy(drawing =>
            {
                if (drawing is PolylineDrawing poly)
                {
                    return poly._thickness == 0 ? 0 : 1;
                }
                else
                {
                    return 2;
                }
            }))
            {
                item.Draw();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"{RSRMainDrawer._name} failed to draw on Screen.");
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }
}



public static class RSRMainDrawer // TODO: Check if we can merge this entire class into 'PainterManager.Init'
{
    internal static string _name = "RSR - Overlay (Main Drawer)";

    internal static readonly List<IDrawing> _drawingElements = [];
    internal static IDrawing2D[] _drawingElements2D = [];

    private static WindowSystem? windowSystem;

    #region Config
    public static bool Enable { get; set; } = false;

    public static bool UseTaskToAccelerate { get; set; } = false; // TODO: Check if we can remove this entirely

    public static Vector4 ViewPadding { get; set; } = Vector4.One * 50;  // TODO: Check if we can remove this entirely
    #endregion

    private static bool _initiated = false;

    public static void Init(IDalamudPluginInterface pluginInterface, string name)
    {
        if (_initiated) return;
        _initiated = true;

        _name = name;
        pluginInterface.Create<Service>();
        windowSystem = new WindowSystem(_name);
        windowSystem.AddWindow(new OverlayWindow());

        Svc.PluginInterface.UiBuilder.Draw += OnDraw;
        Svc.Framework.Update += Update;
    }

    private static void OnDraw()
    {
        if (Svc.GameGui.GameUiHidden) return;
        if (!Service.Config.TeachingMode) return;
        windowSystem?.Draw();
    }

    public static void Dispose()
    {
        if (!_initiated) return;
        _initiated = false;

        foreach (var item in new List<IDrawing>(_drawingElements))
        {
            item.Dispose();
        }

        Svc.PluginInterface.UiBuilder.Draw -= OnDraw;
        Svc.Framework.Update -= Update;
    }

    private static void Update(IFramework framework)
    {
        if (!Enable || Svc.ClientState == null || Svc.ClientState.LocalPlayer == null) return;

        if (_started) return;
        _started = true;

        System.Threading.Tasks.Task.Run(UpdateData);
    }

    private static bool _started = false;
    private static async void UpdateData()
    {
        try
        {
            List<System.Threading.Tasks.Task> tasks = [];

            foreach (var ele in new List<IDrawing>(_drawingElements))
            {
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        ele.UpdateOnFrameMain();
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Warning(ex, "Something wrong with " + nameof(IDrawing.UpdateOnFrameMain));
                    }
                }));
            }

            await System.Threading.Tasks.Task.WhenAll([.. tasks]);

            if (UseTaskToAccelerate)  // TODO: Check if we can remove this entirely
            {
                _drawingElements2D = await To2DAsync();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Something wrong with drawing");
        }

        _started = false;
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

public abstract class BasicDrawing : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// If it is enabled.
    /// </summary>
    public virtual bool Enable { get; set; } = true;

    /// <summary>
    /// The time that it will disappear.
    /// </summary>
    public DateTime DeadTime { get; set; } = DateTime.MinValue;

    private protected BasicDrawing()
    {
        Svc.Framework.Update += Framework_Update;
    }

    private void Framework_Update(IFramework framework)
    {
        if (DeadTime != DateTime.MinValue && DeadTime < DateTime.Now)
        {
            Dispose();
            return;
        }

        AdditionalUpdate();
    }

    private protected virtual void AdditionalUpdate()
    {

    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Svc.Framework.Update -= Framework_Update;
        AdditionalDispose();
        GC.SuppressFinalize(this);
    }

    private protected virtual void AdditionalDispose()
    {

    }
}

public abstract class IDrawing : BasicDrawing
{
    private protected IDrawing()  // TODO: Has 0 references check if we can remove this entirely
    {
        RSRMainDrawer._drawingElements.Add(this);
    }

    internal void UpdateOnFrameMain()
    {
        if (!Enable) return;
        UpdateOnFrame();
    }

    /// <summary>
    /// The things that it should update on every frame.
    /// </summary>
    protected abstract void UpdateOnFrame();

    internal IEnumerable<IDrawing2D> To2DMain()
    {
        if (!Enable) return [];
        return To2D();
    }

    private protected abstract IEnumerable<IDrawing2D> To2D();

    private protected override void AdditionalDispose()
    {
        RSRMainDrawer._drawingElements.Remove(this);
    }
}

/// <summary>
/// The Hot bar ID
/// </summary>
public readonly record struct HotbarID(HotbarSlotType SlotType, uint Id)
{
    // TODO: Initial testing shows no difference with this block commented out or not.
    ///// <summary>
    ///// Convert from a action id.
    ///// </summary>
    ///// <param name="actionId"></param>
    //public static implicit operator HotbarID(uint actionId) => new(HotbarSlotType.Action, actionId);
}