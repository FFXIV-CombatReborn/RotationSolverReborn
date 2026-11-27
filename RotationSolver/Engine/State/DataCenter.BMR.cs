using ECommons.DalamudServices;
using ECommons.Logging;
using RotationSolver.Integrations.IPC;

namespace RotationSolver.Engine.State;

internal static partial class DataCenter
{
	#region BossModReborn Timeline Integration
	private static readonly FrameCached<bool> _bmrEnabled = new(() => IsPluginLoaded("BossModReborn", "BossModReborn"), 1000);
	private static readonly FrameCached<bool> _autoPvpSeriesGrindEnabled = new(() => IsPluginLoaded("Auto PVP Series Grind", "AutoPvpSeriesGrind"), 1000);

	public static bool BMREnabled => _bmrEnabled.Value;

	public static bool AutoPvpSeriesGrindEnabled => _autoPvpSeriesGrindEnabled.Value;

	private static bool IsPluginLoaded(string name, string internalName)
	{
		foreach (var x in Svc.PluginInterface.InstalledPlugins)
		{
			if ((x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.InternalName.Equals(internalName, StringComparison.OrdinalIgnoreCase)) && x.IsLoaded)
			{
				return true;
			}
		}

		return false;
	}
	public static bool PvPAutomationBlocked => IsPvP && AutoPvpSeriesGrindEnabled;

	public static bool BMRHasActiveModule { get; set; }
	public static string? BMRActiveModuleName { get; set; }
	public static float BMRNextRaidwideIn { get; set; } = float.MaxValue;
	public static float BMRNextTankbusterIn { get; set; } = float.MaxValue;
	public static float BMRNextKnockbackIn { get; set; } = float.MaxValue;
	public static float BMRNextDowntimeIn { get; set; } = float.MaxValue;
	public static float BMRNextDowntimeEndIn { get; set; } = float.MaxValue;
	public static float BMRNextVulnerableIn { get; set; } = float.MaxValue;
	public static float BMRNextVulnerableEndIn { get; set; } = float.MaxValue;
	public static float BMRNextDamageIn { get; set; } = float.MaxValue;
	public static PredictedDamageType BMRNextDamageType { get; set; } = PredictedDamageType.None;
	public static float BMRSpecialModeIn { get; set; } = float.MaxValue;
	public static SpecialMode BMRSpecialModeType { get; set; } = SpecialMode.Normal;

	/// <summary>
	/// True if BossMod's boss module AI hints are requesting the current cast be cancelled.
	/// </summary>
	public static bool BMRForceCancelCast { get; set; }

	/// <summary>
	/// True if BossMod's AI controller is requesting the current cast be cancelled.
	/// </summary>
	public static bool BMRForceCancelCastAI { get; set; }

	/// <summary>
	/// True if BossMod is moving.
	/// </summary>
	public static bool BMRIsMoving { get; set; }
	public static float BMRDebugTimelineRaidwide { get; set; } = float.MaxValue;
	public static float BMRDebugTimelineTankbuster { get; set; } = float.MaxValue;
	public static float BMRDebugHintsRaidwide { get; set; } = float.MaxValue;
	public static float BMRDebugHintsTankbuster { get; set; } = float.MaxValue;
	public static float BMRDebugGenericDamageIn { get; set; } = float.MaxValue;
	public static int BMRDebugGenericDamageType { get; set; }
	public static bool BMRDebugTimelineRwFunc { get; set; }
	public static bool BMRDebugTimelineTbFunc { get; set; }
	public static bool BMRDebugHintsRwFunc { get; set; }
	public static bool BMRDebugHintsTbFunc { get; set; }

	/// <summary>
	/// Delegate wired up by BossModUpdater to the <c>Hints.IsPositionSafe</c> IPC endpoint.
	/// When null, BossModReborn is not available and all positions are considered safe.
	/// </summary>
	public static Func<Vector3, bool>? BMRIsPositionSafe { get; set; }

	/// <summary>
	/// Delegate wired up by BossModUpdater to the <c>Hints.IsDashSafe</c> IPC endpoint.
	/// When null, BossModReborn is not available and all dashes are considered safe.
	/// </summary>
	public static Func<Vector3, Vector3, bool>? BMRIsDashSafe { get; set; }

	/// <summary>
	///
	/// </summary>
	public static Func<float, bool, bool>? BMRIsFixedDashSafe { get; set; }

	/// <summary>
	/// The most recently polled set of upcoming planned actions from BossMod's Cooldown Planner,
	/// wired up by BMRPlanUpdater. Empty when no plan is active or BossModReborn is unavailable.
	/// </summary>
	public static List<BMRPlannedAction> BMRPlannedActions { get; set; } = [];

	/// <summary>
	/// Returns the currently active planned action (if any) matching the given adjusted action id,
	/// i.e. one whose activation window has started (ActivationIn &lt;= 0) and not yet ended
	/// (WindowEndIn &gt; 0). Only entries resolved to a concrete game action (ActionType == 1) are considered.
	/// </summary>
	public static BMRPlannedAction? GetActivePlannedAction(uint actionId)
	{
		var actions = BMRPlannedActions;
		for (var i = 0; i < actions.Count; i++)
		{
			var action = actions[i];
			if (action.ActionId == actionId && action.ActivationIn <= 0f && action.WindowEndIn > 0f)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"GetActivePlannedAction: Found active planned action {action.ActionId} with ActivationIn {action.ActivationIn} and WindowEndIn {action.WindowEndIn}");
				}

				return action;
			}
		}

		return null;
	}

	/// <summary>
	/// Returns true if the destination is safe to move to, or if BossModReborn IPC is unavailable.
	/// </summary>
	public static bool IsMovementDestinationSafe(Vector3 destination)
	{
		if (BMRIsPositionSafe == null)
		{
			return true;
		}

		try
		{ return BMRIsPositionSafe(destination); }
		catch { return true; }
	}

	/// <summary>
	/// Returns true if the dash from <paramref name="from"/> to <paramref name="to"/> is safe,
	/// or if BossModReborn IPC is unavailable.
	/// </summary>
	public static bool IsDashSafe(Vector3 from, Vector3 to)
	{
		if (BMRIsDashSafe == null)
		{
			return true;
		}

		try
		{ return BMRIsDashSafe(from, to); }
		catch { return true; }
	}

	/// <summary>
	///
	/// </summary>
	public static bool IsFixedDashSafe(float range, bool backwards)
	{
		if (BMRIsFixedDashSafe == null)
		{
			return true;
		}

		try
		{ return BMRIsFixedDashSafe(range, backwards); }
		catch { return true; }
	}
	public static void ResetBmrData()
	{
		BMRHasActiveModule = false;
		BMRActiveModuleName = null;
		BMRNextRaidwideIn = float.MaxValue;
		BMRNextTankbusterIn = float.MaxValue;
		BMRNextKnockbackIn = float.MaxValue;
		BMRNextDowntimeIn = float.MaxValue;
		BMRNextDowntimeEndIn = float.MaxValue;
		BMRNextVulnerableIn = float.MaxValue;
		BMRNextVulnerableEndIn = float.MaxValue;
		BMRNextDamageIn = float.MaxValue;
		BMRNextDamageType = 0;
		BMRSpecialModeIn = float.MaxValue;
		BMRSpecialModeType = 0;
		BMRForceCancelCast = false;
		BMRForceCancelCastAI = false;
		BMRIsMoving = false;
		BMRDebugTimelineRaidwide = float.MaxValue;
		BMRDebugTimelineTankbuster = float.MaxValue;
		BMRDebugHintsRaidwide = float.MaxValue;
		BMRDebugHintsTankbuster = float.MaxValue;
		BMRDebugGenericDamageIn = float.MaxValue;
		BMRDebugGenericDamageType = 0;
		BMRDebugTimelineRwFunc = false;
		BMRDebugTimelineTbFunc = false;
		BMRDebugHintsRwFunc = false;
		BMRDebugHintsTbFunc = false;
		BMRIsPositionSafe = null;
		BMRIsDashSafe = null;
		BMRIsFixedDashSafe = null;
	}

	/// <summary>
	/// Clears any cached Cooldown Planner data (called when the feature is disabled or BMR becomes unavailable).
	/// </summary>
	public static void ResetBmrPlanData()
	{
		BMRPlannedActions = [];
	}
	#endregion

	#region BossModReborn Crucible Kinships
	private const long BmrCrucibleOpponentsRefreshMs = 10000;
	private const long BmrActiveModuleTypeRefreshMs = 1000;

	private static List<BMRCrucibleOpponent> _bmrCrucibleOpponents = [];
	private static long _bmrCrucibleOpponentsTick = -BmrCrucibleOpponentsRefreshMs;
	private static string? _bmrActiveModuleType;
	private static long _bmrActiveModuleTypeTick = -BmrActiveModuleTypeRefreshMs;

	/// <summary>
	///
	/// </summary>
	public static IReadOnlyList<BMRCrucibleOpponent> BMRCrucibleOpponents
	{
		get
		{
			var now = Environment.TickCount64;
			if (now - _bmrCrucibleOpponentsTick >= BmrCrucibleOpponentsRefreshMs)
			{
				_bmrCrucibleOpponentsTick = now;
				_bmrCrucibleOpponents = BMRCrucible_IPCSubscriber.IsEnabled ? BMRCrucible_IPCSubscriber.GetCrucibleOpponents() : [];
			}

			return _bmrCrucibleOpponents;
		}
	}

	/// <summary>
	///
	/// </summary>
	public static string? BMRActiveModuleType
	{
		get
		{
			var now = Environment.TickCount64;
			if (now - _bmrActiveModuleTypeTick >= BmrActiveModuleTypeRefreshMs)
			{
				_bmrActiveModuleTypeTick = now;
				try
				{
					_bmrActiveModuleType = BMRCrucible_IPCSubscriber.IsEnabled ? BMRCrucible_IPCSubscriber.ActiveModuleType?.Invoke() : null;
				}
				catch
				{
					_bmrActiveModuleType = null;
				}
			}

			return _bmrActiveModuleType;
		}
	}
	#endregion
}
