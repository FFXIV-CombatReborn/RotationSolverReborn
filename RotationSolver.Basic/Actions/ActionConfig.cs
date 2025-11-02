namespace RotationSolver.Basic.Actions;

/// <summary>
/// User config.
/// </summary>
public class ActionConfig()
{
    private bool _isEnable = true;

    /// <summary>
    /// If this action is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => IBaseAction.ForceEnable || _isEnable;
        set => _isEnable = value;
    }

    private bool _isIntercepted = true;

    /// <summary>
    /// 
    /// </summary>
    public bool IsIntercepted
    {
        get => _isIntercepted;
        set => _isIntercepted = value;
    }

    /// <summary>
    /// Should check the status for this action.
    /// </summary>
    /// <markdown file="Actions" name="Should this action check status effects">
    /// This means that this actions provides a status to the target. If that target
    /// already has the status provided by the selected, RSR will not cast it.
    /// Some statuses can be stacked together, like damage over time actions,
    /// and RSR will take that into account and only check if you are its source.
    /// </markdown>
    public bool ShouldCheckStatus { get; set; } = true;

    /// <summary>
    /// Should check the target status for this action.
    /// </summary>
    public bool ShouldCheckTargetStatus { get; set; } = true;

    /// <summary>
    /// Should check the combo for this action.
    /// </summary>
    public bool ShouldCheckCombo { get; set; } = true;

    /// <summary>
    /// The status count in gcd for adding the status.
    /// </summary>
    /// <markdown file="Actions" name="Number of GCDs before the DoT is reapplied">
    /// The minimum amount of GCDs that you can cast before the damage-over-time
    /// action is re-applied to the target. If your cast time is of 2.5 seconds,
    /// setting this to "2" would re-apply the damage-over-time action within 5
    /// seconds before it falls off.
    /// </markdown>
    public byte StatusGcdCount { get; set; } = 2;

    /// <summary>
    /// Is this action a Single Target Healing GCD.
    /// </summary>
    public bool GCDSingleHeal { get; set; } = false;

    /// <summary>
    /// The aoe count of this action.
    /// </summary>
    /// <markdown file="Actions" name="Number of targets needed to use this action">
    /// Set the minimum number of targets that the specified action should hit
    /// for it to be considered in the rotation.
    /// </markdown>
    public byte AoeCount { get; set; } = 3;

    /// <summary>
    /// How many ttk should this action use.
    /// </summary>
    /// <markdown file="Actions" name="Time-to-kill threshold required for this action to be used">
    /// Minimum time-to-kill on the target for this action to be considered enabled in the rotation and used.
    /// </markdown>
    public int TimeToKill { get; set; } = 0;

    /// <summary>
    /// The heal ratio for the auto heal.
    /// </summary>
    /// <markdown file="Actions" name="HP ratio for automatic healing">
    /// The maximum HP percentage the target must be before at for this action
    /// to be considered in the rotation. If it is set to "80%", then it will
    /// not heal the target if they are at 81% HP. It will heal them at 80% and below.
    /// This setting works in conjunction with
    /// <see cref="RotationSolver.Basic.Configuration.Configs._autoHeal">
    /// Automatic Healing Thresholds </see>.
    /// </markdown>
    public float AutoHealRatio { get; set; } = 0.8f;

    /// <summary>
    /// Is this action in the cd window.
    /// </summary>
    public bool IsOnCooldownWindow { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    public bool MinHPFeature { get; set; } = false;

    /// <summary>
    /// 
    /// </summary>
    public float MinHPPercent { get; set; } = 0.2f;

    /// <summary>
    /// One-time flag to indicate the AOE-count reset has been applied.
    /// </summary>
    public bool AoeResetDone { get; set; } = false;
}