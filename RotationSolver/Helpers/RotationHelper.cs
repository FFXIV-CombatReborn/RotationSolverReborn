using Dalamud.Interface.Colors;

namespace RotationSolver.Helpers;

internal static class RotationHelper
{
	private static readonly Dictionary<Type, bool> _extraRotation = [];
	private static readonly Dictionary<Type, RotationAttribute?> _rotationAttributes = [];

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
		var type = rotation.GetType();
		if (!_extraRotation.TryGetValue(type, out var isExtra))
		{
			isExtra = type.GetCustomAttribute<ExtraRotationAttribute>() != null;
			_extraRotation[type] = isExtra;
		}

		return isExtra;
	}

	public static RotationAttribute? GetAttributes(this ICustomRotation rotation)
	{
		var type = rotation.GetType();
		if (!_rotationAttributes.TryGetValue(type, out var attributes))
		{
			attributes = type.GetCustomAttribute<RotationAttribute>();
			_rotationAttributes[type] = attributes;
		}

		return attributes;
	}
}
