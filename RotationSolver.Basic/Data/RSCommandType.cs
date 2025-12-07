namespace RotationSolver.Basic.Data;

/// <summary>
/// Special State.
/// </summary>
public enum SpecialCommandType : byte
{
    /// <summary>
    /// To end this special duration before the set time.
    /// </summary>
    [Description("To end this special duration before the set time.")]
    EndSpecial,

    /// <summary>
    /// Open a window to use AoE heal.
    /// </summary>
    [Description("Open a window to use AoE heal.")]
    HealArea,

    /// <summary>
    /// Open a window to use single heal.
    /// </summary>
    [Description("Open a window to use single heal.")]
    HealSingle,

    /// <summary>
    /// Open a window to use AoE defense.
    /// </summary>
    [Description("Open a window to use AoE defense.")]
    DefenseArea,

    /// <summary>
    /// Open a window to use single defense.
    /// </summary>
    [Description("Open a window to use single defense.")]
    DefenseSingle,

    /// <summary>
    /// Open a window to use Esuna, tank stance actions or True North.
    /// </summary>
    [Description("Open a window to use Esuna, tank stance actions or True North.")]
    DispelStancePositional,

    /// <summary>
    /// Open a window to use Raise or Shirk.
    /// </summary>
    [Description("Open a window to use Raise or Shirk.")]
    RaiseShirk,

    /// <summary>
    /// Open a window to move forward.
    /// </summary>
    [Description("Open a window to move forward.")]
    MoveForward,

    /// <summary>
    /// Open a window to move back.
    /// </summary>
    [Description("Open a window to move back.")]
    MoveBack,

    /// <summary>
    /// Open a window to use knockback immunity actions.
    /// </summary>
    [Description("Open a window to use knockback immunity actions.")]
    AntiKnockback,

    /// <summary>
    /// Open a window to burst.
    /// </summary>
    [Description("Open a window to burst.")]
    Burst,

    /// <summary>
    /// Open a window to speed up.
    /// </summary>
    [Description("Open a window to speed up.")]
    Speed,

    /// <summary>
    /// Open a window to use limit break.
    /// </summary>
    [Description("Open a window to use limit break.")]
    LimitBreak,

    /// <summary>
    /// Open a window to do not use the casting action.
    /// </summary>
    [Description("Open a window to do not use the casting action.")]
    NoCasting,

    /// <summary>
    /// Intercepting action.
    /// </summary>
    [Description("Indicator for when RSR is intercepting action.")]
    Intercepting,
}

/// <summary>
/// The state of the plugin.
/// </summary>
public enum StateCommandType : byte
{
    /// <summary>
    /// Stop the addon. Always remember to turn it off when it is not in use!
    /// </summary>
    [Description("Stop the addon. Always remember to turn it off when it is not in use!")]
    Off,

    /// <summary>
    /// Start the addon in Auto mode. When out of combat or when combat starts, switches the target according to the set condition.
    /// </summary>
    [Description("Start the addon in Auto mode. When out of combat or when combat starts, switches the target according to the set condition. " +
        "\r\n Optionally: You can add the target type to the end of the command you want RSR to do. For example: /rotation Auto Big")]
    Auto,

    /// <summary>
    /// Start the addon in Target-Only mode. RSR will auto-select targets per normal logic but will not perform any actions.
    /// </summary>
    [Description("Start in Target-Only mode. RSR will auto-select targets per normal logic but will not perform any actions.")]
    TargetOnly,

    /// <summary>
    /// Start the addon in Manual mode. You need to choose the target manually. This will bypass any engage settings that you have set up and will start attacking immediately once something is targeted.
    /// </summary>
    [Description("Start the addon in Manual mode. You need to choose the target manually. This will bypass any engage settings that you have set up and will start attacking immediately once something is targeted.")]
    Manual,

    /// <summary>
    /// 
    /// </summary>
    [Description("This mode is managed by the Autoduty plugin")]
    AutoDuty,

    /// <summary>
    /// 
    /// </summary>
    [Description("This mode is managed by the Henchman plugin, or any other plugin that requires RSR just do rotation and not targetting.")]
    Henched,

    /// <summary>
    /// 
    /// </summary>
    [Description("Optional mode for PvP specific activities.")]
    PvP,
}

/// <summary>
/// Some Other Commands.
/// </summary>
public enum OtherCommandType : byte
{
    /// <summary>
    /// Open the settings.
    /// </summary>
    [Description("Open the settings.")]
    Settings,

    /// <summary>
    /// Open the rotations.
    /// </summary>
    [Description("Open the rotations.")]
    Rotations,

    /// <summary>
    /// Open the rotations.
    /// </summary>
    [Description("Open the duty rotations.")]
    DutyRotations,

    /// <summary>
    /// Perform the actions.
    /// </summary>
    [Description("Perform the actions.")]
    DoActions,

    /// <summary>
    /// Toggle the actions.
    /// </summary>
    [Description("Toggle the actions.")]
    ToggleActions,

    /// <summary>
    /// Do the next action.
    /// </summary>
    [Description("Do the next action.")]
    NextAction,

    /// <summary>
    /// Cycles between states following settings in Target > Configuration.
    /// </summary>
    [Description("Cycles between states following settings in Target > Configuration.")]
    Cycle,
}