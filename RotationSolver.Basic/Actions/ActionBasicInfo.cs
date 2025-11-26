using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ExCSS;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The action info for the <see cref="Lumina.Excel.Sheets.Action"/>.
/// </summary>
public readonly struct ActionBasicInfo
{
    /// <summary>
    /// Actions that do not require casting.
    /// </summary>
    internal static readonly uint[] ActionsNoNeedCasting =
    [
        5,
        (uint)ActionID.PowerfulShotPvP,
        (uint)ActionID.BlastChargePvP,
    ];

    private readonly IBaseAction _action;

    /// <summary>
    /// Gets the name of the action.
    /// </summary>
    public readonly string Name => _action.Action.Name.ExtractText();

    /// <summary>
    /// Gets the unique identifier of the action.
    /// </summary>
    public readonly uint ID => _action.Action.RowId;

    /// <summary>
    /// Gets the range of the action.
    /// </summary>
    public readonly sbyte Range => _action.Action.Range;

    /// <summary>
    /// Gets the effect range of the action.
    /// </summary>
    public readonly byte EffectRange => _action.Action.EffectRange;

    /// <summary>
    /// Gets the icon ID of the action.
    /// </summary>
    public readonly uint IconID => ID == (uint)ActionID.SprintPvE ? 104u : _action.Action.Icon;

    /// <summary>
    /// Gets the adjusted ID of the action, considering any modifications.
    /// </summary>
    public readonly uint AdjustedID => (uint)Service.GetAdjustedActionId((ActionID)ID);

    /// <summary>
    /// Gets the attack type of the action.
    /// </summary>
    public readonly AttackType AttackType => (AttackType)(_action.Action.AttackType.RowId != 0 ? _action.Action.AttackType.RowId : byte.MaxValue);

    /// <summary>
    /// Gets the aspect of the action.
    /// </summary>
    public Aspect Aspect { get; }

    /// <summary>
    /// Gets the level required to use the action.
    /// </summary>
    public readonly byte Level => _action.Action.ClassJobLevel;

    /// <summary>
    /// 
    /// </summary>
    public readonly uint UnlockLink => _action.Action.UnlockLink.RowId;

    /// <summary>
    /// 
    /// </summary>
    public unsafe bool IsQuestUnlocked()
    {
        if (UnlockLink == 0 && _action.Setting.UnlockedByQuestID == 0)
        {
            return true;
        }

        if (UnlockLink != 0 && !UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(UnlockLink))
        {
            return false;
        }

        if (_action.Setting.UnlockedByQuestID != 0 && !UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(_action.Setting.UnlockedByQuestID))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the player's level is sufficient to use the action.
    /// </summary>
    public readonly bool EnoughLevel => Player.Level >= Level;

    /// <summary>
    /// 
    /// </summary>
    public bool EnoughLevelAndQuest()
    {
        if (EnoughLevel && IsQuestUnlocked())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the action is a PvP action.
    /// </summary>
    public readonly bool IsPvP => _action.Action.IsPvP;

    /// <summary>
    /// Gets the cast type of the action.
    /// </summary>
    public readonly byte CastType => _action.Action.CastType;

    /// <summary>
    /// Gets the casting time of the action.
    /// </summary>
    public readonly unsafe float CastTime => ((ActionID)AdjustedID).GetCastTime();

    /// <summary>
    /// Gets the MP required to use the action.
    /// </summary>
    public readonly unsafe uint MPNeed
    {
        get
        {
            uint? mpOver = _action.Setting.MPOverride?.Invoke();
            if (mpOver.HasValue)
            {
                return mpOver.Value;
            }

            uint mp = (uint)ActionManager.GetActionCost(ActionType.Action, AdjustedID, 0, 0, 0, 0);
            return mp < 100 ? 0 : mp;
        }
    }


    /// <summary>
    /// Whether the action manager says the current ability is ready and valid.
    /// </summary>
    public unsafe bool ActionManagerStatusValid()
    {
        return ActionManager.Instance() != null && ActionManager.Instance()->GetActionStatus(ActionType.Action, ID) == 0;
    }

    /// <summary>
    /// Determines whether the action is on the player's hotbar or slot.
    /// </summary>
    public readonly bool IsOnSlot => _action.Action.ClassJob.RowId == (uint)Job.BLU
                ? DataCenter.BluSlots.Contains(ID)
                : IsDutyAction ? DataCenter.DutyActions.Contains(ID) : IsPvP == DataCenter.IsPvP;

    /// <summary>
    /// Determines whether the action is a limit break action.
    /// </summary>
    public bool IsLimitBreak { get; }

    /// <summary>
    /// Determines whether the action is a PvP limit break action.
    /// </summary>
    public bool IsPvPLimitBreak { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsMountAction { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsSpecialAction { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsSystemAction { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsAbility { get; }

    /// <summary>
    /// Determines whether the action is a general global cooldown (GCD) action.
    /// </summary>
    public bool IsGeneralGCD { get; }

    /// <summary>
    /// Determines whether the action is a real global cooldown (GCD) action.
    /// </summary>
    public bool IsRealGCD { get; }

    /// <summary>
    /// Determines whether the action is a duty action.
    /// </summary>
    public bool IsDutyAction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionBasicInfo"/> struct.
    /// </summary>
    /// <param name="action">The base action.</param>
    /// <param name="isDutyAction">Indicates whether the action is a duty action.</param>
    public ActionBasicInfo(IBaseAction action, bool isDutyAction)
    {
        _action = action;
        IsGeneralGCD = _action.Action.IsGeneralGCD();
        IsRealGCD = _action.Action.IsRealGCD();
        IsLimitBreak = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.LimitBreak;
        IsPvPLimitBreak = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.LimitBreak_15;
        IsMountAction = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.Mount;
        IsSpecialAction = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.Special;
        IsAbility = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.Ability;
        IsSystemAction = (ActionCate?)_action.Action.ActionCategory.Value.RowId
            is ActionCate.System or ActionCate.System_11;
        IsDutyAction = isDutyAction;
        Aspect = (Aspect)_action.Action.Aspect;
    }

    /// <summary>
    /// Performs a basic check to determine whether the action can be used.
    /// </summary>
    /// <param name="skipStatusProvideCheck">Whether to skip the status provide check.</param>
    /// /// <param name="skipStatusNeed">Whether to skip the casting check.</param>
    /// <param name="skipComboCheck">Whether to skip the combo check.</param>
    /// <param name="skipCastingCheck">Whether to skip the casting check.</param>
    /// /// <param name="checkActionManager">Whether to check the action manager directly for skills being usable.</param>
    /// <returns>True if the action passes the basic check; otherwise, false.</returns>
    internal readonly unsafe bool BasicCheck(bool skipStatusProvideCheck, bool skipStatusNeed,  bool skipComboCheck, bool skipCastingCheck, bool checkActionManager = false)
    {
        // 1. Player and action slot checks
        if (Player.Object.StatusList == null)
        {
            return false;
        }

        if (!EnoughLevel)
        {
            return false;
        }

        var type = ActionHelper.GetActionCate(_action.Action);
        if (type is ActionCate.Weaponskill)
        {
            if (Player.Object.HasStatus(false, StatusID.Pacification_620))
            {
                return false;
            }
        }

        if (type is ActionCate.Spell)
        {
            if (Player.Object.HasStatus(false, StatusID.Silence))
            {
                return false;
            }
        }

        if (IsLimitBreak)
        {
            return false;
        }

        if (!IsActionEnabled() || !IsOnSlot)
        {
            return false;
        }

        if (IsActionDisabled() || !HasEnoughMP())
        {
            return false;
        }

        if (!IsQuestUnlocked())
        {
            PluginLog.Warning($"Do your class quests, action not unlocked: {Name}");
            BasicWarningHelper.AddSystemWarning($"Do your class quests, action not unlocked: {Name}");
            return false;
        }

        if (IsRealGCD)
        {
            if (ConfigurationHelper.BadStatusGCD.Contains(ActionManager.Instance()->GetActionStatus(ActionType.Action, AdjustedID)))
            {
                return false;
            }
        }

        if (IsAbility && !IsRealGCD)
        {
            if (ConfigurationHelper.BadStatusAbility.Contains(ActionManager.Instance()->GetActionStatus(ActionType.EventAction, AdjustedID)))
            {
                return false;
            }
        }

        // Status checks: need or provide
        if (IsStatusNeeded(skipStatusNeed) || IsStatusProvided(skipStatusProvideCheck))
        {
            return false;
        }

        // Combo and role checks
        if (!IsComboValid(skipComboCheck))
        {
            return false;
        }

        if (!IsActionJobValid())
        {
            return false;
        }

        // 5. Optional: ask the game directly if the action is usable
        // In terms of "whether we can cast something" this check functionally negates everything else as we're asking the game directly if this is usable
        // That *said* there is a lot of logic elsewhere here for prioritizing things, so we're simply going to add this as an optional check for handling abilities we want to verify are usable
        if (checkActionManager && !ActionManagerStatusValid())
        {
            return false;
        }

        return !NeedsCasting(skipCastingCheck) && (!IsGeneralGCD || !IsStatusProvidedDuringGCD()) && IsActionCheckValid() && IsRotationCheckValid();
    }

    private bool NeedsCasting(bool skipCastingCheck)
    {
        // Must have a cast time
        if (CastTime <= 0f)
            return false;

        // Must not have a instant cast status
        if (!Player.Object.WillStatusEnd(0, true, StatusHelper.SwiftcastStatus))
            return false;

        // Must not be in the no-cast list
        if (ActionsNoNeedCasting.Contains(ID))
            return false;

        // Must be in a state where casting is not possible
        if (DataCenter.SpecialType == SpecialCommandType.NoCasting ||
            (DateTime.Now > DataCenter.KnockbackStart && DateTime.Now < DataCenter.KnockbackFinished) ||
            (DataCenter.NoPoslock && DataCenter.IsMoving && !skipCastingCheck))
            return true;

        return false;
    }

    private bool IsActionEnabled()
    {
        return _action.Config?.IsEnabled ?? false;
    }

    private bool IsActionDisabled()
    {
        return !IBaseAction.ForceEnable && (DataCenter.DisabledActionSequencer?.Contains(ID) ?? false);
    }

    /// <summary>
    /// Determines whether the player has enough MP to use the action.
    /// </summary>
    public bool HasEnoughMP()
    {
        if (DataCenter.CurrentMp >= MPNeed)
        {
            return true;
        }

        if (Player.Job == Job.WHM)
        {
            // Freecure makes the next Cure II cost 0 MP
            if (ID == (uint)ActionID.CureIiPvE && Player.Object.HasStatus(true, StatusID.Freecure))
            {
                return true;
            }

            // Thin Air covers any expensive spell (including Raise)
            if (Player.Object.HasStatus(true, StatusID.ThinAir) || CustomRotation.IsLastAction(ActionID.ThinAirPvE))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsStatusNeeded(bool skipStatusNeed)
    {
        return Player.Object.StatusList != null && !skipStatusNeed && _action.Setting.StatusNeed != null && Player.Object.WillStatusEndGCD(_action.Config.StatusGcdCount, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusNeed);
    }

    private bool IsStatusProvided(bool skipStatusProvideCheck)
    {
        return Player.Object.StatusList != null && !skipStatusProvideCheck && _action.Setting.StatusProvide != null && !Player.Object.WillStatusEndGCD(_action.Config.StatusGcdCount, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusProvide);
    }

    private bool IsComboValid(bool skipComboCheck)
    {
        return skipComboCheck || !IsGeneralGCD || CheckForCombo();
    }

    private bool IsActionJobValid()
    {
        if (_action.Action.ClassJobCategory.Value.DoesJobMatchCategory(DataCenter.Job) == true)
        {
            return true;
        }

        return false;
    }

    private bool IsRotationCheckValid()
    {
        return IBaseAction.ForceEnable || (_action.Setting.RotationCheck?.Invoke() ?? true);
    }

    private bool IsStatusProvidedDuringGCD()
    {
        return _action.Setting.StatusProvide?.Length > 0 && _action.Setting.IsFriendly && IActionHelper.IsLastGCD(true, _action) && DataCenter.TimeSinceLastAction.TotalSeconds < 3;
    }

    private bool IsActionCheckValid()
    {
        return _action.Setting.ActionCheck?.Invoke() ?? true;
    }

    private readonly bool CheckForCombo()
    {
        if (!_action.Config.ShouldCheckCombo)
        {
            return true;
        }

        if (_action.Setting.ComboIdsNot != null)
        {
            if (_action.Setting.ComboIdsNot.Contains(DataCenter.LastComboAction))
            {
                return false;
            }
        }

        ActionID[] comboActions = _action.Action.ActionCombo.RowId != 0
                                ? [(ActionID)_action.Action.ActionCombo.RowId]
                                : [];

        if (_action.Setting.ComboIds != null)
        {
            comboActions = [.. comboActions, .. _action.Setting.ComboIds];
        }

        if (comboActions.Length > 0)
        {
            if (comboActions.Contains(DataCenter.LastComboAction))
            {
                if (DataCenter.ComboTime < DataCenter.DefaultGCDRemain)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}


