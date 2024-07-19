using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;

namespace RotationSolver.UI.HighlightHotbar;

/// <summary>
/// Originally belonged in the XIVDrawer namespace, however we gave it its own class.
/// Since we integrate XIVDrawer into RSR instead of using the submodule we don't have to place it in the same class as HotbarHighlightDrawerManager 
/// </summary>
internal class OverlayWindow : Window
{
    const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoBackground
    | ImGuiWindowFlags.NoBringToFrontOnFocus
    | ImGuiWindowFlags.NoDecoration
    | ImGuiWindowFlags.NoDocking
    | ImGuiWindowFlags.NoFocusOnAppearing
    | ImGuiWindowFlags.NoInputs
    | ImGuiWindowFlags.NoNav;

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

    public override void Draw()
    {
        if (!HotbarHighlightDrawerManager.Enable || Svc.ClientState == null || Svc.ClientState.LocalPlayer == null) return;

        ImGui.GetStyle().AntiAliasedFill = false;

        try
        {
            if (!HotbarHighlightDrawerManager.UseTaskToAccelerate)
            {
                HotbarHighlightDrawerManager._drawingElements2D = HotbarHighlightDrawerManager.To2DAsync().Result;
            }

            foreach (var item in HotbarHighlightDrawerManager._drawingElements2D.OrderBy(drawing =>
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
            Svc.Log.Warning(ex, $"{nameof(OverlayWindow)} failed to draw on Screen.");
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }
}
