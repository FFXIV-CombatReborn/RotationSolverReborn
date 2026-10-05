using ECommons.GameHelpers;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Data;
using RotationSolver.UI.Material;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawDutyRotationBody()
	{
		var rotation = DataCenter.CurrentDutyRotation;
		if (rotation == null)
		{
			return;
		}

		_dutyRotationHeader.Draw();
	}

	private static readonly CollapsingHeaderGroup _dutyRotationHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ GetDutyRotationStatusHead, DrawDutyRotationStatus },
			{ () => UiString.ConfigWindow_DutyRotation_Configuration.GetDescription(), DrawDutyRotationConfiguration },
		},
		(UiString.ConfigWindow_DutyRotation_Status, FontAwesomeIcon.Heartbeat),
		(UiString.ConfigWindow_DutyRotation_Configuration, FontAwesomeIcon.SlidersH));

	private static string GetDutyRotationStatusHead()
	{
		var rotation = DataCenter.CurrentDutyRotation;
		var status = UiString.ConfigWindow_DutyRotation_Status.GetDescription();
		return rotation == null ? string.Empty : status;
	}

	private static void DrawDutyRotationStatus()
	{
		if (DataCenter.CurrentDutyRotation == null)
		{
			return;
		}
		DataCenter.CurrentDutyRotation?.DisplayDutyStatus();
	}

	private static void DrawDutyRotationConfiguration()
	{
		var rotation = DataCenter.CurrentDutyRotation;
		if (rotation == null || !Player.Available)
		{
			return;
		}

		var set = rotation.Configs;
		var phantomJob = DutyRotation.GetPhantomJob();

		foreach (var config in set.Configs)
		{
			if (!config.Type.HasFlag(CombatType.PvE) || !ShouldShowRotationConfig(config, set))
			{
				continue;
			}

			var configuredJob = config switch
			{
				RotationConfigCombo combo => combo.PhantomJob,
				RotationConfigBoolean boolean => boolean.PhantomJob,
				RotationConfigFloat number => number.PhantomJob,
				RotationConfigString text => text.PhantomJob,
				RotationConfigInt integer => integer.PhantomJob,
				_ => DutyRotation.PhantomJob.None,
			};

			if (configuredJob != DutyRotation.PhantomJob.None && configuredJob != phantomJob)
			{
				continue;
			}

			var supporting = config is RotationConfigFloat unitConfig && unitConfig.UnitType != ConfigUnitType.None
				? unitConfig.UnitType.GetDescription()
				: null;

			DrawRotationConfigRow(config, rotation, OtherCommandType.DutyRotations, supporting);
		}
	}

	private static void DrawRotationConfigRow(IRotationConfig config, object rotation, OtherCommandType commandType, string? supporting, string? tooltip = null)
	{
		var type = rotation.GetType();
		var key = $"{type.FullName ?? type.Name}.{config.Name}";
		var id = $"##{config.GetHashCode()}_{key}";
		var command = ToCommandStr(commandType, config.Name, config.DefaultValue);
		void Reset() => config.Value = config.DefaultValue;

		ImGuiHelper.PrepareGroup(key, command, Reset);

		var controlSize = ResolveRotationConfigControlSize(config);
		var row = M3SettingRow.Begin(config.DisplayName, supporting, controlSize);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		DrawRotationConfigControl(config, id, controlSize.X);

		if (row.Hovered && !string.IsNullOrEmpty(tooltip) && supporting == null)
		{
			ImguiTooltips.ShowTooltip(tooltip);
		}

		ImGuiHelper.ReactPopupAt(row.Hovered, key, false);
		M3SettingRow.End(row);
	}
}
