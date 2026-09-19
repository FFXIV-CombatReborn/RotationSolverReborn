using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.UI.HighlightTeachingMode;

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

		// The drawings are built on the framework thread; drawing only reads the latest snapshot.
		var elements = HotbarHighlightManager.Elements2D;
		if (elements.Length == 0)
		{
			return;
		}

		// Save and disable AA fill for performance of large overlays
		var prevAAFill = ImGui.GetStyle().AntiAliasedFill;
		ImGui.GetStyle().AntiAliasedFill = false;

		try
		{
			var drawList = ImGui.GetWindowDrawList();
			if (drawList.Handle == null)
			{
				PluginLog.Warning($"{nameof(OverlayWindow)}: Window draw list is null.");
				return;
			}

			foreach (var item in elements)
			{
				item.Draw();
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

	public override void PostDraw()
	{
		ImGui.PopStyleVar();
		base.PostDraw();
	}
}
