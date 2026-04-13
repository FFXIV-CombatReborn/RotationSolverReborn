using Dalamud.Interface.Windowing;

namespace RotationSolver.UI;

internal abstract class CtrlWindow(string name) : Window(name, BaseFlags)
{
	public const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoScrollbar
						| ImGuiWindowFlags.NoNav
						| ImGuiWindowFlags.NoScrollWithMouse;

	public override void PreDraw()
	{
		Basic.Configuration.Configs config = Service.Config;
		Vector4 bgColor = config.IsControlWindowLock
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