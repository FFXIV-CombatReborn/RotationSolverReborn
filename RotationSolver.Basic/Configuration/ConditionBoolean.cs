namespace RotationSolver.Basic.Configuration;

/// <summary>
/// Represents a boolean condition with additional configuration options.
/// </summary>
internal class ConditionBoolean
{
	private readonly bool _defaultValue;

	/// <summary>
	/// Gets or sets the value of the condition.
	/// </summary>
	public bool Value { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the condition is enabled.
	/// </summary>
	public bool Enable { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the condition is disabled.
	/// </summary>
	public bool Disable { get; set; }

	/// <summary>
	/// Gets the key associated with the condition.
	/// </summary>
	[JsonIgnore]
	public string Key { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ConditionBoolean"/> class.
	/// </summary>
	/// <param name="defaultValue">The default value of the condition.</param>
	/// <param name="key">The key associated with the condition.</param>
	public ConditionBoolean(bool defaultValue, string key)
	{
		_defaultValue = defaultValue;
		Value = defaultValue;
		Key = key;
	}

	/// <summary>
	/// Resets the value of the condition to its default value.
	/// </summary>
	public void ResetValue()
	{
		Value = _defaultValue;
	}

	/// <summary>
	/// Implicitly converts a <see cref="ConditionBoolean"/> to a <see cref="bool"/>.
	/// </summary>
	/// <param name="condition">The condition to convert.</param>
	public static implicit operator bool(ConditionBoolean condition)
	{
		// Forced condition overrides have been removed; simply use the stored value.
		return condition.Value;
	}

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
	{
		return Value.ToString();
	}
}