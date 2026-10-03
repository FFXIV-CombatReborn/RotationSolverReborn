using System.ComponentModel;

namespace RotationSolver.UI;

[AttributeUsage(AttributeTargets.Field)]
internal class TabSkipAttribute : Attribute
{
}

internal enum MainWindowTab : byte
{
	[TabSkip] About,
	[TabSkip] Rotation,

	[Description("Useful information and macro list.")]
	Main,

	[Description("Rotation specific configs.")]
	Job,

	[Description("Configure Duty Rotation.")]
	DutyRotation,

	[Description("Configure abilities and custom conditions for your current job.")]
	Actions,

	[Description("Configure reactive actions and status effect lists.")]
	List,

	[Description("Configure basic settings.")]
	Basic,

	[Description("Configure user interface settings.")]
	UI,

	[Description("Configure general action usage and control settings.")]
	Auto,

	[Description("Configure targeting settings.")]
	Target,

	[Description("Duty specific settings.")]
	Duty,

	[Description("Configure optional helpful features.")]
	Extra,

	[Description("Debug options for developers and rotation writers (disable when not in use).")]
	Debug,

	[Description("Configure AutoDuty settings and view related information.")]
	AutoDuty,
}

internal static class MainWindowTabExtensions
{
	public static FontAwesomeIcon Glyph(this MainWindowTab tab)
	{
		return tab switch
		{
			MainWindowTab.About => FontAwesomeIcon.InfoCircle,
			MainWindowTab.Main => FontAwesomeIcon.Home,
			MainWindowTab.Rotation => FontAwesomeIcon.Sync,
			MainWindowTab.Job => FontAwesomeIcon.UserShield,
			MainWindowTab.DutyRotation => FontAwesomeIcon.Dungeon,
			MainWindowTab.Actions => FontAwesomeIcon.Bolt,
			MainWindowTab.List => FontAwesomeIcon.ListUl,
			MainWindowTab.Basic => FontAwesomeIcon.SlidersH,
			MainWindowTab.UI => FontAwesomeIcon.Desktop,
			MainWindowTab.Auto => FontAwesomeIcon.Robot,
			MainWindowTab.Target => FontAwesomeIcon.Crosshairs,
			MainWindowTab.Duty => FontAwesomeIcon.Shield,
			MainWindowTab.Extra => FontAwesomeIcon.PuzzlePiece,
			MainWindowTab.Debug => FontAwesomeIcon.Bug,
			MainWindowTab.AutoDuty => FontAwesomeIcon.Route,
			_ => FontAwesomeIcon.Circle,
		};
	}

	public static string CNString(this MainWindowTab tab)
	{
		return tab switch
		{
			MainWindowTab.About => "关于",
			MainWindowTab.Rotation => "循环",
			MainWindowTab.Main => "主窗口",
			MainWindowTab.Job => "职业",
			MainWindowTab.Duty => "任务",
			MainWindowTab.Actions => "技能",
			MainWindowTab.List => "列表",
			MainWindowTab.Basic => "基础",
			MainWindowTab.UI => "界面",
			MainWindowTab.Auto => "自动",
			MainWindowTab.Target => "目标",
			MainWindowTab.Extra => "额外",
			MainWindowTab.Debug => "调试",
			_ => tab.ToString()
		};
	}
}
