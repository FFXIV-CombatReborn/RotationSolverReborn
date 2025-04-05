using Dalamud.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
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
        TargetConfig = "TargetConfig",
        Extra = "Extra",
        Rotations = "Rotations",
        List = "List",
        List2 = "List2",
        List3 = "List3",
        Debug = "Debug";

    public const int CurrentVersion = 12;
    public int Version { get; set; } = CurrentVersion;
    public bool HasShownMainMenuMessage { get; set; } = false;

    public string LastSeenChangelog { get; set; } = "0.0.0.0";
    public bool FirstTimeSetupDone { get; set; } = false;

    public List<ActionEventInfo> Events { get; private set; } = [];
    public SortedSet<Job> DisabledJobs { get; private set; } = [];

    public string[] RotationLibs { get; set; } = [];
    public List<TargetingType> TargetingTypes { get; set; } = [];

    public MacroInfo DutyStart { get; set; } = new MacroInfo();
    public MacroInfo DutyEnd { get; set; } = new MacroInfo();

    [UI("What kind of AoE moves to use.", Description = "Full - Use all AoE actions\nCleave - Use only single target AoE actions\nOff - Use no AoE at all", Filter = AutoActionUsage, Section = 3)]
    public AoEType AoEType { get; set; } = AoEType.Full;

    [ConditionBool, UI("Disable automatically during area transitions.",
        Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffBetweenArea = true;

    [ConditionBool, UI("Disable automatically during cutscenes.",
        Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffCutScene = true;

    [ConditionBool, UI("Auto turn off when switching jobs",
               Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffSwitchClass = true;

    [ConditionBool, UI("Auto turn off when dead.",
        Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffWhenDead = true;

    [ConditionBool, UI("Auto turn off when duty is completed.",
        Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffWhenDutyCompleted = true;

    [ConditionBool, UI("Use movement actions towards the object/mob in the center of the screen",
        Description = "Use movement actions towards the object/mob in the center of the screen, otherwise toward object/mob your character is facing.",
        Filter = TargetConfig, Section = 2)]
    private static readonly bool _moveTowardsScreenCenter = false;

    [ConditionBool, UI("Audio notification when status changes",
        Filter = UiInformation)]
    private static readonly bool _sayOutStateChanged = false;

    [ConditionBool, UI("Enable changelog window popup on update",
        Filter = UiInformation)]
    private static readonly bool _changelogPopup = true;

    [ConditionBool, UI("Show plugin status in server info bar.",
        Filter = UiInformation)]
    private static readonly bool _showInfoOnDtr = true;

    [ConditionBool, UI("Display plugin status in toast popup",
        Filter = UiInformation)]
    private static readonly bool _showInfoOnToast = false;

    [ConditionBool, UI("Lock movement when casting or performing certain actions.", Filter = Extra)]
    private static readonly bool _poslockCasting = false;

    [UI("", Action = ActionID.PassageOfArmsPvE, Parent = nameof(PoslockCasting))]
    public bool PosPassageOfArms { get; set; } = false;

    [UI("", Action = ActionID.FlamethrowerPvE, Parent = nameof(PoslockCasting))]
    public bool PosFlameThrower { get; set; } = false;

    [UI("", Action = ActionID.ImprovisationPvE, Parent = nameof(PoslockCasting))]
    public bool PosImprovisation { get; set; } = false;

    [JobConfig, UI("Only used automatically if coded into the rotation", Filter = AutoActionUsage, PvPFilter = JobFilterType.NoJob)]
    private readonly TinctureUseType _TinctureType = TinctureUseType.Nowhere;

    [ConditionBool, UI("Automatically use Anti-Knockback role actions (Arms Length, Surecast)", Filter = AutoActionUsage)]
    private static readonly bool _useKnockback = true;

    [ConditionBool, UI("Automatically use HP Potions", Description = "Experimental.",
        Filter = AutoActionUsage)]
    private static readonly bool _useHpPotions = false;

    [ConditionBool, UI("Automatically use MP Potions", Description = "Experimental.",
        Filter = AutoActionUsage)]
    private static readonly bool _useMpPotions = false;

    [ConditionBool, UI("Prioritize mob/object targets with attack markers",
        Filter = TargetConfig)]
    private static readonly bool _chooseAttackMark = true;

    [ConditionBool, UI("Prioritize enemy parts (i.e. Titan's Heart)",
        Filter = TargetConfig)]
    private static readonly bool _prioEnemyParts = true;

    [ConditionBool, UI("Allow the use of AOEs against priority-marked targets.",
        Parent = nameof(ChooseAttackMark))]
    private static readonly bool _canAttackMarkAOE = true;

    [ConditionBool, UI("Never attack targets with stop markers.",
        Filter = TargetConfig)]
    private static readonly bool _filterStopMark = true;

    [ConditionBool, UI("Treat 1hp targets as invincible.",
        Filter = TargetConfig)]
    private static readonly bool _filterOneHPInvincible = true;

    [ConditionBool, UI("Ignore immune Ark Angels in Jenuo: The First Walk.",
        Filter = TargetConfig)]
    private static readonly bool _jeunoTarget = true;

    [ConditionBool, UI("Ignore immune targets in Cloud of Darkness Chaotic.",
        Filter = TargetConfig)]
    private static readonly bool _cODTarget = true;

    [ConditionBool, UI("Ignore Strong of Shield target (Hansel and Gretel) in The Tower at Paradigm's Breach if you will hit shield.",
        Filter = TargetConfig)]
    private static readonly bool _strongOfSheildTarget = true;

    [ConditionBool, UI("Teaching mode", Filter = UiInformation)]
    private static readonly bool _teachingMode = false;

    [ConditionBool, UI("Simulate the effect of pressing abilities",
        Filter = UiInformation)]
    private static readonly bool _keyBoardNoise = true;

    [ConditionBool, UI("Move to the furthest position for targeting area movement actions.",
        Filter = TargetConfig, Section = 2)]
    private static readonly bool _moveAreaActionFarthest = false;

    [ConditionBool, UI("Activate auto mode when countdown starts",
        Filter = BasicAutoSwitch, Section = 1)]
    private static readonly bool _startOnCountdown = true;

    [ConditionBool, UI("Start manual mode instead of auto mode when countdown starts",
               Parent = nameof(StartOnCountdown))]
    private static readonly bool _countdownStartsManualMode = false;

    [ConditionBool, UI("Cancel auto mode if combat starts early during countdown",
        Filter = BasicAutoSwitch, Section = 1)]
    private static readonly bool _cancelStateOnCombatBeforeCountdown = false;

    [ConditionBool, UI("Auto turn on manual mode when attacked.",
        Filter = BasicAutoSwitch, Section = 1)]
    private static readonly bool _startOnAttackedBySomeone = false;

    [ConditionBool, UI("Don't attack new mobs by AoE. (Dangerous)", Description = "Never use any AoE action when this may attack mobs that are not hostile targets.",
        Filter = BasicAutoSwitch)]
    private static readonly bool _noNewHostiles = false;

    [ConditionBool, UI("Use healing abilities when playing a non-healer role.",
        Filter = HealingActionCondition, Section = 1,
        PvEFilter = JobFilterType.NoHealer, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _useHealWhenNotAHealer = true;

    [ConditionBool, UI("Hard Target enemies for hostile actions", Description = "If this is disabled, RSR will only soft-target allies for heals, shields, etc.",
        Filter = TargetConfig, Section = 3)]
    private static readonly bool _switchTargetFriendly = false;

    [JobConfig, UI("Use interrupt abilities if possible.",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Interrupt,
        PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _interruptibleMoreCheck = true;

    [UI("Framework Update Method (Experimental: Changing this off of game thread will cause crashes)",
        Filter = BasicParams)]
    public FrameworkStyle FrameworkStyle { get; set; } = FrameworkStyle.MainThread;

    [ConditionBool, UI("Stop casting if the target dies.", Filter = Extra)]
    private static readonly bool _useStopCasting = false;

    [ConditionBool, UI("Cleanse all dispellable debuffs (not just those in the status list).",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Dispel, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _dispelAll = false;

    [ConditionBool, UI("Only attack targets in view.",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _onlyAttackInView = false;

    [ConditionBool, UI("Only attack targets in vision cone",
                Filter = TargetConfig, Section = 1)]
    private static readonly bool _onlyAttackInVisionCone = false;

    [ConditionBool, UI("Debug Mode", Filter = Debug)]
    private static readonly bool _inDebug = false;

    [ConditionBool, UI("Load rotations automatically at startup", Filter = Rotations)]
    private static readonly bool _loadRotationsAtStartup = true;

    [ConditionBool, UI("Load default rotations", Description = "Load the rotations provided by the Combat Reborn team", Filter = Rotations)]
    private static readonly bool _loadDefaultRotations = true;

    [ConditionBool, UI("Download custom rotations from the internet",
               Description = "This will allow RSR to download custom rotations from the internet. This is a security risk and should only be enabled if you trust the source of the rotations.",
               Filter = Rotations)]
    private static readonly bool _downloadCustomRotations = true;

    [ConditionBool, UI("Monitor local rotations for changes (Developer Mode)",
               Filter = Rotations)]
    private static readonly bool _autoReloadRotations = false;

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

    [ConditionBool, UI("Target Hunt/Relic/Leve priority. (Relic behavior bugged)",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _targetHuntingRelicLevePriority = true;

    [ConditionBool, UI("Target quest priority.",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _targetQuestPriority = true;

    [ConditionBool, UI("Block targeting quest mobs belonging to other players (Broken).",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool targetQuestThings = true;

    [ConditionBool, UI("Ignore target dummies",
               Filter = TargetConfig, Section = 1)]
    private static readonly bool _disableTargetDummys = false;

    [ConditionBool, UI("Display do action feedback on toast",
        Filter = UiInformation)]
    private static readonly bool _showToastsAboutDoAction = false;

    [ConditionBool, UI("Allow rotations that use this config to use abilities defined in the rotation as burst", Filter = AutoActionUsage, Section = 4)]
    private static readonly bool _autoBurst = true;

    [ConditionBool, UI("Disable hostile actions if something is casting an action on the Gaze/Stop list (EXPERIMENTAL)", Filter = AutoActionUsage, Section = 4)]
    private static readonly bool _castingStop = false;

    [UI("Configurable amount of time before the cast finishes that RSR stops taking actions", Filter = AutoActionUsage, Section = 4, Parent = nameof(CastingStop))]
    [Range(0, 15, ConfigUnitType.Seconds)]
    public float CastingStopTime { get; set; } = 2.5f;

    [ConditionBool, UI("Disable for the entire duration (Enabling this will prevent your actions for the entire cast.)", Filter = AutoActionUsage, Section = 4, Parent = nameof(CastingStop))]
    private static readonly bool _castingStopCalculate = false;

    [ConditionBool, UI("Automatic Healing Thresholds", Filter = HealingActionCondition, Section = 1, Order = 1)]
    private static readonly bool _autoHeal = true;

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

    [ConditionBool, UI("Auto True North (Melee DPS)",
        Parent = nameof(UseAbility),
        PvEFilter = JobFilterType.Melee)]
    private static readonly bool _autoUseTrueNorth = true;

    [ConditionBool, UI("Use movement speed increase abilities when out of combat.", Parent = nameof(UseAbility))]
    private static readonly bool _autoSpeedOutOfCombat = true;

    [ConditionBool, UI("Use beneficial ground-targeted actions", Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _useGroundBeneficialAbility = true;

    [ConditionBool, UI("Use beneficial AoE actions when moving.", Parent = nameof(UseGroundBeneficialAbility))]
    private static readonly bool _useGroundBeneficialAbilityWhenMoving = false;

    [ConditionBool, UI("Show Cooldown Window", Filter = UiWindows)]
    private static readonly bool _showCooldownWindow = false;

    [ConditionBool, UI("Record AOE actions", Filter = List)]
    private static readonly bool _recordCastingArea = true;

    [ConditionBool, UI("Target Fate priority",
        Filter = TargetConfig, Section = 1)]
    private static readonly bool _targetFatePriority = true;

    [ConditionBool, UI("Auto turn off RSR when combat is over for more than:",
        Filter = BasicAutoSwitch)]
    private static readonly bool _autoOffAfterCombat = true;

    [ConditionBool, UI("Auto Open treasure chests",
        Filter = Extra)]
    private static readonly bool _autoOpenChest = true;

    [ConditionBool, UI("Enable RSR click counter in main menu",
        Filter = Extra)]
    private static readonly bool _enableClickingCount = true;

    [ConditionBool, UI("Auto close the loot window when auto opened the chest.",
        Parent = nameof(AutoOpenChest))]
    private static readonly bool _autoCloseChestWindow = true;

    [ConditionBool, UI("Hide all warnings",
        Filter = UiInformation)]
    private static readonly bool _hideWarning = false;

    [ConditionBool, UI("Only heal self when not a Healer",
        Filter = HealingActionCondition, Section = 1,
        PvPFilter = JobFilterType.NoHealer, PvEFilter = JobFilterType.NoHealer)]
    private static readonly bool _onlyHealSelfWhenNoHealer = false;

    [ConditionBool, UI("Show toggled setting and new value in chat.",
        Filter = UiInformation)]
    private static readonly bool _ShowToggledSettingInChat = false;

    [ConditionBool, UI("Record knockback actions", Filter = List2)]
    private static readonly bool _recordKnockbackies = false;

    [UI("Use additional conditions", Filter = BasicParams)]
    public bool UseAdditionalConditions { get; set; } = false;

    [ConditionBool, UI("Set Blue Mage Actions Automatically", Description = "When using a Blue Mage Rotation, RSR can automatically set your spell book to the spells required by that rotation.", Filter = Extra)]
    private static readonly bool _setBluActions = true;

    #region Float
    [UI("Auto turn off RSR when combat is over for more than...",
        Parent = nameof(AutoOffAfterCombat))]
    [Range(0, 600, ConfigUnitType.Seconds)]
    public float AutoOffAfterCombatTime { get; set; } = 30;

    [UI("The angle of your vision cone", Parent = nameof(OnlyAttackInVisionCone))]
    [Range(0, 90, ConfigUnitType.Degree, 0.02f)]
    public float AngleOfVisionCone { get; set; } = 45;

    [UI("Melee Range action using offset",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Melee, PvPFilter = JobFilterType.NoJob)]
    [Range(0, 5, ConfigUnitType.Yalms, 0.02f)]
    public float MeleeRangeOffset { get; set; } = 1;

    [UI("When their minimum HP is lower than this.", Parent = nameof(HealWhenNothingTodo))]
    [Range(0, 1, ConfigUnitType.Percent, 0.002f)]
    public float HealWhenNothingTodoBelow { get; set; } = 0.8f;

    [UI("Heal tank first if their HP is lower than this.",
        Filter = HealingActionCondition, Section = 1,
        PvEFilter = JobFilterType.Healer, PvPFilter = JobFilterType.Healer)]
    [Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    public float HealthTankRatio { get; set; } = 0.45f;

    [UI("Heal healer first if their HP is lower than this.",
        Filter = HealingActionCondition, Section = 1,
        PvEFilter = JobFilterType.Healer, PvPFilter = JobFilterType.Healer)]
    [Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    public float HealthHealerRatio { get; set; } = 0.4f;

    [JobConfig, UI("Hard cast Raise on players while Swiftcast is on cooldown", Description = "If this is enabled and Swiftcast is on cooldown, you will only attempt to raise while standing still.",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _raisePlayerByCasting = true;

    [JobConfig, UI("Raise player by using Swiftcast/Dualcast if available", Description = "If this is disabled, you will never use Swiftcast/Dualcast to raise players.",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _raisePlayerBySwift = true;

    [JobConfig, UI("Prioritize raising dead players over Healing/Defense.",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _raisePlayerFirst = false;

    [JobConfig, UI("Raise styles",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private readonly RaiseType _RaiseType = RaiseType.PartyOnly;

    [JobConfig, UI("Raise players that have the Brink of Death debuff",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _raiseBrinkOfDeath = true;

    [JobConfig, UI("Raise non-Healers from bottom of party list to the top (Light Party 2 Healer Behavior, Experimental)",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _h2 = false;

    [JobConfig, UI("Raise Red Mage and Summoners first if no Tanks or Healers are dead (Experimental)",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    private static readonly bool _offRaiserRaise = false;

    [JobConfig, UI("How early before next GCD should RSR use swiftcast for raise (Experimental)",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    [Range(0, 1.0f, ConfigUnitType.Seconds, 0.01f)]
    public float SwiftcastBuffer { get; set; } = 0.6f;

    [UI("Random delay range for resurrecting players.",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise, PvPFilter = JobFilterType.NoJob)]
    [Range(0, 10, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 RaiseDelay { get; set; } = new(.25f, .75f);

    [Range(0, 10000, ConfigUnitType.None, 200)]
    [UI("Never raise player if MP is less than this",
        Filter = HealingActionCondition, Section = 2,
        PvEFilter = JobFilterType.Raise)]
    public int LessMPNoRaise { get; set; }

    [UI("HP standard deviation for using AoE heal.", Description = "The health difference between a single party member and the whole party, used for deciding between healing a single party member or AOE healing. Leave this alone if you don't understand its use.",
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

    [ConditionBool, UI("Heal/Dance partner your chocobo", Description = "Experimental.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _chocoboPartyMember = false;

    [ConditionBool, UI("Treat focus targeted player as party member in alliance raids", Description = "Experimental, includes Chaotic.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _focusTargetIsParty = false;

    [ConditionBool, UI("Heal party members with GCD if there is nothing to do in combat.",
        Filter = HealingActionCondition, Section = 3)]
    private static readonly bool _healWhenNothingTodo = true;

    [UI("The duration of special windows opened by /macro commands by default.",
        Filter = BasicTimer, Section = 1)]
    [Range(1, 20, ConfigUnitType.Seconds, 1f)]
    public float SpecialDuration { get; set; } = 3;

    [UI("Range of time before locking onto aggro'd or new target to attack", Description = "(Do not set too low, can rip newly aggro'd dungeon mobs off tanks).", Filter = TargetConfig)]
    [Range(0, 3, ConfigUnitType.Seconds)]
    public Vector2 TargetDelay { get; set; } = new(1, 2);

    [UI("Action Execution Delay.\n(RSR will not take actions during window).",
        Filter = BasicTimer)]
    [Range(0, 1, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 WeaponDelay { get; set; } = new(0, 0);

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
    public Vector2 NotInCombatDelay { get; set; } = new(3, 4);

    [UI("Clicking actions random delay range.",
        Filter = BasicTimer)]
    [Range(0.05f, 0.25f, ConfigUnitType.Seconds, 0.002f)]
    public Vector2 ClickingDelay { get; set; } = new(0.1f, 0.15f);

    [UI("Downtime healing delay range.", Parent = nameof(HealWhenNothingTodo))]
    [Range(0, 5, ConfigUnitType.Seconds, 0.05f)]
    public Vector2 HealWhenNothingTodoDelay { get; set; } = new(0.5f, 1);

    [UI("How soon before countdown is finished to start casting or attacking.",
        Filter = BasicTimer, Section = 1, PvPFilter = JobFilterType.NoJob)]
    [Range(0, 0.7f, ConfigUnitType.Seconds, 0.002f)]
    public float CountDownAhead { get; set; } = 0.4f;

    [UI("The size of the sector angle that can be selected as the moveable target",
        Description = "If the selection mode is based on character facing, i.e., targets within the character's viewpoint are moveable targets.\nIf the selection mode is screen-centered, i.e., targets within a sector drawn upward from the character's point are movable targets.",
        Filter = TargetConfig, Section = 2)]
    [Range(0, 90, ConfigUnitType.Degree, 0.02f)]
    public float MoveTargetAngle { get; set; } = 24;

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

    [UI("Use gapcloser as a damage ability if the distance to your target is less than this.",
        Filter = TargetConfig, Section = 2)]
    [Range(0, 30, ConfigUnitType.Yalms, 1f)]
    public float DistanceForMoving { get; set; } = 1.2f;

    [UI("Stop healing when time to kill is lower than:", Parent = nameof(UseHealWhenNotAHealer))]
    [Range(0, 30, ConfigUnitType.Seconds, 0.02f)]
    public float AutoHealTimeToKill { get; set; } = 8f;

    [UI("The minimum time between updating RSR information. (Setting too low will negatively affect framerate, setting too high will lead to poor performance)",
        Filter = BasicTimer)]
    [JobConfig, Range(0, 0.3f, ConfigUnitType.Seconds, 0.002f)]
    public float MinUpdatingTime { get; set; } = 0.01f;

    [UI("The HP for using Guard.",
        Filter = HealingActionCondition, Section = 3,
        PvEFilter = JobFilterType.NoJob)]
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

    #region Integer

    public int ActionSequencerIndex { get; set; }

    [UI("The modifier key to unlock the movement temporarily", Description = "RB is for gamepad player", Parent = nameof(PoslockCasting))]
    public ConsoleModifiers PoslockModifier { get; set; }

    [Range(0, 5, ConfigUnitType.None, 1)]
    [UI("Effect times", Parent = nameof(KeyBoardNoise))]
    public Vector2Int KeyboardNoise { get; set; } = new(2, 3);

    [Range(0, 10, ConfigUnitType.None)]
    public int TargetingIndex { get; set; }

    [UI("Beneficial AOE Logic", Parent = nameof(UseGroundBeneficialAbility))]
    public BeneficialAreaStrategy2 BeneficialAreaStrategy2 { get; set; } = BeneficialAreaStrategy2.OnCalculated;

    [UI("Number of hostiles", Parent = nameof(UseDefenseAbility),
        PvEFilter = JobFilterType.Tank)]
    [Range(1, 8, ConfigUnitType.None, 0.05f)]
    public int AutoDefenseNumber { get; set; } = 2;

    #endregion

    #region PvP
    [ConditionBool, UI("Ignore TTK for PvP purposes.", Filter = PvPSpecificControls)]
    private static readonly bool _ignorePvPTTK = true;
    
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

    #endregion

    #region Jobs

    [JobConfig, UI("MP threshold under which to use Lucid Dreaming", Filter = HealingActionCondition)]
    [Range(0, 10000, ConfigUnitType.None)]
    private readonly int _lucidDreamingMpThreshold = 6000;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaAbilityHot = 0.55f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaSpellHot = 0.55f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaAbility = 0.75f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthAreaSpell = 0.65f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleAbilityHot = 0.65f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleSpellHot = 0.45f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleAbility = 0.7f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent)]
    private readonly float _healthSingleSpell = 0.55f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    [UI("The HP%% for tank to use invulnerability",
        Filter = AutoActionUsage, Section = 3,
        PvEFilter = JobFilterType.Tank, PvPFilter = JobFilterType.NoJob)]
    private readonly float _healthForDyingTanks = 0.15f;

    [JobConfig, Range(0, 1, ConfigUnitType.Percent, 0.02f)]
    [UI("HP%% needed to use single/self targetted mitigation on Tanks", Parent = nameof(UseDefenseAbility),
        PvEFilter = JobFilterType.Tank)]
    private readonly float _healthForAutoDefense = 1;

    [JobConfig, UI("Engage settings", Filter = TargetConfig, PvPFilter = JobFilterType.NoJob)]
    private readonly TargetHostileType _hostileType = TargetHostileType.AllTargetsWhenSoloInDuty;

    [JobConfig, UI("Override Action Ahead Timer", Description = "If you don't know what this does, you don't need to modify it",
        Filter = BasicTimer)]
    private readonly bool _overrideActionAheadTimer = false;

    [JobConfig, Range(0, 1.0f, ConfigUnitType.Seconds)]
    [UI("Action Ahead (How far in advance of GCD being available RSR will try to queue the next GCD)",
    Description = "This setting controls how many oGCDs RSR will try to fit in a single GCD window\nLower numbers mean more oGCDs, but potentially more GCD clipping",
    Parent = nameof(OverrideActionAheadTimer))]
    private readonly float _action4head = 0.3f;

    [JobConfig]
    private readonly string _PvPRotationChoice = string.Empty;

    [JobConfig]
    private readonly string _rotationChoice = string.Empty;
    #endregion

    [JobConfig]
    private readonly ConcurrentDictionary<uint, ActionConfig> _rotationActionConfig = new ConcurrentDictionary<uint, ActionConfig>();

    [JobConfig]
    private readonly ConcurrentDictionary<uint, ItemConfig> _rotationItemConfig = new ConcurrentDictionary<uint, ItemConfig>();

    [JobChoiceConfig]
    private readonly ConcurrentDictionary<string, string> _rotationConfigurations = new ConcurrentDictionary<string, string>();

    public ConcurrentDictionary<uint, string> DutyRotationChoice = new ConcurrentDictionary<uint, string>();

    public void Save()
    {
#if DEBUG
        Svc.Log.Information("Saved configurations.");
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
}
