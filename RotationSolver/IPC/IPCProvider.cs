using ECommons.EzIpcManager;
using ECommons.Logging;
using RotationSolver.Commands;
using RotationSolver.Updaters;

namespace RotationSolver.IPC
{
	internal class IPCProvider
	{
		internal IPCProvider()
		{
			_ = EzIPC.Init(this, prefix: "RotationSolverReborn");
		}

		[EzIPC]
		public void Test(string param)
		{
			PluginLog.Debug($"IPC Test! Param:{param}");
		}

		[EzIPC]
		public void AddPriorityNameID(uint nameId)
		{
			if (DataCenter.PrioritizedNameIds != null)
			{
				DataCenter.PrioritizedNameIds.Add(nameId);
				PluginLog.Debug($"IPC AddPriorityNameID was called. NameID:{nameId}");
			}
			else
			{
				PluginLog.Error("DataCenter.PrioritizedNameIds is null.");
			}
		}

		[EzIPC]
		public void RemovePriorityNameID(uint nameId)
		{
			if (DataCenter.PrioritizedNameIds != null)
			{
				if (DataCenter.PrioritizedNameIds.Contains(nameId))
				{
					_ = DataCenter.PrioritizedNameIds.Remove(nameId);
					PluginLog.Debug($"IPC RemovePriorityNameID was called. NameID:{nameId}");
				}
				else
				{
					PluginLog.Warning($"IPC RemovePriorityNameID was called but NameID:{nameId} was not found.");
				}
			}
			else
			{
				PluginLog.Error("DataCenter.PrioritizedNameIds is null.");
			}
		}

		[EzIPC]
		public void AddBlacklistNameID(uint nameId)
		{
			if (DataCenter.BlacklistedNameIds != null)
			{
				DataCenter.BlacklistedNameIds.Add(nameId);
				PluginLog.Debug($"IPC AddBlacklistNameID was called. NameID:{nameId}");
			}
			else
			{
				PluginLog.Error("DataCenter.BlacklistedNameIds is null.");
			}
		}

		[EzIPC]
		public void RemoveBlacklistNameID(uint nameId)
		{
			if (DataCenter.BlacklistedNameIds != null)
			{
				if (DataCenter.BlacklistedNameIds.Contains(nameId))
				{
					_ = DataCenter.BlacklistedNameIds.Remove(nameId);
					PluginLog.Debug($"IPC RemoveBlacklistNameID was called. NameID:{nameId}");
				}
				else
				{
					PluginLog.Warning($"IPC RemoveBlacklistNameID was called but NameID:{nameId} was not found.");
				}
			}
			else
			{
				PluginLog.Error("DataCenter.BlacklistedNameIds is null.");
			}
		}

		[EzIPC]
		public void ChangeOperatingMode(StateCommandType stateCommand)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.UpdateState(stateCommand, DataCenter.Role);
			}
			PluginLog.Debug($"IPC ChangeOperatingMode was called. StateCommand:{stateCommand}");
		}

		[EzIPC]
		public void ChangeOperatingModeWithOverrides(StateCommandType stateCommand, TargetHostileType targetHostileType, SettingOverride targetFreely, SettingOverride autoOffAfterCombat, SettingOverride friendlyPartyNpcHealRaise)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.UpdateState(stateCommand, DataCenter.Role, CreateOverrides(targetHostileType, targetFreely, autoOffAfterCombat, friendlyPartyNpcHealRaise));
			}
			PluginLog.Debug($"IPC ChangeOperatingModeWithOverrides was called. StateCommand:{stateCommand} TargetHostileType:{targetHostileType} TargetFreely:{targetFreely} AutoOffAfterCombat:{autoOffAfterCombat} FriendlyPartyNpcHealRaise:{friendlyPartyNpcHealRaise}");
		}

		[EzIPC]
		public void AutodutyChangeOperatingMode(StateCommandType stateCommand, TargetingType targetingType)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.AutodutyUpdateState(stateCommand, DataCenter.Role, targetingType);
			}
			PluginLog.Debug($"IPC AutodutyChangeOperatingMode was called. StateCommand:{stateCommand} TargetingType:{targetingType}");
		}

		[EzIPC]
		public void AutodutyChangeOperatingModeWithOverrides(StateCommandType stateCommand, TargetingType targetingType, TargetHostileType targetHostileType, SettingOverride targetFreely, SettingOverride autoOffAfterCombat, SettingOverride friendlyPartyNpcHealRaise)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.AutodutyUpdateState(stateCommand, DataCenter.Role, targetingType, CreateOverrides(targetHostileType, targetFreely, autoOffAfterCombat, friendlyPartyNpcHealRaise));
			}
			PluginLog.Debug($"IPC AutodutyChangeOperatingModeWithOverrides was called. StateCommand:{stateCommand} TargetingType:{targetingType} TargetHostileType:{targetHostileType} TargetFreely:{targetFreely} AutoOffAfterCombat:{autoOffAfterCombat} FriendlyPartyNpcHealRaise:{friendlyPartyNpcHealRaise}");
		}

		private static IpcStateOverrides CreateOverrides(TargetHostileType targetHostileType, SettingOverride targetFreely, SettingOverride autoOffAfterCombat, SettingOverride friendlyPartyNpcHealRaise)
		{
			return new(targetHostileType, ToOverrideValue(targetFreely), ToOverrideValue(autoOffAfterCombat), ToOverrideValue(friendlyPartyNpcHealRaise));
		}

		private static bool? ToOverrideValue(SettingOverride setting)
		{
			return setting switch
			{
				SettingOverride.ForceOn => true,
				SettingOverride.ForceOff => false,
				_ => null,
			};
		}

		[EzIPC]
		public void TriggerSpecialState(SpecialCommandType specialCommand)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.DoSpecialCommandType(specialCommand, false);
			}
			PluginLog.Debug($"IPC TriggerSpecialState was called. SpecialCommand:{specialCommand}");
		}

		[EzIPC]
		public void TriggerSpecialStateWithDuration(SpecialCommandType specialCommand, float duration)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.DoSpecialCommandType(specialCommand, false);
				DataCenter.SetSpecialTypeWithDuration(specialCommand, duration);
			}
			PluginLog.Debug($"IPC TriggerSpecialStateWithDuration was called. SpecialCommand:{specialCommand}, Duration:{duration}");
		}

		[EzIPC]
		public void OtherCommand(OtherCommandType otherType, string str)
		{
			if (!DataCenter.IsPvP)
			{
				RSCommands.DoOtherCommand(otherType, str);
			}
			PluginLog.Debug($"IPC DoOtherCommand was called. OtherCommandType:{otherType}, String:{str},");
		}

		[EzIPC]
		public void ActionCommand(string action, float time)
		{
			var combinedString = $"{action}-{time}";

			if (!DataCenter.IsPvP)
			{
				RSCommands.DoActionCommand($"{combinedString}");
			}
			PluginLog.Debug($"IPC ActionCommand was called. Action Name:{action}, Time:{time}");
		}

		[EzIPC]
		public void EnableTargetFreelyOverride()
		{
			DataCenter.TargetFreelyOverride = true;
			PluginLog.Debug("IPC EnableTargetFreelyOverride was called.");
		}

		[EzIPC]
		public void DisableTargetFreelyOverride()
		{
			DataCenter.TargetFreelyOverride = false;
			PluginLog.Debug("IPC DisableTargetFreelyOverride was called.");
		}

		[EzIPC]
		public byte GetDesiredPositional()
		{
			return (byte)ActionUpdater.DesiredPositional;
		}

		[EzIPC]
		public bool AutorotationActive()
		{
			return DataCenter.IsActivatedIPC();
		}
	}
}