using RotationSolver.Basic.Traits;

namespace RotationSolver.Basic.Rotations;

public partial class CustomRotation
{
    internal static void LoadActionSetting(ref IBaseAction action)
    {
        Lumina.Excel.Sheets.Action a = action.Action;
        action.Setting.IsFriendly = a.CanTargetAlly || a.CanTargetParty || (!a.CanTargetHostile && action.TargetInfo.EffectRange > 5);
        // TODO: better target type check. (NoNeed?)
    }

    #region Role Actions

    static partial void ModifyTrueNorthPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.TrueNorth];
        setting.ActionCheck = () => !IsLastAbility(ActionID.TrueNorthPvE) && DataCenter.DefaultGCDRemain > 0.630f;
    }

    static partial void ModifyShirkPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Tank;
    }

    static partial void ModifyAddlePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Addle];
        setting.StatusFromSelf = false;
    }

    static partial void ModifySwiftcastPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.SwiftcastStatus;
    }

    static partial void ModifyEsunaPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Dispel;
    }

    static partial void ModifyLucidDreamingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player?.CurrentMp < Service.Config.LucidDreamingMpThreshold && InCombat;
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
        setting.TargetStatusProvide = [StatusID.Feint];
    }

    static partial void ModifyLowBlowPvE(ref ActionSetting setting)
    {
        setting.CanTarget = o =>
        {
            return o is IBattleChara b && !b.IsBossFromIcon() && !IsMoving && b.CastActionId != 0 && (!b.IsCastInterruptible || ActionID.InterjectPvE.IsCoolingDown());
        };
    }

    static partial void ModifyPelotonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () =>
        {
            if (!NotInCombatDelay)
            {
                return false;
            }

            IEnumerable<IBattleChara> players = PartyMembers.GetObjectInRadius(20);

            bool anyInCombat = false;
            bool anyWillStatusEnd = false;
            foreach (var p in players)
            {
                if (ObjectHelper.InCombat(p))
                    anyInCombat = true;
                if (p.WillStatusEnd(3, false, StatusID.Peloton))
                    anyWillStatusEnd = true;
                if (anyInCombat && anyWillStatusEnd)
                    break;
            }
            return !anyInCombat && anyWillStatusEnd;
        };
    }

    static partial void ModifySprintPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.SprintPenalty, StatusID.Dualcast];
    }

    static partial void ModifyIsleSprintPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Dualcast];
    }

    #endregion

    #region PvP
    static partial void ModifyStandardissueElixirPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (!HasHostilesInMaxRange || StatusHelper.PlayerHasStatus(false, StatusID.Covered_1301))
			&& (Player?.CurrentMp <= Player?.MaxMp / 3 || Player?.CurrentHp <= Player?.MaxHp / 3)
            && !IsLastAction(ActionID.StandardissueElixirPvP) && Player.TimeAlive() > 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyRecuperatePvP(ref ActionSetting setting)
    {
        //Recuperate will knock off Guard, likely killing you.
        setting.ActionCheck = () => Player?.MaxHp - Player?.CurrentHp > 15000 && Player?.TimeAlive() > 5;
        setting.TargetType = TargetType.Self;
        setting.IsFriendly = true;
    }

    static partial void ModifyGuardPvP(ref ActionSetting setting)
    {
        //If you've just respawned; you don't wanna Guard.
        setting.ActionCheck = () => Player?.TimeAlive() > 5;
        setting.IsFriendly = true;
    }

    static partial void ModifyPurifyPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses);
        setting.IsFriendly = true;
    }

    static partial void ModifySprintPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Sprint_1342];
        setting.IsFriendly = true;
    }

    static partial void ModifyDervishPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionDervish];
        setting.StatusProvide = [StatusID.Dervish];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBraveryPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionBravery];
        setting.StatusProvide = [StatusID.Bravery];
        setting.TargetType = TargetType.Self;
    }

    static partial void ModifyEagleEyeShotPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionEagleEyeShot];
    }

    static partial void ModifyCometPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionComet];
        setting.IsFriendly = false;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRustPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionRust];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyPhantomDartPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionPhantomDart];
    }

    static partial void ModifyRampagePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionRampage];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyRampartPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionRampart];
    }

    static partial void ModifyFullSwingPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionFullSwing];
    }

    static partial void ModifyHaelanPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionHaelan];
    }

    static partial void ModifyStoneskinIiPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionStoneskinIi];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyDiabrosisPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionDiabrosis];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyBloodbathPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => ObjectHelper.PlayerTimeAlive() > 5;
        setting.StatusNeed = [StatusID.PvPRoleActionBloodbath];
    }

    static partial void ModifySwiftPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionSwift];
    }

    static partial void ModifySmitePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.PvPRoleActionSmite];
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
    public virtual IAction[] AllActions
    {
        get
        {
            var list = new List<IAction>();

            foreach (var i in AllBaseActions)
            {
                if (i.Action.IsInJob())
                    list.Add(i);
            }
            foreach (var i in Medicines)
            {
                if (i.HasIt)
                    list.Add(i);
            }
            foreach (var i in MpPotions)
            {
                if (i.HasIt)
                    list.Add(i);
            }
            foreach (var i in HpPotions)
            {
                if (i.HasIt)
                    list.Add(i);
            }
            foreach (var i in AllItems)
            {
                if (i.HasIt)
                    list.Add(i);
            }
            return [.. list];
        }
    }

    /// <summary>
    /// All traits of this action.
    /// </summary>
    public virtual IBaseTrait[] AllTraits { get; } = [];

    private PropertyInfo[]? _allBools;

    /// <summary>
    /// All bools of this rotation.
    /// </summary>
    public PropertyInfo[] AllBools => _allBools ??= GetType().GetStaticProperties<bool>();

    private PropertyInfo[]? _allBytes;

    /// <summary>
    /// All bytes or integers of this rotation.
    /// </summary>
    public PropertyInfo[] AllBytesOrInt
    {
        get
        {
            if (_allBytes != null)
                return _allBytes;

            var bytes = GetType().GetStaticProperties<byte>();
            var ints = GetType().GetStaticProperties<int>();

            var result = new PropertyInfo[bytes.Length + ints.Length];
            Array.Copy(bytes, 0, result, 0, bytes.Length);
            Array.Copy(ints, 0, result, bytes.Length, ints.Length);

            _allBytes = result;
            return _allBytes;
        }
    }

    private PropertyInfo[]? _allFloats;

    /// <summary>
    /// All floats of this rotation.
    /// </summary>
    public PropertyInfo[] AllFloats => _allFloats ??= GetType().GetStaticProperties<float>();
}