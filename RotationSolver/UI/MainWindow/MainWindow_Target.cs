using Dalamud.Game.ClientState.Keys;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawTarget()
	{
		_targetHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _targetHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ () => UiString.ConfigWindow_Target_Config.GetDescription(), DrawTargetConfig },
			{ () => UiString.ConfigWindow_List_Hostile.GetDescription(), DrawTargetHostile },
		},
		(UiString.ConfigWindow_Target_Config, FontAwesomeIcon.Crosshairs),
		(UiString.ConfigWindow_List_Hostile, FontAwesomeIcon.Skull));

	private static readonly string[] _targetingTypeNames = Enum.GetNames<TargetingType>();

	private static void DrawTargetConfig()
	{
		_allSearchable.DrawItems(Configs.TargetConfig);
	}

	private static void DrawTargetHostile()
	{
		DrawPageIntro(UiString.ConfigWindow_Param_HostileDesc.GetDescription());

		if (M3Widgets.Button("##add_hostile", UiString.ConfigWindow_Target_AddPriority.GetDescription(),
			M3ButtonStyle.Tonal, FontAwesomeIcon.Plus))
		{
			Service.Config.TargetingTypes.Add(TargetingType.Big);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		var names = _targetingTypeNames;
		var comboWidth = 220f * Scale;
		var iconExtent = 30f * Scale;

		for (var i = 0; i < Service.Config.TargetingTypes.Count; i++)
		{
			var targetType = Service.Config.TargetingTypes[i];
			var index = i;

			void Delete()
			{
				if (index < Service.Config.TargetingTypes.Count)
				{
					Service.Config.TargetingTypes.RemoveAt(index);
				}
			}

			void Up()
			{
				if (index >= Service.Config.TargetingTypes.Count)
				{
					return;
				}

				Service.Config.TargetingTypes.RemoveAt(index);
				Service.Config.TargetingTypes.Insert(Math.Max(0, index - 1), targetType);
			}

			void Down()
			{
				if (index >= Service.Config.TargetingTypes.Count)
				{
					return;
				}

				Service.Config.TargetingTypes.RemoveAt(index);
				Service.Config.TargetingTypes.Insert(Math.Min(Service.Config.TargetingTypes.Count, index + 1), targetType);
			}

			var key = $"TargetingTypePopup_{i}";
			ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
				(UiString.ConfigWindow_List_Remove.GetDescription(), Delete, ImGuiHelper.DeleteHint),
				(UiString.ConfigWindow_Actions_MoveUp.GetDescription(), Up, ImGuiHelper.MoveUpHint),
				(UiString.ConfigWindow_Actions_MoveDown.GetDescription(), Down, ImGuiHelper.MoveDownHint));

			// Deleting from the popup can shrink the list mid-loop.
			if (i >= Service.Config.TargetingTypes.Count)
			{
				break;
			}

			var controlWidth = comboWidth + (3f * (iconExtent + (4f * Scale)));
			var row = M3SettingRow.Begin($"{i + 1}. {UiString.ConfigWindow_Param_HostileCondition.GetDescription()}",
				null, new Vector2(controlWidth, M3Widgets.ComboHeight));

			var selected = (int)Service.Config.TargetingTypes[i];
			ImGui.SetCursorScreenPos(row.ControlPosition);
			if (M3Widgets.Combo($"##HostileCondition{i}", ref selected, names, comboWidth))
			{
				Service.Config.TargetingTypes[i] = (TargetingType)selected;
			}

			var buttonY = row.ControlPosition.Y + ((M3Widgets.ComboHeight - iconExtent) * 0.5f);
			var buttonX = row.ControlPosition.X + comboWidth + (4f * Scale);

			ImGui.SetCursorScreenPos(new Vector2(buttonX, buttonY));
			if (M3Widgets.IconButton($"##hostile_up_{i}", FontAwesomeIcon.ArrowUp,
				UiString.ConfigWindow_Actions_MoveUp.GetDescription(), diameter: iconExtent))
			{
				Up();
			}

			buttonX += iconExtent + (4f * Scale);
			ImGui.SetCursorScreenPos(new Vector2(buttonX, buttonY));
			if (M3Widgets.IconButton($"##hostile_down_{i}", FontAwesomeIcon.ArrowDown,
				UiString.ConfigWindow_Actions_MoveDown.GetDescription(), diameter: iconExtent))
			{
				Down();
			}

			buttonX += iconExtent + (4f * Scale);
			ImGui.SetCursorScreenPos(new Vector2(buttonX, buttonY));
			if (M3Widgets.IconButton($"##hostile_delete_{i}", FontAwesomeIcon.Trash,
				UiString.ConfigWindow_List_Remove.GetDescription(), M3ButtonStyle.Text,
				M3.Scheme.Error, iconExtent))
			{
				Delete();
			}

			M3SettingRow.End(row);

			ImGuiHelper.ExecuteHotKeysPopupAt(row.Hovered, key, string.Empty, true,
				(Delete, new[] { VirtualKey.DELETE }),
				(Up, new[] { VirtualKey.UP }),
				(Down, new[] { VirtualKey.DOWN }));
		}
	}
}
