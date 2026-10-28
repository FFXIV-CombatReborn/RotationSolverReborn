namespace RotationSolver.Basic.Configuration.RotationConfig;

/// <summary>
/// Represents a set of rotation configurations.
/// </summary>
public interface IRotationConfigSet : IEnumerable<IRotationConfig>
{
	/// <summary>
	/// Gets the collection of rotation configurations.
	/// </summary>
	HashSet<IRotationConfig> Configs { get; }
}