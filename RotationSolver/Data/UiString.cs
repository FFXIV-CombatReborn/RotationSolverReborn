using System.ComponentModel;
using static RotationSolver.Basic.Rotations.Duties.DutyRotation;

namespace RotationSolver.Data
{
	internal enum UiString
	{
		[Description("The condition value you chose. Click to modify.")]
		ConfigWindow_ConditionSetDesc,

		[Description("Condition Value")]
		ConfigWindow_ConditionSet,

		[Description("Action Condition")]
		ConfigWindow_ActionSet,

		[Description("Trait Condition")]
		ConfigWindow_TraitSet,

		[Description("Target Condition")]
		ConfigWindow_TargetSet,

		[Description("Rotation Condition")]
		ConfigWindow_RotationSet,

		[Description("Named Condition")]
		ConfigWindow_NamedSet,

		[Description("Territory Condition")]
		ConfigWindow_Territoryset,

		[Description("No rotations loaded! Please check the rotations tab!")]
		ConfigWindow_NoRotation,

		[Description("Current duty logic.")]
		ConfigWindow_DutyRotationDesc,

		[Description("Remove")]
		ConfigWindow_List_Remove,

		[Description("Load from folder")]
		ActionSequencer_Load,

		[Description("Analyzes PvE combat information in every frame and finds the best action.")]
		ConfigWindow_About_Punchline,

		[Description("Invalid Rotation! \nPlease update to the latest version or contact {0}!")]
		ConfigWindow_Rotation_InvalidRotation,

		[Description("Click to switch rotations")]
		ConfigWindow_Helper_SwitchRotation,

		[Description("Search Result")]
		ConfigWindow_Search_Result,

		[Description("This includes almost all information available in one combat frame, including the status of all party members, hostile target statuses, skill cooldowns, MP and HP of characters, character locations, hostile target casting status, combo state, combat duration, player level, etc.\n\nIt will then highlight the best action on the hotbar, or help you click it.")]
		ConfigWindow_About_Description,

		[Description("This is designed for GENERAL COMBAT, not for Savage or Ultimate content. \n\nUse it carefully! While not designed specifically for Savage or Ultimate content RSR works fine in them, but it will not solve mechanics for you. Pay attention and use macros.")]
		ConfigWindow_About_Warning,

		[Description("RSR has helped you by clicking actions {0:N0} times.")]
		ConfigWindow_About_ClickingCount,

		[Description("State Macros")]
		ConfigWindow_About_Macros,

		[Description("Action and Setting Macros")]
		ConfigWindow_About_SettingMacros,

		[Description("Compatibility")]
		ConfigWindow_About_Compatibility,

		[Description("Supporters")]
		ConfigWindow_About_Supporters,

		[Description("Links")]
		ConfigWindow_About_Links,

		[Description("System Warnings")]
		ConfigWindow_About_Warnings,

		[Description("Warning Message")]
		ConfigWindow_About_Warnings_Warning,

		[Description("Warning Time")]
		ConfigWindow_About_Warnings_Time,

		[Description("Rotation Solver helps you choose targets and click actions. Any plugin that changes these will affect its decisions.\n\nHere is a list of plugins that have historically (but not always) caused compatibility issues:")]
		ConfigWindow_About_Compatibility_Description,

		[Description("Cannot properly execute the behavior that RSR wants to perform.")]
		ConfigWindow_About_Compatibility_Mistake,

		[Description("Conflicts with RSR decision-making")]
		ConfigWindow_About_Compatibility_Mislead,

		[Description("Causes the game to crash")]
		ConfigWindow_About_Compatibility_Crash,

		[Description("Many thanks to Ko-fi sponsors.")]
		ConfigWindow_About_ThanksToSupporters,

		[Description("Open Config Folder")]
		ConfigWindow_About_OpenConfigFolder,

		[Description("Description")]
		ConfigWindow_Rotation_Description,

		[Description("Configuration")]
		ConfigWindow_Rotation_Configuration,

		[Description("Duty Configuration")]
		ConfigWindow_DutyRotation_Configuration,

		[Description("Status")]
		ConfigWindow_Rotation_Status,

		[Description("Duty Rotation Status")]
		ConfigWindow_DutyRotation_Status,

		[Description("Used to customize when RSR uses specific actions automatically. Click on an action's icon in the left list. Below, you may set the conditions for when that specific action is used. Each action can have different conditions to override the default rotation behavior.")]
		ConfigWindow_Actions_Description,

		[Description("Show on CD window")]
		ConfigWindow_Actions_ShowOnCDWindow,

		[Description("Allow action to be intercepted by the intercept system")]
		ConfigWindow_Actions_IsIntercepted,

		[Description("Prevent this action against a curated list of mobs (ie. Jagd Dolls)")]
		ConfigWindow_Actions_IsRestrictedDOT,

		[Description("Allow action to be restricted by the minimum HP feature")]
		ConfigWindow_Actions_MinHPFeature,

		[Description("If target is below this percent, do not use this action")]
		ConfigWindow_Actions_MinHPPercent,

		[Description("Skip BossModReborn position-safety check for this movement action")]
		ConfigWindow_Actions_SkipPositionSafetyCheck,

		[Description("Time-to-kill threshold required for this action to be used")]
		ConfigWindow_Actions_TTK,

		[Description("Number of targets needed to use this action")]
		ConfigWindow_Actions_AoeCount,

		[Description("Should this action check needed status effects")]
		ConfigWindow_Actions_CheckStatus,

		[Description("Should this action check targets needed status effects")]
		ConfigWindow_Actions_CheckTargetStatus,

		[Description("Number of GCDs before the DOT/Status effect is reapplied")]
		ConfigWindow_Actions_GcdCount,

		[Description("HP ratio for automatic healing (only applies to healing actions)")]
		ConfigWindow_Actions_HealRatio,

		[Description("Forced Conditions have higher priority. If Forced Conditions are met, Disabled Conditions will be ignored.")]
		ConfigWindow_Actions_ConditionDescription,

		[Description("Forced Condition (Unsupported)")]
		ConfigWindow_Actions_ForcedConditionSet,

		[Description("Conditions for forced automatic use of action")]
		ConfigWindow_Actions_ForcedConditionSet_Description,

		[Description("Disabled Condition (Unsupported)")]
		ConfigWindow_Actions_DisabledConditionSet,

		[Description("Conditions that disable automatic use of an action")]
		ConfigWindow_Actions_DisabledConditionSet_Description,

		[Description("In this window, you can set parameters that can be customized using lists.")]
		ConfigWindow_List_Description,

		[Description("Statuses")]
		ConfigWindow_List_Statuses,

		[Description("Actions")]
		ConfigWindow_List_Actions,

		[Description("Map-specific settings")]
		ConfigWindow_List_Territories,

		[Description("Status name or ID")]
		ConfigWindow_List_StatusNameOrId,

		[Description("Invulnerability")]
		ConfigWindow_List_Invincibility,

		[Description("Priority")]
		ConfigWindow_List_Priority,

		[Description("Dispellable debuffs")]
		ConfigWindow_List_DangerousStatus,

		[Description("No-casting debuffs")]
		ConfigWindow_List_NoCastingStatus,

		[Description("Ignores target if it has one of these statuses")]
		ConfigWindow_List_InvincibilityDesc,

		[Description("Attacks the target first if it has one of these statuses")]
		ConfigWindow_List_PriorityDesc,

		[Description("Dispellable debuffs list")]
		ConfigWindow_List_DangerousStatusDesc,

		[Description("Do not take action if you have one of these debuffs")]
		ConfigWindow_List_NoCastingStatusDesc,

		[Description("Copy to Clipboard")]
		ConfigWindow_Actions_Copy,

		[Description("From Clipboard")]
		ActionSequencer_FromClipboard,

		[Description("Add Status")]
		ConfigWindow_List_AddStatus,

		[Description("Action name or ID")]
		ConfigWindow_List_ActionNameOrId,

		[Description("Tank Buster")]
		ConfigWindow_List_HostileCastingTank,

		[Description("AoE")]
		ConfigWindow_List_HostileCastingArea,

		[Description("Knockback")]
		ConfigWindow_List_HostileCastingKnockback,

		[Description("Gaze/Stop")]
		ConfigWindow_List_HostileCastingStop,

		[Description("Use tank personal damage mitigation abilities if the target is casting any of these actions")]
		ConfigWindow_List_HostileCastingTankDesc,

		[Description("Use AoE damage mitigation abilities if the target is casting any of these actions")]
		ConfigWindow_List_HostileCastingAreaDesc,

		[Description("Use knockback prevention abilities if the target is casting any of these actions")]
		ConfigWindow_List_HostileCastingKnockbackDesc,

		[Description("Stop casting or taking actions if the enemy is casting this ability")]
		ConfigWindow_List_HostileCastingStopDesc,

		[Description("Add Action")]
		ConfigWindow_List_AddAction,

		[Description("Don't target")]
		ConfigWindow_List_NoHostile,

		[Description("Don't provoke")]
		ConfigWindow_List_NoProvoke,

		[Description("Beneficial AoE locations")]
		ConfigWindow_List_BeneficialPositions,

		[Description("Enemies that will never be targeted")]
		ConfigWindow_List_NoHostileDesc,

		[Description("The name of the enemy that you don't want to target")]
		ConfigWindow_List_NoHostilesName,

		[Description("Enemies that will never be provoked")]
		ConfigWindow_List_NoProvokeDesc,

		[Description("The name of the enemy that you don't want to provoke")]
		ConfigWindow_List_NoProvokeName,

		[Description("Add beneficial AoE location")]
		ConfigWindow_List_AddPosition,

		[Description("Ability")]
		ActionAbility,

		[Description("Friendly")]
		ActionFriendly,

		[Description("Attack")]
		ActionAttack,

		[Description("Normal Targets")]
		NormalTargets,

		[Description("Targets with Heal-over-Time")]
		HotTargets,

		[Description("HP threshold for AoE healing oGCDs")]
		HpAoe0Gcd,

		[Description("HP threshold for AoE healing GCDs")]
		HpAoeGcd,

		[Description("HP threshold for single-target healing oGCDs")]
		HpSingle0Gcd,

		[Description("HP threshold for single-target healing GCDs")]
		HpSingleGcd,

		[Description("No Move")]
		InfoWindowNoMove,

		[Description("Move")]
		InfoWindowMove,

		[Description("Setting Search")]
		ConfigWindow_Searching,

		[Description("Timer")]
		ConfigWindow_Basic_Timer,

		[Description("Auto Switch")]
		ConfigWindow_Basic_AutoSwitch,

		[Description("Named Conditions")]
		ConfigWindow_Basic_NamedConditions,

		[Description("Others")]
		ConfigWindow_Basic_Others,

		[Description("The animation lock time for individual actions. For example, 0.6s.")]
		ConfigWindow_Basic_AnimationLockTime,

		[Description("The clicking duration - RSR will try to click at this moment.")]
		ConfigWindow_Basic_ClickingDuration,

		[Description("The ideal click time")]
		ConfigWindow_Basic_IdealClickingTime,

		[Description("The actual click time")]
		ConfigWindow_Basic_RealClickingTime,

		[Description("Auto turn-off conditions")]
		ConfigWindow_Basic_SwitchCancelConditionSet,

		[Description("Auto manual mode conditions")]
		ConfigWindow_Basic_SwitchManualConditionSet,

		[Description("Auto automatic mode conditions")]
		ConfigWindow_Basic_SwitchAutoConditionSet,

		[Description("Condition Name")]
		ConfigWindow_Condition_ConditionName,

		[Description("Information")]
		ConfigWindow_UI_Information,

		[Description("Overlay")]
		ConfigWindow_UI_Overlay,

		[Description("Windows")]
		ConfigWindow_UI_Windows,

		[Description("Change how RSR automatically uses actions")]
		ConfigWindow_Auto_Description,

		[Description("Action Usage and Control")]
		ConfigWindow_Auto_ActionUsage,

		[Description("Which actions RSR can use")]
		ConfigWindow_Auto_ActionUsage_Description,

		[Description("Healing Usage and Control")]
		ConfigWindow_Auto_HealingCondition,

		[Description("How RSR should use healing abilities")]
		ConfigWindow_Auto_HealingCondition_Description,

		[Description("Custom State Condition (Unsupported)")]
		ConfigWindow_Auto_StateCondition,

		[Description("Heal Area Forced Condition")]
		ConfigWindow_Auto_HealAreaConditionSet,

		[Description("Heal Single Forced Condition")]
		ConfigWindow_Auto_HealSingleConditionSet,

		[Description("Defense Area Forced Condition")]
		ConfigWindow_Auto_DefenseAreaConditionSet,

		[Description("Defense Single Forced Condition")]
		ConfigWindow_Auto_DefenseSingleConditionSet,

		[Description("Dispel/Stance/Positional Forced Condition")]
		ConfigWindow_Auto_DispelStancePositionalConditionSet,

		[Description("Raise/Shirk Forced Condition")]
		ConfigWindow_Auto_RaiseShirkConditionSet,

		[Description("Move Forward Forced Condition")]
		ConfigWindow_Auto_MoveForwardConditionSet,

		[Description("Move Back Forced Condition")]
		ConfigWindow_Auto_MoveBackConditionSet,

		[Description("Anti-Knockback Forced Condition")]
		ConfigWindow_Auto_AntiKnockbackConditionSet,

		[Description("Speed Forced Condition")]
		ConfigWindow_Auto_SpeedConditionSet,

		[Description("No Casting Condition Set")]
		ConfigWindow_Auto_NoCastingConditionSet,

		[Description("This will change how RSR uses actions")]
		ConfigWindow_Auto_ActionCondition_Description,

		[Description("Configuration")]
		ConfigWindow_Target_Config,

		[Description("Hostile")]
		ConfigWindow_List_Hostile,

		[Description("Enemy targeting logic. Adding more options cycles them when using /rotation Auto.\nUse /rotation Settings TargetingTypes add <option> to add,\n/rotation Settings TargetingTypes remove <option> to remove,\nand /rotation Settings TargetingTypes removeall to remove all options.")]
		ConfigWindow_Param_HostileDesc,

		[Description("Move Up")]
		ConfigWindow_Actions_MoveUp,

		[Description("Move Down")]
		ConfigWindow_Actions_MoveDown,

		[Description("Hostile target selection condition")]
		ConfigWindow_Param_HostileCondition,

		[Description("RSR focuses on the rotation itself. These are side features. Subject to removal at any time.")]
		ConfigWindow_Extra_Description,

		[Description("Event")]
		ConfigWindow_EventItem,

		[Description("Internal")]
		ConfigWindow_Internal,

		[Description("Others")]
		ConfigWindow_Extra_Others,

		[Description("Add Events")]
		ConfigWindow_Events_AddEvent,

		[Description("In this window, you can set which macro will be triggered after using an action.")]
		ConfigWindow_Events_Description,

		[Description("Duty Start: ")]
		ConfigWindow_Events_DutyStart,

		[Description("Duty End: ")]
		ConfigWindow_Events_DutyEnd,

		[Description("Delete Event")]
		ConfigWindow_Events_RemoveEvent,

		[Description("Click to make it reverse.\nIs reversed: {0}")]
		ActionSequencer_NotDescription,

		[Description("Member Name")]
		ConfigWindow_Actions_MemberName,

		[Description("Rotation is null. Please log in or switch jobs!")]
		ConfigWindow_Condition_RotationNullWarning,

		[Description("Ultimate")]
		ConfigWindow_Duty_Ultimate,

		[Description("Savage")]
		ConfigWindow_Duty_Savage,

		[Description("Chaotic Alliance Raid")]
		ConfigWindow_Duty_ChaoticAlliance,

		[Description("Extreme")]
		ConfigWindow_Duty_Extreme,

		[Description("Dungeon")]
		ConfigWindow_Duty_Dungeon,

		[Description("Deep Dungeon")]
		ConfigWindow_Duty_DeepDungeon,

		[Description("Variant Dungeon")]
		ConfigWindow_Duty_VariantDungeon,

		[Description("Treasure Dungeon")]
		ConfigWindow_Duty_TreasureDungeon,

		[Description("Alliance Raid")]
		ConfigWindow_Duty_Alliance,

		[Description("Field Ops")]
		ConfigWindow_Duty_FieldOps,

		[Description("PvP")]
		ConfigWindow_Duty_PvP,

		[Description("The Masked Carnivale")]
		ConfigWindow_Duty_TheMaskedCarnivale,

		[Description("Crucible of the Unbroken")]
		ConfigWindow_Duty_CrucibleOfTheUnbroken,

		[Description("Delay its transition to true")]
		ActionSequencer_Delay_Description,

		[Description("Delay its transition")]
		ActionSequencer_Offset_Description,

		[Description("Sufficient Level")]
		ActionConditionType_EnoughLevel,

		[Description("Time Offset")]
		ActionSequencer_TimeOffset,

		[Description("Charges")]
		ActionSequencer_Charges,

		[Description("Original")]
		ActionSequencer_Original,

		[Description("Adjusted")]
		ActionSequencer_Adjusted,

		[Description("{0}'s target")]
		ActionSequencer_ActionTarget,

		[Description("From All")]
		ActionSequencer_StatusAll,

		[Description("From Self")]
		ActionSequencer_StatusSelf,

		[Description("You should not use this, as this target isn't the action's target. Try selecting it from the action instead.")]
		ConfigWindow_Condition_TargetWarning,

		[Description("Territory Name")]
		ConfigWindow_Condition_TerritoryName,

		[Description("Duty Name")]
		ConfigWindow_Condition_DutyName,

		[Description("Please separately bind damage reduction/shield cooldowns in case RSR fails at a crucial moment in {0}!")]
		HighEndWarning,

		[Description("Click to execute the command")]
		ConfigWindow_Helper_RunCommand,

		[Description("Right-click to copy the command")]
		ConfigWindow_Helper_CopyCommand,

		[Description("Macro No.")]
		ConfigWindow_Events_MacroIndex,

		[Description("Is Shared")]
		ConfigWindow_Events_ShareMacro,

		[Description("Action Name")]
		ConfigWindow_Events_ActionName,

		[Description("Modify {0} to {1}")]
		CommandsChangeSettingsValue,

		[Description("Failed to find the config in this rotation. Please check it.")]
		CommandsCannotFindConfig,

		[Description("Will use it within {0}s")]
		CommandsInsertAction,

		[Description("Cannot find the action. Please check the action name.")]
		CommandsInsertActionFailure,

		[Description("Failed to get both value and config from string. Please make sure you provide both a config option and value.")]
		CommandsMissingArgument,

		[Description("Start")]
		SpecialCommandType_Start,

		[Description("Cancel")]
		SpecialCommandType_Cancel,

		[Description("Heal Area")]
		SpecialCommandType_HealArea,

		[Description("Heal Single")]
		SpecialCommandType_HealSingle,

		[Description("Defense Area")]
		SpecialCommandType_DefenseArea,

		[Description("Defense Single")]
		SpecialCommandType_DefenseSingle,

		[Description("Tank Stance")]
		SpecialCommandType_TankStance,

		[Description("Dispel")]
		SpecialCommandType_Dispel,

		[Description("Positional")]
		SpecialCommandType_Positional,

		[Description("Shirk")]
		SpecialCommandType_Shirk,

		[Description("Raise")]
		SpecialCommandType_Raise,

		[Description("Move Forward")]
		SpecialCommandType_MoveForward,

		[Description("Move Back")]
		SpecialCommandType_MoveBack,

		[Description("Anti-Knockback")]
		SpecialCommandType_AntiKnockback,

		[Description("Burst")]
		SpecialCommandType_Burst,

		[Description("End Special")]
		SpecialCommandType_EndSpecial,

		[Description("Speed")]
		SpecialCommandType_Speed,

		[Description("Limit Break")]
		SpecialCommandType_LimitBreak,

		[Description("No Casting")]
		SpecialCommandType_NoCasting,

		[Description("Auto Target")]
		SpecialCommandType_Smart,

		[Description("Manual Target")]
		SpecialCommandType_Manual,

		[Description("Off")]
		SpecialCommandType_Off,

		[Description("Open config window")]
		Commands_Rotation,

		[Description("Start RSR combat rotation state")]
		Commands_Start,

		[Description("Disable RSR combat rotation state")]
		Commands_Off,

		[Description("Rotation Solver Reborn Settings v")]
		ConfigWindowHeader,

		[Description("This config is job-specific")]
		JobConfigTip,

		[Description("This option is unavailable with your current job\n \nRoles or jobs needed:\n{0}")]
		NotInJob,

		[Description("Welcome to Rotation Solver Reborn!")]
		WelcomeWindow_Header,

		[Description("Here's what you missed since you were last here")]
		WelcomeWindow_WelcomeBack,

		[Description("It looks like you might be new here! Let's get you started!")]
		WelcomeWindow_Welcome,

		[Description("Recent Changes:")]
		WelcomeWindow_Changelog,
	}

	public static class EnumExtensions
	{
		private static readonly Dictionary<Enum, string> _enumDescriptions = [];

		public static string GetDescription<T>(this T value) where T : struct, Enum
		{
			return DescriptionCache<T>.Get(value);
		}

		private static class DescriptionCache<T> where T : struct, Enum
		{
			private static readonly Dictionary<T, string> _descriptions = [];

			public static string Get(T value)
			{
				if (!_descriptions.TryGetValue(value, out var description))
				{
					description = ReadDescription(typeof(T), value.ToString());
					_descriptions[value] = description;
				}

				return description;
			}
		}

		private static string ReadDescription(Type enumType, string name)
		{
			var field = enumType.GetField(name);
			return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
		}

		public static string GetDescription(this Enum value)
		{
			if (_enumDescriptions.TryGetValue(value, out var description))
			{
				return description;
			}

			var field = value.GetType().GetField(value.ToString());
			if (field == null)
			{
				_enumDescriptions.Add(value, value.ToString());
				return value.ToString();
			}

			var attribute = field.GetCustomAttribute<DescriptionAttribute>();

			var descString = attribute == null ? value.ToString() : attribute.Description;
			if (UI.RotationConfigWindow.CNLanguageClient) {
				descString = value switch {
					UiString.ConfigWindow_ConditionSetDesc => "您选择的条件值。点击修改。",
					UiString.ConfigWindow_ConditionSet => "条件值",
					UiString.ConfigWindow_ActionSet => "动作条件",
					UiString.ConfigWindow_TraitSet => "特性条件",
					UiString.ConfigWindow_TargetSet => "目标条件",
					UiString.ConfigWindow_RotationSet => "循环条件",
					UiString.ConfigWindow_NamedSet => "命名条件",
					UiString.ConfigWindow_Territoryset => "区域条件",
					UiString.ConfigWindow_NoRotation => "未加载任何循环！请检查“循环”标签页！",
					UiString.ConfigWindow_DutyRotationDesc => "当前任务逻辑。",
					UiString.ConfigWindow_List_Remove => "移除",
					UiString.ActionSequencer_Load => "从文件夹加载",
					UiString.ConfigWindow_About_Punchline => "分析每帧的PvE战斗信息并找出最佳动作。",
					UiString.ConfigWindow_Rotation_InvalidRotation => "无效循环！\n请更新到最新版本或联系 {0}！",
					UiString.ConfigWindow_Helper_SwitchRotation => "点击切换循环",
					UiString.ConfigWindow_Search_Result => "搜索结果",
					UiString.ConfigWindow_About_Description => "这包括单个战斗帧中几乎所有的可用信息，包括所有队员的状态、敌对目标状态、技能冷却、角色MP和HP、角色位置、敌对目标咏唱状态、连击状态、战斗时长、玩家等级等。\n\n随后它会在热键栏上高亮最佳动作，或帮助你点击它。",
					UiString.ConfigWindow_About_Warning => "这是为一般战斗设计的，而不是为零式或绝境战内容设计的。\n\n请谨慎使用！虽然RSR并非专门为零式或绝境战内容设计，但在其中也能正常工作，但它不会替你处理机制。请注意并配合使用宏。",
					UiString.ConfigWindow_About_ClickingCount => "RSR 已通过点击动作帮助了你 {0:N0} 次。",
					UiString.ConfigWindow_About_Macros => "状态宏",
					UiString.ConfigWindow_About_SettingMacros => "动作与设置宏",
					UiString.ConfigWindow_About_Compatibility => "兼容性",
					UiString.ConfigWindow_About_Supporters => "支持者",
					UiString.ConfigWindow_About_Links => "链接",
					UiString.ConfigWindow_About_Warnings => "系统警告",
					UiString.ConfigWindow_About_Warnings_Warning => "警告消息",
					UiString.ConfigWindow_About_Warnings_Time => "警告时间",
					UiString.ConfigWindow_About_Compatibility_Description => "Rotation Solver会帮助你选择目标并点击动作。任何改变这些的插件都会影响它的决策。\n\n以下插件历史上（但并非总是）造成过兼容性问题：",
					UiString.ConfigWindow_About_Compatibility_Mistake => "无法正确执行RSR想要执行的行为。",
					UiString.ConfigWindow_About_Compatibility_Mislead => "与RSR的决策冲突",
					UiString.ConfigWindow_About_Compatibility_Crash => "导致游戏崩溃",
					UiString.ConfigWindow_About_ThanksToSupporters => "非常感谢Ko-fi赞助者。",
					UiString.ConfigWindow_About_OpenConfigFolder => "打开配置文件夹",
					UiString.ConfigWindow_Rotation_Description => "描述",
					UiString.ConfigWindow_Rotation_Configuration => "配置",
					UiString.ConfigWindow_DutyRotation_Configuration => "任务配置",
					UiString.ConfigWindow_Rotation_Status => "状态",
					UiString.ConfigWindow_DutyRotation_Status => "任务循环状态",
					UiString.ConfigWindow_Actions_Description => "用于自定义RSR何时自动使用特定动作。在左侧列表中点击动作图标。下方可以设置该特定动作的使用条件。每个动作都可以有不同的条件，以覆盖默认循环行为。",
					UiString.ConfigWindow_Actions_ShowOnCDWindow => "在冷却窗口中显示",
					UiString.ConfigWindow_Actions_IsIntercepted => "允许该动作被拦截系统拦截",
					UiString.ConfigWindow_Actions_IsRestrictedDOT => "防止对精选怪物列表（例如 狩猎人偶）使用此动作",
					UiString.ConfigWindow_Actions_MinHPFeature => "允许该动作受最低HP功能限制",
					UiString.ConfigWindow_Actions_MinHPPercent => "如果目标低于此百分比，则不要使用此动作",
					UiString.ConfigWindow_Actions_SkipPositionSafetyCheck => "对此移动动作跳过 BossModReborn的位置安全检查",
					UiString.ConfigWindow_Actions_TTK => "使用此动作所需的击杀时间阈值",
					UiString.ConfigWindow_Actions_AoeCount => "使用此动作所需的目标数量",
					UiString.ConfigWindow_Actions_CheckStatus => "此动作是否应检查所需状态效果",
					UiString.ConfigWindow_Actions_CheckTargetStatus => "此动作是否应检查目标的所需状态效果",
					UiString.ConfigWindow_Actions_GcdCount => "重新施加DOT/状态效果前的GCD数量",
					UiString.ConfigWindow_Actions_HealRatio => "自动治疗的HP比例（仅适用于治疗动作）",
					UiString.ConfigWindow_Actions_ConditionDescription => "强制条件具有更高优先级。如果满足强制条件，将忽略禁用条件。",
					UiString.ConfigWindow_Actions_ForcedConditionSet => "强制条件（不支持）",
					UiString.ConfigWindow_Actions_ForcedConditionSet_Description => "强制自动使用动作的条件",
					UiString.ConfigWindow_Actions_DisabledConditionSet => "禁用条件（不支持）",
					UiString.ConfigWindow_Actions_DisabledConditionSet_Description => "禁用自动使用动作的条件",
					UiString.ConfigWindow_List_Description => "在此窗口中，你可以设置可通过列表自定义的参数。",
					UiString.ConfigWindow_List_Statuses => "状态",
					UiString.ConfigWindow_List_Actions => "动作",
					UiString.ConfigWindow_List_Territories => "地图特定设置",
					UiString.ConfigWindow_List_StatusNameOrId => "状态名称或 ID",
					UiString.ConfigWindow_List_Invincibility => "无敌",
					UiString.ConfigWindow_List_Priority => "优先级",
					UiString.ConfigWindow_List_DangerousStatus => "可驱散的减益",
					UiString.ConfigWindow_List_NoCastingStatus => "禁止咏唱的减益",
					UiString.ConfigWindow_List_InvincibilityDesc => "如果目标拥有这些状态之一，则忽略该目标",
					UiString.ConfigWindow_List_PriorityDesc => "如果目标拥有这些状态之一，则优先攻击该目标",
					UiString.ConfigWindow_List_DangerousStatusDesc => "可驱散减益列表",
					UiString.ConfigWindow_List_NoCastingStatusDesc => "如果你拥有这些减益之一，则不要采取动作",
					UiString.ConfigWindow_Actions_Copy => "复制到剪贴板",
					UiString.ActionSequencer_FromClipboard => "从剪贴板",
					UiString.ConfigWindow_List_AddStatus => "添加状态",
					UiString.ConfigWindow_List_ActionNameOrId => "动作名称或 ID",
					UiString.ConfigWindow_List_HostileCastingTank => "坦克死刑",
					UiString.ConfigWindow_List_HostileCastingArea => "范围攻击",
					UiString.ConfigWindow_List_HostileCastingKnockback => "击退",
					UiString.ConfigWindow_List_HostileCastingStop => "凝视/停止",
					UiString.ConfigWindow_List_HostileCastingTankDesc => "如果目标正在咏唱这些动作之一，则使用坦克个人减伤能力",
					UiString.ConfigWindow_List_HostileCastingAreaDesc => "如果目标正在咏唱这些动作之一，则使用范围减伤能力",
					UiString.ConfigWindow_List_HostileCastingKnockbackDesc => "如果目标正在咏唱这些动作之一，则使用防击退能力",
					UiString.ConfigWindow_List_HostileCastingStopDesc => "如果敌人正在咏唱此能力，则停止咏唱或采取动作",
					UiString.ConfigWindow_List_AddAction => "添加动作",
					UiString.ConfigWindow_List_NoHostile => "不要选中",
					UiString.ConfigWindow_List_NoProvoke => "不要挑衅",
					UiString.ConfigWindow_List_BeneficialPositions => "有益范围位置",
					UiString.ConfigWindow_List_NoHostileDesc => "永远不会被选为目标的敌人",
					UiString.ConfigWindow_List_NoHostilesName => "你不想选为目标的敌人名称",
					UiString.ConfigWindow_List_NoProvokeDesc => "永远不会被挑衅的敌人",
					UiString.ConfigWindow_List_NoProvokeName => "你不想挑衅的敌人名称",
					UiString.ConfigWindow_List_AddPosition => "添加有益范围位置",
					UiString.ActionAbility => "能力",
					UiString.ActionFriendly => "友方",
					UiString.ActionAttack => "攻击",
					UiString.NormalTargets => "普通目标",
					UiString.HotTargets => "带有持续治疗的目标",
					UiString.HpAoe0Gcd => "范围治疗oGCD的HP阈值",
					UiString.HpAoeGcd => "范围治疗GCD的HP阈值",
					UiString.HpSingle0Gcd => "单体治疗oGCD的HP阈值",
					UiString.HpSingleGcd => "单体治疗GCD的HP阈值",
					UiString.InfoWindowNoMove => "不移动",
					UiString.InfoWindowMove => "移动",
					UiString.ConfigWindow_Searching => "搜索设置",
					UiString.ConfigWindow_Basic_Timer => "计时器",
					UiString.ConfigWindow_Basic_AutoSwitch => "自动切换",
					UiString.ConfigWindow_Basic_NamedConditions => "命名条件",
					UiString.ConfigWindow_Basic_Others => "其他",
					UiString.ConfigWindow_Basic_AnimationLockTime => "单个动作的动画锁定时间。例如 0.6 秒。",
					UiString.ConfigWindow_Basic_ClickingDuration => "点击持续时间 - RSR将尝试在此刻点击。",
					UiString.ConfigWindow_Basic_IdealClickingTime => "理想点击时间",
					UiString.ConfigWindow_Basic_RealClickingTime => "实际点击时间",
					UiString.ConfigWindow_Basic_SwitchCancelConditionSet => "自动关闭条件",
					UiString.ConfigWindow_Basic_SwitchManualConditionSet => "自动切换至手动模式的条件",
					UiString.ConfigWindow_Basic_SwitchAutoConditionSet => "自动切换至自动模式的条件",
					UiString.ConfigWindow_Condition_ConditionName => "条件名称",
					UiString.ConfigWindow_UI_Information => "信息",
					UiString.ConfigWindow_UI_Overlay => "悬浮窗",
					UiString.ConfigWindow_UI_Windows => "窗口",
					UiString.ConfigWindow_Auto_Description => "更改RSR自动使用动作的方式",
					UiString.ConfigWindow_Auto_ActionUsage => "动作使用与控制",
					UiString.ConfigWindow_Auto_ActionUsage_Description => "RSR 可以使用哪些动作",
					UiString.ConfigWindow_Auto_HealingCondition => "治疗使用与控制",
					UiString.ConfigWindow_Auto_HealingCondition_Description => "RSR 应如何使用治疗能力",
					UiString.ConfigWindow_Auto_StateCondition => "自定义状态条件（不支持）",
					UiString.ConfigWindow_Auto_HealAreaConditionSet => "范围治疗强制条件",
					UiString.ConfigWindow_Auto_HealSingleConditionSet => "单体治疗强制条件",
					UiString.ConfigWindow_Auto_DefenseAreaConditionSet => "范围防御强制条件",
					UiString.ConfigWindow_Auto_DefenseSingleConditionSet => "单体防御强制条件",
					UiString.ConfigWindow_Auto_DispelStancePositionalConditionSet => "驱散/姿态/身位强制条件",
					UiString.ConfigWindow_Auto_RaiseShirkConditionSet => "复活/退避强制条件",
					UiString.ConfigWindow_Auto_MoveForwardConditionSet => "前进强制条件",
					UiString.ConfigWindow_Auto_MoveBackConditionSet => "后退强制条件",
					UiString.ConfigWindow_Auto_AntiKnockbackConditionSet => "防击退强制条件",
					UiString.ConfigWindow_Auto_SpeedConditionSet => "加速强制条件",
					UiString.ConfigWindow_Auto_NoCastingConditionSet => "禁止咏唱条件集",
					UiString.ConfigWindow_Auto_ActionCondition_Description => "这将更改RSR使用动作的方式",
					UiString.ConfigWindow_Target_Config => "配置",
					UiString.ConfigWindow_List_Hostile => "敌对",
					UiString.ConfigWindow_Param_HostileDesc =>
						"敌人目标选择逻辑。添加更多选项后，使用 /rotation Auto 时会在它们之间循环。\n使用 /rotation Settings TargetingTypes add <option> 添加，\n/rotation Settings TargetingTypes remove <option> 移除，\n以及 /rotation Settings TargetingTypes removeall 移除所有选项。",
					UiString.ConfigWindow_Actions_MoveUp => "上移",
					UiString.ConfigWindow_Actions_MoveDown => "下移",
					UiString.ConfigWindow_Param_HostileCondition => "敌对目标选择条件",
					UiString.ConfigWindow_Extra_Description => "RSR 专注于循环本身。这些是附加功能，可能随时移除。",
					UiString.ConfigWindow_EventItem => "事件",
					UiString.ConfigWindow_Internal => "内部",
					UiString.ConfigWindow_Extra_Others => "其他",
					UiString.ConfigWindow_Events_AddEvent => "添加事件",
					UiString.ConfigWindow_Events_Description => "在此窗口中，你可以设置使用动作后触发哪个宏。",
					UiString.ConfigWindow_Events_DutyStart => "任务开始：",
					UiString.ConfigWindow_Events_DutyEnd => "任务结束：",
					UiString.ConfigWindow_Events_RemoveEvent => "删除事件",
					UiString.ActionSequencer_NotDescription => "点击以反转。\n已反转：{0}",
					UiString.ConfigWindow_Actions_MemberName => "成员名称",
					UiString.ConfigWindow_Condition_RotationNullWarning => "循环为空。请登录或切换职业！",
					UiString.ConfigWindow_Duty_Ultimate => "绝境战",
					UiString.ConfigWindow_Duty_Savage => "零式",
					UiString.ConfigWindow_Duty_ChaoticAlliance => "诛灭战",
					UiString.ConfigWindow_Duty_Extreme => "极神",
					UiString.ConfigWindow_Duty_Dungeon => "迷宫",
					UiString.ConfigWindow_Duty_DeepDungeon => "深层迷宫",
					UiString.ConfigWindow_Duty_VariantDungeon => "多变迷宫",
					UiString.ConfigWindow_Duty_TreasureDungeon => "寻宝迷宫",
					UiString.ConfigWindow_Duty_Alliance => "友好部族任务",
					UiString.ConfigWindow_Duty_FieldOps => "特殊场景探索",
					UiString.ConfigWindow_Duty_PvP => "PvP",
					UiString.ConfigWindow_Duty_TheMaskedCarnivale => "假面狂欢",
					UiString.ConfigWindow_Duty_CrucibleOfTheUnbroken => "斗兽奇弈",
					UiString.ActionSequencer_Delay_Description => "延迟其转换为true",
					UiString.ActionSequencer_Offset_Description => "延迟其转换",
					UiString.ActionConditionType_EnoughLevel => "等级足够",
					UiString.ActionSequencer_TimeOffset => "时间偏移",
					UiString.ActionSequencer_Charges => "充能",
					UiString.ActionSequencer_Original => "原始",
					UiString.ActionSequencer_Adjusted => "调整后",
					UiString.ActionSequencer_ActionTarget => "{0} 的目标",
					UiString.ActionSequencer_StatusAll => "来自所有",
					UiString.ActionSequencer_StatusSelf => "来自自身",
					UiString.ConfigWindow_Condition_TargetWarning => "你不应使用此项，因为该目标不是动作的目标。请改为从动作中选择。",
					UiString.ConfigWindow_Condition_TerritoryName => "区域名称",
					UiString.ConfigWindow_Condition_DutyName => "任务名称",
					UiString.HighEndWarning => "请在 {0} 的关键时刻另行绑定减伤/护盾冷却，以防RSR失效！",
					UiString.ConfigWindow_Helper_RunCommand => "点击执行命令",
					UiString.ConfigWindow_Helper_CopyCommand => "右键复制命令",
					UiString.ConfigWindow_Events_MacroIndex => "宏编号",
					UiString.ConfigWindow_Events_ShareMacro => "是否共享",
					UiString.ConfigWindow_Events_ActionName => "动作名称",
					UiString.CommandsChangeSettingsValue => "将 {0} 修改为 {1}",
					UiString.CommandsCannotFindConfig => "在此循环中找不到该配置。请检查。",
					UiString.CommandsInsertAction => "将在 {0} 秒内使用",
					UiString.CommandsInsertActionFailure => "找不到该动作。请检查动作名称。",
					UiString.CommandsMissingArgument => "无法从字符串中同时获取值和配置。请确保同时提供配置选项和值。",
					UiString.SpecialCommandType_Start => "开始",
					UiString.SpecialCommandType_Cancel => "取消",
					UiString.SpecialCommandType_HealArea => "范围治疗",
					UiString.SpecialCommandType_HealSingle => "单体治疗",
					UiString.SpecialCommandType_DefenseArea => "范围防御",
					UiString.SpecialCommandType_DefenseSingle => "单体防御",
					UiString.SpecialCommandType_TankStance => "坦克姿态",
					UiString.SpecialCommandType_Dispel => "驱散",
					UiString.SpecialCommandType_Positional => "身位",
					UiString.SpecialCommandType_Shirk => "退避",
					UiString.SpecialCommandType_Raise => "复活",
					UiString.SpecialCommandType_MoveForward => "前进",
					UiString.SpecialCommandType_MoveBack => "后退",
					UiString.SpecialCommandType_AntiKnockback => "防击退",
					UiString.SpecialCommandType_Burst => "爆发",
					UiString.SpecialCommandType_EndSpecial => "结束特殊",
					UiString.SpecialCommandType_Speed => "加速",
					UiString.SpecialCommandType_LimitBreak => "极限技",
					UiString.SpecialCommandType_NoCasting => "禁止咏唱",
					UiString.SpecialCommandType_Smart => "自动目标",
					UiString.SpecialCommandType_Manual => "手动目标",
					UiString.SpecialCommandType_Off => "关闭",
					UiString.Commands_Rotation => "打开配置窗口",
					UiString.Commands_Start => "启动RSR战斗循环状态",
					UiString.Commands_Off => "禁用RSR战斗循环状态",
					UiString.ConfigWindowHeader => "RSR设置 v",
					UiString.JobConfigTip => "此配置为职业专用",
					UiString.NotInJob => "此选项在你当前职业下不可用\n\n所需职能或职业：\n{0}",
					UiString.WelcomeWindow_Header => "欢迎使用RSR！",
					UiString.WelcomeWindow_WelcomeBack => "这是你上次离开后错过的内容",
					UiString.WelcomeWindow_Welcome => "看起来你可能是新来的！让我们开始吧！",
					UiString.WelcomeWindow_Changelog => "最近更改：",

					SpecialCommandType.EndSpecial => "停止插件。不使用时请务必记得关闭它！",
					SpecialCommandType.HealArea => "打开窗口以使用范围治疗。",
					SpecialCommandType.HealSingle => "打开窗口以使用单体治疗。",
					SpecialCommandType.DefenseArea => "打开窗口以使用范围防御。",
					SpecialCommandType.DefenseSingle => "打开窗口以使用单体防御。",
					SpecialCommandType.DispelStancePositional => "打开窗口以使用康复、坦克姿态技能或真北。",
					SpecialCommandType.RaiseShirk => "打开窗口以使用复活或退避。",
					SpecialCommandType.MoveForward => "打开窗口以前进。",
					SpecialCommandType.MoveBack => "打开窗口以后退。",
					SpecialCommandType.AntiKnockback => "打开窗口以使用防击退技能。",
					SpecialCommandType.Burst => "打开窗口以进行爆发。",
					SpecialCommandType.Speed => "打开窗口以加速。",
					SpecialCommandType.LimitBreak => "打开窗口以使用极限技。",
					SpecialCommandType.NoCasting => "打开窗口以不使用咏唱技能。",
					SpecialCommandType.Intercepting => "RSR 正在拦截动作时的指示器。",

					StateCommandType.Off => "停止插件。不使用时请务必记得关闭它！",
					StateCommandType.Auto => "以自动模式启动插件。当脱战或进入战斗时，根据设定条件切换目标。\n可选：你可以在希望RSR执行的命令末尾添加目标类型。例如：/rotation Auto Big",
					StateCommandType.TargetOnly => "以仅选目标模式启动。RSR会按正常逻辑自动选择目标，但不会执行任何动作。",
					StateCommandType.Manual => "以手动模式启动插件。你需要手动选择目标。这将绕过你设置的所有交战设置，并且一旦选中目标就会立即开始攻击。",
					StateCommandType.AutoDuty => "此模式由 Autoduty 插件管理。",
					StateCommandType.Henched => "此模式由Henchman插件管理，或由任何其他需要 RSR 只执行循环而不进行目标选择的插件管理。",
					StateCommandType.PvP => "用于PvP特定活动的可选模式。",

					OtherCommandType.Settings => "打开设置。",
					OtherCommandType.Rotations => "打开循环。",
					OtherCommandType.DutyRotations => "打开任务循环。",
					OtherCommandType.DoActions => "执行动作。",
					OtherCommandType.ToggleActions => "切换动作。",
					OtherCommandType.NextAction => "执行下一个动作。",
					OtherCommandType.Cycle => "根据 目标 > 设置 中的设置在各状态之间循环。",

					ConfigUnitType.None => "无单位类型。",
					ConfigUnitType.Seconds => "时间单位，以秒为单位。",
					ConfigUnitType.Degree => "角度单位，以度为单位。",
					ConfigUnitType.Yalms => "距离单位，以米为单位。",
					ConfigUnitType.Percent => "比例单位，以百分比表示。",
					ConfigUnitType.Pixels => "显示单位，以像素为单位。",

					CanUseOption.None => "未指定选项。",
					CanUseOption.SkipStatusProvideCheck => "跳过状态提供检查。",
					CanUseOption.SkipComboCheck => "跳过连击检查。",
					CanUseOption.SkipCastingCheck => "跳过咏唱和移动检查。",
					CanUseOption.UsedUp => "用完所有层数。",
					CanUseOption.OnLastAbility => "在最后一个能力技时。",
					CanUseOption.SkipClippingCheck => "跳过截断检查。",
					CanUseOption.SkipAoeCheck => "跳过范围检查。",
					CanUseOption.targetOverride => "覆盖目标类型。",

					CycleType.CycleNormal => "在首个自动、手动和关闭之间循环。",
					CycleType.CycleAllAuto => "在每个自动、手动和关闭之间循环。",
					CycleType.CycleAuto => "在自动和关闭之间循环。",
					CycleType.CycleManual => "在手动和关闭之间循环。",
					CycleType.CycleManualAuto => "在手动和自动之间循环。",
					
					DTRType.DTRNormal => "在首个自动、手动和关闭之间循环。",
					DTRType.DTRAllAuto => "在每个自动、手动和关闭之间循环。",
					DTRType.DTRAuto => "在自动和关闭之间循环。",
					DTRType.DTRManual => "在手动和关闭之间循环。",
					DTRType.DTRManualAuto => "在手动和自动之间循环。",

					DescType.None => "无描述。",
					DescType.BurstActions => "爆发动作。",
					DescType.HealAreaGCD => "范围治疗 GCD。",
					DescType.HealAreaAbility => "范围治疗能力技。",
					DescType.HealSingleGCD => "单体治疗 GCD。",
					DescType.HealSingleAbility => "单体治疗能力技。",
					DescType.DefenseAreaGCD => "范围防御 GCD。",
					DescType.DefenseAreaAbility => "范围防御能力技。",
					DescType.DefenseSingleGCD => "单体防御 GCD。",
					DescType.DefenseSingleAbility => "单体防御能力技。",
					DescType.MoveForwardGCD => "前进 GCD。",
					DescType.MoveForwardAbility => "前进能力技。",
					DescType.MoveBackAbility => "后退能力技。",
					DescType.SpeedAbility => "加速能力技。",

					HardCastRaiseType.NoHardCast => "不硬读复活。",
					HardCastRaiseType.HardCastNormal => "在即刻咏唱冷却时复活。",
					HardCastRaiseType.HardCastOnlyHealer => "在即刻咏唱冷却且其他治疗死亡时复活。",
					HardCastRaiseType.HardCastSwiftCooldown => "在即刻咏唱冷却且冷却时间高于复活咏唱时间时复活。",
					HardCastRaiseType.HardCastOnlyHealerSwiftCooldown => "在即刻咏唱冷却、冷却时间高于复活咏唱时间且其他治疗死亡时复活。",

					RaiseType.PartyOnly => "仅复活小队成员。",
					RaiseType.PartyAndAllianceSupports => "复活小队成员和团队支援职业。",
					RaiseType.PartyAndAllianceHealers => "复活小队成员和团队治疗职业。",
					RaiseType.All => "在任务中复活所有人。",
					RaiseType.AllOutOfDuty => "复活所有人。",
					RaiseType.PartyHealersOnly => "仅复活小队治疗职业。",

					TargetHostileType.AllTargetsCanAttack => "所有在任意技能范围内可攻击的目标（坦克/自动任务）。",
					TargetHostileType.TargetsHaveTarget => "先前已交战的目标（非坦克）。",
					TargetHostileType.AllTargetsWhenSoloInDuty => "在任务中单人时所有目标（包括神秘新月），或先前已交战的目标。",
					TargetHostileType.AllTargetsWhenSolo => "单人时所有目标，或先前已交战的目标。",
					TargetHostileType.SoloDeepDungeonSmart => "单人深层迷宫：如果单人，脱战时拉最近的单个敌人；战斗中仅攻击先前已交战的目标。",

					TargetingType.Big => "最大。",
					TargetingType.Small => "最小。",
					TargetingType.HighHP => "高 HP。",
					TargetingType.LowHP => "低 HP。",
					TargetingType.HighHPPercent => "高 HP%。",
					TargetingType.LowHPPercent => "低 HP%。",
					TargetingType.HighMaxHP => "高最大 HP。",
					TargetingType.LowMaxHP => "低最大 HP。",
					TargetingType.Nearest => "最近。",
					TargetingType.Farthest => "最远。",
					TargetingType.PvPHealers => "PvP 中优先治疗职业。",
					TargetingType.PvPTanks => "PvP 中优先坦克。",
					TargetingType.PvPDPS => "PvP 中优先 DPS。",
					
					TinctureUseType.Nowhere => "不使用 Gemdraught/爆发药/药水。",
					TinctureUseType.InHighEndDuty => "在高难度任务中使用 Gemdraught/爆发药/药水。",
					TinctureUseType.Anywhere => "在任何地方使用 Gemdraught/爆发药/药水。",

					PhantomJob.Freelancer => "辅助自由人",
					PhantomJob.Knight => "辅助骑士",
					PhantomJob.Berserker => "辅助狂战士",
					PhantomJob.Monk => "辅助武僧",
					PhantomJob.Ranger => "辅助猎人",
					PhantomJob.Samurai => "辅助武士",
					PhantomJob.Bard => "辅助吟游诗人",
					PhantomJob.Geomancer => "辅助风水师",
					PhantomJob.TimeMage => "辅助时魔法师",
					PhantomJob.Cannoneer => "辅助炮击士",
					PhantomJob.Chemist => "辅助药师",
					PhantomJob.Oracle => "辅助预言家",
					PhantomJob.Thief => "辅助盗贼",
					PhantomJob.MysticKnight => "辅助神秘骑士",
					PhantomJob.Gladiator => "辅助剑斗士",
					PhantomJob.Dancer => "辅助舞者",
					PhantomJob.Ninja => "辅助忍者",
					PhantomJob.WhiteMage => "辅助白魔法师",
					PhantomJob.BlackMage => "辅助黑魔法师",
					PhantomJob.Dragoon => "辅助龙骑士",
					PhantomJob.Summoner => "辅助召唤师",
					PhantomJob.BlueMage => "辅助青魔法师",
					PhantomJob.RedMage => "辅助赤魔法师",
					PhantomJob.Necromancer => "辅助死灵法师",
					PhantomJob.None => "无",
					_ => descString
				};
			}

			_enumDescriptions.Add(value, descString);
			return descString;
		}
	}
}
