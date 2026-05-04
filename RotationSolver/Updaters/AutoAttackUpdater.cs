using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Updaters
{
	internal static class AutoAttackUpdater
	{
		private static Hook<SetAutoAttackStateDelegate>? _setAutoAttackStateHook;

		private unsafe delegate bool SetAutoAttackStateDelegate(AutoAttackState* self, bool value, bool sendPacket, bool isInstant);

		public static unsafe void Enable()
		{
			try
			{
				var setAutoAttackStateAddress = AutoAttackState.Addresses.SetImpl.Value;
				_setAutoAttackStateHook = Svc.Hook.HookFromAddress<SetAutoAttackStateDelegate>(setAutoAttackStateAddress, SetAutoAttackStateDetour);
				_setAutoAttackStateHook?.Enable();

				PluginLog.Debug("[AutoAttackUpdater] Auto attack state hook initialized");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[AutoAttackUpdater] Failed to initialize auto attack hook: {ex}");
			}
		}

		public static void Disable()
		{
			try
			{
				_setAutoAttackStateHook?.Disable();
				_setAutoAttackStateHook?.Dispose();
				_setAutoAttackStateHook = null;

				PluginLog.Debug("[AutoAttackUpdater] Auto attack state hook disposed");
			}
			catch (Exception ex)
			{
				PluginLog.Error($"[AutoAttackUpdater] Failed to dispose auto attack hook: {ex}");
			}
		}

		/// <summary>
		/// Called every frame. If auto attacks are currently active but a NoCastingStatus is
		/// present, sends the toggle-auto-attack general action to disable them.
		/// </summary>
		public static unsafe void Update()
		{
			if (!Player.Available) return;

			try
			{
				var uiState = UIState.Instance();
				if (uiState == null) return;

				if (uiState->WeaponState.AutoAttackState.IsAutoAttacking && PlayerHasNoCastingStatus())
				{
					// GeneralAction 1 is the auto-attack toggle — same method the game uses
					ActionManager.Instance()->UseAction(ActionType.GeneralAction, 1);
					PluginLog.Information("[AutoAttackUpdater] Disabled active auto attacks due to NoCastingStatus.");
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[AutoAttackUpdater] Error in Update (auto attack disable): {ex.Message}");
			}
		}

		internal static bool PlayerHasNoCastingStatus()
		{
			try
			{
				HashSet<uint> noCastingStatus = OtherConfiguration.NoCastingStatus;
				if (noCastingStatus == null || noCastingStatus.Count == 0) return false;
				if (Player.Object?.StatusList == null) return false;

				// Check for Motion Tracker separately
				if (StatusHelper.PlayerHasStatus(false, StatusID.MotionTracker))
				{
					return true;
				}

				if (DataCenter.BMRSpecialModeType == SpecialMode.Pyretic)
				{
					return true;
				}

				foreach (var status in Player.Object.StatusList)
				{
					if (noCastingStatus.Contains(status.StatusId))
						return true;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"[AutoAttackUpdater] Error checking NoCastingStatus: {ex.Message}");
			}
			return false;
		}

		private static unsafe bool SetAutoAttackStateDetour(AutoAttackState* self, bool value, bool sendPacket, bool isInstant)
		{
			// Block attempts to enable auto attacks while a NoCastingStatus is active
			if (value && Player.Available && PlayerHasNoCastingStatus())
			{
				PluginLog.Debug("[AutoAttackUpdater] Prevented auto attack activation due to NoCastingStatus.");
				return true;
			}

			return _setAutoAttackStateHook!.Original(self, value, sendPacket, isInstant);
		}
	}
}
