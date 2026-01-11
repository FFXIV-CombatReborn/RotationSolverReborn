using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Configuration.Conditions;

/// <summary>
/// Represents a condition based on the target.
/// </summary>
[Description("Target Condition")]
internal class TargetCondition : DelayCondition
{
    internal IBaseAction? _action;
    public ActionID ID { get; set; } = ActionID.None;

    public bool FromSelf;
    internal Status? Status;
    public StatusID StatusId { get; set; }
    public TargetType TargetType;
    public TargetConditionType TargetConditionType;

    public float DistanceOrTime;
    public int GCD, Param2;

    public string CastingActionName = string.Empty;

    public string CombatRole = string.Empty;

    /// <summary>
    /// Checks if the condition is true inside the rotation.
    /// </summary>
    /// <param name="rotation">The custom rotation instance.</param>
    /// <returns><c>true</c> if the condition is met; otherwise, <c>false</c>.</returns>
    protected override bool IsTrueInside(ICustomRotation rotation)
    {
        IBattleChara? tar = _action?.TargetInfo.FindTarget(true, false, false, default)?.Target ?? TargetType switch
        {
            TargetType.Target => Svc.Targets.Target as IBattleChara,
            TargetType.HostileTarget => DataCenter.HostileTarget,
            TargetType.Player => Player.Object,
            _ => null,
        };

        return TargetConditionType == TargetConditionType.IsNull
            ? tar == null
            : tar != null && TargetConditionType switch
            {
                TargetConditionType.HasStatus => tar.HasStatus(FromSelf, StatusId),
                TargetConditionType.IsBossFromTTK => tar.IsBossFromTTK(),
                TargetConditionType.IsBossFromIcon => tar.IsBossFromIcon(),
                TargetConditionType.IsDying => tar.IsDying(),
                TargetConditionType.InCombat => tar.InCombat(),
                TargetConditionType.Distance => CheckDistance(tar),
                TargetConditionType.StatusEnd => !tar.WillStatusEnd(DistanceOrTime, FromSelf, StatusId),
                TargetConditionType.StatusEndGCD => !tar.WillStatusEndGCD((uint)GCD, DistanceOrTime, FromSelf, StatusId),
                TargetConditionType.TimeToKill => CheckTimeToKill(tar),
                TargetConditionType.CastingAction => CheckCastingAction(tar),
                TargetConditionType.CastingActionTime => CheckCastingActionTime(tar),
                TargetConditionType.HP => CheckHP(tar),
                TargetConditionType.HPRatio => CheckHPRatio(tar),
                TargetConditionType.MP => CheckMP(tar),
                TargetConditionType.TargetName => CheckTargetName(tar),
                TargetConditionType.TargetRole => CheckTargetRole(tar),
                _ => false,
            };
    }

    private bool CheckDistance(IBattleChara tar)
    {
        return Param2 switch
        {
            0 => tar.DistanceToPlayer() > DistanceOrTime,
            1 => tar.DistanceToPlayer() < DistanceOrTime,
            2 => tar.DistanceToPlayer() == DistanceOrTime,
            _ => false,
        };
    }

    private bool CheckTimeToKill(IBattleChara tar)
    {
        return Param2 switch
        {
            0 => tar.GetTTK() > DistanceOrTime,
            1 => tar.GetTTK() < DistanceOrTime,
            2 => tar.GetTTK() == DistanceOrTime,
            _ => false,
        };
    }

    private bool CheckCastingAction(IBattleChara tar)
    {
        if (string.IsNullOrEmpty(CastingActionName) || tar.CastActionId == 0)
        {
            return false;
        }

        Lumina.Excel.Sheets.Action actionRow = Service.GetSheet<Lumina.Excel.Sheets.Action>().GetRow(tar.CastActionId);
        string castName = actionRow.Name.ExtractText();
        return CastingActionName == castName;
    }

    private bool CheckCastingActionTime(IBattleChara tar)
    {
        if (!tar.IsCasting || tar.CastActionId == 0)
        {
            return false;
        }

        float castTime = tar.TotalCastTime - tar.CurrentCastTime;
        return Param2 switch
        {
            0 => castTime > DistanceOrTime + DataCenter.DefaultGCDRemain,
            1 => castTime < DistanceOrTime + DataCenter.DefaultGCDRemain,
            2 => castTime == DistanceOrTime + DataCenter.DefaultGCDRemain,
            _ => false,
        };
    }

    private bool CheckHP(IBattleChara tar)
    {
        return Param2 switch
        {
            0 => tar.CurrentHp > GCD,
            1 => tar.CurrentHp < GCD,
            2 => tar.CurrentHp == GCD,
            _ => false,
        };
    }

    private bool CheckHPRatio(IBattleChara tar)
    {
        return Param2 switch
        {
            0 => tar.GetHealthRatio() > DistanceOrTime,
            1 => tar.GetHealthRatio() < DistanceOrTime,
            2 => tar.GetHealthRatio() == DistanceOrTime,
            _ => false,
        };
    }

    private bool CheckMP(IBattleChara tar)
    {
        return Param2 switch
        {
            0 => tar.CurrentMp > GCD,
            1 => tar.CurrentMp < GCD,
            2 => tar.CurrentMp == GCD,
            _ => false,
        };
    }

    private bool CheckTargetName(IBattleChara tar)
    {
        return !string.IsNullOrEmpty(CastingActionName) && tar.Name.TextValue == CastingActionName;
    }
    private bool CheckTargetRole(IBattleChara tar)
    {
        return !string.IsNullOrEmpty(CombatRole) && tar.GetRole().ToString() == CombatRole;
    }
}

internal enum TargetType : byte
{
    [Description("Hostile Target")]
    HostileTarget,

    [Description("Player")]
    Player,

    [Description("Target")]
    Target,
}

internal enum TargetConditionType : byte
{
    [Description("Is Null")]
    IsNull,

    [Description("Has status")]
    HasStatus,

    [Description("Is Dying")]
    IsDying,

    [Description("Is Boss From TTK")]
    IsBossFromTTK,

    [Description("Is Boss From Icon")]
    IsBossFromIcon,

    [Description("In Combat")]
    InCombat,

    [Description("Distance")]
    Distance,

    [Description("Status end")]
    StatusEnd,

    [Description("Status End GCD")]
    StatusEndGCD,

    [Description("Casting Action")]
    CastingAction,

    [Description("Casting Action Time Until")]
    CastingActionTime,

    [Description("Time To Kill")]
    TimeToKill,

    [Description("HP")]
    HP,

    [Description("HP%")]
    HPRatio,

    [Description("MP")]
    MP,

    [Description("Target Name")]
    TargetName,

    [Description("Target Role")]
    TargetRole,
}
