using Dalamud.Game.Command;
using ECommons.DalamudServices;
using RotationSolver.Data;

namespace RotationSolver.Commands
{
    public static partial class RSCommands
    {
        internal static void Enable()
        {
            Svc.Commands.AddHandler(Service.COMMAND, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open config window",
                ShowInHelp = true,
            });
            Svc.Commands.AddHandler(Service.ALTCOMMAND, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open config window",
                ShowInHelp = true,
            });
        }

        internal static void Disable()
        {
            Svc.Commands.RemoveHandler(Service.COMMAND);
            Svc.Commands.RemoveHandler(Service.ALTCOMMAND);
        }

        private static void OnCommand(string command, string arguments)
        {
            DoOneCommand(arguments);
        }

        private static void DoOneCommand(string command)
        {
            if (command.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                command = "off";
            }

            if (TryGetOneEnum<StateCommandType>(command, out var stateType))
            {
                var indexStr = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (!int.TryParse(indexStr, out var index))
                {
                    index = -1;
                }
                DoStateCommandType(stateType, index);
            }
            else if (TryGetOneEnum<SpecialCommandType>(command, out var specialType))
            {
                DoSpecialCommandType(specialType);
            }
            else if (TryGetOneEnum<OtherCommandType>(command, out var otherType))
            {
                var extraCommand = command.Substring(otherType.ToString().Length).Trim();
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
            try
            {
                type = Enum.GetValues<T>().First(c => command.StartsWith(c.ToString(), StringComparison.OrdinalIgnoreCase));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
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
