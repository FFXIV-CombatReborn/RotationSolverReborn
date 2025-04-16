using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The action cooldown information.
/// </summary>
public readonly struct ActionCooldownInfo : ICooldown
{
    private readonly IBaseAction _action;

    /// <summary>
    /// The cooldown group.
    /// </summary>
    public byte CoolDownGroup { get; }

    /// <summary>
    /// Gets the cooldown detail.
    /// </summary>
    private unsafe RecastDetail* CoolDownDetail => ActionIdHelper.GetCoolDownDetail(CoolDownGroup);

    /// <summary>
    /// Gets the total recast time.
    /// </summary>
    private unsafe float RecastTime => CoolDownDetail == null ? 0 : CoolDownDetail->Total;

    /// <summary>
    /// Gets the elapsed recast time minus the default GCD remain.
    /// </summary>
    public float RecastTimeElapsed => RecastTimeElapsedRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Gets the raw elapsed recast time.
    /// </summary>
    internal unsafe float RecastTimeElapsedRaw => CoolDownDetail == null ? 0 : CoolDownDetail->Elapsed;
    float ICooldown.RecastTimeElapsedRaw => RecastTimeElapsedRaw;

    /// <summary>
    /// Gets a value indicating whether the action is cooling down.
    /// </summary>
    public unsafe bool IsCoolingDown => ActionIdHelper.IsCoolingDown(CoolDownGroup);

    /// <summary>
    /// Gets the remaining recast time.
    /// </summary>
    private float RecastTimeRemain => RecastTime - RecastTimeElapsedRaw;

    /// <summary>
    /// Gets a value indicating whether the action has at least one charge.
    /// </summary>
    public bool HasOneCharge => !IsCoolingDown || RecastTimeElapsedRaw >= RecastTimeOneChargeRaw;

    /// <summary>
    /// Gets the current number of charges.
    /// </summary>
    public unsafe ushort CurrentCharges => (ushort)ActionManager.Instance()->GetCurrentCharges(_action.Info.AdjustedID);

    /// <summary>
    /// Gets the maximum number of charges.
    /// </summary>
    public unsafe ushort MaxCharges => Math.Max(ActionManager.GetMaxCharges(_action.Info.AdjustedID, (uint)Player.Level), (ushort)1);

    /// <summary>
    /// Gets the raw recast time for one charge.
    /// </summary>
    public float RecastTimeOneChargeRaw => ActionManager.GetAdjustedRecastTime(ActionType.Action, _action.Info.AdjustedID) / 1000f;
    float ICooldown.RecastTimeOneChargeRaw => RecastTimeOneChargeRaw;

    /// <summary>
    /// Gets the remaining recast time for one charge minus the default GCD remain.
    /// </summary>
    public float RecastTimeRemainOneCharge => RecastTimeRemainOneChargeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Gets the raw remaining recast time for one charge.
    /// </summary>
    private float RecastTimeRemainOneChargeRaw => RecastTimeRemain % RecastTimeOneChargeRaw;

    /// <summary>
    /// Gets the elapsed recast time for one charge minus the default GCD elapsed.
    /// </summary>
    public float RecastTimeElapsedOneCharge => RecastTimeElapsedOneChargeRaw - DataCenter.DefaultGCDElapsed;

    /// <summary>
    /// Gets the raw elapsed recast time for one charge.
    /// </summary>
    private float RecastTimeElapsedOneChargeRaw => RecastTimeElapsedRaw % RecastTimeOneChargeRaw;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionCooldownInfo"/> struct.
    /// </summary>
    /// <param name="action">The action.</param>
    public ActionCooldownInfo(IBaseAction action)
    {
        _action = action;
        CoolDownGroup = _action.Action.GetCoolDownGroup();
    }

    /// <summary>
    /// Determines whether one charge has elapsed after the specified GCD count and offset.
    /// </summary>
    /// <param name="gcdCount">The GCD count.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>True if one charge has elapsed; otherwise, false.</returns>
    public bool ElapsedOneChargeAfterGCD(uint gcdCount = 0, float offset = 0)
        => ElapsedOneChargeAfter(DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// Determines whether one charge has elapsed after the specified time.
    /// </summary>
    /// <param name="time">The time.</param>
    /// <returns>True if one charge has elapsed; otherwise, false.</returns>
    public bool ElapsedOneChargeAfter(float time)
        => IsCoolingDown && time <= RecastTimeElapsedOneCharge;

    /// <summary>
    /// Determines whether the action has elapsed after the specified GCD count and offset.
    /// </summary>
    /// <param name="gcdCount">The GCD count.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>True if the action has elapsed; otherwise, false.</returns>
    public bool ElapsedAfterGCD(uint gcdCount = 0, float offset = 0)
        => ElapsedAfter(DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// Determines whether the action has elapsed after the specified time.
    /// </summary>
    /// <param name="time">The time.</param>
    /// <returns>True if the action has elapsed; otherwise, false.</returns>
    public bool ElapsedAfter(float time)
        => IsCoolingDown && time <= RecastTimeElapsed;

    /// <summary>
    /// Determines whether the action will have one charge after the specified GCD count and offset.
    /// </summary>
    /// <param name="gcdCount">The GCD count.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>True if the action will have one charge; otherwise, false.</returns>
    public bool WillHaveOneChargeGCD(uint gcdCount = 0, float offset = 0)
        => WillHaveOneCharge(DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// Determines whether the action will have one charge after the specified remaining time.
    /// </summary>
    /// <param name="remain">The remaining time.</param>
    /// <returns>True if the action will have one charge; otherwise, false.</returns>
    public bool WillHaveOneCharge(float remain)
        => HasOneCharge || RecastTimeRemainOneCharge <= remain;

    /// <summary>
    /// Determines whether the action will have the specified number of charges after the given GCD count and offset.
    /// </summary>
    /// <param name="charges">The number of charges.</param>
    /// <param name="gcdCount">The GCD count.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>True if the action will have the specified number of charges; otherwise, false.</returns>
    public bool WillHaveXChargesGCD(uint charges, uint gcdCount = 0, float offset = 0)
        => WillHaveXCharges(charges, DataCenter.GCDTime(gcdCount, offset));

    /// <summary>
    /// Determines whether the action will have the specified number of charges after the given remaining time.
    /// </summary>
    /// <param name="charges">The number of charges.</param>
    /// <param name="remain">The remaining time.</param>
    /// <returns>True if the action will have the specified number of charges; otherwise, false.</returns>
    public bool WillHaveXCharges(uint charges, float remain)
    {
        if (charges <= CurrentCharges)
            return true;

        float requiredTime = (charges - CurrentCharges - 1) * RecastTimeOneChargeRaw;
        return RecastTimeRemainOneCharge <= remain - requiredTime;
    }

    /// <summary>
    /// Determines whether the action was just used after the specified time.
    /// </summary>
    /// <param name="time">The time.</param>
    /// <returns>True if the action was just used; otherwise, false.</returns>
    public bool JustUsedAfter(float time)
    {
        var elapsed = RecastTimeElapsedRaw % RecastTimeOneChargeRaw;
        return elapsed + DataCenter.DefaultGCDRemain < time;
    }

    /// <summary>
    /// Checks the cooldown status of the action.
    /// </summary>
    /// <param name="isEmpty">Indicates whether the action is empty.</param>
    /// <param name="gcdCountForAbility">The GCD count for the ability.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    internal bool CooldownCheck(bool isEmpty, byte gcdCountForAbility)
    {
        if (!_action.Info.IsGeneralGCD)
        {
            if (IsCoolingDown)
            {
                if (_action.Info.IsRealGCD)
                {
                    if (!WillHaveOneChargeGCD(0, 0)) return false;
                }
                else
                {
                    if (!HasOneCharge && RecastTimeRemainOneChargeRaw > DataCenter.DefaultGCDRemain) return false;
                }
            }

            if (!isEmpty)
            {
                if (RecastTimeRemain > DataCenter.DefaultGCDRemain + DataCenter.DefaultGCDTotal * gcdCountForAbility)
                    return false;
            }
        }

        if (!_action.Info.IsRealGCD)
        {
            if (ActionManagerHelper.GetCurrentAnimationLock() > 0) return false;
        }
        return true;
    }
}
