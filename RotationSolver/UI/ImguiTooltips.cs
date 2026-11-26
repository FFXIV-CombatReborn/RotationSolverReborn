using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace RotationSolver.UI;

internal static class ImguiTooltips
{
	private const ImGuiWindowFlags TooltipFlag =
		  ImGuiWindowFlags.Tooltip |
		  ImGuiWindowFlags.NoMove |
		  ImGuiWindowFlags.NoSavedSettings |
		  ImGuiWindowFlags.NoBringToFrontOnFocus |
		  ImGuiWindowFlags.NoDecoration |
		  ImGuiWindowFlags.NoInputs |
		  ImGuiWindowFlags.AlwaysAutoResize;

	private const string TooltipId = "RotationSolverReborn Tooltips";

	public static void HoveredTooltip(string? text)
	{
		if (ImGui.IsItemHovered())
		{
			ShowTooltip(text);
		}
	}

	public static void ShowTooltip(string? text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			ShowTooltip(() =>
			{
				using var wrap = ImRaii.TextWrapPos(ImGui.GetFontSize() * 36f);
				ImGui.TextUnformatted(text);
			});
		}
	}

	public static void ShowTooltip(Action? act)
	{
		if (act == null || Service.Config.ShowTooltips != true)
		{
			return;
		}

		ImGui.SetNextWindowBgAlpha(1);

		using var color = ImRaii.PushColor(ImGuiCol.BorderShadow, ImGuiColors.DalamudWhite);

		var globalScale = ImGuiHelpers.GlobalScale;
		ImGui.SetNextWindowSizeConstraints(new Vector2(150, 0) * globalScale, new Vector2(1200, 1500) * globalScale);
		ImGui.SetWindowPos(TooltipId, ImGui.GetIO().MousePos);

		try
		{
			if (ImGui.Begin(TooltipId, TooltipFlag))
			{
				act();
			}
		}
		finally
		{
			ImGui.End();
		}
	}
}
