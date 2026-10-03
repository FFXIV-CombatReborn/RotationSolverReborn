using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static readonly (UiString Title, string Filter, FontAwesomeIcon Icon)[] _dutySpecificSections =
	[
		(UiString.ConfigWindow_Duty_Ultimate, Configs.DutySpecificUltimate, FontAwesomeIcon.Crown),
		(UiString.ConfigWindow_Duty_Savage, Configs.DutySpecificSavage, FontAwesomeIcon.Skull),
		(UiString.ConfigWindow_Duty_Extreme, Configs.DutySpecificExtreme, FontAwesomeIcon.Fire),
		(UiString.ConfigWindow_Duty_ChaoticAlliance, Configs.DutySpecificChaoticAlliance, FontAwesomeIcon.Users),
		(UiString.ConfigWindow_Duty_Alliance, Configs.DutySpecificAlliance, FontAwesomeIcon.Users),
		(UiString.ConfigWindow_Duty_Dungeon, Configs.DutySpecificDungeon, FontAwesomeIcon.Dungeon),
		(UiString.ConfigWindow_Duty_DeepDungeon, Configs.DutySpecificDeepDungeon, FontAwesomeIcon.LayerGroup),
		(UiString.ConfigWindow_Duty_VariantDungeon, Configs.DutySpecificVariantDungeon, FontAwesomeIcon.CodeBranch),
		(UiString.ConfigWindow_Duty_TreasureDungeon, Configs.DutySpecificTreasureDungeon, FontAwesomeIcon.Gem),
		(UiString.ConfigWindow_Duty_FieldOps, Configs.DutySpecificFieldOps, FontAwesomeIcon.Map),
		(UiString.ConfigWindow_Duty_PvP, Configs.DutySpecificPvP, FontAwesomeIcon.Khanda),
		(UiString.ConfigWindow_Duty_TheMaskedCarnivale, Configs.DutySpecificTheMaskedCarnivale, FontAwesomeIcon.TheaterMasks),
		(UiString.ConfigWindow_Duty_CrucibleOfTheUnbroken, Configs.DutySpecificCrucibleOfTheUnbroken, FontAwesomeIcon.Hammer),
	];

	private static readonly CollapsingHeaderGroup _dutySpecificHeader = BuildDutySpecificHeaderGroup();

	private static CollapsingHeaderGroup BuildDutySpecificHeaderGroup()
	{
		Dictionary<Func<string>, Action> headers = [];
		var group = new CollapsingHeaderGroup(headers);

		foreach (var (title, filter, icon) in _dutySpecificSections)
		{
			headers[() => title.GetDescription()] = () => _allSearchable.DrawItems(filter);
			group.SetHeaderIcon(title.GetDescription(), icon);
		}

		return group;
	}

	private static void DrawDutySpecific()
	{
		_dutySpecificHeader.Draw();
	}
}
