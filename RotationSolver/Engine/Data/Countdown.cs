using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Runtime.InteropServices;

namespace RotationSolver.Engine.Data;

/// <summary>
/// Represents a countdown timer.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct Countdown
{
	/// <summary>
	/// 
	/// </summary>
	public static bool CountdownActive
	{
		get
		{
			var dialog = AgentCountDownSettingDialog.Instance();
			return dialog != null && dialog->Active;
		}
	}

	/// <summary>
	/// Gets the remaining time of the countdown.
	/// </summary>
	public static float TimeRemaining
	{
		get
		{
			var dialog = AgentCountDownSettingDialog.Instance();
			return dialog != null && dialog->Active ? MathF.Max(0f, dialog->TimeRemaining) : 0f;
		}
	}
}