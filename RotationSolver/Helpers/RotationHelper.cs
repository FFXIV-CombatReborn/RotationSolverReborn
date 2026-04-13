using Dalamud.Interface.Colors;

namespace RotationSolver.Helpers;

internal static class RotationHelper
{
	private static readonly Dictionary<ICustomRotation, bool> _extraRotation = [];
	private static readonly Dictionary<ICustomRotation, RotationAttribute> _rotationAttributes = [];

	public static unsafe Vector4 GetColor(this ICustomRotation rotation)
	{
		if (!rotation.IsEnabled)
		{
			return *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled);
		}

		if (!rotation.IsValid)
		{
			return ImGuiColors.DPSRed;
		}

		if (rotation.IsExtra())
		{
			return ImGuiColors.DalamudViolet;
		}

		return ImGuiColors.DalamudWhite;
	}

	public static bool IsExtra(this ICustomRotation rotation)
	{
		if (_extraRotation.TryGetValue(rotation, out bool isExtra))
		{
			return isExtra;
		}

		ExtraRotationAttribute? extraRotationAttribute = rotation.GetType().GetCustomAttribute<ExtraRotationAttribute>();
		_extraRotation[rotation] = extraRotationAttribute != null;
		return _extraRotation[rotation];
	}

	public static RotationAttribute? GetAttributes(this ICustomRotation rotation)
	{
		if (_rotationAttributes.TryGetValue(rotation, out RotationAttribute? attributes))
		{
			return attributes;
		}
		attributes = rotation.GetType().GetCustomAttribute<RotationAttribute>();
		if (attributes != null)
		{
			_rotationAttributes[rotation] = attributes;
		}

		return attributes;
	}

}
