namespace RotationSolver.Basic.Data;

/// <summary>
/// The type of targeting.
/// </summary>
public enum TargetingType
{
    /// <summary>
    /// Find the target whose hit box is biggest.
    /// </summary>
    [Description("Big")]
    Big,

    /// <summary>
    /// Find the target whose hit box is smallest.
    /// </summary>
    [Description("Small")]
    Small,

    /// <summary>
    /// Find the target whose HP is highest.
    /// </summary>
    [Description("High HP")]
    HighHP,

    /// <summary>
    /// Find the target whose HP is lowest.
    /// </summary>
    [Description("Low HP")]
    LowHP,

    /// <summary>
    /// Find the target whose HP percentage is highest.
    /// </summary>
    [Description("High HP%")]
    HighHPPercent,

    /// <summary>
    /// Find the target whose HP percentage is lowest.
    /// </summary>
    [Description("Low HP%")]
    LowHPPercent,

    /// <summary>
    /// Find the target whose max HP is highest.
    /// </summary>
    [Description("High Max HP")]
    HighMaxHP,

    /// <summary>
    /// Find the target whose max HP is lowest.
    /// </summary>
    [Description("Low Max HP")]
    LowMaxHP,

    /// <summary>
    /// Find the target that is nearest.
    /// </summary>
    [Description("Nearest")]
    Nearest,

    /// <summary>
    /// Find the target that is farthest.
    /// </summary>
    [Description("Farthest")]
    Farthest,

    /// <summary>
    /// PVP: Find the nearest Healer.
    /// </summary>
    [Description("Focus Healers in PvP")]
    PvPHealers,

    /// <summary>
    /// PVP: Find the nearest Tank.
    /// </summary>
    [Description("Focus Tanks in PvP")]
    PvPTanks,

    /// <summary>
    /// PVP: Find the nearest DPS.
    /// </summary>
    [Description("Focus DPS in PvP")]
    PvPDPS
}