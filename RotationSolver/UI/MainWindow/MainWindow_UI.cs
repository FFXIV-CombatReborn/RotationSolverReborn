using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawUI()
	{
		_UIHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _UIHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{
				() => UiString.ConfigWindow_UI_Information.GetDescription(),
				() => _allSearchable.DrawItems(Configs.UiInformation)
			},
			{
				() => UiString.ConfigWindow_UI_Windows.GetDescription(),
				() => _allSearchable.DrawItems(Configs.UiWindows)
			},
		},
		(UiString.ConfigWindow_UI_Information, FontAwesomeIcon.InfoCircle),
		(UiString.ConfigWindow_UI_Windows, FontAwesomeIcon.WindowRestore));
}
