using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawBasic()
	{
		_baseHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _baseHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ () => UiString.ConfigWindow_Basic_Timer.GetDescription(), DrawBasicTimer },
			{ () => UiString.ConfigWindow_Basic_Others.GetDescription(), DrawBasicOthers },
		},
		(UiString.ConfigWindow_Basic_Timer, FontAwesomeIcon.Stopwatch),
		(UiString.ConfigWindow_Basic_Others, FontAwesomeIcon.EllipsisH));

	private static void DrawBasicTimer()
	{
		_allSearchable.DrawItems(Configs.BasicTimer);
	}

	private static void DrawBasicOthers()
	{
		_allSearchable.DrawItems(Configs.BasicParams);
	}
}
