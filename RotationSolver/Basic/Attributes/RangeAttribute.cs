namespace RotationSolver.Basic.Attributes;

/// <summary>
/// The range of the configs.
/// </summary>
/// <param name="minValue">min value</param>
/// <param name="maxValue">max value</param>
/// <param name="unitType">the unit type</param>
/// <param name="speed">the speed.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RangeAttribute(float minValue, float maxValue, ConfigUnitType unitType, float speed = 0.005f) : Attribute
{
	/// <summary>
	/// Min Value
	/// </summary>
	public float MinValue => minValue;

	/// <summary>
	/// Max Value
	/// </summary>
	public float MaxValue => maxValue;

	/// <summary>
	/// Speed
	/// </summary>
	public float Speed => speed;

	/// <summary>
	/// The unit type.
	/// </summary>
	public ConfigUnitType UnitType => unitType;
}

/// <summary>
/// The config unit type.
/// </summary>
public enum ConfigUnitType : byte
{
	/// <summary>
	/// None unit type.
	/// </summary>
	None,

	/// <summary>
	/// 
	/// </summary>
	[Description("Time Unit, in seconds.")]
	Seconds,

	/// <summary>
	/// 
	/// </summary>
	[Description("Angle Unit, in degrees.")]
	Degree,

	/// <summary>
	/// 
	/// </summary>
	[Description("Distance Unit, in yalms.")]
	Yalms,

	/// <summary>
	/// 
	/// </summary>
	[Description("Ratio Unit, as percentage.")]
	Percent,

	/// <summary>
	/// 
	/// </summary>
	[Description("Display Unit, in pixels.")]
	Pixels,
}