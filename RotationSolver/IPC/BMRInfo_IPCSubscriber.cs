using ECommons.EzIpcManager;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace RotationSolver.IPC;

internal static class BMRInfo_IPCSubscriber
{
	private static readonly EzIPCDisposalToken[] _disposalTokens =
		EzIPC.Init(typeof(BMRInfo_IPCSubscriber), "BossMod", SafeWrapper.AnyException);

	internal static bool IsEnabled => IPCSubscriber_Common.IsReady("BossModReborn");

	[EzIPC("Hints.ForceCancelCastMechanic", true)]
	internal static readonly Func<bool>? ForceCancelCast;

	[EzIPC("Hints.ForceCancelCastMechanicAI", true)]
	internal static readonly Func<bool>? ForceCancelCastAI;

	[EzIPC("AI.IsNavigating", true)]
	internal static readonly Func<bool>? IsNavigating;

	[EzIPC("Movement.IsMoving", true)]
	internal static readonly Func<bool>? IsMoving;

	internal static void Dispose() => IPCSubscriber_Common.DisposeAll(_disposalTokens);
}
