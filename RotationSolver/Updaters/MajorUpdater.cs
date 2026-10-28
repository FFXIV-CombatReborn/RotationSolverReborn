using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Commands;
using RotationSolver.IPC;
using RotationSolver.UI.HighlightTeachingMode;

namespace RotationSolver.Updaters;

internal static class MajorUpdater
{
	private static TimeSpan _timeSinceUpdate = TimeSpan.Zero;
	private static bool _rotationsLoaded;
	private static bool _bmrDataLive;

	private static readonly List<VfxNewData> _vfxRemaining = [];
	private static readonly List<string> _expiredWarnings = [];

	public static bool IsValid
	{
		get
		{
			if (!Player.Available)
			{
				return false;
			}

			return !Svc.Condition[ConditionFlag.BetweenAreas] && !Svc.Condition[ConditionFlag.BetweenAreas51] && !Svc.Condition[ConditionFlag.LoggingOut];
		}
	}

	private static readonly HashSet<string> _loggedErrors = [];
	private const int MaxLoggedErrors = 256;

	public static void Enable()
	{
		Svc.Framework.Update += OnUpdate;
	}

	private static void OnUpdate(IFramework framework)
	{
		if (!ShouldRunThisCycle(framework, out var isValid))
		{
			return;
		}

		if (Service.Config.TeachingMode)
		{
			Run("HotbarHighlightManager.HotbarIDs.Clear", HotbarHighlightManager.HotbarIDs.Clear);
		}

		if (!isValid)
		{
			Run("RSRInvalidUpdate", InvalidUpdate);
			return;
		}

		Run("ActionUpdater.UpdateCombatInfo", ActionUpdater.UpdateCombatInfo);
		Run("RotationUpdater.UpdateRotation", RotationUpdater.UpdateRotation);

		var isActivated = DataCenter.IsActivated();
		ActivatedCoreUpdate(isActivated);
		ActivatedHighlightUpdate(isActivated);
		CommonUpdate();
		Run("CleanupUpdate", CleanupUpdate);
		StateUpdate();
		Run("Secondary RSRUpdate", MiscAndTargetFreelyUpdate);
	}

	private static bool ShouldRunThisCycle(IFramework framework, out bool isValid)
	{
		isValid = false;
		try
		{
			_timeSinceUpdate += framework.UpdateDelta;

			if (Service.Config.MinUpdatingTime > 0f && _timeSinceUpdate < TimeSpan.FromSeconds(Service.Config.MinUpdatingTime))
			{
				return false;
			}

			_timeSinceUpdate = TimeSpan.Zero;
			isValid = IsValid;
			RotationSolverPlugin.ShowFirstStartTutorialIfNeeded();

			if (isValid)
			{
				RotationSolverPlugin.ShowChangelogIfUpdated();

				if (!_rotationsLoaded)
				{
					RotationUpdater.LoadBuiltInRotations();
					_rotationsLoaded = true;
				}
			}

			return true;
		}
		catch (Exception ex)
		{
			LogOnce("GateUpdate Exception", ex);
			return false;
		}
	}

	private static void InvalidUpdate()
	{
		if (Svc.Condition[ConditionFlag.BetweenAreas])
		{
			ActionUpdater.MarkAreaChange();
		}

		RSCommands.UpdateRotationState();
		ActionUpdater.ClearNextAction();
		MiscUpdater.UpdateEntry();
	}

	private static void ActivatedCoreUpdate(bool isActivated)
	{
		var autoOnEnabled = Service.Config.AutoOnYes && (Service.Config.StartOnAllianceIsInCombat2
			|| Service.Config.StartOnAttackedBySomeone2
			|| Service.Config.StartOnFieldOpInCombat2
			|| Service.Config.StartOnPartyIsInCombat2) && !DataCenter.IsInDutyReplay();

		if (autoOnEnabled || isActivated)
		{
			Run("(RSRActivatedCore): TargetUpdater.UpdateTargets", TargetUpdater.UpdateTargets);
		}

		if (!isActivated)
		{
			ResetBmrDataIfLive();
			return;
		}

		Run("(RSRActivatedCore): MacroUpdater.UpdateMacro", MacroUpdater.UpdateMacro);

		if (DataCenter.BMREnabled)
		{
			_bmrDataLive = true;
			Run("(RSRActivatedCore): BossModUpdater.Update", BossModUpdater.Update);
			Run("(RSRActivatedCore): BMRPlanUpdater.Update", BMRPlanUpdater.Update);
		}
		else
		{
			ResetBmrDataIfLive();
		}

		Run("(RSRActivatedCore): StateUpdater.UpdateState", StateUpdater.UpdateState);
		Run("(RSRActivatedCore): AutoAttackUpdater.Update", AutoAttackUpdater.Update);
		Run("(RSRActivatedCore): ActionUpdater.UpdateNextAction", ActionUpdater.UpdateNextAction);

		var canDoAction = false;
		try
		{
			canDoAction = ActionUpdater.CanDoAction();
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): ActionUpdater.CanDoAction Exception", ex);
		}

		try
		{
			MovingUpdater.UpdateCanMove(canDoAction);
		}
		catch (Exception ex)
		{
			LogOnce("(RSRActivatedCore): MovingUpdater.UpdateCanMove Exception", ex);
		}

		if (canDoAction)
		{
			Run("(RSRActivatedCore): RSCommands.DoAction", RSCommands.DoAction);
		}

		if (DataCenter.IsTargetOnly)
		{
			Run("(RSRActivatedCore): RSCommands.UpdateTargetFromNextAction (TargetOnly)", RSCommands.UpdateTargetFromNextAction);
		}

		if (!DataCenter.IsTargetOnly && Service.Config.TeachingMode && Service.Config.TeachingModeAutoTarget && DataCenter.InCombat)
		{
			Run("(RSRActivatedCore): RSCommands.UpdateTargetFromNextAction (TeachingMode)", RSCommands.UpdateTargetFromNextAction);
		}

		Run("(RSRActivatedCore): Wrath_IPCSubscriber.DisableAutoRotation", Wrath_IPCSubscriber.DisableAutoRotation);
	}

	private static void ResetBmrDataIfLive()
	{
		if (!_bmrDataLive)
		{
			return;
		}

		_bmrDataLive = false;
		DataCenter.ResetBmrData();
		DataCenter.ResetBmrPlanData();
	}

	private static void ActivatedHighlightUpdate(bool isActivated)
	{
		if (!isActivated)
		{
			HotbarHighlightManager.ClearElements();

			if (HotbarDisabledColor.HasTint)
			{
				Run("Hotbar Disabled Redden", HotbarDisabledColor.Reset);
			}
			return;
		}

		if (Service.Config.TeachingMode && ActionUpdater.NextAction is not null)
		{
			Run("Hotbar Highlighting", () =>
			{
				var hotbar = HotbarAddonHelper.GetHotbarID(ActionUpdater.NextAction);
				if (hotbar.HasValue)
				{
					_ = HotbarHighlightManager.HotbarIDs.Add(hotbar.Value);
				}
			});

			Run("Hotbar Highlight Update", HotbarHighlightManager.UpdateElements);
		}
		else
		{
			HotbarHighlightManager.ClearElements();
		}

		if (Service.Config.ReddenDisabledHotbarActions || HotbarDisabledColor.HasTint)
		{
			Run("Hotbar Disabled Redden", HotbarDisabledColor.ApplyFrame);
		}
	}

	private static void CommonUpdate()
	{
		Run("CommonUpdate", RotationSolverPlugin.UpdateDisplayWindow);

		Run("HotbarKeybindHelper.Update", HotbarKeybindHelper.Update);
	}

	private static void CleanupUpdate()
	{
		if (DataCenter.SystemWarnings.Count > 0)
		{
			var expiry = DateTime.Now - TimeSpan.FromMinutes(10);
			_expiredWarnings.Clear();
			foreach (var kvp in DataCenter.SystemWarnings)
			{
				if (kvp.Value < expiry)
				{
					_expiredWarnings.Add(kvp.Key);
				}
			}
			foreach (var key in _expiredWarnings)
			{
				_ = DataCenter.SystemWarnings.Remove(key);
			}
		}

		if (DataCenter.VfxDataQueue.IsEmpty)
		{
			return;
		}

		_vfxRemaining.Clear();
		while (DataCenter.VfxDataQueue.TryDequeue(out var vfx))
		{
			try
			{
				var keepFor = vfx.Duration >= 0.5f ? vfx.Duration : 5.0;
				if (vfx.TimeDuration.TotalSeconds <= keepFor)
				{
					_vfxRemaining.Add(vfx);
				}
			}
			catch
			{
				_vfxRemaining.Add(vfx);
			}
		}

		foreach (var item in _vfxRemaining)
		{
			DataCenter.VfxDataQueue.Enqueue(item);
		}
	}

	private static void StateUpdate()
	{
		Run("RotationAndStateUpdate", RSCommands.UpdateRotationState);

		if (Service.Config.TeachingMode)
		{
			Run("HotbarHighlightManager.UpdateSettings", HotbarHighlightManager.UpdateSettings);
		}
	}

	private static void MiscAndTargetFreelyUpdate()
	{
		MiscUpdater.UpdateMisc();

		if (!DataCenter.TargetFreelyEnabled || DataCenter.IsPvP || !DataCenter.State || !DataCenter.InCombat
			|| ActionUpdater.NextAction != null || Player.Object == null || Svc.Targets.Target != null)
		{
			return;
		}

		IBattleChara? closestEnemy = null;
		var minDistance = float.MaxValue;

		foreach (var enemy in DataCenter.AllHostileTargets)
		{
			if (enemy == null || !enemy.IsEnemy() || enemy == Player.Object)
			{
				continue;
			}

			var distance = Vector3.Distance(Player.Object.Position, enemy.Position);
			if (distance < minDistance)
			{
				minDistance = distance;
				closestEnemy = enemy;
			}
		}

		if (closestEnemy == null)
		{
			return;
		}

		if (!Service.Config.TargetDelayEnable)
		{
			Svc.Targets.Target = closestEnemy;
		}
		else
		{
			RSCommands.SetTargetWithDelay(closestEnemy);
		}
	}

	private static void Run(string context, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			LogOnce(context + " Exception", ex);
		}
	}

	private static void LogOnce(string context, Exception ex)
	{
		var key = $"{context}|{ex.GetType().FullName}|{ex.Message}";
		if (_loggedErrors.Count >= MaxLoggedErrors)
		{
			_loggedErrors.Clear();
		}

		if (!_loggedErrors.Add(key))
		{
			return;
		}

		PluginLog.Error($"{context}: {ex}");
		if (Service.Config.InDebug)
		{
			_ = BasicWarningHelper.AddSystemWarning(context);
		}
	}

	public static void Dispose()
	{
		Svc.Framework.Update -= OnUpdate;

		MiscUpdater.Dispose();
		ActionUpdater.ClearNextAction();
		AutoAttackUpdater.Disable();
	}
}
