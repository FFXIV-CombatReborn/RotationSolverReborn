using RebornMaterial;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawUI()
	{
		_UIHeader?.Draw();
	}

	private static void DrawCustomizeUI()
	{
		_allSearchable.DrawItems(Configs.UiCustomize);

		var config = Service.Config;
		var defaults = Service.ConfigDefault;
		var isDefault = config.UiTextScale == defaults.UiTextScale
			&& config.UiElementScale == defaults.UiElementScale
			&& config.UiPaddingScale == defaults.UiPaddingScale
			&& config.UiMotionScale == defaults.UiMotionScale;
		if (M3Widgets.Button("##ui_reset_sizes", "Reset sizes", M3ButtonStyle.Text, FontAwesomeIcon.Undo, enabled: !isDefault))
		{
			config.UiTextScale = defaults.UiTextScale;
			config.UiElementScale = defaults.UiElementScale;
			config.UiPaddingScale = defaults.UiPaddingScale;
			config.UiMotionScale = defaults.UiMotionScale;
		}
	}

	private static readonly CollapsingHeaderGroup _UIHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{
				() => UiString.ConfigWindow_UI_Information.GetDescription(),
				() => _allSearchable.DrawItems(Configs.UiInformation)
			},
			{
				() => UiString.ConfigWindow_UI_Customize.GetDescription(),
				DrawCustomizeUI
			},
			{
				() => UiString.ConfigWindow_UI_Windows.GetDescription(),
				() => _allSearchable.DrawItems(Configs.UiWindows)
			},
			{
				() => UiString.ConfigWindow_UI_Hotbars.GetDescription(),
				() => _allSearchable.DrawItems(Configs.UiHotbars)
			},
		},
		(UiString.ConfigWindow_UI_Information, FontAwesomeIcon.InfoCircle),
		(UiString.ConfigWindow_UI_Customize, FontAwesomeIcon.Palette),
		(UiString.ConfigWindow_UI_Windows, FontAwesomeIcon.WindowRestore),
		(UiString.ConfigWindow_UI_Hotbars, FontAwesomeIcon.Th));
}
