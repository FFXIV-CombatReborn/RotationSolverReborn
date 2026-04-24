using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Updaters;

namespace RotationSolver.UI;

internal class NextActionWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ControlWindow.BaseFlags
	| ImGuiWindowFlags.AlwaysAutoResize
	| ImGuiWindowFlags.NoCollapse
	| ImGuiWindowFlags.NoTitleBar
	| ImGuiWindowFlags.NoResize;

	public NextActionWindow()
		: base(nameof(NextActionWindow), BaseFlags)
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
		Basic.Configuration.Configs config = Service.Config;
		float width = config.ControlWindowGCDSize * config.ControlWindowNextSizeRatio;
		DrawGcdCooldown(width, false);

		float percent = 0f;

		ActionManager* actionManager = ActionManager.Instance();
		if (actionManager == null)
		{
			// Handle the case where actionManager is null
			return;
		}

		RecastDetail* group = actionManager->GetRecastGroupDetail(ActionHelper.GCDCooldownGroup - 1);
		if (group == null)
		{
			// Handle the case where group is null
			return;
		}

		if (group->Elapsed == group->Total || group->Total == 0)
		{
			percent = 1;
		}
		else
		{
			percent = group->Elapsed / group->Total;
			if (ActionUpdater.NextAction != ActionUpdater.NextGCDAction)
			{
				percent++;
			}
		}

		_ = ControlWindow.DrawIAction(ActionUpdater.NextAction, width, percent);

		// Teaching Mode: show a target hint if the rotation wants a different target
		if (Service.Config.TeachingMode && Service.Config.TeachingModeShowTargetHint)
		{
			DrawTeachingModeTargetHint(width);
		}
	}

	private static void DrawTeachingModeTargetHint(float width)
	{
		IBattleChara? suggestedTarget = null;
		if (ActionUpdater.NextAction is BaseAction baseAct)
		{
			suggestedTarget = baseAct.Target.Target;
		}

		if (suggestedTarget == null)
		{
			return;
		}

		string name = suggestedTarget.Name.TextValue;
		if (string.IsNullOrEmpty(name))
		{
			return;
		}

		bool isCurrentTarget = Svc.Targets.Target?.GameObjectId == suggestedTarget.GameObjectId;
		bool isSelf = suggestedTarget.GameObjectId == (Player.Object?.GameObjectId ?? 0);

		string label = $"Target: {name}";
		Vector4 color = isSelf
			? ImGuiColors.DalamudWhite
			: isCurrentTarget
				? ImGuiColors.HealerGreen
				: ImGuiColors.DalamudOrange;

		float textWidth = ImGui.CalcTextSize(label).X;
		float offsetX = (width - textWidth) / 2f;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, offsetX));

		ImGui.PushStyleColor(ImGuiCol.Text, color);
		ImGui.Selectable(label, false, ImGuiSelectableFlags.None, new Vector2(textWidth, 0));
		ImGui.PopStyleColor();

		if (ImGui.IsItemClicked() && !isSelf)
		{
			Svc.Targets.Target = suggestedTarget;
		}

		if (ImGui.IsItemHovered() && !isSelf)
		{
			ImGui.SetTooltip("Click to target");
		}
	}

	public static void DrawGcdCooldown(float width, bool drawTitle)
	{
		float remain = DataCenter.DefaultGCDRemain;
		float total = DataCenter.DefaultGCDTotal;
		float elapsed = DataCenter.DefaultGCDElapsed;

		if (drawTitle)
		{
			string str = $"{remain:F2}s / {total:F2}s";
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (width / 2) - (ImGui.CalcTextSize(str).X / 2));
			ImGui.Text(str);
		}

		Vector2 cursor = ImGui.GetCursorPos() + ImGui.GetWindowPos();
		float height = Service.Config.ControlProgressHeight;

		ImGui.ProgressBar(elapsed / total, new Vector2(width, height), string.Empty);

		float actionRemain = DataCenter.DefaultGCDRemain;
		if (actionRemain > 0)
		{
			float value = total - DataCenter.CalculatedActionAhead;

			var playerObject = Player.Object;
			if (playerObject != null && value > playerObject.TotalCastTime)
			{
				Vector2 pt = cursor + (new Vector2(width, 0) * value / total);

				ImGui.GetWindowDrawList().AddLine(pt, pt + new Vector2(0, height),
					ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudRed), 2);
			}
		}
	}
}