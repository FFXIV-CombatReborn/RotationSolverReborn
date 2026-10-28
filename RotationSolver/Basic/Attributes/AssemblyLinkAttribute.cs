namespace RotationSolver.Basic.Attributes;

/// <summary>
/// An attribute to add the help and support link.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class AssemblyLinkAttribute : Attribute
{
	/// <summary>
	/// A link for donate.
	/// </summary>
	public string? Donate { get; set; }

	/// <summary>
	/// A link for your user name in GitHub.
	/// </summary>
	public string? UserName { get; set; }

	/// <summary>
	/// A link for the repo in your GitHub.
	/// </summary>
	public string? Repository { get; set; }
}
