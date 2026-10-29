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
	/// Gets the name of the action.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the unique identifier of the action.
	/// </summary>
	public readonly uint ID => _action.Action.RowId;

	/// <summary>
	/// Gets the range of the action.
	/// </summary>
	public readonly sbyte Range => IsInstinctualAxe(ID) ? (sbyte)3 : (sbyte)ActionManager.GetActionRange(ID);

	internal static bool IsInstinctualAxe(uint id)
	{
		return (ActionID)id is ActionID.AvalancheAxePvE or ActionID.SpinningAxePvE or ActionID.MistralAxePvE or ActionID.GaleAxePvE;
	}

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
			{
				return [AttackType];
			}

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
		return AttackType == attackType || Array.IndexOf(_action.Setting.AdditionalAttackTypes ?? [], attackType) >= 0;
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
			{
				return [Aspect];
			}

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
		return Aspect == aspect || Array.IndexOf(_action.Setting.AdditionalAspects ?? [], aspect) >= 0;
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
			var mpOver = _action.Setting.MPOverride?.Invoke();
			if (mpOver.HasValue)
			{
				return mpOver.Value;
			}

			var mp = (uint)ActionManager.GetActionCost(ActionType.Action, AdjustedID, 0, 0, 0, 0);
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
			if (_action.Setting.RequiredBluSlotActionId != 0)
			{
				foreach (var slotId in DataCenter.BluSlots)
				{
					if (slotId == _action.Setting.RequiredBluSlotActionId)
					{
						return true;
					}
				}
				return false;
			}

			if (_action.Action.ClassJob.RowId == (uint)Job.BLU)
			{
				foreach (var slotId in DataCenter.BluSlots)
				{
					if (slotId == ID)
					{
						return true;
					}
				}
				return false;
			}

			if (_action.Setting.IsQuestBattleAction)
			{
				return DataCenter.IsInQuestBattle;
			}

			if (IsDutyAction)
			{
				foreach (var actionId in DataCenter.DutyActions)
				{
					if (actionId == ID)
					{
						return true;
					}
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
		Name = _action.Action.Name.ExtractText();
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

	internal readonly unsafe bool BasicCheck(bool skipStatusProvideCheck, bool skipStatusNeed, bool skipComboCheck, bool skipCastingCheck, bool checkActionManager = false, TargetType targetOverride = default)
	{
		if (Player.Object == null)
		{
			return false;
		}

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

		if (!IsActionEnabled())
		{
			return false;
		}

		if (!IsOnSlot)
		{
			return false;
		}

		if (!HasEnoughMP())
		{
			return false;
		}

		if (!IsQuestUnlocked())
		{
			BasicWarningHelper.AddSystemWarning($"Do your class quests, action not unlocked: {Name}");
			return false;
		}

		if (!_action.Setting.IgnoresBadStatus && (IsRealGCD || IsAbility)
			&& Array.IndexOf(ConfigurationHelper.BadStatusAction, (ActionStatus)ActionManager.Instance()->GetActionStatus(ActionType.Action, AdjustedID)) >= 0)
		{
			return false;
		}

		if (IsStatusNeeded(skipStatusNeed) || IsStatusProvided(skipStatusProvideCheck))
		{
			return false;
		}

		if (_action.Info.BreaksCombo && !IsComboValid(skipComboCheck))
		{
			return false;
		}

		if (!IsActionJobValid())
		{
			return false;
		}

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

		if (CastTime <= 0f)
		{
			return false;
		}

		if (!Player.Object.WillStatusEnd(0, true, StatusHelper.SwiftcastStatus))
		{
			return false;
		}

		if (Array.IndexOf(ActionsNoNeedCasting, ID) >= 0)
		{
			return false;
		}

		if (DataCenter.SpecialType == SpecialCommandType.NoCasting ||
			(DateTime.Now > DataCenter.KnockbackStart && DateTime.Now < DataCenter.KnockbackFinished) ||
			(DataCenter.NoPoslock && DataCenter.IsMoving && !skipCastingCheck) || DataCenter.BMRForceCancelCast || DataCenter.BMRIsMoving)
		{
			return true;
		}

		return false;
	}

	private bool IsActionEnabled()
	{
		return _action.Config?.IsEnabled ?? false;
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
			if (ID == (uint)ActionID.CureIiPvE && StatusHelper.PlayerHasStatus(true, StatusID.Freecure))
			{
				return true;
			}

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

		return Player.Object.StatusList != null && !skipStatusNeed && _action.Setting.StatusNeed != null && Player.Object.WillStatusEndGCD(0, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusNeed);
	}

	private bool IsStatusProvided(bool skipStatusProvideCheck)
	{
		if (Player.Object == null)
		{
			return false;
		}

		return Player.Object.StatusList != null && !skipStatusProvideCheck && _action.Setting.StatusProvide != null && _action.Config.ShouldCheckStatus && !Player.Object.WillStatusEndGCD(_action.Config.StatusRefreshGcdCount, 0, _action.Setting.StatusFromSelf, _action.Setting.StatusProvide);
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

		var comboActions = _action.Setting.ComboActionsCache;
		if (comboActions == null)
		{
			ActionID[] baseCombo = _action.Action.ActionCombo.RowId != 0
									? [(ActionID)_action.Action.ActionCombo.RowId]
									: [];

			comboActions = _action.Setting.ComboIds != null
				? [.. baseCombo, .. _action.Setting.ComboIds]
				: baseCombo;

			_action.Setting.ComboActionsCache = comboActions;
		}

		if (comboActions.Length > 0)
		{
			var foundCombo = false;
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


