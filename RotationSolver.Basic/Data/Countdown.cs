using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Runtime.InteropServices;

namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents a countdown timer.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct Countdown
{
	/// <summary>
	/// 
	/// </summary>
	public static bool CountdownActive => AgentCountDownSettingDialog.Instance()->Active;

	/// <summary>
	/// Gets the remaining time of the countdown.
	/// </summary>
	public static float TimeRemaining => CountdownActive ? MathF.Max(0f, AgentCountDownSettingDialog.Instance()->TimeRemaining) : 0f;

	//public static float TimeRemaining
	//{
	//	get
	//	{
	//		var inst = Instance;
	//		if (inst == null)
	//		{
	//			return 0;
	//		}

	//		var remainingTime = inst->Active != 0 ? inst->Timer : 0;
	//		return remainingTime;
	//	}
	//}
}