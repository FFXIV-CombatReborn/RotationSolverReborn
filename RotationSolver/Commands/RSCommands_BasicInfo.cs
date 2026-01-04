using Dalamud.Game.Command;
using ECommons.DalamudServices;
using RotationSolver.Data;

namespace RotationSolver.Commands
{
    public static partial class RSCommands
    {
        internal static void Enable()
        {
            _ = Svc.Commands.AddHandler(Service.COMMAND, new CommandInfo(OnCommand)
            {
                HelpMessage = UiString.Commands_Rotation.GetDescription(),
                ShowInHelp = true,
            });
            _ = Svc.Commands.AddHandler(Service.ALTCOMMAND, new CommandInfo(OnCommand)
            {
                HelpMessage = UiString.Commands_Rotation.GetDescription(),
                ShowInHelp = true,
            });
            _ = Svc.Commands.AddHandler(Service.AUTOCOMMAND, new CommandInfo(OnCommand)
            {
                HelpMessage = UiString.Commands_Start.GetDescription(),
                ShowInHelp = true,
            });
            _ = Svc.Commands.AddHandler(Service.OFFCOMMAND, new CommandInfo(OnCommand)
            {
                HelpMessage = UiString.Commands_Off.GetDescription(),
                ShowInHelp = true,
            });
        }

        internal static void Disable()
        {
            _ = Svc.Commands.RemoveHandler(Service.COMMAND);
            _ = Svc.Commands.RemoveHandler(Service.ALTCOMMAND);
            _ = Svc.Commands.RemoveHandler(Service.AUTOCOMMAND);
            _ = Svc.Commands.RemoveHandler(Service.OFFCOMMAND);
        }

        private static void OnCommand(string command, string arguments)
        {
            DoOneCommand(arguments ?? string.Empty);
        }

        private static void DoOneCommand(string command)
        {
            command = (command ?? string.Empty).Trim();

            // No args => open config
            if (command.Length == 0)
            {
                RotationSolverPlugin.OpenConfigWindow();
                return;
            }

            if (string.Equals(command, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                command = "off";
            }

            if (TryGetOneEnum<StateCommandType>(command, out StateCommandType stateType))
            {
                // Split command into parts
                var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int index = -1;

                // Try to parse the second argument as TargetingType if present
                if (parts.Length > 1)
                {
                    string value = parts[1];
                    if (Enum.TryParse(typeof(TargetingType), value, true, out object? parsedEnumSet))
                    {
                        TargetingType targetingTypeSet = (TargetingType)parsedEnumSet;
                        int idx = Service.Config.TargetingTypes.IndexOf(targetingTypeSet);
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
            else if (TryGetOneEnum<SpecialCommandType>(command, out SpecialCommandType specialType))
            {
                DoSpecialCommandType(specialType);
            }
            else if (TryGetOneEnum<OtherCommandType>(command, out OtherCommandType otherType))
            {
                string extraCommand = command[otherType.ToString().Length..].Trim();
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

            // Parse only the first token (case-insensitive).
            int spaceIdx = command.IndexOf(' ');
            string token = spaceIdx >= 0 ? command[..spaceIdx] : command;

            return Enum.TryParse(token, ignoreCase: true, out type);
        }

        internal static string GetCommandStr(this Enum command, string extraCommand = "")
        {
            string cmdStr = $"{Service.COMMAND} {command}";
            if (!string.IsNullOrEmpty(extraCommand))
            {
                cmdStr += $" {extraCommand}";
            }
            return cmdStr;
        }
    }
}