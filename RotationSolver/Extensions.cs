using RotationSolver.Data;

namespace RotationSolver
{
	public static class CommandTypeExtensions
	{
		public static string ToStateString(this StateCommandType stateType)
		{
			return stateType == StateCommandType.Auto || stateType == StateCommandType.TargetOnly
				? $"{stateType} ({DataCenter.TargetingType.GetDescription()})"
				: stateType.ToString();
		}

		public static string ToSpecialString(this SpecialCommandType specialType)
		{
			return specialType.ToString();
		}
	}
}