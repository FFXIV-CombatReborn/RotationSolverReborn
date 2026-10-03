using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using RotationSolver.UI.Material;

namespace RotationSolver.UI.ExtraWindows;

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

	private M3.FontScope _font;

	public override void PreDraw()
	{
		_font = M3.PushBody();
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
		_font.Dispose();
		_font = default;
		base.PostDraw();
	}

	public override unsafe void Draw()
	{
		var config = Service.Config;
		var gcdWidth = config.ControlWindowGCDSize * config.ControlWindowNextSizeRatio;
		var abilityWidth = config.ControlWindow0GCDSize * config.ControlWindowNextSizeRatio;
		var totalWidth = gcdWidth + abilityWidth + ImGui.GetStyle().ItemSpacing.X;

		var title = "Intercept System";
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (totalWidth / 2) - (ImGui.CalcTextSize(title).X / 2));
		ImGui.TextColored(ImGuiColors.DalamudYellow, title);

		ImGui.Spacing();

		var cur = DataCenter.CurrentInterceptedAction;

		if (cur == null)
		{
			ImGui.TextColored(ImGuiColors.DalamudGrey, "No intercepted actions queued.");
			return;
		}

		ImGui.TextColored(ImGuiColors.DalamudWhite, "Current Intercepted Action");
		ControlWindow.DrawIAction(cur, gcdWidth, 1);
	}
}
