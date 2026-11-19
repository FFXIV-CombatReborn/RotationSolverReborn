namespace RotationSolver.Engine.Attributes;

[AttributeUsage(AttributeTargets.Property)]
internal class IDAttribute(uint id) : Attribute
{
	public uint ID => id;
}
