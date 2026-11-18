using Lumina.Excel.Sheets;

namespace RotationSolver.Rotations;

public partial class CustomRotation
{
	#region Territory Info

	/// <summary>
	/// Type of the content player is in.
	/// </summary>
	protected static TerritoryContentType TerritoryContentType => DataCenter.Territory?.ContentType ?? TerritoryContentType.None;

	/// <summary>
	/// Is player in high-end duty, savage, extrene or ultimate.
	/// </summary>
	[Description("Is in the high-end duty")]
	public static bool IsInHighEndDuty => DataCenter.Territory?.IsHighEndDuty ?? false;

	/// <summary>
	/// Is player in a normal or chaotic Alliance Raid.
	/// </summary>
	[Description("Is in an Alliance Raid (including Chaotic)")]
	public static bool IsInAllianceRaid => DataCenter.IsInAllianceRaid;

	/// <summary>
	/// Is player in UCoB duty.
	/// </summary>
	[Description("Is in UCoB duty")]
	public static bool IsInUCoB => DataCenter.IsInUCoB;

	/// <summary>
	/// Is player in UwU duty.
	/// </summary>
	[Description("Is in UwU duty")]
	public static bool IsInUwU => DataCenter.IsInUwU;

	/// <summary>
	/// Is player in TEA duty.
	/// </summary>
	[Description("Is in TEA duty")]
	public static bool IsInTEA => DataCenter.IsInTEA;

	/// <summary>
	/// Is player in DSR duty.
	/// </summary>
	[Description("Is in DSR duty")]
	public static bool IsInDSR => DataCenter.IsInDSR;

	/// <summary>
	/// Is player in TOP duty.
	/// </summary>
	[Description("Is in TOP duty")]
	public static bool IsInTOP => DataCenter.IsInTOP;

	///<summary>
	/// Is player in FRU duty.
	///</summary>
	[Description("Is in FRU duty")]
	public static bool IsInFRU => DataCenter.IsInFRU;

	///<summary>
	/// Is player in DMU duty.
	///</summary>
	[Description("Is in DMU duty")]
	public static bool IsInDMU => DataCenter.IsInDMU;

	///<summary>
	/// Is player in COD duty.
	///</summary>
	[Description("Is in COD duty")]
	public static bool IsInCOD => DataCenter.IsInCOD;

	/// <summary>
	/// Is player in any instanced duty.
	/// </summary>
	[Description("Is player in duty")]
	public static bool IsInDuty => DataCenter.IsInDuty;

	/// <summary>
	/// Is in specified territory.
	/// </summary>
	/// <param name="territoryId">The ID of the territory to check.</param>
	/// <returns>True if the player is in the specified territory; otherwise, false.</returns>
	[Description("Is in specified territory")]
	public static bool IsInTerritory(ushort territoryId) => DataCenter.IsInTerritory(territoryId);

	#endregion

	/// <summary>
	/// Average amount of times a rotation calls IsLastGCD, IsLastAbility, or IsLastAction.
	/// </summary>
	public double AverageCountOfLastUsing { get; internal set; } = 0;

	/// <summary>
	/// Max amount of times a rotation calls IsLastGCD, IsLastAbility, or IsLastAction.
	/// </summary>
	public int MaxCountOfLastUsing { get; internal set; } = 0;

	/// <summary>
	/// The average count of not recommend members using.
	/// </summary>
	public double AverageCountOfCombatTimeUsing { get; internal set; } = 0;

	/// <summary>
	/// The max count of not recommend members using.
	/// </summary>
	public int MaxCountOfCombatTimeUsing { get; internal set; } = 0;
	internal long CountOfTracking { get; set; } = 0;

	internal static int CountingOfLastUsing { get; set; } = 0;
	internal static int CountingOfCombatTimeUsing { get; set; } = 0;

	/// <summary>
	///  The actions that were used by player successfully. The first one is the latest successfully used one.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	protected static ActionRec[] RecordActions
	{
		get
		{
			CountingOfLastUsing++;
			return DataCenter.RecordActions;
		}
	}

	/// <summary>
	/// How much time has passed since the last action was used.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	public static TimeSpan TimeSinceLastAction
	{
		get
		{
			CountingOfLastUsing++;
			return DataCenter.TimeSinceLastAction;
		}
	}

	/// <summary>
	/// Last used GCD.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="isAdjust">Check for adjust id not raw id.</param>
	/// <param name="actions">True if any of this is matched.</param>
	/// <returns></returns>
	[Description("Just used GCD")]
	public static bool IsLastGCD(bool isAdjust, params ReadOnlySpan<IAction> actions)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastGCD(isAdjust, actions);
	}

	/// <summary>
	/// Last used GCD.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="ids">True if any of this is matched.</param>
	/// <returns></returns>
	public static bool IsLastGCD(params ReadOnlySpan<ActionID> ids)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastGCD(ids);
	}

	/// <summary>
	/// Last used Ability.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="isAdjust">Check for adjust id not raw id.</param>
	/// <param name="actions">True if any of this is matched.</param>
	/// <returns></returns>
	[Description("Just used Ability")]
	public static bool IsLastAbility(bool isAdjust, params ReadOnlySpan<IAction> actions)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastAbility(isAdjust, actions);
	}

	/// <summary>
	/// Last used Ability.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="ids">True if any of this is matched.</param>
	/// <returns></returns>
	public static bool IsLastAbility(params ReadOnlySpan<ActionID> ids)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastAbility(ids);
	}

	/// <summary>
	/// Last used Action.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="isAdjust">Check for adjust id not raw id.</param>
	/// <param name="actions">True if any of this is matched.</param>
	/// <returns></returns>
	[Description("Just used Action")]
	public static bool IsLastAction(bool isAdjust, params ReadOnlySpan<IAction> actions)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastAction(isAdjust, actions);
	}

	/// <summary>
	/// Last used Action.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="ids">True if any of this is matched.</param>
	/// <returns></returns>
	public static bool IsLastAction(params ReadOnlySpan<ActionID> ids)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastAction(ids);
	}

	/// <summary>
	/// Returns the number of seconds since the last action was changed, capped at 10 seconds.
	/// </summary>
	public static float TimeSinceLastActionCapped
	{
		get
		{
			var seconds = (float)TimeSinceLastAction.TotalSeconds;
			return Math.Min(seconds, 10f);
		}
	}

	/// <summary>
	/// Last used Combo Action.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="isAdjust">Check for adjust id not raw id.</param>
	/// <param name="actions">True if any of this is matched.</param>
	/// <returns></returns>
	[Description("Just used Combo Action")]
	public static bool IsLastComboAction(bool isAdjust, params ReadOnlySpan<IAction> actions)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastComboAction(isAdjust, actions);
	}

	/// <summary>
	/// Last used Combo Action.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="ids">True if any of this is matched.</param>
	/// <returns></returns>
	public static bool IsLastComboAction(params ReadOnlySpan<ActionID> ids)
	{
		CountingOfLastUsing++;
		return IActionHelper.IsLastComboAction(ids);
	}

	/// <summary>
	///
	/// </summary>
	public static bool IsNoActionCombo()
	{
		return IActionHelper.IsNoActionCombo();
	}

	/// <summary>
	/// Have you already weaved an oGCD.
	/// </summary>
	public static bool HasWeaved()
	{
		return IActionHelper.IsLastActionAbility();
	}

	/// <summary>
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="GCD"></param>
	/// <returns></returns>
	protected static bool CombatElapsedLessGCD(int GCD)
	{
		CountingOfCombatTimeUsing++;
		return CombatElapsedLess(GCD * DataCenter.DefaultGCDTotal);
	}

	/// <summary>
	/// Whether the battle lasted less than <paramref name="time"/> seconds
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="time">time in second.</param>
	/// <returns></returns>
	protected static bool CombatElapsedLess(float time)
	{
		CountingOfCombatTimeUsing++;
		return CombatTime <= time;
	}

	/// <summary>
	/// How long combat has been going.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	[Description("Combat time")]
	public static float CombatTime
	{
		get
		{
			CountingOfCombatTimeUsing++;
			return InCombat ? DataCenter.CombatTimeRaw + DataCenter.DefaultGCDRemain : 0;
		}
	}

	/// <summary>
	/// How long is remaining on the Combo Timer.
	/// <br>WARNING: Do not make this method the main logic of your rotation.</br>
	/// </summary>
	[Description("Combo time")]
	public static float LiveComboTime
	{
		get
		{
			try
			{
				return DataCenter.ComboTime;
			}
			catch (Exception)
			{
				return 0f;
			}
		}
	}

	/// <summary>
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="GCD"></param>
	/// <returns></returns>
	protected static bool StopMovingElapsedLessGCD(int GCD)
	{
		return StopMovingElapsedLess(GCD * DataCenter.DefaultGCDTotal);
	}

	/// <summary>
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="time">time in second.</param>
	/// <returns></returns>
	protected static bool StopMovingElapsedLess(float time)
	{
		return StopMovingTime <= time;
	}

	/// <summary>
	/// How long the player has been standing still.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	[Description("Stop moving time")]
	public static float StopMovingTime => IsMoving ? 0 : DataCenter.StopMovingRaw + DataCenter.DefaultGCDRemain;

	/// <summary>
	/// How long the player has been moving.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	[Description("Moving time")]
	public static float MovingTime => IsMoving ? DataCenter.MovingRaw + DataCenter.DefaultGCDRemain : 0;
	/// <summary>
	/// How long the player has been alive.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	[Description("How long the player has been alive.")]
	public static float AliveTime => ObjectHelper.PlayerIsAlive() ? DataCenter.AliveTimeRaw + DataCenter.DefaultGCDRemain : 0;

	/// <summary>
	/// How long the player has been dead.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	[Description("How long the player has been dead.")]
	public static float DeadTime => ObjectHelper.PlayerIsAlive() ? 0 : DataCenter.DeadTimeRaw + DataCenter.DefaultGCDRemain;

	/// <summary>
	/// Time from GCD.
	/// </summary>
	/// <param name="gcdCount"></param>
	/// <param name="offset"></param>
	/// <returns></returns>
	protected static float GCDTime(uint gcdCount = 0, float offset = 0)
	{
		return DataCenter.GCDTime(gcdCount, offset);
	}
}
