using Dalamud.Game.ClientState.Keys;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

using RotationSolver.UI.SearchableConfigs;

namespace RotationSolver.UI;

public partial class RotationConfigWindow
{
	private string _searchText = string.Empty;
	private ISearchable[] _searchResults = [];

	internal static SearchableCollection _allSearchable = new();

	private void SearchingBox()
	{
		if (ImGui.InputTextWithHint("##Ascended Rotation Solver Reborn Search Box", UiString.ConfigWindow_Searching.GetDescription(), ref _searchText, 128, ImGuiInputTextFlags.AutoSelectAll))
		{
			_searchResults = _allSearchable.SearchItems(_searchText);
		}
	}

	#region Basic
	private static void DrawBasic()
	{
		_baseHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _baseHeader = new(new Dictionary<Func<string>, Action>
	{
		{ UiString.ConfigWindow_Basic_Timer.GetDescription, DrawBasicTimer },
		{ UiString.ConfigWindow_Basic_Others.GetDescription, DrawBasicOthers },
	});

	private static void DrawBasicTimer()
	{
		_allSearchable.DrawItems(Configs.BasicTimer);
	}

	private static readonly Dictionary<int, bool> _isOpen = [];

	private static void DrawBasicOthers()
	{
		_allSearchable.DrawItems(Configs.BasicParams);
	}
	#endregion

	#region UI
	private static void DrawUI()
	{
		_UIHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _UIHeader = new(new Dictionary<Func<string>, Action>
	{
		{
			UiString.ConfigWindow_UI_Information.GetDescription,
			() => _allSearchable.DrawItems(Configs.UiInformation)
		},
		{
			UiString.ConfigWindow_UI_Windows.GetDescription,
			() => _allSearchable.DrawItems(Configs.UiWindows)
		},
	});

	#endregion

	#region Auto
	private const int HeaderSize = 18;

	/// <summary>
	/// Draws the auto section of the configuration window.
	/// </summary>
	private void DrawAuto()
	{
		ImGui.TextWrapped(UiString.ConfigWindow_Auto_Description.GetDescription());
		_autoHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _autoHeader = new(new Dictionary<Func<string>, Action>
	{
		{ UiString.ConfigWindow_Basic_AutoSwitch.GetDescription, DrawBasicAutoSwitch },
		{ UiString.ConfigWindow_Auto_ActionUsage.GetDescription, DrawActionUsageControl },
		{ UiString.ConfigWindow_Auto_HealingCondition.GetDescription, DrawHealingActionCondition },
	})
	{
		HeaderSize = HeaderSize,
	};

	private static void DrawBasicAutoSwitch()
	{
		_allSearchable.DrawItems(Configs.BasicAutoSwitch);
	}

	/// <summary>
	/// Draws the Action Usage and Control section.
	/// </summary>
	private static void DrawActionUsageControl()
	{
		ImGui.TextWrapped(UiString.ConfigWindow_Auto_ActionUsage_Description.GetDescription());
		ImGui.Separator();
		_allSearchable.DrawItems(Configs.AutoActionUsage);
	}

	/// <summary>
	/// Draws the healing action condition section.
	/// </summary>
	private static void DrawHealingActionCondition()
	{
		ImGui.TextWrapped(UiString.ConfigWindow_Auto_HealingCondition_Description.GetDescription());
		ImGui.Separator();
		_allSearchable.DrawItems(Configs.HealingActionCondition);
	}
	#endregion

	#region Target
	private static void DrawTarget()
	{
		_targetHeader?.Draw();
	}

	/// <summary>
	/// Header group for target-related configurations.
	/// </summary>
	private static readonly CollapsingHeaderGroup _targetHeader = new(new Dictionary<Func<string>, Action>
	{
	{ UiString.ConfigWindow_Target_Config.GetDescription, DrawTargetConfig },
	{ UiString.ConfigWindow_List_Hostile.GetDescription, DrawTargetHostile },
	});

	/// <summary>
	/// Draws the target configuration items.
	/// </summary>
	private static void DrawTargetConfig()
	{
		_allSearchable.DrawItems(Configs.TargetConfig);
	}

	private static void DrawTargetHostile()
	{
		if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add Hostile"))
		{
			Service.Config.TargetingTypes.Add(TargetingType.Big);
		}
		ImGui.SameLine();
		ImGui.TextWrapped(UiString.ConfigWindow_Param_HostileDesc.GetDescription());

		for (var i = 0; i < Service.Config.TargetingTypes.Count; i++)
		{
			var targetType = Service.Config.TargetingTypes[i];
			var key = $"TargetingTypePopup_{i}";

			void Delete()
			{
				Service.Config.TargetingTypes.RemoveAt(i);
			}

			void Up()
			{
				Service.Config.TargetingTypes.RemoveAt(i);
				Service.Config.TargetingTypes.Insert(Math.Max(0, i - 1), targetType);
			}

			void Down()
			{
				Service.Config.TargetingTypes.RemoveAt(i);
				Service.Config.TargetingTypes.Insert(Math.Min(Service.Config.TargetingTypes.Count - 1, i + 1), targetType);
			}

			ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
				(UiString.ConfigWindow_List_Remove.GetDescription(), Delete, pairsArray2),
				(UiString.ConfigWindow_Actions_MoveUp.GetDescription(), Up, pairsArray0),
				(UiString.ConfigWindow_Actions_MoveDown.GetDescription(), Down, pairsArray1));

			var names = Enum.GetNames<TargetingType>();
			var targetingType = (int)Service.Config.TargetingTypes[i];
			var text = UiString.ConfigWindow_Param_HostileCondition.GetDescription();
			ImGui.SetNextItemWidth(ImGui.CalcTextSize(text).X + (30 * Scale));
			if (ImGui.Combo(text + "##HostileCondition" + i, ref targetingType, names, names.Length))
			{
				Service.Config.TargetingTypes[i] = (TargetingType)targetingType;
			}

			ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, true,
				(Delete, new[] { VirtualKey.DELETE }),
				(Up, new[] { VirtualKey.UP }),
				(Down, new[] { VirtualKey.DOWN }));
		}

		ImGui.Separator();

		if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add PvP Hostile"))
		{
			Service.Config.TargetingTypesPvP.Add(TargetingType.PvPSmart);
		}
		ImGui.SameLine();
		ImGui.TextWrapped(UiString.ConfigWindow_Param_HostileDescPvP.GetDescription());

		for (var i = 0; i < Service.Config.TargetingTypesPvP.Count; i++)
		{
			var pvpTargetType = Service.Config.TargetingTypesPvP[i];
			var pvpKey = $"TargetingTypesPvPPopup_{i}";

			void DeletePvP()
			{
				Service.Config.TargetingTypesPvP.RemoveAt(i);
			}

			void UpPvP()
			{
				Service.Config.TargetingTypesPvP.RemoveAt(i);
				Service.Config.TargetingTypesPvP.Insert(Math.Max(0, i - 1), pvpTargetType);
			}

			void DownPvP()
			{
				Service.Config.TargetingTypesPvP.RemoveAt(i);
				Service.Config.TargetingTypesPvP.Insert(Math.Min(Service.Config.TargetingTypesPvP.Count - 1, i + 1), pvpTargetType);
			}

			ImGuiHelper.DrawHotKeysPopup(pvpKey, string.Empty,
				(UiString.ConfigWindow_List_Remove.GetDescription(), DeletePvP, pairsArray2),
				(UiString.ConfigWindow_Actions_MoveUp.GetDescription(), UpPvP, pairsArray0),
				(UiString.ConfigWindow_Actions_MoveDown.GetDescription(), DownPvP, pairsArray1));

			var pvpNames = Enum.GetNames<TargetingType>();
			var pvpTargetingType = (int)Service.Config.TargetingTypesPvP[i];
			var pvpText = UiString.ConfigWindow_Param_HostileCondition.GetDescription();
			ImGui.SetNextItemWidth(ImGui.CalcTextSize(pvpText).X + (30 * Scale));
			if (ImGui.Combo(pvpText + "##PvPHostileCondition" + i, ref pvpTargetingType, pvpNames, pvpNames.Length))
			{
				Service.Config.TargetingTypesPvP[i] = (TargetingType)pvpTargetingType;
			}

			ImGuiHelper.ExecuteHotKeysPopup(pvpKey, string.Empty, string.Empty, true,
				(DeletePvP, new[] { VirtualKey.DELETE }),
				(UpPvP, new[] { VirtualKey.UP }),
				(DownPvP, new[] { VirtualKey.DOWN }));
		}

		ImGui.Separator();
		ImGui.TextUnformatted(UiString.ConfigWindow_Param_PvPScoringPreset.GetDescription());
		var pvpNames2 = Enum.GetNames<RotationSolver.Basic.Actions.PvPTargetSelection.ScoringPreset>();
		var presetIndex = (int)Service.Config.PvPScoringPreset;
		ImGui.SetNextItemWidth(200 * Scale);
		if (ImGui.Combo("##PvPScoringPreset", ref presetIndex, pvpNames2, pvpNames2.Length))
		{
			Service.Config.PvPScoringPreset =
				(RotationSolver.Basic.Actions.PvPTargetSelection.ScoringPreset)presetIndex;
		}

		ImGui.Spacing();
		var showOverlay = Service.Config.PvPSmartShowDebugOverlay;
		if (ImGui.Checkbox(UiString.ConfigWindow_Param_PvPSmartShowDebugOverlay.GetDescription(), ref showOverlay))
		{
			Service.Config.PvPSmartShowDebugOverlay = showOverlay;
		}
	}
	#endregion

	#region Extra
	private static void DrawExtra()
	{
		ImGui.TextWrapped(UiString.ConfigWindow_Extra_Description.GetDescription());
		_extraHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _extraHeader = new(new Dictionary<Func<string>, Action>
	{
	{ UiString.ConfigWindow_EventItem.GetDescription, DrawEventTab },
	{ UiString.ConfigWindow_Internal.GetDescription, DrawInternalTab },
	{
		UiString.ConfigWindow_Extra_Others.GetDescription,
		() => _allSearchable.DrawItems(Configs.Extra)
	},
	});
	private static readonly string[] pairsArray0 = ["↑"];
	private static readonly string[] pairsArray1 = ["↓"];
	private static readonly string[] pairsArray2 = ["Delete"];

	private static void DrawInternalTab()
	{
		ImGui.Text($"Configs/Backups location: {Svc.PluginInterface.ConfigFile.Directory}");

		if (ImGui.Button("Backup Configs"))
		{
			Service.Config.Backup();
		}

		if (ImGui.Button("Restore Configs"))
		{
			Service.Config.Restore();
		}
	}

	private static void DrawEventTab()
	{
		if (ImGui.Button(UiString.ConfigWindow_Events_AddEvent.GetDescription()))
		{
			Service.Config.Events.Add(new ActionEventInfo());
		}
		ImGui.SameLine();

		ImGui.TextWrapped(UiString.ConfigWindow_Events_Description.GetDescription());

		ImGui.Text(UiString.ConfigWindow_Events_DutyStart.GetDescription());
		ImGui.SameLine();
		Service.Config.DutyStart.DisplayMacro();

		ImGui.Text(UiString.ConfigWindow_Events_DutyEnd.GetDescription());
		ImGui.SameLine();
		Service.Config.DutyEnd.DisplayMacro();

		ImGui.Separator();

		for (var i = 0; i < Service.Config.Events.Count; i++)
		{
			var eve = Service.Config.Events[i];
			eve.DisplayEvent();

			ImGui.SameLine();

			if (ImGui.Button($"{UiString.ConfigWindow_Events_RemoveEvent.GetDescription()}##RemoveEvent{eve.GetHashCode()}"))
			{
				Service.Config.Events.RemoveAt(i);
				i--; // Adjust index after removal
			}
			ImGui.Separator();
		}
	}
	#endregion
}
