using ECommons.EzIpcManager;
using ECommons.Logging;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace RotationSolver.Integrations.IPC;

internal static class BMRCrucible_IPCSubscriber
{
	private static readonly EzIPCDisposalToken[] _disposalTokens =
		EzIPC.Init(typeof(BMRCrucible_IPCSubscriber), "BossMod", SafeWrapper.AnyException);

	internal static bool IsEnabled => IPCSubscriber_Common.IsReady("BossModReborn");

	[EzIPC("Crucible.PreferredKinships", true)]
	internal static readonly Func<string>? PreferredKinships;

	[EzIPC("ActiveModuleType", true)]
	internal static readonly Func<string?>? ActiveModuleType;

	internal static List<BMRCrucibleOpponent> GetCrucibleOpponents()
	{
		var func = PreferredKinships;
		if (func == null)
		{
			return [];
		}

		try
		{
			var json = func();
			return string.IsNullOrEmpty(json) ? [] : System.Text.Json.JsonSerializer.Deserialize<List<BMRCrucibleOpponent>>(json) ?? [];
		}
		catch (Exception ex)
		{
			PluginLog.LogDebug($"[BMRCrucible_IPCSubscriber] Failed to read Crucible.PreferredKinships: {ex}");
			return [];
		}
	}

	internal static void Dispose() => IPCSubscriber_Common.DisposeAll(_disposalTokens);
}
