using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.UI.HighlightTeachingMode;
using System.Diagnostics;

namespace RotationSolver.UI;

/// <summary>
/// The Overlay Window
/// </summary>
internal class OverlayWindow : Window
{
    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoBackground
    | ImGuiWindowFlags.NoBringToFrontOnFocus
    | ImGuiWindowFlags.NoDecoration
    | ImGuiWindowFlags.NoDocking
    | ImGuiWindowFlags.NoFocusOnAppearing
    | ImGuiWindowFlags.NoInputs
    | ImGuiWindowFlags.NoNav;

    // Async update support and throttling for sync path
    private IDrawing2D[]? _elements;
    private readonly Stopwatch _throttle = Stopwatch.StartNew();
    private const int SyncUpdateMs = 33; // ~30 FPS updates in sync mode

    public OverlayWindow()
        : base(nameof(OverlayWindow), BaseFlags, true)
    {
        IsOpen = true;
        AllowClickthrough = true;
        RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);

        base.PreDraw();
    }

    public override unsafe void Draw()
    {
        if (!HotbarHighlightManager.Enable || Svc.ClientState == null || Svc.Objects.LocalPlayer == null)
        {
            return;
        }

        // Save and disable AA fill for performance of large overlays
        bool prevAAFill = ImGui.GetStyle().AntiAliasedFill;
        ImGui.GetStyle().AntiAliasedFill = false;

        try
        {
			if (_throttle.ElapsedMilliseconds >= SyncUpdateMs)
			{
				var result = HotbarHighlightManager.To2DAsync().GetAwaiter().GetResult() ?? [];
				var list = new List<IDrawing2D>(result);
				list.Sort((a, b) => GetDrawingOrder(a).CompareTo(GetDrawingOrder(b)));
				_elements = [.. list];
				_throttle.Restart();
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            if (drawList.Handle == null)
            {
                PluginLog.Warning($"{nameof(OverlayWindow)}: Window draw list is null.");
                return;
            }

            var elements = _elements;
            if (elements != null)
            {
                foreach (IDrawing2D item in elements)
                {
                    item.Draw();
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"{nameof(OverlayWindow)} failed to draw on Screen. {ex.Message}");
        }
        finally
        {
            ImGui.GetStyle().AntiAliasedFill = prevAAFill;
        }
    }

    private static int GetDrawingOrder(object drawing)
    {
        return drawing switch
        {
            PolylineDrawing poly => poly._thickness == 0 ? 0 : 1,
            ImageDrawing => 1,
            _ => 2,
        };
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }
}
