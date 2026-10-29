using Dalamud.Game.Command;
using ECommons.DalamudServices;
using RotationSolver.Data;

namespace RotationSolver.Commands
{
	public static partial class RSCommands
	{
		internal static void Enable()
		{
			DataCenter.OnSpecialTypeChanged = specialType => _specialString = specialType.ToSpecialString();

			var helpMessage = $"{UiString.Commands_Rotation.GetDescription()}\n"
				+ $"{Service.COMMAND} Auto → {UiString.Commands_Start.GetDescription()}\n"
				+ $"{Service.COMMAND} Off → {UiString.Commands_Off.GetDescription()}";

			_ = Svc.Commands.AddHandler(Service.COMMAND, new CommandInfo(OnCommand)
			{
				HelpMessage = helpMessage,
				ShowInHelp = true,
			});
			_ = Svc.Commands.AddHandler(Service.ALTCOMMAND, new CommandInfo(OnCommand)
			{
				HelpMessage = $"{UiString.Commands_Rotation.GetDescription()}\n",
				ShowInHelp = true,
			});
		}

		internal static void Disable()
		{
			_ = Svc.Commands.RemoveHandler(Service.COMMAND);
			_ = Svc.Commands.RemoveHandler(Service.ALTCOMMAND);
		}

		private static void OnCommand(string command, string arguments)
		{
			DoOneCommand(arguments ?? string.Empty);
		}

		private static void DoOneCommand(string command)
		{
			command = (command ?? string.Empty).Trim();

			if (command.Length == 0)
			{
				RotationSolverPlugin.OpenConfigWindow();
				return;
			}

			if (string.Equals(command, "cancel", StringComparison.OrdinalIgnoreCase))
			{
				command = "off";
			}

			if (TryGetOneEnum<StateCommandType>(command, out var stateType))
			{
				var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				var index = -1;

				if (parts.Length > 1)
				{
					var value = parts[1];
					if (Enum.TryParse(typeof(TargetingType), value, true, out var parsedEnumSet))
					{
						var targetingTypeSet = (TargetingType)parsedEnumSet;
						var idx = Service.Config.TargetingTypes.IndexOf(targetingTypeSet);
						if (idx >= 0)
						{
							Service.Config.TargetingIndex = idx;
							if (Service.Config.ShowToggledSettingInChat)
							{
								Svc.Chat.Print($"Set current TargetingType to {targetingTypeSet}.");
							}
							index = idx;
						}
						else
						{
							Svc.Chat.PrintError($"{targetingTypeSet} is not in TargetingTypes list.");
							return;
						}
					}
					else if (!int.TryParse(value, out index))
					{
						index = -1;
					}
				}

				DoStateCommandType(stateType, index);
			}
			else if (TryGetOneEnum<SpecialCommandType>(command, out var specialType))
			{
				DoSpecialCommandType(specialType);
			}
			else if (TryGetOneEnum<OtherCommandType>(command, out var otherType))
			{
				var extraCommand = command[otherType.ToString().Length..].Trim();
				DoOtherCommand(otherType, extraCommand);
			}
			else
			{
				RotationSolverPlugin.OpenConfigWindow();
			}
		}

		private static bool TryGetOneEnum<T>(string command, out T type) where T : struct, Enum
		{
			type = default;

			if (string.IsNullOrWhiteSpace(command))
			{
				return false;
			}

			var spaceIdx = command.IndexOf(' ');
			var token = spaceIdx >= 0 ? command[..spaceIdx] : command;

			return token.Length > 0 && char.IsLetter(token[0]) && !token.Contains(',')
				&& Enum.TryParse(token, ignoreCase: true, out type) && Enum.IsDefined(type);
		}

		internal static string GetCommandStr(this Enum command, string extraCommand = "")
		{
			var cmdStr = $"{Service.COMMAND} {command}";
			if (!string.IsNullOrEmpty(extraCommand))
			{
				cmdStr += $" {extraCommand}";
			}
			return cmdStr;
		}
	}
}