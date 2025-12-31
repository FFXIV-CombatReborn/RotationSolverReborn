using Dalamud.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using System.Collections.Concurrent;
using static RotationSolver.Basic.Configuration.ConfigTypes;

namespace RotationSolver.Basic.Configuration;

internal partial class Configs : IPluginConfiguration
{
    [JsonIgnore]
    public const string
        BasicTimer = "BasicTimer",
        BasicAutoSwitch = "BasicAutoSwitch",
        BasicParams = "BasicParams",
        UiInformation = "UiInformation",
        UiWindows = "UiWindows",
        PvPSpecificControls = "PvPSpecificControls",
        AutoActionUsage = "AutoActionUsage",
        HealingActionCondition = "HealingActionCondition",
        PhantomDutyRotationConfiguration = "PhantomDutyRotationConfiguration",
        TargetConfig = "TargetConfig",
        Extra = "Extra",
        Rotations = "Rotations",
        List = "List",
        List2 = "List2",
        List3 = "List3",
        Debug = "Debug";

    public const int CurrentVersion = 12;
    public int Version { get; set; } = CurrentVersion;

    public string LastSeenChangelog { get; set; } = "0.0.0.0";
    public bool FirstTimeSetupDone { get; set; } = false;

    public List<ActionEventInfo> Events { get; private set; } = [];
    public SortedSet<Job> DisabledJobs { get; private set; } = [];

    public string[] RotationLibs { get; set; } = [];
    public List<TargetingType> TargetingTypes { get; set; } = [];

    public MacroInfo DutyStart { get; set; } = new MacroInfo();
    public MacroInfo DutyEnd { get; set; } = new MacroInfo();

    [ConditionBool, UI("Intercept player input and queue it for RSR to execute the action. (PvE only)",
    Filter = AutoActionUsage, Section = 5)]
    private static readonly bool _interceptAction2 = false;

    [ConditionBool, UI("Allow intercepting Spells.",
    Filter = AutoActionUsage, Section = 5, Parent = nameof(InterceptAction2))]
    private static readonly bool _interceptSpell2 = false;

    [ConditionBool, UI("Allow intercepting Weaponskills.",
    Filter = AutoActionUsage, Section = 5, Parent = nameof(InterceptAction2))]
    private static readonly bool _interceptWeaponskill2 = false;

    [ConditionBool, UI("Allow intercepting Abilities.",
    Filter = AutoActionUsage, Section = 5, Parent = nameof(InterceptAction2))]
    private static readonly bool _interceptAbility2 = false;

    [ConditionBool, UI("Allow intercepting actions in macros.",
    Filter = AutoActionUsage, Section = 5, Parent = nameof(InterceptAction2))]
    private static readonly bool _interceptMacro = false;

    [ConditionBool, UI("Allow intercepting actions that are currently on cooldown.",
    Filter = AutoActionUsage, Section = 5, Parent = nameof(InterceptAction2))]
    private static readonly bool _interceptCooldown = false;

    [UI("Intercepted action execution window (amount of time RSR is allowed to attempt to use an action after it has been intercepted)",
    Filter = AutoActionUsage, Section = 5)]
    [Range(1, 10, ConfigUnitType.Seconds)]
    public float InterceptActionTime { get; set; } = 5;

    /// <markdown file="Auto" name="What kind of AoE moves to use" section="Action Usage and Control">
    /// - Full: Use all available AoE actions.
    /// - Cleave: Use only single-target AoE actions.
    /// - Off: Do not use any AoE actions.
    /// </markdown>
    [UI("What kind of AoE moves to use.",
    Description = "Full: Use all available AoE actions.\nCleave: Use only single-target AoE actions.\nOff: Do not use any AoE actions.",
    Filter = AutoActionUsage, Section = 3)]
    public AoEType AoEType { get; set; } = AoEType.Full;

    [ConditionBool, UI("Ignore status application against mobs that are status capped.",
    Filter = AutoActionUsage, Section = 3)]
    private static readonly bool _statuscap2 = true;

    [ConditionBool, UI("Don't attack new mobs by AoE.", Description = "Never use any AoE action when this may attack mobs that are not hostile targets.",
        Filter = AutoActionUsage, Section = 3)]
    private static readonly bool _noNewHostiles = false;

    [ConditionBool, UI("Disable automatically during area transitions.",
    Description = "Automatically turn off combat state when moving between different areas.",
    Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffBetweenArea = true;

    [ConditionBool, UI("Disable automatically during cutscenes.",
    Description = "Automatically turn off combat state during cutscenes.",
    Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffCutScene = true;

    [ConditionBool, UI("Auto turn off when switching jobs",
    Description = "Automatically turn off combat state when you change your job/class.",
    Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffSwitchClass = true;

    [ConditionBool, UI("Auto turn off when dead in PvE.",
    Description = "Automatically turn off combat state when your character dies.",
    Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffWhenDead = true;

    [ConditionBool, UI("Auto turn off when duty is completed.",
    Description = "Automatically turn off combat state when a duty (instance) ends.",
    Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffWhenDutyCompleted = true;

    [ConditionBool, UI("Enable changelog window popup on update",
    Description = "Show a popup window with the changelog when the plugin updates.",
    Filter = UiInformation)]
    private static readonly bool _changelogPopup = true;

    [ConditionBool, UI("Show plugin status in DTR bar.",
    Description = "Display the plugin's current status in the server information bar.",
    Filter = UiInformation)]
    private static readonly bool _showInfoOnDtr = true;

    [UI("DTR Behaviour", Filter = UiInformation, Parent = nameof(ShowInfoOnDtr))]
    public DTRType DTRType { get; set; } = DTRType.DTRNormal;

    [ConditionBool, UI("Display plugin status in toast popup",
    Description = "Show a toast notification with the combat state when changed.",
    Filter = UiInformation)]
    private static readonly bool _showInfoOnToast = false;

    [ConditionBool, UI("Lock movement when casting or performing certain actions.",
    Description = "Prevents your character from moving while casting or using specific actions.",
    Filter = Extra)]
    private static readonly bool _poslockCasting = false;

    [UI("", Action = ActionID.PassageOfArmsPvE, Parent = nameof(PoslockCasting))]
    public bool PosPassageOfArms { get; set; } = false;

    [UI("", Action = ActionID.FlamethrowerPvE, Parent = nameof(PoslockCasting))]
    public bool PosFlameThrower { get; set; } = false;

    [UI("", Action = ActionID.ImprovisationPvE, Parent = nameof(PoslockCasting))]
    public bool PosImprovisation { get; set; } = false;

    /// <markdown file="Auto" name="Gemdraughts/Tinctures/Pots Usage" section="Action Usage and Control">
    /// Sets whether to use damage-boosting potions and in which duty. You also need to enable the specific
    /// potion in the `Actions` tab, under `Items`.
    /// </markdown>
    [JobConfig, UI("Only used automatically if coded into the rotation",
    Description = "This setting is only used if the rotation specifically supports automatic tincture usage.",
    Filter = AutoActionUsage, PvPFilter = JobFilterType.NoJob)]
    private readonly TinctureUseType _TinctureType = TinctureUseType.Nowhere;

    [ConditionBool, UI("Automatically use Anti-Knockback role actions (Arms Length, Surecast)",
    Description = "Enable to automatically use anti-knockback abilities when needed based on anti-knockback action list in List menu.",
    Filter = AutoActionUsage)]
    private static readonly bool _useKnockback = true;

    [ConditionBool, UI("Automatically use HP Potions",
    Description = "Enable to allow the plugin to use HP potions automatically.",
    Filter = AutoActionUsage)]
    private static readonly bool _useHpPotions = false;

	[UI("Use HP Potions when HP% is lower than this", Parent = nameof(UseHpPotions))]
	[Range(0, 1, ConfigUnitType.Percent, 0.002f)]
	public float UseHpPotionsPercent { get; set; } = 0.5f;

	[ConditionBool, UI("Automatically use MP Potions",
    Description = "Enable to allow the plugin to use MP potions automatically.",
    Filter = AutoActionUsage)]
    private static readonly bool _useMpPotions = false;

	[UI("Use MP Potions when MP% is lower than this", Parent = nameof(UseMpPotions))]
	[Range(0, 1, ConfigUnitType.Percent, 0.002f)]
	public float UseMpPotionsPercent { get; set; } = 0.5f;

	[ConditionBool, UI("Automatically use Phoenix Down",
    Description = "Enable to allow the plugin to use Phoenix Down item. (Experimental feature)",
    Filter = AutoActionUsage)]
    private static readonly bool _usePhoenixDown = false;

    [UI("Use damaging gap closer abilites if the distance to your target is less than this.",
        Filter = AutoActionUsage)]
    [Range(0, 30, ConfigUnitType.Yalms, 1f)]
    public float DistanceForMoving2 { get; set; } = 3f;

    [ConditionBool, UI("Allow the use of AOEs against priority-marked targets.",
    Description = "Enable to allow AoE actions to hit targets with priority markers.",
    Parent = nameof(ChooseAttackMark))]
    private static readonly bool _canAttackMarkAOE = true;

    [ConditionBool, UI("Teaching mode", Filter = UiInformation)]
    private static readonly bool _teachingMode = false;

    [ConditionBool, UI("Simulate the effect of pressing abilities",
        Filter = UiInformation)]
    private static readonly bool _keyBoardNoise = true;

    [ConditionBool, UI("Activate auto mode when countdown starts",
        Filter = BasicAutoSwitch, Section = 1)]
    private static readonly bool _startOnCountdown = true;

    [ConditionBool, UI("Start manual mode instead of auto mode when countdown starts",
               Parent = nameof(StartOnCountdown))]
    private static readonly bool _countdownStartsManualMode = false;

    [ConditionBool, UI("Cancel auto mode if combat starts early during countdown",
        Filter = BasicAutoSwitch, Section = 1)]
    private static readonly bool _cancelStateOnCombatBeforeCountdown = false;

	[ConditionBool, UI("I understand that Auto On settings will turn RSRs autorotation on, automatically.",
		Filter = BasicAutoSwitch, Section = 1)]
	private static readonly bool _AutoOnYes = false;

	[ConditionBool, UI("Auto turn on manual mode when attacked.",
			   Parent = nameof(AutoOnYes))]
    private static readonly bool _startOnAttackedBySomeone2 = false;
    
    [ConditionBool, UI("Auto turn on auto mode when party is in combat.",
			   Parent = nameof(AutoOnYes))]
    private static readonly bool _startOnPartyIsInCombat2 = false;
    
    [ConditionBool, UI("Auto turn on auto mode when alliance is in combat.",
			   Parent = nameof(AutoOnYes))]
    private static readonly bool _startOnAllianceIsInCombat2 = false;
    
    [ConditionBool, UI("Auto turn on auto mode when in combat in Bozja/Eureka/Occult Fate/CE",
			   Parent = nameof(AutoOnYes))]
    private static readonly bool _startOnFieldOpInCombat2 = false;

    /// <markdown file="Auto" name="Use healing abilities when playing a non-healer role" section="Healing Usage and Control">
    /// Allow usage of healing abilities when not playing as a healer (such as Vercure, Bloodbath, etc.)
    /// </markdown>
    [ConditionBool, UI("Use healing abilities when playing a non-healer role.",
        Filter = HealingActionCondition, Section = 1)]
    private static readonly bool _useHealWhenNotAHealer = true;

    [JobConfig, UI("Use interrupt abilities if possible.",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Interrupt,
        PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _interruptibleMoreCheck = true;

    [ConditionBool, UI("Provoke anything not on the no provoke list.",
        Filter = AutoActionUsage, Section = 3)]
    private static readonly bool _provokeAnything = false;

    [ConditionBool, UI("Stop casting if the target dies.", Filter = Extra)]
    private static readonly bool _useStopCasting = false;

    /// <markdown file="Auto" name="Cleanse all dispellable debuffs" section="Action Usage and Control">
    /// Enabling this setting will force the usage of Esuna on all target that are affected by a
    /// cleansable debuff.
    /// </markdown>
    [ConditionBool, UI("Cleanse all dispellable debuffs regardless of healing.",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Dispel, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _dispelAll = false;

    [ConditionBool, UI("Debug Mode", Filter = Debug)]
    private static readonly bool _inDebug = false;

    [ConditionBool, UI("Make /rotation Manual a toggle command.",
        Filter = BasicParams)]
    private static readonly bool _toggleManual = false;

    [ConditionBool, UI("Make /rotation Auto a toggle command. (Normal behavior cycles between targeting settings)",
        Filter = BasicParams)]
    private static readonly bool _toggleAuto = false;

    [ConditionBool, UI("Only show these windows if there are enemies or in duty",
        Filter = UiWindows)]
    private static readonly bool _onlyShowWithHostileOrInDuty = false;

    [ConditionBool, UI("Show Control Window",
        Filter = UiWindows)]
    private static readonly bool _showControlWindow = false;

    [ConditionBool, UI("Lock Control Window",
        Filter = UiWindows)]
    private static readonly bool _isControlWindowLock = false;

    [ConditionBool, UI("Show Next Action Window", Filter = UiWindows)]
    private static readonly bool _showNextActionWindow = false;

    [ConditionBool, UI("No Inputs", Parent = nameof(ShowNextActionWindow))]
    private static readonly bool _isInfoWindowNoInputs = false;

    [ConditionBool, UI("No Move", Parent = nameof(ShowNextActionWindow))]
    private static readonly bool _isInfoWindowNoMove = false;

    [ConditionBool, UI("Show Items' Cooldown",
        Parent = nameof(ShowCooldownWindow))]
    private static readonly bool _showItemsCooldown = false;

    [ConditionBool, UI("Show GCD Cooldown",
        Parent = nameof(ShowCooldownWindow))]
    private static readonly bool _showGCDCooldown = false;

    [ConditionBool, UI("Show Original Cooldown",
        Filter = UiInformation)]
    private static readonly bool _useOriginalCooldown = false;

    [ConditionBool, UI("Always Show Cooldowns", Filter = UiInformation)]
    private static readonly bool _showCooldownsAlways = false;

    [ConditionBool, UI("Show tooltips",
        Filter = UiInformation)]
    private static readonly bool _showTooltips = true;

    [ConditionBool, UI("Show Action Context Menu Enable/Disable toggle",
        Filter = UiWindows)]
    private static readonly bool _showContext = true;

    [ConditionBool, UI("Show random usage hints at top of configuration window",
        Description = "Displays rotating tips in the main panel; updates every 7 seconds.",
        Filter = UiInformation)]
    private static readonly bool _showHints = true;

    [ConditionBool, UI("Color disabled actions on hotbars",
        Description = "When enabled, actions you have disabled in RSR will be tinted on in-game hotbars.",
        Filter = UiInformation)]
    private static readonly bool _reddenDisabledHotbarActions = false;

    [UI("Disabled actions hotbar tint color", Parent = nameof(ReddenDisabledHotbarActions), Filter = UiInformation)]
    public Vector4 HotbarDisabledTintColor { get; set; } = new(1f, 0f, 0f, 0.40f);

    [ConditionBool, UI("Display do action feedback on toast",
        Filter = UiInformation)]
    private static readonly bool _showToastsAboutDoAction = false;

    [ConditionBool, UI("Allow rotations that use this config to use abilities defined in the rotation as burst", Filter = AutoActionUsage, Section = 4)]
    private static readonly bool _autoBurst = true;

    /// <markdown file="Auto" name="Disable hostile actions if something is casting an action on the Gaze/Stop list" section="Action Usage and Control">
    /// This setting is linked with the <see cref="RotationSolver.Basic.Configuration.OtherConfiguration.HostileCastingStop">Gaze/Stop list.</see>
    /// </markdown>
    [ConditionBool, UI("Disable hostile actions if something is casting an action on the Gaze/Stop list (EXPERIMENTAL)", Filter = AutoActionUsage, Section = 4)]
    private static readonly bool _castingStop = false;

    [UI("Configurable amount of time before the cast finishes that RSR stops taking actions", Filter = AutoActionUsage, Section = 4, Parent = nameof(CastingStop))]
    [Range(0, 15, ConfigUnitType.Seconds)]
    public float CastingStopTime { get; set; } = 2.5f;

    [ConditionBool, UI("Disable for the entire duration (Enabling this will prevent your actions for the entire cast.)", Filter = AutoActionUsage, Section = 4, Parent = nameof(CastingStop))]
    private static readonly bool _castingStopCalculate = false;

    /// <markdown file="Auto" name="Automatic Healing Thresholds" section="Healing Usage and Control" isSubsection="1">
    /// When enabled, you can customize the healing thresholds for when healing will be cast occur on target(s).
    /// </markdown>
    [ConditionBool, UI("Automatic Healing Thresholds", Filter = HealingActionCondition, Section = 1, Order = 1)]
    private static readonly bool _autoHeal = true;

    /// <markdown file="Auto" name="Stop Healing Cast After Reaching Threshold" section="Healing Usage and Control" isSubsection="1">
    /// When enabled, you can customize the healing thresholds for when healing will be cast occur on target(s).
    /// </markdown>
    [ConditionBool, UI("Stop single target GCD healing after reaching threshold. (EXTREMELY Experimental)", Filter = HealingActionCondition, Section = 1, Order = 2, Description = "If you have another healer on the team, their healing might put the target player(s) above the healing threshold and you'll waste MP. This interrupts the cast if it happens.")]
    private static readonly bool _stopHealingAfterThresholdExperimental2 = false;

    /// <markdown file="Auto" name="Auto-use oGCD abilities" section="Action Usage and Control" isSubsection="1">
    /// Whether to use oGCD abilities or not at all.
    /// </markdown>
    [ConditionBool, UI("Auto-use oGCD abilities", Filter = AutoActionUsage)]
    private static readonly bool _useAbility = true;

    [ConditionBool, UI("Use defensive abilities", Description = "It is recommended to check this option if you are playing Raids or you can plan the heal and defense ability usage by yourself.",
        Parent = nameof(UseAbility))]
    private static readonly bool _useDefenseAbility = true;

    [ConditionBool, UI("Automatically activate tank stance", Parent = nameof(UseAbility),
        PvEFilter = JobFilterType.Tank)]
    private static readonly bool _autoTankStance = true;

    [ConditionBool, UI("Auto provoke when there is another tank in party", Description = "Automatically use provoke when an enemy is attacking a non-tank member of the party while there is more than one tank in party.",
        Parent = nameof(UseAbility), PvEFilter = JobFilterType.Tank)]
    private static readonly bool _autoProvokeForTank = true;

    /// <markdown file="Auto" name="Auto True North" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._useAbility">
    /// Whether to cast True North when playing as a melee DPS when you do not have the right
    /// positional on the enemy.
    /// </markdown>
    [ConditionBool, UI("Auto True North (Melee DPS)",
        Parent = nameof(UseAbility),
        PvEFilter = JobFilterType.Melee)]
    private static readonly bool _autoUseTrueNorth = true;

    [ConditionBool, UI("Use movement speed increase abilities when out of combat and in duty.", Parent = nameof(UseAbility))]
    private static readonly bool _autoSpeedOutOfCombat = true;

    [ConditionBool, UI("Use movement speed increase abilities when out of combat and out of duty.", Parent = nameof(UseAbility))]
    private static readonly bool _autoSpeedOutOfCombatNoDuty = false;

    [ConditionBool, UI("Use beneficial ground-targeted actions", Description = "1.    Self-Target Fallback:\r\nIf range is zero, always targets the player and returns all affectable targets at the player's position.\r\n2.    Preferred Positions (OnLocations):\r\n•    Tries to get predefined beneficial positions for the current territory.\r\n•    If none are found and the content is a trial or raid, uses fallback points (e.g., 0,0 or 100,100 point as those are the center of arenas most of the time).\r\n•    Picks the closest point to the player, applies a small random offset, and checks if it’s within effect range.\r\n•    If so, returns that as the target area.\r\n3.    Boss Positional Fallback:\r\n•    If the current target is a boss with positional requirements and within range, uses the boss’s position (or a point within range) as the target area.\r\n4.    Party Member Fallback:\r\n•    Gathers party members within range + effect range.\r\n•    Attempts to find a party member who is being attacked (tank or focus target).\r\n•    If found, calculates whether to stay at the player’s position or move closer to the tank, based on distances and effect range.\r\n•    If not found or not needed, defaults to the player’s position.", Section = 3)]
    private static readonly bool _useGroundBeneficialAbility = true;

    /// <markdown file="Auto" name="Use beneficial ground-targeted actions when moving" section="Healing Usage and Control">
    /// Enable to allow the usage of ground AoE actions while moving, such as Earthly Star (AST), Sacred Soil (SCH), etc.
    /// </markdown>
    [JobConfig, UI("Use beneficial ground-targeted actions when moving.", Parent = nameof(UseGroundBeneficialAbility))]
    private static readonly bool _useGroundBeneficialAbilityWhenMoving = false;

    [JobConfig, UI("Use beneficial ground-targeted actions only on self, skipping other logic.", Parent = nameof(UseGroundBeneficialAbility))]
    private static readonly bool _useGroundBeneficialAbilityOnlySelf = false;

    [ConditionBool, UI("Show Cooldown Window", Filter = UiWindows)]
    private static readonly bool _showCooldownWindow = false;

    [ConditionBool, UI("Show Action Timeline Window (currently bugged)", Filter = UiWindows)]
    private static readonly bool _showActionTimelineWindow = false;

    [ConditionBool, UI("Only show timeline in combat", Parent = nameof(ShowActionTimelineWindow))]
    private static readonly bool _actionTimelineOnlyInCombat = true;

    [ConditionBool, UI("Only show timeline when RSR is active", Parent = nameof(ShowActionTimelineWindow))]
    private static readonly bool _actionTimelineOnlyWhenActive = true;

    [ConditionBool, UI("Show oGCD actions in timeline", Parent = nameof(ShowActionTimelineWindow))]
    private static readonly bool _actionTimelineShowOGCD = true;

    [ConditionBool, UI("Show auto-attacks in timeline", Parent = nameof(ShowActionTimelineWindow))]
    private static readonly bool _actionTimelineShowAutoAttack = false;

    [ConditionBool, UI("Save timeline to JSON file after combat", Parent = nameof(ShowActionTimelineWindow))]
    private static readonly bool _actionTimelineSaveToFile = false;

    [ConditionBool, UI("Record AOE actions", Filter = List)]
    private static readonly bool _recordCastingArea = true;

    [ConditionBool, UI("Auto turn off RSR when combat is over for more than:",
        Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffAfterCombat = true;

    [ConditionBool, UI("Enable RSR click counter in main menu",
        Filter = Extra)]
    private static readonly bool _enableClickingCount = true;

    [ConditionBool, UI("Hide all warnings",
        Filter = UiInformation)]
    private static readonly bool _hideWarning = false;

    /// <markdown file="Auto" name="Only heal self when not a Healer" section="Healing Usage and Control">
    /// When enabled and not a healer, skills that can target someone and heal will only target you.
    /// </markdown>
    [ConditionBool, UI("Only heal self when not a Healer",
        Filter = HealingActionCondition, Section = 1)]
    private static readonly bool _onlyHealSelfWhenNoHealer = false;

    [ConditionBool, UI("Only use healing abilities as a non-healer if there are no living healers in the party.",
    Description = "When enabled, non-healer jobs (such as DPS or tanks) will only use healing abilities if there are no healers in the party, or if all healers are incapacitated (at 0 HP). \r\nIf at least one healer is alive, non-healers will not use healing abilities.",
    Filter = HealingActionCondition, Section = 1)]
    private static readonly bool _onlyHealAsNonHealIfNoHealers = false;

    [ConditionBool, UI("Show toggled setting and new value in chat.",
        Filter = UiInformation)]
    private static readonly bool _ShowToggledSettingInChat = false;

    [ConditionBool, UI("Record knockback actions", Filter = List2)]
    private static readonly bool _recordKnockbackies = false;

    [ConditionBool, UI("Set Blue Mage Actions Automatically", Description = "When using a Blue Mage Rotation, RSR can automatically set your spell book to the spells required by that rotation.", Filter = Extra)]
    private static readonly bool _setBluActions2 = false;

    #region Float
    [UI("Auto turn off RSR when combat is over for more than...",
        Parent = nameof(AutoOffAfterCombat))]
    [Range(0, 600, ConfigUnitType.Seconds)]
    public float AutoOffAfterCombatTime { get; set; } = 30;

    [UI("The angle of your vision cone", Parent = nameof(OnlyAttackInVisionCone))]
    [Range(0, 90, ConfigUnitType.Degree, 0.02f)]
    public float AngleOfVisionCone { get; set; } = 45;

    /// <markdown file="Auto" name="Melee Range action using offset" section="Action Usage and Control">
    /// Additional buffer in yalms where ranged attacks will be used for melee classes. For example,
    /// if you are playing as Samurai and this setting is set to `1`, Enpi will only be used starting
    /// at 4 yalms.
    ///
    /// This is because the default "overflow" for the melee range is 3 yalms, which means you can cast
    /// melee attacks 3 yalms outside the enemy's hitbox. This setting (the offset) takes max melee range and
    /// adds the value you set.
    ///
    /// This setting exists to leave you time to approach and enter in melee range of the enemy without wasting
    /// a GCD.
    /// </markdown>
    [UI("Melee Range action using offset",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Melee, PvPFilter = JobFilterType.Melee)]
    [Range(0, 5, ConfigUnitType.Yalms, 0.02f)]
    public float MeleeRangeOffset { get; set; } = 1;

    [UI("When their minimum HP is lower than this.", Parent = nameof(HealWhenNothingTodo))]
    [Range(0, 1, ConfigUnitType.Percent, 0.002f)]
    public float HealWhenNothingTodoBelow { get; set; } = 0.8f;

    [UI("Heal tank first if their HP is lower than this.",
        Filter = HealingActionCondition, Section = 1)]
    [Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    public float HealthTankRatio { get; set; } = 0.45f;

    [UI("Heal healer first if their HP is lower than this.",
        Filter = HealingActionCondition, Section = 1)]
    [Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    public float HealthHealerRatio { get; set; } = 0.4f;

    [UI("Heal self first if your HP is lower than this.",
        Filter = HealingActionCondition, Section = 1)]
    [Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    public float HealthSelfRatio { get; set; } = 0.4f;

    #region
    [JobConfig, UI("Prioritize raising dead players over Healing/Defense.",
        Filter = HealingActionCondition, Section = 2)]
    private static readonly bool _raisePlayerFirst = false;

    [JobConfig, UI("Raise player by using Swiftcast/Dualcast if available", Description = "If this is disabled, you will never use Swiftcast/Dualcast to raise players.",
        Filter = HealingActionCondition, Section = 2)]
    private static readonly bool _raisePlayerBySwift = true;

    [JobConfig, UI("Hard cast Raise logic",
        Filter = HealingActionCondition, Section = 2)]
    private readonly HardCastRaiseType _HardCastRaiseType = HardCastRaiseType.HardCastNormal;

    [JobConfig, UI("Raise styles",
        Filter = HealingActionCondition, Section = 2)]
    private readonly RaiseType _RaiseType = RaiseType.PartyOnly;

    [JobConfig, UI("Raise players that have the Brink of Death debuff",
        Filter = HealingActionCondition, Section = 2)]
    private static readonly bool _raiseBrinkOfDeath = true;

    [JobConfig, UI("Raise non-Healers from bottom of party list to the top (Light Party 2 Healer Behavior, Experimental)",
        Filter = HealingActionCondition, Section = 2)]
    private static readonly bool _h2 = false;

    [JobConfig, UI("Raise Red Mage and Summoners first if no Tanks or Healers are dead (Experimental)",
        Filter = HealingActionCondition, Section = 2)]
    private static readonly bool _offRaiserRaise = false;

    #endregion

    /// <markdown file="Auto" name="How early before next GCD should RSR use swiftcast for raise" section="Healing Usage and Control">
    /// If your cast a GCD and your cooldown is of 2.5 seconds, if a teammate dies when your cooldown starts, the Swiftcast action will wait
    /// the specified amount of time before your cooldown ends to cast Swiftcast. This is to prevent using your Swiftcast too early and waste it
    /// if your co-healer manages to raise your target within your global cooldown period.
    /// </markdown>
    [JobConfig, UI("How early before next GCD should RSR use swiftcast for raise (Experimental)",
        Filter = HealingActionCondition, Section = 2)]
    [Range(0, 1.0f, ConfigUnitType.Seconds, 0.01f)]
    public float SwiftcastBuffer { get; set; } = 0.6f;

    /// <markdown file="Auto" name="Random delay range for resurrecting players" section="Healing Usage and Control">
    /// In order to not make is so obvious that you use RSR, casting a raise action will be delayed by a random amount of seconds
    /// between the two values.
    /// </markdown>
    [UI("Random delay range for resurrecting players.",
        Filter = HealingActionCondition, Section = 2)]
    [Range(0, 10, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 RaiseDelay2 { get; set; } = new(3f, 3f);

    [UI("Random delay range for dispelling statuses.",
        Filter = HealingActionCondition, Section = 2)]
    [Range(0, 10, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 EsunaDelay { get; set; } = new(0f, 0f);

    [Range(0, 10000, ConfigUnitType.None, 100)]
    [UI("Never raise player if MP is less than this",
        Filter = HealingActionCondition, Section = 2)]
    public int LessMPNoRaise { get; set; } = 2400;

    /// <markdown file="Extra" name="HP standard deviation for using AoE heal">
    /// Controls how much party members' HP must differ before using AoE healing instead
    /// of single-target heals. Lower values require party members to have more similar HP
    /// for AoE healing to trigger (more selective). Higher values allow AoE healing even
    /// when HP differences are larger (less selective). Adjust only if you want to fine-tune
    /// AoE heal behavior.
    /// </markdown>
    [UI("HP standard deviation for using AoE heal.", Description = "Controls how much party members' HP must differ before using AoE healing instead of single-target heals. Lower values require party members to have more similar HP for AoE healing to trigger (more selective). Higher values allow AoE healing even when HP differences are larger (less selective). Adjust only if you want to fine-tune AoE heal behavior.",
    Filter = Extra)]
    [Range(0, 0.5f, ConfigUnitType.Percent, 0.02f)]
    public float HealthDifference { get; set; } = 0.25f;

    [ConditionBool, UI("Heal party members when not in combat.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _healOutOfCombat = false;

    [ConditionBool, UI("Heal solo instance NPCs (Only enable as needed)", Description = "Experimental.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _friendlyBattleNPCHeal = false;

    [ConditionBool, UI("Heal and raise Party NPCs.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _friendlyPartyNPCHealRaise3 = true;

    [ConditionBool, UI("Treat your chocobo as a party member",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _chocoboPartyMember = false;

    [ConditionBool, UI("Treat focus targeted player as party member in alliance raids", Description = "Experimental, includes Chaotic.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _focusTargetIsParty = false;

    [ConditionBool, UI("Heal party members with GCD if there is nothing to do in combat.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _healWhenNothingTodo = true;

    [ConditionBool, UI("Prioritize Low HP tank for tankbusters.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _priolowtank = false;

    /// <markdown file="Basic" name="The duration of special windows opened by /rotation commands by default">
    /// The duration of special windows opened by /rotation commands by default.
    /// (Found in Main => Macros)
    /// </markdown>
    [UI("The duration of special windows opened by /rotation commands by default.",
        Filter = BasicTimer, Section = 1)]
    [Range(1, 20, ConfigUnitType.Seconds, 1f)]
    public float SpecialDuration { get; set; } = 3;

    [UI("Random range of delay for RSR to stop attacking when the target is dead or immune to damage.",
        Parent = nameof(UseStopCasting))]
    [Range(0, 3, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 StopCastingDelay { get; set; } = new(0.5f, 1);

    [UI("The range of random delay before interrupting hostile targets.",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Interrupt, PvPFilter = JobFilterType.NoJob)]
    [Range(0, 3, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 InterruptDelay { get; set; } = new(0.5f, 1);

    [UI("Provoke random delay range.", Parent = nameof(AutoProvokeForTank))]
    [Range(0, 10, ConfigUnitType.Seconds, 0.05f)]
    public Vector2 ProvokeDelay { get; set; } = new(0.5f, 1);

    [UI("Not In Combat random delay range.",
        Filter = BasicParams)]
    [Range(0, 10, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 NotInCombatDelay { get; set; } = new(2, 3);

    /// <markdown file="Basic" name="Clicking actions random delay range">
    /// Delay between showing the clicking/pressing effect on the actions
    /// in your hotbars.
    /// </markdown>
    [UI("Clicking actions random delay range.",
        Filter = BasicTimer)]
    [Range(0.00f, 0.25f, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 ClickingDelay { get; set; } = new(0.1f, 0.2f);

    [UI("Downtime healing delay range.", Parent = nameof(HealWhenNothingTodo))]
    [Range(0, 5, ConfigUnitType.Seconds, 0.05f)]
    public Vector2 HealWhenNothingTodoDelay { get; set; } = new(0.5f, 1);

    /// <markdown file="Basic" name="How soon before countdown is finished to start casting or attacking">
    /// How soon before countdown is finished to start casting or attacking.
    /// </markdown>
    [UI("How soon before countdown is finished to start casting or attacking.",
        Filter = BasicTimer, Section = 1, PvPFilter = JobFilterType.NoJob)]
    [Range(0, 0.7f, ConfigUnitType.Seconds, 0.002f)]
    public float CountDownAhead { get; set; } = 0.4f;

    [UI("Cooldown window icon size")]
    [Range(0, 80, ConfigUnitType.Pixels, 0.2f)]
    public float CooldownWindowIconSize { get; set; } = 30;

    [UI("Next Action Size Ratio", Parent = nameof(ShowControlWindow))]
    [Range(0, 10, ConfigUnitType.Percent, 0.02f)]
    public float ControlWindowNextSizeRatio { get; set; } = 1.5f;

    [UI("GCD icon size", Parent = nameof(ShowControlWindow))]
    [Range(0, 80, ConfigUnitType.Pixels, 0.2f)]
    public float ControlWindowGCDSize { get; set; } = 40;

    [UI("oGCD icon size", Parent = nameof(ShowControlWindow))]
    [Range(0, 80, ConfigUnitType.Pixels, 0.2f)]
    public float ControlWindow0GCDSize { get; set; } = 30;

    [UI("Control Progress Height")]
    [Range(2, 30, ConfigUnitType.Yalms)]
    public float ControlProgressHeight { get; set; } = 8;

    [UI("Stop healing when time to kill is lower than:", Parent = nameof(UseHealWhenNotAHealer))]
    [Range(0, 30, ConfigUnitType.Seconds, 0.02f)]
    public float AutoHealTimeToKill { get; set; } = 8f;

    [UI("The minimum time between updating RSR information. (Raising this will help with framerate issues but can cause issues with rotation performance)",
    Filter = BasicTimer)]
    [JobConfig, Range(0, 0.3f, ConfigUnitType.Seconds, 0.002f)]
    public float MinUpdatingTime { get; set; } = 0.00f;

    /// <markdown file="Basic" name="Action Ahead">
    /// Percent of your GCD time remaining on a GCD cycle before RSR will try to queue the next GCD.
    ///
    /// This setting controls how many oGCDs RSR will try to fit in a single GCD window.
    /// Lower numbers mean more oGCDs, but potentially more GCD clipping.
    /// </markdown>
    [JobConfig, Range(0.05f, 0.25f, ConfigUnitType.Percent)]
    [UI("Action Ahead (Percent of your GCD time remaining on a GCD cycle before RSR will try to queue the next GCD)", Filter = BasicTimer,
    Description = "This setting controls how many oGCDs RSR will try to fit in a single GCD window\nLower numbers mean more oGCDs, but potentially more GCD clipping")]
    private readonly float _action6head = 0.25f;

    /// <summary>
    /// Remove extra lag-induced animation lock delay from instant casts (read tooltip!)
    /// Do NOT use with XivAlexander or NoClippy - this should automatically disable itself if they are detected, but double check first!
    /// </summary>
    [ConditionBool, UI("Remove extra lag-induced animation lock delay from instant casts (read tooltip!)", 
    Description = "Do NOT use with XivAlexander, BMR tweaks enabled, or NoClippy - this should automatically disable itself if they are detected, but double check first!",
    Filter = Extra)]
    private static readonly bool _removeAnimationLockDelay = false;

    /// <summary>
    /// Animation lock max. simulated delay in milliseconds
    /// Configures the maximum simulated delay in milliseconds when using animation lock removal - this is required and cannot be reduced to zero.
    /// Setting this to 20ms will enable triple-weaving when using autorotation. The minimum setting to remove triple-weaving is 26ms.
    /// The minimum of 20ms has been accepted by FFLogs and should not cause issues with your logs.
    /// </summary>
    [UI("Animation lock max. simulated delay (read tooltip!)", 
    Description = "Configures the maximum simulated delay in milliseconds when using animation lock removal - this is required and cannot be reduced to zero. Setting this to 20ms will enable triple-weaving when using autorotation. The minimum setting to remove triple-weaving is 26ms. The minimum of 20ms has been accepted by FFLogs and should not cause issues with your logs.",
    Parent = nameof(RemoveAnimationLockDelay), Filter = Extra)]
    [Range(20, 50, ConfigUnitType.None, 1f)]
    public int AnimationLockDelayMax2 { get; set; } = 26;

    [UI("Animation lock delay smoothing factor", Parent = nameof(RemoveAnimationLockDelay), Filter = Extra)]
    [Range(0.3f, 0.95f, ConfigUnitType.None, 0.01f)]
    public float AnimLockDelaySmoothing { get; set; } = 0.3f;

    /// <summary>
    /// Remove extra framerate-induced cooldown delay
    /// Dynamically adjusts cooldown and animation locks to ensure queued actions resolve immediately regardless of framerate limitations
    /// </summary>
    [ConditionBool, UI("Remove extra framerate-induced cooldown delay", 
    Description = "Dynamically adjusts cooldown and animation locks to ensure queued actions resolve immediately regardless of framerate limitations",
    Filter = Extra)]
    private static readonly bool _removeCooldownDelay = false;

    [UI("Max cooldown adjustment (ms)", Parent = nameof(RemoveCooldownDelay), Filter = Extra)]
    [Range(10, 150, ConfigUnitType.None, 1f)]
    public int CooldownAdjustMaxMs { get; set; } = 100;

    // Cactbot timeline integration
    [ConditionBool, UI("Enable cactbot timeline integration (Extremely experimental)",
        Description = "Connects to OverlayPlugin's WebSocket server and reacts to cactbot broadcast messages (raidwide, tankbuster, knockback, downtime/untargetable).",
        Filter = Extra)]
    private static readonly bool _enableCactbotTimeline = false;

    [ConditionBool, UI("Show cactbot event toasts (debug)",
    Description = "Show a toast when a cactbot broadcast is received and mapped to a RotationSolver special.",
    Filter = Extra)]
    private static readonly bool _showCactbotToasts = false;

    [JobConfig, UI("The HP for using Guard.",
        Filter = PvPSpecificControls)]
    [Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    public float HealthForGuard { get; set; } = 0.15f;

    [UI("Highlight color.", Parent = nameof(TeachingMode))]
    public Vector4 TeachingModeColor { get; set; } = new(0f, 1f, 0f, 1f);

    [UI("Target color", Parent = nameof(TargetColor))]
    public Vector4 TargetColor { get; set; } = new(1f, 0.2f, 0f, 0.8f);

    [UI("Locked Control Window's Background", Parent = nameof(ShowControlWindow))]
    public Vector4 ControlWindowLockBg { get; set; } = new(0, 0, 0, 0.55f);

    [UI("Unlocked Control Window's Background", Parent = nameof(ShowControlWindow))]
    public Vector4 ControlWindowUnlockBg { get; set; } = new(0, 0, 0, 0.75f);

    [UI("Info Window's Background", Filter = UiWindows)]
    public Vector4 InfoWindowBg { get; set; } = new(0, 0, 0, 0.4f);
    #endregion

    #region Target
    [ConditionBool, UI("Use movement actions towards the object/mob in the center of the screen",
    Description = "If enabled, movement actions target the object or mob at the center of your screen. If disabled, they target the object or mob your character is facing.",
    Filter = TargetConfig, Section = 2)]
    private static readonly bool _moveTowardsScreenCenter = false;

    [ConditionBool, UI("Prioritize mob/object targets with attack markers",
    Description = "Targets with attack markers will be prioritized for actions.",
    Filter = TargetConfig)]
    private static readonly bool _chooseAttackMark = true;

    [ConditionBool, UI("Prioritize enemy parts",
    Description = "Enemy parts, such as Titan's Heart, will be prioritized as targets.",
    Filter = TargetConfig)]
    private static readonly bool _prioEnemyParts = true;

    [ConditionBool, UI("Never attack targets with stop markers.",
    Description = "Targets with stop markers will not be attacked.",
    Filter = TargetConfig)]
    private static readonly bool _filterStopMark = true;

    [ConditionBool, UI("Treat 1hp targets as invincible.",
    Description = "Targets with only 1 HP will be treated as invincible and ignored; for rare cases where target is invincible but is not given a status for it.",
    Filter = TargetConfig)]
    private static readonly bool _filterOneHPInvincible = true;

    [ConditionBool, UI("Ignore Non-Fate targets while in a Fate and Fate targets if not in Fate.",
    Description = "When in a Fate, only Fate targets are considered. When not in a Fate, Fate targets are ignored.",
    Filter = TargetConfig)]
    private static readonly bool _ignoreNonFateInFate = true;

    [ConditionBool, UI("Move to the furthest position for targeting area movement actions.",
        Filter = TargetConfig, Section = 2)]
    private static readonly bool _moveAreaActionFarthest = false;

    [ConditionBool, UI("Hard Target enemies for hostile actions", Description = "If this is disabled, RSR will only soft-target allies for heals, shields, etc.",
        Filter = TargetConfig, Section = 3)]
    private static readonly bool _switchTargetFriendly = false;

    [ConditionBool, UI("Set target to closest targetable enemy if no valid action target nearby and target not set",
        Filter = TargetConfig, Section = 3)]
    private static readonly bool _targetFreely = false;

    [ConditionBool, UI("Only attack targets in view.",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _onlyAttackInView = false;

    [ConditionBool, UI("Only attack targets in vision cone",
                Filter = TargetConfig, Section = 1)]
    private static readonly bool _onlyAttackInVisionCone = false;

    [ConditionBool, UI("Target Hunt/Relic/Leve priority. (Relic behavior bugged)",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _targetHuntingRelicLevePriority = true;

    [ConditionBool, UI("Target quest priority (Overrides engage setting).",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _targetQuestPriority = true;

    [ConditionBool, UI("Block targeting quest mobs belonging to other players (Broken).",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool targetQuestThings2 = false;

    [ConditionBool, UI("Ignore all other FATE target when Forlorn available (Experimental).",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool forlornPriority = true;

    [ConditionBool, UI("Ignore target dummies",
               Filter = TargetConfig, Section = 1)]
    private static readonly bool _disableTargetDummys = false;

    [ConditionBool, UI("Target Fate priority",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _targetFatePriority = true;

    [ConditionBool, UI("Delay autotarget. (Experimental)",
        Filter = TargetConfig)]
    private static readonly bool _targetDelayEnable = false;

    [UI("Range of time before locking onto aggro'd or new target to attack", Description = "(Do not set too low, can rip newly aggro'd dungeon mobs off tanks).",
        Filter = TargetConfig, Parent = nameof(TargetDelayEnable))]
    [Range(0, 3, ConfigUnitType.Seconds)]
    public Vector2 TargetDelay { get; set; } = new(0f, 0f);

    [UI("The size of the sector angle that can be selected as the moveable target",
        Description = "If the selection mode is based on character facing, i.e., targets within the character's viewpoint are moveable targets.\nIf the selection mode is screen-centered, i.e., targets within a sector drawn upward from the character's point are movable targets.",
        Filter = TargetConfig, Section = 2)]
    [Range(0, 90, ConfigUnitType.Degree, 0.02f)]
    public float MoveTargetAngle { get; set; } = 24;

    [ConditionBool, UI("Treat target dummy as a boss.",
        Filter = TargetConfig, Section = 3)]
    private static readonly bool _dummyBoss = true;

    [UI("If target's TTK is higher than this, regard it as boss.",
        Filter = TargetConfig, Section = 1)]
    [Range(10, 1800, ConfigUnitType.Seconds, 0.02f)]
    public float BossTimeToKill { get; set; } = 90;

    [UI("If target's TTK is lower than this, regard it as dying.",
                Filter = TargetConfig, Section = 1)]
    [Range(0, 60, ConfigUnitType.Seconds, 0.02f)]
    public float DyingTimeToKill { get; set; } = 10;

    [UI("If target's HP percentage is lower than this, regard it as dying.",
                Filter = TargetConfig, Section = 1)]
    [Range(0, 0.1f, ConfigUnitType.Percent, 0.01f)]
    public float IsDyingConfig { get; set; } = 0.02f;

    [ConditionBool, UI("Prioritize Low HP targets instead of High HP targets when using Small Target and multiple Small targets present.",
        Filter = TargetConfig)]
    private static readonly bool _smallHP = false;

    [ConditionBool, UI("Prioritize Low HP targets instead of High HP targets when using Big Target and multiple Big targets present.",
        Filter = TargetConfig)]
    private static readonly bool _bigHP = false;

    [UI("/rotation Cycle behaviour", Filter = TargetConfig)]
    public CycleType CycleType { get; set; } = CycleType.CycleNormal;

    [JobConfig, UI("Engage settings", Filter = TargetConfig, PvPFilter = JobFilterType.NoJob)]
    private readonly TargetHostileType _hostileType = TargetHostileType.AllTargetsWhenSoloInDuty;
    #endregion

    #region Integer

    public int ActionSequencerIndex { get; set; }

    [UI("The modifier key to unlock the movement temporarily", Description = "RB is for gamepad player", Parent = nameof(PoslockCasting))]
    public ConsoleModifiers PoslockModifier { get; set; }

    [Range(0, 5, ConfigUnitType.None, 1)]
    [UI("Random range of simulated presses per action", Parent = nameof(KeyBoardNoise))]
    public Vector2Int KeyboardNoise { get; set; } = new(2, 3);

    [Range(0, 10, ConfigUnitType.None)]
    public int TargetingIndex { get; set; }

    [UI("Number of hostiles", Parent = nameof(UseDefenseAbility),
        PvEFilter = JobFilterType.Tank)]
    [Range(1, 8, ConfigUnitType.None, 0.05f)]
    public int AutoDefenseNumber { get; set; } = 2;

    #endregion

    #region PvP
    [ConditionBool, UI("Ignore TTK for PvP purposes.", Filter = PvPSpecificControls)]
    private static readonly bool _ignorePvPTTK = true;

    [ConditionBool, UI("Prioritize A tier tomeliths in Shatter.", Filter = PvPSpecificControls)]
    private static readonly bool _prioAtomelith = false;

    [ConditionBool, UI("Prioritize B tier tomeliths in Shatter.", Filter = PvPSpecificControls)]
    private static readonly bool _prioBtomelith = false;

    [JobConfig, UI("Ignore Invincibility for PvP purposes.", Filter = PvPSpecificControls)]
    private static readonly bool _ignorePvPInvincibility = false;

    [ConditionBool, UI("Auto turn off when dead in PvP.",
         Filter = PvPSpecificControls)]
    private static readonly bool _autoOffWhenDeadPvP = true;

    [ConditionBool, UI("Auto turn off when PvP match ends.",
         Filter = PvPSpecificControls)]
    private static readonly bool _autoOffPvPMatchEnd = true;

    [ConditionBool, UI("Auto turn on when PvP match starts.",
         Filter = PvPSpecificControls)]
    private static readonly bool _autoOnPvPMatchStart = true;

    [ConditionBool, UI("Set RSR to PvP specific state when enabled in PvP zone.",
         Filter = PvPSpecificControls)]
    private static readonly bool _pvpStateControl = false;

    #endregion

    #region Jobs

    [JobConfig, UI("MP threshold under which to use Lucid Dreaming", Filter = HealingActionCondition)]
    [Range(0, 10000, ConfigUnitType.None)]
    private readonly int _lucidDreamingMpThreshold = 6000;

    /// <markdown file="Auto" name="HP threshold for AoE healing oGCDs (Heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **oGCD** AoE healing abilities **when they have a heal-over-time already applied to them, by you or a teammate**.
    /// 
    /// This is calculated with the <see cref="RotationSolver.Basic.Configuration.Configs.HealthDifference">standard AoE healing deviation</see>.
    ///
    /// Use the button this setting to calculate your preferred values.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaAbilityHot = 0.55f;

    /// <markdown file="Auto" name="HP threshold for AoE healing GCDs (Heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **GCD** AoE healing abilities **when they have a heal-over-time already applied to them, by you or a teammate**.
    /// 
    /// This is calculated with the <see cref="RotationSolver.Basic.Configuration.Configs.HealthDifference">standard AoE healing deviation</see>.
    ///
    /// Use the button this setting to calculate your preferred values.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaSpellHot = 0.55f;

    /// <markdown file="Auto" name="HP threshold for AoE healing GCDs (No heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **oGCD** AoE healing abilities.
    /// 
    /// This is calculated with the <see cref="RotationSolver.Basic.Configuration.Configs.HealthDifference">standard AoE healing deviation</see>.
    ///
    /// Use the button this setting to calculate your preferred values.
    /// </markdown> 
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaAbility = 0.75f;

    /// <markdown file="Auto" name="HP threshold for AoE healing GCDs (No heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **GCD** AoE healing abilities.
    /// 
    /// This is calculated with the <see cref="RotationSolver.Basic.Configuration.Configs.HealthDifference">standard AoE healing deviation</see>.
    ///
    /// Use the button this setting to calculate your preferred values.
    /// </markdown> 
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaSpell = 0.65f;

    /// <markdown file="Auto" name="HP threshold for single-target healing oGCDs (Heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **oGCD** single-target healing abilities **when they have a heal-over-time already applied to them, by you or a teammate**.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleAbilityHot = 0.65f;

    /// <markdown file="Auto" name="HP threshold for single-target healing GCDs (Heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **GCD** single-target healing abilities **when they have a heal-over-time already applied to them, by you or a teammate**.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleSpellHot = 0.55f;

    /// <markdown file="Auto" name="HP threshold for single-target healing oGCDs (No Heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **oGCD** AoE healing abilities.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleAbility = 0.7f;

    /// <markdown file="Auto" name="HP threshold for single-target healing GCDs (No Heal-over-time)" section="Healing Usage and Control" subsection="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Relates to **GCD** single-target healing abilities.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleSpell = 0.65f;

    /// <markdown file="Auto" name="The HP% for tank to use invulnerability" section="Action Usage and Control">
    /// The threshold to automatically use your tank invulnerability action when falling below the HP percentage set.
    /// </markdown>
    [JobConfig, Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    [UI("The HP%% for tank to use invulnerability",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Tank, PvPFilter = JobFilterType.NoJob)]
    private readonly float _healthForDyingTanks = 0.15f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    [UI("HP%% needed to use single/self targetted mitigation on Tanks", Parent = nameof(UseDefenseAbility),
        PvEFilter = JobFilterType.Tank)]
    private readonly float _healthForAutoDefense = 1;

    [JobConfig]
    private readonly string _PvPRotationChoice = string.Empty;

    [JobConfig]
    private readonly string _rotationChoice = string.Empty;
    #endregion

    [JobConfig]
    private readonly ConcurrentDictionary<uint, ActionConfig> _rotationActionConfig = new();

    [JobConfig]
    private readonly ConcurrentDictionary<uint, ItemConfig> _rotationItemConfig = new();

    [JobChoiceConfig]
    private readonly ConcurrentDictionary<string, string> _rotationConfigurations = new();

    public ConcurrentDictionary<uint, string> DutyRotationChoice = new();

    public void Save()
    {
#if DEBUG
        PluginLog.Information("Saved configurations.");
#endif
        File.WriteAllText(Svc.PluginInterface.ConfigFile.FullName,
            JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public static Configs Migrate(Configs oldConfigs)
    {
        // Implement migration logic if needed
        if (oldConfigs.Version != CurrentVersion)
        {
            // Reset to default if versions do not match
            return new Configs();
        }
        return oldConfigs;
    }

    public void Backup()
    {
        Save();
        File.Copy(Svc.PluginInterface.ConfigFile.FullName, Svc.PluginInterface.ConfigFile.Directory + "\\RotationSolver_Backup.json", true);
        Svc.Toasts.ShowNormal("Configs backed up.");
    }

    public void Restore()
    {
        File.Copy(Svc.PluginInterface.ConfigFile.FullName, Svc.PluginInterface.ConfigFile.Directory + "\\RotationSolver_SafetySave.json", true);
        File.Copy(Svc.PluginInterface.ConfigFile.Directory + "\\RotationSolver_Backup.json", Svc.PluginInterface.ConfigFile.FullName, true);

        Configs restoredConfigs = JsonConvert.DeserializeObject<Configs>(
                                      File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName))
                                  ?? new Configs();

        if (restoredConfigs.Version != CurrentVersion)
        {
            Svc.Toasts.ShowNormal("Backed up configs are not compatible with the current version.");
            return;
        }

        Service.Config = restoredConfigs;
        Save();
        Svc.Toasts.ShowNormal("Configs restored. Closing to set.");
        DataCenter.HoldingRestore = true;
    }
}
