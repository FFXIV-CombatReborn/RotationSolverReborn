namespace RotationSolver.Basic.Resources;

internal static class StatusLists
{
	public static readonly uint[] DangerousStatus =
	[
		2, 3, 5, 6, 7, 9, 10, 15, 17, 193,
		273, 423, 482, 561, 564, 620, 910, 926, 1343, 1345,
		1346, 1348, 2104, 2965, 3364, 3464, 3521, 3776, 4594,
	];

	public static readonly uint[] PriorityStatus =
	[
		944, 1290, 2655,
	];

	public static readonly uint[] InvincibleStatus =
	[
		325, 328, 394, 469, 529, 592, 656, 671, 775, 776,
		895, 969, 981, 1240, 1302, 1567, 1570, 1697, 1829, 1936,
		2267, 2413, 2654, 3012, 3039, 3052, 4175, 4410, 4443, 4875,
		5145, 5434, 5465,
	];

	public static readonly uint[] NoCastingStatus =
	[
		514, 626, 639, 939, 960, 1049, 1072, 1132, 1133, 1269,
		1284, 1384, 1462, 1599, 2109, 2657, 3314, 3322, 3400, 3522,
		3793, 3802, 4342, 4747, 4748, 4751, 4759, 4760, 4976, 5191,
		5546,
	];
}
