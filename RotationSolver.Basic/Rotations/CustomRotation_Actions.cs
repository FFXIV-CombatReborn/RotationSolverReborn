using RotationSolver.Basic.Traits;

namespace RotationSolver.Basic.Rotations;

partial class CustomRotation
{
    internal static void LoadActionSetting(ref IBaseAction action)
    {
        var a = action.Action;
        if (a.CanTargetAlly || a.CanTargetParty)
        {
            action.Setting.IsFriendly = true;
        }
        else if (a.CanTargetHostile)
        {
            action.Setting.IsFriendly = false;
        }
        else
        {
            action.Setting.IsFriendly = action.TargetInfo.EffectRange > 5;
        }
        // TODO: better target type check. (NoNeed?)
    }

    #region Role Actions

    static partial void ModifyTrueNorthPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = new[] { StatusID.TrueNorth };
        setting.ActionCheck = () => !IsLastAbility(ActionID.TrueNorthPvE);
    }

    static partial void ModifyShirkPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Tank;
    }

    static partial void ModifyAddlePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = new[] { StatusID.Addle };
        setting.StatusFromSelf = false;
    }

    static partial void ModifySwiftcastPvE(ref ActionSetting setting)
    {
        // setting.StatusProvide = StatusHelper.SwiftcastStatus;
    }

    static partial void ModifyEsunaPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Dispel;
    }

    static partial void ModifyLucidDreamingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.CurrentMp < Service.Config.LucidDreamingMpThreshold && InCombat;
    }

    static partial void ModifySecondWindPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player?.GetHealthRatio() < Service.Config.HealthSingleAbility && InCombat;
    }

    static partial void ModifyRampartPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
    }

    static partial void ModifyBloodbathPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player?.GetHealthRatio() < Service.Config.HealthSingleAbility && InCombat && HasHostilesInRange;
    }

    static partial void ModifyFeintPvE(ref ActionSetting setting)
    {
        setting.StatusFromSelf = false;
        setting.TargetStatusProvide = new[] { StatusID.Feint };
    }

    static partial void ModifyLowBlowPvE(ref ActionSetting setting)
    {
        setting.CanTarget = o =>
        {
            if (o is not IBattleChara b) return false;

            if (b.IsBossFromIcon() || IsMoving || b.CastActionId == 0) return false;

            if (!b.IsCastInterruptible || ActionID.InterjectPvE.IsCoolingDown()) return true;
            return false;
        };
    }

    static partial void ModifyPelotonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () =>
        {
            if (!NotInCombatDelay) return false;
            var players = PartyMembers.GetObjectInRadius(20);
            if (players.Any(ObjectHelper.InCombat)) return false;
            return players.Any(p => p.WillStatusEnd(3, false, StatusID.Peloton));
        };
    }

    static partial void ModifySprintPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = new[] { StatusID.SprintPenalty, StatusID.Dualcast };
    }

    static partial void ModifyIsleSprintPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = new[] { StatusID.Dualcast };
    }

    #endregion

    #region PvP
    static partial void ModifyStandardissueElixirPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasHostilesInMaxRange
            && (Player.CurrentMp <= Player.MaxMp / 3 || Player.CurrentHp <= Player.MaxHp / 3)
            && !IsLastAction(ActionID.StandardissueElixirPvP) && Player.TimeAlive() > 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyRecuperatePvP(ref ActionSetting setting)
    {
        //Recuperate will knock off Guard, likely killing you.
        setting.ActionCheck = () => Player.MaxHp - Player.CurrentHp > 15000 && Player.TimeAlive() > 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyGuardPvP(ref ActionSetting setting)
    {
        //If you've just respawned; you don't wanna Guard.
        setting.ActionCheck = () => Player.TimeAlive() > 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyPurifyPvP(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Dispel;
        setting.IsFriendly = true;
    }

    static partial void ModifySprintPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = new[] { StatusID.Sprint_1342 };
        setting.IsFriendly = true;
    }

    static partial void ModifyDervishPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBraveryPvP(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyEagleEyeShotPvP(ref ActionSetting setting)
    {
        
    }

    static partial void ModifyCometPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRustPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhantomDartPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyRampagePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRampartPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyFullSwingPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyHaelanPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyStoneskinIiPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDiabrosisPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBloodbathPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifySwiftPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifySmitePvP(ref ActionSetting setting)
    {

    }

    #endregion
    private protected virtual IBaseAction? Raise => null;
    private protected virtual IBaseAction? TankStance => null;

    private protected virtual IBaseAction? LimitBreak1 => null;
    private protected virtual IBaseAction? LimitBreak2 => null;
    private protected virtual IBaseAction? LimitBreak3 => null;
    private protected virtual IBaseAction? LimitBreakPvP => null;

    /// <summary>
    /// All actions of this rotation.
    /// </summary>
    public virtual IAction[] AllActions =>
    [
        .. AllBaseActions.Where(i => i.Action.IsInJob()),
        .. Medicines.Where(i => i.HasIt),
        .. MpPotions.Where(i => i.HasIt),
        .. HpPotions.Where(i => i.HasIt),
        .. AllItems.Where(i => i.HasIt),
    ];

    /// <summary>
    /// All traits of this action.
    /// </summary>
    public virtual IBaseTrait[] AllTraits { get; } = Array.Empty<IBaseTrait>();

    private PropertyInfo[]? _allBools;

    /// <summary>
    /// All bools of this rotation.
    /// </summary>
    public PropertyInfo[] AllBools => _allBools ??= GetType().GetStaticProperties<bool>();

    private PropertyInfo[]? _allBytes;

    /// <summary>
    /// All bytes or integers of this rotation.
    /// </summary>
    public PropertyInfo[] AllBytesOrInt => _allBytes ??= GetType().GetStaticProperties<byte>().Union(GetType().GetStaticProperties<int>()).ToArray();

    private PropertyInfo[]? _allFloats;

    /// <summary>
    /// All floats of this rotation.
    /// </summary>
    public PropertyInfo[] AllFloats => _allFloats ??= GetType().GetStaticProperties<float>();
}
