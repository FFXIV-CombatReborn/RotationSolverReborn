using ECommons.ExcelServices;

namespace RotationSolver.Basic.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class JobsAttribute(params Job[] jobs) : Attribute
{
	public Job[] Jobs => jobs;
}
