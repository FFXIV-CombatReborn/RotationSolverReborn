namespace RotationSolver.Basic.Configuration
{
	/// <summary>
	/// Contains various types used in the configuration.
	/// </summary>
	public static class ConfigTypes
	{
		/// <summary>
		/// The type of AoE actions to use.
		/// </summary>
		public enum AoEType
		{
			/// <summary>
			/// No AoE.
			/// </summary>
			Off = 0,

			/// <summary>
			/// Only single-target AoE.
			/// </summary>
			Cleave = 1,

			/// <summary>
			/// Full AoE.
			/// </summary>
			Full = 2,
		}
	}
}