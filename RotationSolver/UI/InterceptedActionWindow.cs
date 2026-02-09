using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;

namespace RotationSolver.UI;

internal class InterceptedActionWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ControlWindow.BaseFlags
		| ImGuiWindowFlags.AlwaysAutoResize
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoResize;

	public InterceptedActionWindow()
		: base(nameof(InterceptedActionWindow), BaseFlags)
	{
	}

	public override void PreDraw()
	{
		ImGui.PushStyleColor(ImGuiCol.WindowBg, Service.Config.InfoWindowBg);

		Flags = BaseFlags;
		if (Service.Config.IsInfoWindowNoInputs)
		{
			Flags |= ImGuiWindowFlags.NoInputs;
		}
		if (Service.Config.IsInfoWindowNoMove)
		{
			Flags |= ImGuiWindowFlags.NoMove;
		}
		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
		base.PreDraw();
	}

	public override void PostDraw()
	{
		ImGui.PopStyleColor();
		ImGui.PopStyleVar();
		base.PostDraw();
	}

	public override unsafe void Draw()
	{
		// Keep consistent sizing with Control/NextAction windows
		Basic.Configuration.Configs config = Service.Config;
		float gcdWidth = config.ControlWindowGCDSize * config.ControlWindowNextSizeRatio;
		float abilityWidth = config.ControlWindow0GCDSize * config.ControlWindowNextSizeRatio;
		float totalWidth = gcdWidth + abilityWidth + ImGui.GetStyle().ItemSpacing.X;

		// Title
		string title = "Intercept System";
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (totalWidth / 2) - (ImGui.CalcTextSize(title).X / 2));
		ImGui.TextColored(ImGuiColors.DalamudYellow, title);

		ImGui.Spacing();

		// Draw the "current" intercepted action (left)
		var cur = DataCenter.CurrentInterceptedAction;

		// If there's no current intercepted action, show placeholder text
		if (cur == null)
		{
			ImGui.TextColored(ImGuiColors.DalamudGrey, "No intercepted actions queued.");
			return;
		}

		// Draw current intercepted action (large / left)
		ImGui.TextColored(ImGuiColors.DalamudWhite, "Current Intercepted Action");
		ControlWindow.DrawIAction(cur, gcdWidth, 1);
	}
}