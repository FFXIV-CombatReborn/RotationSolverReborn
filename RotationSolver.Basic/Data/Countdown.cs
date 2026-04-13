using FFXIVClientStructs.FFXIV.Client.System.Framework;
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
	/// The timer value.
	/// </summary>
	[FieldOffset(0x28)] public float Timer;

	/// <summary>
	/// Indicates whether the countdown is active.
	/// </summary>
	[FieldOffset(0x38)] public byte Active;

	/// <summary>
	/// The initiator of the countdown.
	/// </summary>
	[FieldOffset(0x3C)] public uint Initiator;

	/// <summary>
	/// Gets the instance of the countdown struct.
	/// </summary>
	public static unsafe Countdown* Instance
	{
		get
		{
			Countdown* instance = (Countdown*)Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.CountDownSettingDialog);
			return instance == null ? throw new InvalidOperationException("Countdown instance is null.") : instance;
		}
	}

	/// <summary>
	/// Gets the remaining time of the countdown.
	/// </summary>
	public static float TimeRemaining
	{
		get
		{
			Countdown* inst = Instance;
			if (inst == null)
			{
				return 0;
			}

			float remainingTime = inst->Active != 0 ? inst->Timer : 0;
			return remainingTime;
		}
	}
}