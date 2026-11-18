using ECommons.ExcelServices;

namespace RotationSolver.Engine.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class JobsAttribute(params Job[] jobs) : Attribute
{
	public Job[] Jobs => jobs;
}
