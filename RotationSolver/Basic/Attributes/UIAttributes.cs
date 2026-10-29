namespace RotationSolver.Basic.Attributes;

/// <summary>
/// The attribute for the UI configs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UIAttribute"/> class.
/// </remarks>
/// <param name="name">The name of this config.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class UIAttribute(string name) : Attribute
{

	/// <summary>
	/// Gets the name of this config.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	/// Gets or sets the description about this UI item.
	/// </summary>
	public string Description { get; set; } = "";

	/// <summary>
	/// Gets or sets the parent of this UI item.
	/// </summary>
	public string Parent { get; set; } = "";

	/// <summary>
	/// Gets or sets the filter to get this UI item.
	/// </summary>
	public string Filter { get; set; } = "";

	/// <summary>
	/// Gets or sets the order of this item.
	/// </summary>
	public byte Order { get; set; } = 0;

	/// <summary>
	/// Gets or sets the section of this item.
	/// </summary>
	public byte Section { get; set; } = 0;

	/// <summary>
	/// Gets or sets the action ID.
	/// </summary>
	public ActionID Action { get; set; }

	/// <summary>
	/// Gets or sets the filter for PvP.
	/// </summary>
	public JobFilterType PvPFilter { get; set; }

	/// <summary>
	/// Gets or sets the filter for PvE.
	/// </summary>
	public JobFilterType PvEFilter { get; set; }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public enum JobFilterType : byte
{
	None,
	NoJob,
	NoHealer,
	Healer,
	Raise,
	Interrupt,
	Dispel,
	Tank,
	Melee,
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal class JobConfigAttribute : Attribute
{
	public JobConfigAttribute() { }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal class JobChoiceConfigAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field)]
internal class ConditionBoolAttribute : Attribute
{
}