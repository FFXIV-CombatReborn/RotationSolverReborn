namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents the raid languages.
/// </summary>
internal class RaidLangs
{
#pragma warning disable IDE1006 // Naming Styles
	/// <summary>
	/// Gets or sets the dictionary of languages.
	/// </summary>
	public Dictionary<string, Lang> langs { get; set; } = [];

	/// <summary>
	/// Represents a language with replaceable text and sync data.
	/// </summary>
	internal class Lang
	{
		/// <summary>
		/// Gets or sets the dictionary of replaceable sync data.
		/// </summary>
		public Dictionary<string, string> replaceSync { get; set; } = [];

		/// <summary>
		/// Gets or sets the dictionary of replaceable text data.
		/// </summary>
		public Dictionary<string, string> replaceText { get; set; } = [];
	}
#pragma warning restore IDE1006 // Naming Styles
}