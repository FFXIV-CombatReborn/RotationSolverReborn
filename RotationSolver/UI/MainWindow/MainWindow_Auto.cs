using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private void DrawAuto()
	{
		DrawPageIntro(UiString.ConfigWindow_Auto_Description.GetDescription());
		_autoHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _autoHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ () => UiString.ConfigWindow_Basic_AutoSwitch.GetDescription(), DrawBasicAutoSwitch },
			{ () => UiString.ConfigWindow_Auto_ActionUsage.GetDescription(), DrawActionUsageControl },
			{ () => UiString.ConfigWindow_Auto_Intercept.GetDescription(), DrawIntercept },
			{ () => UiString.ConfigWindow_Auto_HealingCondition.GetDescription(), DrawHealingActionCondition },
			{ () => UiString.ConfigWindow_Auto_RaiseCondition.GetDescription(), DrawRaiseActionCondition },
		},
		(UiString.ConfigWindow_Basic_AutoSwitch, FontAwesomeIcon.ToggleOn),
		(UiString.ConfigWindow_Auto_ActionUsage, FontAwesomeIcon.Bolt),
		(UiString.ConfigWindow_Auto_Intercept, FontAwesomeIcon.HandPaper),
		(UiString.ConfigWindow_Auto_HealingCondition, FontAwesomeIcon.Heartbeat),
		(UiString.ConfigWindow_Auto_RaiseCondition, FontAwesomeIcon.Ankh));

	private static void DrawBasicAutoSwitch()
	{
		_allSearchable.DrawItems(Configs.BasicAutoSwitch);
	}

	private static void DrawActionUsageControl()
	{
		DrawPageIntro(UiString.ConfigWindow_Auto_ActionUsage_Description.GetDescription());
		_allSearchable.DrawItems(Configs.AutoActionUsage);
	}

	private static void DrawIntercept()
	{
		DrawPageIntro(UiString.ConfigWindow_Auto_Intercept_Description.GetDescription());
		_allSearchable.DrawItems(Configs.AutoIntercept);
	}

	private static void DrawHealingActionCondition()
	{
		DrawPageIntro(UiString.ConfigWindow_Auto_HealingCondition_Description.GetDescription());
		_allSearchable.DrawItems(Configs.HealingActionCondition);
	}

	private static void DrawRaiseActionCondition()
	{
		DrawPageIntro(UiString.ConfigWindow_Auto_RaiseCondition_Description.GetDescription());
		_allSearchable.DrawItems(Configs.RaiseActionCondition);
	}
}
