using ECommons.ExcelServices;
using ECommons.GameHelpers;
using RebornMaterial;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static readonly string[] _supporters =
	[
	"????",
	"Juju",
	"3Some",
	"ABA",
	"Akurosuki",
	"Alkeid",
	"Armerun",
	"catfourteen",
	"Chaos_co",
	"DeadCode",
	"Drama",
	"Endings",
	"Enyo",
	"ExiledxSnake",
	"fishsticks",
	"Frogs",
	"Goku",
	"Headrushed",
	"Hex",
	"Jayhow",
	"kaen",
	"Kaspil",
	"Kuroiyaru",
	"kuromiromi",
	"Lemon",
	"LouBird",
	"Miracle Ace",
	"Mirai",
	"Miri",
	"No",
	"NoDice",
	"Plogons",
	"Preset",
	"prismagreen",
	"Radium226",
	"Robsie",
	"sinbad",
	"smf26",
	"Twoshoes",
	"Utterly Hopeless!",
	"Vaex_Darastrix",
	"wangel",
	"KuwoBlack"
	];

	private static readonly string[] _baseUsageHints =
	[
		"Right-click any action, setting, or toggle to view/copy its macro chat command.",
		"Use /rsr as a shorter alias for /rotation.",
		"Use /rotation Auto, /rotation Manual, or /rotation Off to change modes quickly.",
		"Use the search box (top-left) to jump directly to settings.",
		"Click the external-link icon in search results to jump to that menu.",
		"Right-click a setting label to copy a ready-to-use /rotation Settings command.",
		"Actions tab: click an action icon to configure, enable/disable, or set hotkeys.",
		"Actions: toggle 'Show on CD Window' to include an action in the cooldown overlay.",
		"Actions: enable 'Intercepted' to let RSR fire an action you queue (PvE only).",
		"UI > Information: enable DTR status, toasts, original cooldowns, and these hints.",
		"UI > Windows: enable Next Action, Control, Cooldown, and Timeline windows.",
		"Next Action: 'No Inputs' and 'No Move' options change overlay behavior.",
		"Only show windows in duty or with enemies: UI > Windows > Only show with hostile or in duty.",
		"List tab: manage dispels, priority statuses, knockbacks, invincibility, and no-casting lists.",
		"List tab: use 'Reset and Update' to restore curated lists quickly.",
		"Status lists: press '+' to search by name or ID; fuzzy search is supported.",
		"Status lists: right-click an icon to remove; Delete key works in the popup too.",
		"Target tab: tweak target selection, vision cone, engage behavior, and dummy/boss handling.",
		"Target tab: set /rotation Cycle behaviour and targeting delays.",
		"Manage TargetingTypes via chat: /rotation Settings TargetingTypes add|remove <Type>.",
		"Auto > Action Usage: allow/deny oGCDs, set AoE style, tinctures, interrupts, and True North.",
		"Auto > Healing: adjust thresholds and non-healer healing behavior.",
		"Healer: customize Raise/Swiftcast and prioritization in Auto > Healing.",
		"Ground AoEs: Auto > Healing has options to place beneficial ground actions smartly.",
		"Basic > Timer: tune Action Ahead and Min Updating Time to balance performance vs weaving.",
		"Basic > Auto Switch: auto on/off for countdowns, deaths, area transitions, and more.",
		"Teaching Mode highlights targets; color is in UI > Information.",
		"Job tab: edit DNC partner, SGE Kardia tank, and AST card priorities when on those jobs.",
		"About > Macros lists available chat/macro commands and helpful syntax.",
		"About > Links: open config folder, GitHub, Ko-fi, and Discord.",
		"Extra > Internal: Backup/Restore configs safely.",
		"Extra: optional tweaks like removing animation/cooldown delay.",
		"Click the cube icon at the bottom-left of the sidebar to copy diagnostic info to clipboard.",
		"Timeline window can visualize recent actions (UI > Windows).",
		"Do damage, don't die",
		"Healing: the only HP that matters is the last one",
		"Be kind",
		"You can remove some self-buffs with “/statusoff <Name>” (e.g., Peloton) when needed.",
		"RSR works best with Legacy Type movement settings."
	];
	private static readonly string[] _baseUsageHintsCN =
	[
		"右键点击任意动作、设置或开关，可查看/复制其宏聊天命令。",
		"使用 /rsr 作为 /rotation 的简短别名。",
		"使用 /rotation Auto、/rotation Manual 或 /rotation Off 快速切换模式。",
		"使用搜索框（左上角）直接跳转到设置。",
		"点击搜索结果中的外链图标，可跳转到该菜单。",
		"右键点击设置标签，可复制一条可直接使用的 /rotation Settings 命令。",
		"技能 标签页：点击动作图标可配置、启用/禁用或设置热键。",
		"技能：切换 '在冷却窗口中显示'，将某个动作加入冷却覆盖层。",
		"技能：启用 'Intercepted'，让 RSR 施放你排队的动作（仅 PvE）。",
		"界面 > 信息：启用 DTR 状态、通知、原始冷却显示以及这些提示。",
		"界面 > 窗口：启用 Next Action、Control、Cooldown 和 Timeline 窗口。",
		"Next Action：'No Inputs' 和 'No Move' 选项会改变覆盖层行为。",
		"仅在副本中或附近有敌人时显示窗口：UI > Windows > Only show with hostile or in duty。",
		"列表 标签页：管理驱散、优先级状态、击退、无敌和禁止施法列表。",
		"列表 标签页：使用 'Reset and Update' 快速恢复精选列表。",
		"状态列表：点击 '+' 可按名称或 ID 搜索；支持模糊搜索。",
		"状态列表：右键点击图标可移除；在弹出窗口中也可用 Delete 键。",
		"目标 标签页：调整目标选择、视野锥、接敌行为以及木桩/Boss 处理。",
		"目标 标签页：设置 /rotation Cycle 行为和目标选择延迟。",
		"通过聊天管理 TargetingTypes：/rotation Settings TargetingTypes add|remove <Type>。",
		"自动 > 动作使用与控制：允许/禁止 oGCD，设置 AoE 风格、爆发药、打断和 True North。",
		"自动 > 治疗用与控制：调整阈值和非治疗职业的治疗行为。",
		"治疗职业：在 自动 > 治疗用与控制 中自定义 复活/即刻咏唱 和优先级。",
		"地面 AoE：自动 > 治疗用与控制 中有选项可智能放置有益地面技能。",
		"基础 > 计时器：调整 Action Ahead 和 Min Updating Time，以平衡性能与插入技能。",
		"基础 > 自动切换：根据倒计时、死亡、区域切换等自动开启/关闭。",
		"教学模式 会高亮目标；颜色在 界面 > 信息 中设置。",
		"职业 标签页：在对应职业时编辑 DNC 舞伴、SGE 心关 坦克和 AST 卡片优先级。",
		"关于 > 宏 列出了可用的聊天/宏命令和实用语法。",
		"关于 > 链接：打开配置文件夹、GitHub、Ko-fi 和 Discord。",
		"额外 > 内部：安全地备份/恢复配置。",
		"额外：可选调整，例如移除动画/冷却延迟。",
		"点击侧边栏左下角的立方体图标，将诊断信息复制到剪贴板。",
		"时间线 窗口可可视化最近动作（界面 > 窗口）。",
		"打出伤害，别死",
		"治疗：唯一重要的 HP 就是最后一点",
		"友善待人",
		"需要时可以用“/statusoff <Name>”移除某些自身增益（例如 速行）。",
		"RSR在 Legacy Type 移动设置下效果最佳。"
	];
	private int _hintIndex = 0;
	private float _lastHintSwitch = 0f;
	private static readonly Random _hintRng = new();
	private string? _cachedTipText = null;
	private int _cachedTipIndex = -1;

	private bool CheckErrors()
	{
		if (_crashPlugins.Count != 0)
		{
			return true;
		}

		if (DataCenter.SystemWarnings != null && DataCenter.SystemWarnings.Count > 0)
		{
			return true;
		}

		if (DataCenter.DalamudStagingEnabled)
		{
			return true;
		}

		return Player.Available && (Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
		Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
		Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN);
	}

	private static string GetDynamicHintText(int index)
	{
		if (_supporters != null && _supporters.Length > 0 && index % 5 == 0)
		{
			var supporterIndex = _hintRng.Next(_supporters.Length);
			var supporter = _supporters[supporterIndex];
			return $"Special thanks to supporter: {supporter}!";
		}
		if (CNLanguageClient)
		{
			if (_baseUsageHintsCN != null && _baseUsageHintsCN.Length > 0 && index >= 0 && index < _baseUsageHintsCN.Length)
			{
				return _baseUsageHintsCN[index];
			}
		}
		else
		{
			if (_baseUsageHints != null && _baseUsageHints.Length > 0 && index >= 0 && index < _baseUsageHints.Length)
			{
				return _baseUsageHints[index];
			}
		}
		return "Thank you for using Rotation Solver Reborn!";
	}

	private bool DrawNotices()
	{
		var drewAnything = false;
		var hasErrors = CheckErrors();

		if (hasErrors)
		{
			if (DataCenter.DalamudStagingEnabled)
			{
				_ = M3Widgets.Banner("##staging_banner",
					"You are running the staging branch of Dalamud. For best compatibility, use XIVLauncher and switch back to the release branch when one is available for your game version.",
					M3Severity.Warning, FontAwesomeIcon.Flask);
				ImGui.Dummy(new Vector2(0f, M3.Space2));
				drewAnything = true;
			}

			if (Player.Available && Player.Job is Job.CRP or Job.BSM or Job.ARM or Job.GSM
				or Job.LTW or Job.WVR or Job.ALC or Job.CUL
				or Job.MIN or Job.FSH or Job.BTN)
			{
				_ = M3Widgets.Banner("##unsupported_job_banner",
					$"You are on an unsupported class: {Player.Job}. Rotation Solver only drives combat jobs.",
					M3Severity.Error, FontAwesomeIcon.Hammer);
				ImGui.Dummy(new Vector2(0f, M3.Space2));
				drewAnything = true;
			}

			if (DataCenter.SystemWarnings is { Count: > 0 })
			{
				List<string> warningsToRemove = [];
				var index = 0;

				foreach (var warning in DataCenter.SystemWarnings.Keys)
				{
					var id = $"##system_warning_{index++}";
					if (M3Widgets.Banner(id, warning, M3Severity.Warning, FontAwesomeIcon.ExclamationTriangle,
						out var bannerHovered, "Details",
						"Click Details to open plugin compatibility. Right-click the banner to dismiss it."))
					{
						SetActiveTab(MainWindowTab.About);
						_aboutHeaders.OpenHeaderByTitle(UiString.ConfigWindow_About_Compatibility.GetDescription());
					}

					if (bannerHovered && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					{
						warningsToRemove.Add(warning);
					}

					ImGui.Dummy(new Vector2(0f, M3.Space2));
					drewAnything = true;
				}

				foreach (var warning in warningsToRemove)
				{
					_ = DataCenter.SystemWarnings.Remove(warning);
				}
			}
		}

		if (!Service.Config.ShowHints)
		{
			return drewAnything;
		}

		var hints = CNLanguageClient ? _baseUsageHintsCN : _baseUsageHints;
		if (hints == null || hints.Length == 0)
		{
			return drewAnything;
		}

		const float HintSwitchIntervalSeconds = 8f;
		var now = (float)ImGui.GetTime();
		if (!hasErrors)
		{
			if (now - _lastHintSwitch >= HintSwitchIntervalSeconds)
			{
				_lastHintSwitch = now;
				_hintIndex++;
				if (_hintIndex >= hints.Length)
				{
					_hintIndex = 0;
				}

				_cachedTipIndex = -1;
				_cachedTipText = null;
			}
		}
		else
		{
			_lastHintSwitch = now;
		}

		if (_cachedTipIndex != _hintIndex || string.IsNullOrEmpty(_cachedTipText))
		{
			_cachedTipText = GetDynamicHintText(_hintIndex);
			_cachedTipIndex = _hintIndex;
		}

		if (M3Widgets.Banner("##usage_tip", _cachedTipText ?? string.Empty, M3Severity.Info, FontAwesomeIcon.Lightbulb,
			CNLanguageClient ? "复制" : "Copy"))
		{
			try
			{
				ImGui.SetClipboardText(_cachedTipText ?? string.Empty);
			}
			catch
			{
				// Clipboard can fail while the game is losing focus.
			}
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));
		return true;
	}
}
