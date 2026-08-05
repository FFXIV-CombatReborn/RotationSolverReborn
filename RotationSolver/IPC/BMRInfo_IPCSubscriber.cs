using ECommons.EzIpcManager;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace RotationSolver.IPC;

/// <summary>
/// Subscribes to BossMod's IPC endpoints that expose miscellaneous AIHints information not already
/// covered by <see cref="BMRTimeline_IPCSubscriber"/> or <see cref="BMRPlan_IPCSubscriber"/>, such as
/// the force-cancel-cast flags used to interrupt casts that are no longer safe/useful.
/// </summary>
internal static class BMRInfo_IPCSubscriber
{
	private static readonly EzIPCDisposalToken[] _disposalTokens =
		EzIPC.Init(typeof(BMRInfo_IPCSubscriber), "BossMod", SafeWrapper.AnyException);

	internal static bool IsEnabled => IPCSubscriber_Common.IsReady("BossModReborn");

	/// <summary>
	/// True if BossMod's boss module AI hints are requesting the current cast be cancelled.
	/// </summary>
	[EzIPC("Hints.ForceCancelCast", true)]
	internal static readonly Func<bool>? ForceCancelCast;

	/// <summary>
	/// True if BossMod's AI controller is requesting the current cast be cancelled.
	/// </summary>
	[EzIPC("Hints.ForceCancelCastAI", true)]
	internal static readonly Func<bool>? ForceCancelCastAI;

	/// <summary>
	/// True if BossMod's AI controller is navigating.
	/// </summary>
	[EzIPC("AI.IsNavigating", true)]
	internal static readonly Func<bool>? IsNavigating;

	/// <summary>
	/// True if BossMod is moving.
	/// </summary>
	[EzIPC("Movement.IsMoving", true)]
	internal static readonly Func<bool>? IsMoving;

	internal static void Dispose() => IPCSubscriber_Common.DisposeAll(_disposalTokens);
}
