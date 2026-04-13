namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents a 2D vector with integer components.
/// </summary>
internal readonly struct Vector2Int
{
	/// <summary>
	/// Gets the X component of the vector.
	/// </summary>
	public int X { get; }

	/// <summary>
	/// Gets the Y component of the vector.
	/// </summary>
	public int Y { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Vector2Int"/> struct.
	/// </summary>
	/// <param name="x">The X component of the vector.</param>
	/// <param name="y">The Y component of the vector.</param>
	public Vector2Int(int x, int y)
	{
		X = x;
		Y = y;
	}

	/// <summary>
	/// Creates a new instance of <see cref="Vector2Int"/> with a modified X component.
	/// </summary>
	/// <param name="x">The new X component.</param>
	/// <returns>A new <see cref="Vector2Int"/> instance.</returns>
	public Vector2Int WithX(int x)
	{
		return new Vector2Int(x, Y);
	}

	/// <summary>
	/// Creates a new instance of <see cref="Vector2Int"/> with a modified Y component.
	/// </summary>
	/// <param name="y">The new Y component.</param>
	/// <returns>A new <see cref="Vector2Int"/> instance.</returns>
	public Vector2Int WithY(int y)
	{
		return new Vector2Int(X, y);
	}
}