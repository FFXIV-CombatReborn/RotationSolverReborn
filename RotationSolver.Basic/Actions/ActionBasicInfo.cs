using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
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
	/// Returns <see cref="ActionSetting.AttackTypeOverride"/> when set; otherwise falls back to the game data value.
	/// For multi-type actions use <see cref="AttackTypes"/> or <see cref="HasAttackType"/>.
	/// </summary>
	public AttackType AttackType => _action.Setting.AttackTypeOverride ?? (AttackType)(_action.Action.AttackType.RowId != 0 ? _action.Action.AttackType.RowId : byte.MaxValue);

	/// <summary>
	/// Gets all attack types for this action, including any additional types set via
	/// <see cref="ActionSetting.AdditionalAttackTypes"/>.
	/// </summary>
	public IReadOnlyList<AttackType> AttackTypes
	{
		get
		{
			var additional = _action.Setting.AdditionalAttackTypes;
			if (additional == null || additional.Length == 0)
				return [AttackType];
			var result = new AttackType[1 + additional.Length];
			result[0] = AttackType;
			additional.CopyTo(result, 1);
			return result;
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> if this action has the specified attack type,
	/// accounting for all values in <see cref="AttackTypes"/>.
	/// </summary>
	public bool HasAttackType(AttackType attackType)
	{
		var attackTypes = AttackTypes;
		for (int i = 0; i < attackTypes.Count; i++)
		{
			if (attackTypes[i] == attackType)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Gets the aspect of the action.
	/// Returns <see cref="ActionSetting.AspectOverride"/> when set; otherwise falls back to the game data value.
	/// For multi-aspect actions use <see cref="Aspects"/> or <see cref="HasAspect"/>.
	/// </summary>
	public Aspect Aspect => _action.Setting.AspectOverride ?? (Aspect)_action.Action.Aspect;

	/// <summary>
	/// Gets all aspects for this action, including any additional aspects set via
	/// <see cref="ActionSetting.AdditionalAspects"/>.
	/// </summary>
	public IReadOnlyList<Aspect> Aspects
	{
		get
		{
			var additional = _action.Setting.AdditionalAspects;
			if (additional == null || additional.Length == 0)
				return [Aspect];
			var result = new Aspect[1 + additional.Length];
			result[0] = Aspect;
			additional.CopyTo(result, 1);
			return result;
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> if this action has the specified aspect,
	/// accounting for all values in <see cref="Aspects"/>.
	/// </summary>
	public bool HasAspect(Aspect aspect)
	{
		var aspects = Aspects;
		for (int i = 0; i < aspects.Count; i++)
		{
			if (aspects[i] == aspect)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Gets the level required to use the action.
	/// </summary>
	public readonly byte Level => _action.Action.ClassJobLevel;

	/// <summary>
	/// Gets the row ID of the unlock link associated with this action, used to check quest or unlock requirements.
	/// </summary>
	public readonly uint UnlockLink => _action.Action.UnlockLink.RowId;

	/// <summary>
	/// Determines whether the action has been unlocked by completing any associated quests or unlock links.
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
	public readonly bool EnoughLevel => DataCenter.PlayerSyncedLevel() >= Level;

	/// <summary>
	/// Determines whether the player meets both the level requirement and has unlocked this action via its associated quest.
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
	/// Gets a value indicating whether this action is a PvP action.
	/// </summary>
	public readonly bool IsPvP => _action.Action.IsPvP;

	/// <summary>
	/// Gets a value indicating whether this action can target the player themselves.
	/// </summary>
	public readonly bool CanTargetSelf => _action.Action.CanTargetSelf;

	/// <summary>
	/// Gets a value indicating whether this action can target party members.
	/// </summary>
	public readonly bool CanTargetParty => _action.Action.CanTargetParty;

	/// <summary>
	/// Gets a value indicating whether this action can target alliance members outside the player's party.
	/// </summary>
	public readonly bool CanTargetAlliance => _action.Action.CanTargetAlliance;

	/// <summary>
	/// Gets a value indicating whether this action can target hostile (enemy) entities.
	/// </summary>
	public readonly bool CanTargetHostile => _action.Action.CanTargetHostile;

	/// <summary>
	/// Gets a value indicating whether this action can target friendly (ally) entities.
	/// </summary>
	public readonly bool CanTargetAlly => _action.Action.CanTargetAlly;

	/// <summary>
	/// Gets a value indicating whether this action can target the player's own pet.
	/// </summary>
	public readonly bool CanTargetOwnPet => _action.Action.CanTargetOwnPet;

	/// <summary>
	/// Gets a value indicating whether this action can target a party member's pet.
	/// </summary>
	public readonly bool CanTargetPartyPet => _action.Action.CanTargetPartyPet;

	/// <summary>
	/// Gets a value indicating whether using this action will break an active combo chain.
	/// </summary>
	public readonly bool BreaksCombo => !_action.Action.PreservesCombo;

	/// <summary>
	/// Gets a value indicating whether this action targets a ground area rather than a specific entity.
	/// </summary>
	public readonly bool TargetAreaAction => _action.Action.TargetArea;

	/// <summary>
	/// Gets a value indicating whether this action requires an unobstructed line of sight to its target.
	/// </summary>
	public readonly bool RequiresLineOfSight => _action.Action.RequiresLineOfSight;

	/// <summary>
	/// Gets a value indicating whether the player must be facing the target to use this action.
	/// </summary>
	public readonly bool NeedToFaceTarget => _action.Action.NeedToFaceTarget;

	/// <summary>
	/// Determines if the action has the "Raise" behavior, which allows it to target dead allies for resurrection.
	/// </summary>
	public readonly bool RaiseAction => _action.Action.DeadTargetBehaviour == 1;

	/// <summary>
	/// Gets the cast type of the action.
	/// </summary>
	public readonly CastType CastType => (CastType)_action.Action.CastType;

	/// <summary>
	/// Gets the casting time of the action.
	/// </summary>
	public readonly float CastTime => ((ActionID)AdjustedID).GetCastTime();

	/// <summary>
	/// Gets the MP required to use the action.
	/// </summary>
	public readonly uint MPNeed
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
	public readonly bool IsOnSlot
	{
		get
		{
			// BLU morph actions: only visible when their parent action is in an active BLU slot
			if (_action.Setting.RequiredBluSlotActionId != 0)
			{
				foreach (var slotId in DataCenter.BluSlots)
				{
					if (slotId == _action.Setting.RequiredBluSlotActionId)
						return true;
				}
				return false;
			}

			if (_action.Action.ClassJob.RowId == (uint)Job.BLU)
			{
				foreach (var slotId in DataCenter.BluSlots)
				{
					if (slotId == ID)
						return true;
				}
				return false;
			}

			if (IsDutyAction)
			{
				foreach (var actionId in DataCenter.DutyActions)
				{
					if (actionId == ID)
						return true;
				}
				return false;
			}

			return IsPvP == DataCenter.IsPvP;
		}
	}

	/// <summary>
	/// Determines whether the action is a limit break action.
	/// </summary>
	public bool IsLimitBreak { get; }

	/// <summary>
	/// Determines whether the action is a PvP limit break action.
	/// </summary>
	public bool IsPvPLimitBreak { get; }

	/// <summary>
	/// Gets a value indicating whether this action is a mount action.
	/// </summary>
	public bool IsMountAction { get; }

	/// <summary>
	/// Gets a value indicating whether this action belongs to the Special action category.
	/// </summary>
	public bool IsSpecialAction { get; }

	/// <summary>
	/// Gets a value indicating whether this action belongs to a System action category.
	/// </summary>
	public bool IsSystemAction { get; }

	/// <summary>
	/// Gets a value indicating whether this action is an off-global-cooldown ability.
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
	}

	/// <summary>
	/// Performs a basic check to determine whether the action can be used.
	/// </summary>
	/// <param name="skipStatusProvideCheck">Whether to skip the status provide check.</param>
	/// <param name="skipStatusNeed">Whether to skip the casting check.</param>
	/// <param name="skipComboCheck">Whether to skip the combo check.</param>
	/// <param name="skipCastingCheck">Whether to skip the casting check.</param>
	/// <param name="checkActionManager">Whether to check the action manager directly for skills being usable.</param>
	/// <param name="targetOverride">Overrides the default target type for the action.</param>
	/// <returns>True if the action passes the basic check; otherwise, false.</returns>
	internal readonly unsafe bool BasicCheck(bool skipStatusProvideCheck, bool skipStatusNeed, bool skipComboCheck, bool skipCastingCheck, bool checkActionManager = false, TargetType targetOverride = default)
	{
		if (Player.Object == null)
		{
			return false;
		}

		// 1. Player and action slot checks
		if (Player.Object.StatusList == null)
		{
			return false;
		}

		if (DataCenter.Orbonne && (_action.ID == 14415 || _action.ID == 14414) && IsActionCheckValid() && ActionManagerStatusValid())
		{
			return true;
		}

		if (!EnoughLevel)
		{
			return false;
		}

		var type = ActionHelper.GetActionCate(_action.Action);
		if (!_action.Setting.IgnoresBadStatus)
		{
			if (type is ActionCate.Weaponskill)
			{
				if (StatusHelper.PlayerHasStatus(false, StatusID.Pacification_620))
				{
					return false;
				}
			}

			if (type is ActionCate.Spell)
			{
				if (StatusHelper.PlayerHasStatus(false, StatusID.Silence))
				{
					return false;
				}
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

		if (!_action.Setting.IgnoresBadStatus)
		{
			if (IsRealGCD)
			{
				var status = ActionManager.Instance()->GetActionStatus(ActionType.Action, AdjustedID);
				bool statusFound = false;
				foreach (var badStatus in ConfigurationHelper.BadStatusGCD)
				{
					if (badStatus == status)
					{
						statusFound = true;
						break;
					}
				}
				if (statusFound)
				{
					return false;
				}
			}

			if (IsAbility && !IsRealGCD)
			{
				var status = ActionManager.Instance()->GetActionStatus(ActionType.EventAction, AdjustedID);
				bool statusFound = false;
				foreach (var badStatus in ConfigurationHelper.BadStatusAbility)
				{
					if (badStatus == status)
					{
						statusFound = true;
						break;
					}
				}
				if (statusFound)
				{
					return false;
				}
			}
		}

		// Status checks: need or provide
		if (IsStatusNeeded(skipStatusNeed) || IsStatusProvided(skipStatusProvideCheck))
		{
			return false;
		}

		// Combo and role checks
		if (_action.Info.BreaksCombo && !IsComboValid(skipComboCheck))
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
		if (Player.Object == null)
		{
			return false;
		}

		// Must have a cast time
		if (CastTime <= 0f)
			return false;

		// Must not have a instant cast status
		if (!Player.Object.WillStatusEnd(0, true, StatusHelper.SwiftcastStatus))
			return false;

		// Must not be in the no-cast list
		if (Array.IndexOf(ActionsNoNeedCasting, ID) >= 0)
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
		return !IBaseAction.ForceEnable && _action.Config?.IsEnabled == false;
	}

	/// <summary>
	/// Determines whether the player has enough MP to use the action.
	/// </summary>
	public bool HasEnoughMP()
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (DataCenter.CurrentMp >= MPNeed)
		{
			return true;
		}

		if (Player.Job == Job.WHM)
		{
			// Freecure makes the next Cure II cost 0 MP
			if (ID == (uint)ActionID.CureIiPvE && StatusHelper.PlayerHasStatus(true, StatusID.Freecure))
			{
				return true;
			}

			// Thin Air covers any expensive spell (including Raise)
			if (StatusHelper.PlayerHasStatus(true, StatusID.ThinAir) || CustomRotation.IsLastAction(ActionID.ThinAirPvE))
			{
				return true;
			}
		}

		return false;
	}

	private bool IsStatusNeeded(bool skipStatusNeed)
	{
		if (Player.Object == null)
		{
			return false;
		}

		return Player.Object.StatusList != null && !skipStatusNeed && _action.Setting.StatusNeed != null && Player.Object.WillStatusEndGCD(_action.Config.StatusGcdCount, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusNeed);
	}

	private bool IsStatusProvided(bool skipStatusProvideCheck)
	{
		if (Player.Object == null)
		{
			return false;
		}

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
			foreach (var comboIdNot in _action.Setting.ComboIdsNot)
			{
				if (comboIdNot == DataCenter.LastComboAction)
				{
					return false;
				}
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
			bool foundCombo = false;
			foreach (var comboAction in comboActions)
			{
				if (comboAction == DataCenter.LastComboAction)
				{
					foundCombo = true;
					break;
				}
			}

			if (foundCombo)
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


