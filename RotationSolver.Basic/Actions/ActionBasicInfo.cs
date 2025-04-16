using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The action info for the <see cref="Lumina.Excel.Sheets.Action"/>.
/// </summary>
public readonly struct ActionBasicInfo
{
    internal static readonly uint[] ActionsNoNeedCasting =
    [
        5,
        (uint)ActionID.PowerfulShotPvP,
        (uint)ActionID.BlastChargePvP,
    ];

    private readonly IBaseAction _action;

    /// <summary>
    /// The name of the action.
    /// </summary>
    public readonly string Name => _action.Action.Name.ExtractText();

    /// <summary>
    /// The ID of the action.
    /// </summary>
    public readonly uint ID => _action.Action.RowId;

    /// <summary>
    /// The Range of the action.
    /// </summary>
    public readonly sbyte Range => _action.Action.Range;

    /// <summary>
    /// The EffectRange of the action.
    /// </summary>
    public readonly byte EffectRange => _action.Action.EffectRange;

    /// <summary>
    /// The icon of the action.
    /// </summary>
    public readonly uint IconID => ID == (uint)ActionID.SprintPvE ? 104u : _action.Action.Icon;

    /// <summary>
    /// The adjust id of this action.
    /// </summary>
    public readonly uint AdjustedID => (uint)Service.GetAdjustedActionId((ActionID)ID);

    /// <summary>
    /// The attack type of this action.
    /// </summary>
    public readonly AttackType AttackType => (AttackType)(_action.Action.AttackType.RowId != 0 ? _action.Action.AttackType.RowId : byte.MaxValue);

    /// <summary>
    /// The aspect of this action.
    /// </summary>
    public Aspect Aspect { get; }

    /// <summary>
    /// The animation lock time of this action.
    /// </summary>
    [Obsolete("Use ActionManagerHelper.GetCurrentAnimationLock()")]
    public readonly float AnimationLockTime => ActionManagerHelper.GetCurrentAnimationLock();

    /// <summary>
    /// The level of this action.
    /// </summary>
    public readonly byte Level => _action.Action.ClassJobLevel;

    /// <summary>
    /// If this action is enough level to use.
    /// </summary>
    public readonly bool EnoughLevel => Player.Level >= Level;

    /// <summary>
    /// If this action a pvp action.
    /// </summary>
    public readonly bool IsPvP => _action.Action.IsPvP;

    /// <summary>
    /// If this action a pvp action.
    /// </summary>
    public readonly byte CastType => _action.Action.CastType;

    /// <summary>
    /// Casting time.
    /// </summary>
    public readonly unsafe float CastTime => ((ActionID)AdjustedID).GetCastTime();

    /// <summary>
    /// How many mp does this action needs.
    /// </summary>
    public readonly unsafe uint MPNeed
    {
        get
        {
            if (IsPvP && ID != 29711) return 0;

            var mpOver = _action.Setting.MPOverride?.Invoke();
            if (mpOver.HasValue) return mpOver.Value;

            var mp = (uint)ActionManager.GetActionCost(ActionType.Action, AdjustedID, 0, 0, 0, 0);
            if (mp < 100) return 0;

            return mp;
        }
    }

    /// <summary>
    /// Is this action on the slot.
    /// </summary>
    public readonly bool IsOnSlot
    {
        get
        {
            if (_action.Action.ClassJob.RowId == (uint)Job.BLU)
            {
                return DataCenter.BluSlots.Contains(ID);
            }

            if (IsDutyAction)
            {
                return DataCenter.DutyActions.Contains(ID);
            }

            return IsPvP == DataCenter.IsPvP;
        }
    }

    /// <summary>
    /// Is this action is a lb action.
    /// </summary>
    public bool IsLimitBreak { get; }

    /// <summary>
    /// Is this action a general gcd.
    /// </summary>
    public bool IsGeneralGCD { get; }

    /// <summary>
    /// Is this action a real gcd.
    /// </summary>
    public bool IsRealGCD { get; }

    /// <summary>
    /// Is this action a duty action.
    /// </summary>
    public bool IsDutyAction { get; }

    /// <summary>
    /// The basic way to create a basic info
    /// </summary>
    /// <param name="action">the action</param>
    /// <param name="isDutyAction">if it is a duty action.</param>
    public ActionBasicInfo(IBaseAction action, bool isDutyAction)
    {
        _action = action;
        IsGeneralGCD = _action.Action.IsGeneralGCD();
        IsRealGCD = _action.Action.IsRealGCD();
        IsLimitBreak = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.LimitBreak or ActionCate.LimitBreak_15;
        IsDutyAction = isDutyAction;
        Aspect = (Aspect)_action.Action.Aspect;
    }

    internal readonly bool BasicCheck(bool skipStatusProvideCheck, bool skipComboCheck, bool skipCastingCheck)
    {
        if (!IsActionEnabled() || !IsOnSlot) return false;
        if (IsLimitBreak) return true;
        if (IsActionDisabled() || !EnoughLevel || !HasEnoughMP() || !SpellUnlocked) return false;

        if (IsStatusNeeded() || IsStatusProvided(skipStatusProvideCheck)) return false;
        if (IsLimitBreakLevelLow() || !IsComboValid(skipComboCheck) || !IsRoleActionValid()) return false;
        if (NeedsCasting(skipCastingCheck)) return false;
        if (IsGeneralGCD && IsStatusProvidedDuringGCD()) return false;
        if (!IsActionCheckValid() || !IsRotationCheckValid()) return false;

        return true;
    }

    /// <summary>
    /// Whether the spell is unlocked by the player
    /// </summary>
    public unsafe bool SpellUnlocked => _action.Action.UnlockLink.RowId <= 0 || UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(_action.Action.UnlockLink.RowId);

    private bool IsActionEnabled() => _action.Config?.IsEnabled ?? false;

    private bool IsActionDisabled() => !IBaseAction.ForceEnable && (DataCenter.DisabledActionSequencer?.Contains(ID) ?? false);

    private bool HasEnoughMP() => DataCenter.CurrentMp >= MPNeed;

    private bool IsStatusNeeded()
    {
        var player = Player.Object;
        return _action.Setting.StatusNeed != null && player.WillStatusEndGCD(_action.Config.StatusGcdCount, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusNeed);
    }

    private bool IsStatusProvided(bool skipStatusProvideCheck)
    {
        var player = Player.Object;
        return !skipStatusProvideCheck && _action.Setting.StatusProvide != null && !player.WillStatusEndGCD(_action.Config.StatusGcdCount, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusProvide);
    }

    private bool IsLimitBreakLevelLow() => _action.Action.ActionCategory.RowId == 15 && CustomRotation.LimitBreakLevel <= 1;

    private bool IsComboValid(bool skipComboCheck) => skipComboCheck || !IsGeneralGCD || CheckForCombo();

    private bool IsRoleActionValid()
    {
        return !_action.Action.IsRoleAction || (_action.Action.ClassJobCategory.Value.DoesJobMatchCategory(DataCenter.Job) == true);
    }

    private bool IsRotationCheckValid()
    {
        return IBaseAction.ForceEnable || (_action.Setting.RotationCheck?.Invoke() ?? true);
    }

    private bool NeedsCasting(bool skipCastingCheck)
    {
        var player = Player.Object;
        return CastTime > 0 && !player.HasStatus(true, new[] { StatusID.Swiftcast, StatusID.Triplecast, StatusID.Dualcast }) && !ActionsNoNeedCasting.Contains(ID) &&
               (DataCenter.SpecialType == SpecialCommandType.NoCasting || (DateTime.Now > DataCenter.KnockbackStart && DateTime.Now < DataCenter.KnockbackFinished) ||
                (DataCenter.NoPoslock && DataCenter.IsMoving && !skipCastingCheck));
    }

    private bool IsStatusProvidedDuringGCD()
    {
        return _action.Setting.StatusProvide?.Length > 0 && _action.Setting.IsFriendly && IActionHelper.IsLastGCD(true, _action) && DataCenter.TimeSinceLastAction.TotalSeconds < 3;
    }

    private bool IsActionCheckValid() => _action.Setting.ActionCheck?.Invoke() ?? true;

    private readonly bool CheckForCombo()
    {
        if (!_action.Config.ShouldCheckCombo) return true;

        if (_action.Setting.ComboIdsNot != null)
        {
            if (_action.Setting.ComboIdsNot.Contains(DataCenter.LastComboAction)) return false;
        }

        var comboActions = _action.Action.ActionCombo.RowId != 0
    ? new ActionID[] { (ActionID)_action.Action.ActionCombo.RowId }
    : Array.Empty<ActionID>();

        if (_action.Setting.ComboIds != null) comboActions = [.. comboActions, .. _action.Setting.ComboIds];

        if (comboActions.Length > 0)
        {
            if (comboActions.Contains(DataCenter.LastComboAction))
            {
                if (DataCenter.ComboTime < DataCenter.DefaultGCDRemain) return false;
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}


