using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The base action for all actions.
/// </summary>
public class BaseAction : IBaseAction
{
	/// <summary>
	/// Gets or sets the target to use for the action.
	/// </summary>
	/// <value>
	/// A <see cref="TargetResult"/> representing the target of the action.
	/// </value>
	public TargetResult Target { get; set; } = default;

	/// <summary>
	/// Gets the target for preview purposes.
	/// </summary>
	/// <value>
	/// A nullable <see cref="TargetResult"/> representing the preview target, or <c>null</c> if no preview target is available.
	/// </value>
	public TargetResult? PreviewTarget { get; private set; } = null;

	/// <inheritdoc/>
	public Action Action { get; }

	/// <inheritdoc/>
	public ActionTargetInfo TargetInfo { get; }

	/// <inheritdoc/>
	public ActionBasicInfo Info { get; }

	/// <inheritdoc/>
	public ActionCooldownInfo Cooldown { get; }

	ICooldown IAction.Cooldown => Cooldown;

	/// <inheritdoc/>
	public uint ID => Info.ID;

	/// <inheritdoc/>
	public uint AdjustedID => Info.AdjustedID;

	/// <inheritdoc/>
	public static float AnimationLockTime => Player.AnimationLock;

	/// <inheritdoc/>
	public uint SortKey => Cooldown.CoolDownGroup;

	/// <inheritdoc/>
	public uint IconID => Info.IconID;

	/// <inheritdoc/>
	public string Name => Info.Name;

	/// <inheritdoc/>
	public string Description => string.Empty;

	/// <inheritdoc/>
	public byte Level => Info.Level;

	/// <inheritdoc/>
	public bool IsEnabled
	{
		get => Config.IsEnabled;
		set => Config.IsEnabled = value;
	}

	/// <inheritdoc/>
	public bool IsIntercepted
	{
		get => Config.IsIntercepted;
		set => Config.IsIntercepted = value;
	}

	/// <summary>
	/// 
	/// </summary>
	public bool IsRestrictedDOT
	{
		get => Config.IsRestrictedDOT;
		set => Config.IsRestrictedDOT = value;
	}

	/// <inheritdoc/>
	public bool IsOnCooldownWindow
	{
		get => Config.IsOnCooldownWindow;
		set => Config.IsOnCooldownWindow = value;
	}

	/// <inheritdoc/>
	public bool MinHPFeature
	{
		get => Config.MinHPFeature;
		set => Config.MinHPFeature = value;
	}

	/// <inheritdoc/>
	public bool SkipPositionSafetyCheck
	{
		get => Config.SkipPositionSafetyCheck;
		set => Config.SkipPositionSafetyCheck = value;
	}

	/// <inheritdoc/>
	public float MinHPPercent
	{
		get => Config.MinHPPercent;
		set => Config.MinHPPercent = value;
	}

	/// <inheritdoc/>
	public bool EnoughLevel => Info.EnoughLevel;

	/// <inheritdoc/>
	public ActionSetting Setting { get; set; }

	/// <inheritdoc/>
	public ActionConfig Config
	{
		get
		{
			if (!Service.Config.RotationActionConfig.TryGetValue(ID, out var value) || DataCenter.ResetActionConfigs)
			{
				value = Setting.CreateConfig?.Invoke() ?? new ActionConfig();
				Service.Config.RotationActionConfig[ID] = value;

				if (!Action.ClassJob.IsValid)
				{
					// Log the error for debugging purposes
					if (Service.Config.InDebug)
					{
						PluginLog.Debug($"ClassJob is not valid for Action ID: {ID}");
					}
					return value;
				}

				_ = Action.ClassJob.Value;

				if (Setting.TargetStatusProvide != null)
				{
					value.TimeToKill = 0;
				}
			}

			// Keep user configs in sync with rotation defaults: whenever the default value for
			// AOE Count or Status Refresh GCD Count changes between plugin updates, reset the
			// user's stored value to the new default, without touching other user-configured fields.
			ActionConfig? defaults = null;
			ActionConfig GetDefaults() => defaults ??= Setting.CreateConfig?.Invoke() ?? new ActionConfig();

			bool changed = false;

			byte defaultAoe = GetDefaults().AoeCount;
			if (!value.AoeResetDone || value.AppliedDefaultAoeCount != defaultAoe)
			{
				value.AoeCount = defaultAoe;
				value.AppliedDefaultAoeCount = defaultAoe;
				value.AoeResetDone = true;
				changed = true;
			}

			byte defaultStatusRefresh = GetDefaults().StatusRefreshGcdCount;
			if (value.AppliedDefaultStatusRefreshGcdCount != defaultStatusRefresh)
			{
				value.StatusRefreshGcdCount = defaultStatusRefresh;
				value.AppliedDefaultStatusRefreshGcdCount = defaultStatusRefresh;
				changed = true;
			}

			if (changed)
			{
				Service.Config.RotationActionConfig[ID] = value;
			}

			return value;
		}
	}

	/// <summary>
	/// The default constructor
	/// </summary>
	/// <param name="actionID">action id</param>
	/// <param name="isDutyAction">is this action a duty action</param>
	public BaseAction(ActionID actionID, bool isDutyAction = false)
	{
		Action = Service.GetSheet<Action>().GetRow((uint)actionID);
		TargetInfo = new(this);
		Info = new(this, isDutyAction);
		Cooldown = new(this);

		Setting = new();
	}

	/// <inheritdoc/>
	public bool CanUse(out IAction act, bool skipStatusProvideCheck = false, bool skipStatusNeed = false, bool skipTargetStatusNeedCheck = false, bool skipComboCheck = false, bool skipCastingCheck = false,
	bool usedUp = false, bool skipAoeCheck = false, bool skipTTKCheck = false, byte gcdCountForAbility = 0, bool checkActionManagerDirectly = false, TargetType targetOverride = default)
	{
		act = this;

		ActionTracer.Try(this);

		if (IBaseAction.ActionPreview)
		{
			skipCastingCheck = true;
		}
		else
		{
			Setting.EndSpecial = IBaseAction.ShouldEndSpecial;
		}

		if (IBaseAction.AllEmpty)
		{
			usedUp = true;
		}

		if (!Info.BasicCheck(skipStatusProvideCheck, skipStatusNeed, skipComboCheck, skipCastingCheck, checkActionManagerDirectly, targetOverride))
		{
			return ActionTracer.Reject(this, "BasicCheck");
		}

		if (!Cooldown.CooldownCheck(usedUp, gcdCountForAbility))
		{
			return ActionTracer.Reject(this, "Cooldown");
		}

		if (Setting.SpecialType == SpecialActionType.MeleeRangedAttack && IActionHelper.IsLastAction(IActionHelper.MovingActions))
		{
			return ActionTracer.Reject(this, "MeleeRangedAfterMove");
		}

		if (!skipTTKCheck)
		{
			if (!DataCenter.IsPvP || (DataCenter.IsPvP && !Service.Config.IgnorePvPttk))
			{
				if (!IsTimeToKillValid())
				{
					return ActionTracer.Reject(this, "TTKBelowThreshold");
				}
			}
		}
		PreviewTarget = TargetInfo.FindTarget(skipAoeCheck, skipStatusProvideCheck, skipTargetStatusNeedCheck, targetOverride);
		if (PreviewTarget == null)
		{
			return ActionTracer.Reject(this, "NoTarget");
		}

		if (!IBaseAction.ActionPreview)
		{
			Target = PreviewTarget.Value;
		}

		ActionTracer.Accept(this);
		return true;
	}

	private bool IsTimeToKillValid()
	{
		return DataCenter.AverageTTK >= Config.TimeToKill;
	}

	/// <inheritdoc/>
	public unsafe bool Use()
	{
		if (Player.Object == null)
		{
			return false;
		}

		var target = Target;
		var adjustId = AdjustedID;

		if (TargetInfo.IsTargetArea)
		{
			if (adjustId != ID || !target.Position.HasValue)
			{
				return false;
			}

			var loc = target.Position.Value;

			// Use ActionManagerEx for enhanced timing if tweaks are enabled
			if (Service.Config.RemoveAnimationLockDelay || Service.Config.RemoveCooldownDelay)
			{
				return ActionManagerEx.Instance.UseActionLocationWithTweaks(ActionType.Action, ID, Player.Object.GameObjectId, &loc);
			}
			else
			{
				var actionManager = ActionManager.Instance();
				return actionManager != null &&
					   actionManager->UseActionLocation(ActionType.Action, ID, Player.Object.GameObjectId, &loc);
			}
		}
		else
		{
			var targetId = target.Target?.GameObjectId ?? Player.Object.GameObjectId;

			if (targetId == 0 || Svc.Objects.SearchById(targetId) == null)
			{
				return false;
			}

			// Use ActionManagerEx for enhanced timing if tweaks are enabled
			if (Service.Config.RemoveAnimationLockDelay || Service.Config.RemoveCooldownDelay)
			{
				return ActionManagerEx.Instance.UseActionWithTweaks(ActionType.Action, adjustId, targetId);
			}
			else
			{
				var actionManager = ActionManager.Instance();
				return actionManager != null &&
					   actionManager->UseAction(ActionType.Action, adjustId, targetId);
			}
		}
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return Name;
	}
}