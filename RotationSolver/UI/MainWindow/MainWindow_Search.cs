using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using RotationSolver.UI.Material;
using RotationSolver.UI.SearchableConfigs;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private string _searchText = string.Empty;
	private ISearchable[] _searchResults = [];

	private void SearchingBox(float width)
	{
		if (M3Widgets.SearchField("##Rotation Solver Reborn Search Box",
			UiString.ConfigWindow_Searching.GetDescription(), ref _searchText, width))
		{
			_searchResults = _allSearchable.SearchItems(_searchText);
		}
	}

	private void DrawSearchResults()
	{
		foreach (var searchable in _searchResults)
		{
			if (searchable == null)
			{
				continue;
			}

			var path = searchable is Searchable s && !string.IsNullOrEmpty(s.Filter)
				? GetFilterMenuPath(s.Filter)
				: string.Empty;

			using var card = M3Card.Begin($"search_{searchable.GetHashCode()}", null, style: M3CardStyle.Outlined);
			searchable.Draw();

			if (searchable is Searchable target && !string.IsNullOrEmpty(path))
			{
				if (M3SettingRow.NavigationRow($"##jump_{target.ID}_{target.GetHashCode()}",
					$"Open {path}", null, FontAwesomeIcon.ExternalLinkAlt, FontAwesomeIcon.ChevronRight,
					M3.Scheme.Tertiary))
				{
					NavigateToFilter(target.Filter);
					_searchResults = [];
					_searchText = string.Empty;
				}
			}
		}
	}

	private readonly record struct FilterLocation(
		MainWindowTab Tab,
		CollapsingHeaderGroup? Group,
		UiString? Section);

	private static Dictionary<string, FilterLocation>? _filterLocations;

	private static Dictionary<string, FilterLocation> FilterLocations => _filterLocations ??= new()
	{
		[Configs.BasicTimer] = new(MainWindowTab.Basic, _baseHeader, UiString.ConfigWindow_Basic_Timer),
		[Configs.BasicParams] = new(MainWindowTab.Basic, _baseHeader, UiString.ConfigWindow_Basic_Others),

		[Configs.UiInformation] = new(MainWindowTab.UI, _UIHeader, UiString.ConfigWindow_UI_Information),
		[Configs.UiWindows] = new(MainWindowTab.UI, _UIHeader, UiString.ConfigWindow_UI_Windows),

		[Configs.BasicAutoSwitch] = new(MainWindowTab.Auto, _autoHeader, UiString.ConfigWindow_Basic_AutoSwitch),
		[Configs.AutoActionUsage] = new(MainWindowTab.Auto, _autoHeader, UiString.ConfigWindow_Auto_ActionUsage),
		[Configs.HealingActionCondition] = new(MainWindowTab.Auto, _autoHeader, UiString.ConfigWindow_Auto_HealingCondition),

		[Configs.DutySpecificUltimate] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_Ultimate),
		[Configs.DutySpecificSavage] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_Savage),
		[Configs.DutySpecificExtreme] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_Extreme),
		[Configs.DutySpecificChaoticAlliance] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_ChaoticAlliance),
		[Configs.DutySpecificAlliance] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_Alliance),
		[Configs.DutySpecificDungeon] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_Dungeon),
		[Configs.DutySpecificDeepDungeon] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_DeepDungeon),
		[Configs.DutySpecificVariantDungeon] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_VariantDungeon),
		[Configs.DutySpecificTreasureDungeon] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_TreasureDungeon),
		[Configs.DutySpecificFieldOps] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_FieldOps),
		[Configs.DutySpecificPvP] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_PvP),
		[Configs.DutySpecificTheMaskedCarnivale] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_TheMaskedCarnivale),
		[Configs.DutySpecificCrucibleOfTheUnbroken] = new(MainWindowTab.Duty, _dutySpecificHeader, UiString.ConfigWindow_Duty_CrucibleOfTheUnbroken),

		[Configs.TargetConfig] = new(MainWindowTab.Target, _targetHeader, UiString.ConfigWindow_Target_Config),
		[Configs.Extra] = new(MainWindowTab.Extra, _extraHeader, UiString.ConfigWindow_Extra_Others),

		[Configs.List] = new(MainWindowTab.List, _idsHeader, UiString.ConfigWindow_List_Actions),
		[Configs.List2] = new(MainWindowTab.List, _idsHeader, UiString.ConfigWindow_List_Actions),
		[Configs.List3] = new(MainWindowTab.List, _idsHeader, UiString.ConfigWindow_List_Actions),

		[Configs.Debug] = new(MainWindowTab.Debug, null, null),
	};

	private static string GetFilterMenuPath(string filter)
	{
		if (!FilterLocations.TryGetValue(filter, out var location))
		{
			return string.Empty;
		}

		return location.Section is { } section
			? $"{location.Tab} > {section.GetDescription()}"
			: location.Tab.ToString();
	}

	private void NavigateToFilter(string filter)
	{
		if (!FilterLocations.TryGetValue(filter, out var location))
		{
			return;
		}

		_activeTab = location.Tab;
		if (location.Section is { } section)
		{
			location.Group?.OpenHeaderByTitle(section.GetDescription());
		}
	}
}
