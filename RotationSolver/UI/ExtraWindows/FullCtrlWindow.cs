using Dalamud.Interface.Windowing;

namespace RotationSolver.UI.ExtraWindows;

internal abstract class FullCtrlWindow(string name) : Window(name, BaseFlags)
{
	public const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoScrollbar
						| ImGuiWindowFlags.NoNav
						| ImGuiWindowFlags.NoScrollWithMouse;

	public override void PreDraw()
	{
		var config = Service.Config;
		var bgColor = config.IsControlWindowLock
			? config.ControlWindowLockBg
			: config.ControlWindowUnlockBg;
		ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);

		Flags = BaseFlags;
		if (config.IsControlWindowLock)
		{
			Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
		}

		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		ImGui.PopStyleColor();
		ImGui.PopStyleVar();
	}
}
