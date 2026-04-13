namespace RotationSolver.Basic.Traits;

/// <summary>
/// Represents a trait with an ID, level requirements, and texture properties.
/// </summary>
public interface IBaseTrait : IEnoughLevel, ITexture
{
	/// <summary>
	/// Gets the ID of the trait.
	/// </summary>
	uint ID { get; }
}