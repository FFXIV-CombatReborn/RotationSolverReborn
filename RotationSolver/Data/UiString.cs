using System.ComponentModel;

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

        [Description("This is designed for GENERAL COMBAT, not for Savage or Ultimate content. \n\nUse it carefully! While not designed specifically for Savage or Ultimate content RSR works fine in them, but it will not solve mechancis for you. Pay attention and use macros.")]
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

        [Description("Rotation Solver helps you choose targets and click actions. Any plugin that changes these will affect its decisions.\n\nHere is a list of known incompatible plugins:")]
        ConfigWindow_About_Compatibility_Description,

        [Description("Cannot properly execute the behavior that RSR wants to perform.")]
        ConfigWindow_About_Compatibility_Mistake,

        [Description("Conflicts with RSR decision-making")]
        ConfigWindow_About_Compatibility_Mislead,

        [Description("Causes the game to crash")]
        ConfigWindow_About_Compatibility_Crash,

        [Description("Many thanks to our sponsors.")]
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

        [Description("Use Forced Enable Condition")]
        ForcedEnableCondition,

        [Description("The conditions that force this to be true")]
        ForcedEnableConditionDesc,

        [Description("Use Forced Disable Condition")]
        ForcedDisableCondition,

        [Description("The conditions that force this to be false")]
        ForcedDisableConditionDesc,

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

        [Description("PvP-Specific Controls")]
        ConfigWindow_Auto_PvPSpecific,
    }

    public static class EnumExtensions
    {
        private static readonly Dictionary<Enum, string> _enumDescriptions = [];

        public static string GetDescription(this Enum value)
        {
            if (_enumDescriptions.TryGetValue(value, out string? description))
            {
                return description;
            }

            FieldInfo? field = value.GetType().GetField(value.ToString());
            if (field == null)
            {
                _enumDescriptions.Add(value, value.ToString());
                return value.ToString();
            }

            DescriptionAttribute? attribute = field.GetCustomAttribute<DescriptionAttribute>();

            string descString = attribute == null ? value.ToString() : attribute.Description;
            _enumDescriptions.Add(value, descString);
            return descString;
        }
    }
}