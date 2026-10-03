using Dalamud.Interface.Utility;

namespace RotationSolver.UI.Material;

internal static class M3
{
	public static readonly Vector4 DefaultSeed = M3ColorMath.FromRgb(0xB0201F);

	// Remember the configured color, not the fallback, or an unusable color would rebuild the theme on every access.
	private static Vector4 _configuredSeed = DefaultSeed;
	private static M3Scheme _scheme = M3Scheme.FromSeed(DefaultSeed);

	private static float _windowScale = 1f;

	private const float ElementBaseline = 0.75f;

	private static float _elementScale = ElementBaseline;

	public static float Scale => ImGuiHelpers.GlobalScale * _elementScale * _windowScale;

	// Keeps the old size while a control is held, so the size slider doesn't resize under the mouse.
	public static void BeginFrame()
	{
		if (!ImGui.IsAnyItemActive())
		{
			_elementScale = ElementBaseline * Math.Clamp(Service.Config.UiElementScale, 0.5f, 2.5f);
		}
	}

	// Dalamud fonts already include the global scale, so it's left out here.
	public static float TextScale => Math.Clamp(Service.Config.UiTextScale, 0.5f, 3f) * _windowScale;

	// Push before M3Style.Push so the window's padding and corners scale too.
	public static WindowScaleScope PushWindowScale(float scale)
	{
		var previous = _windowScale;
		_windowScale = previous * Math.Clamp(scale, 0.1f, 10f);
		return new WindowScaleScope(previous);
	}

	public static M3Scheme Scheme
	{
		get
		{
			var seed = Service.Config.UiAccentColor;
			if (seed.X != _configuredSeed.X || seed.Y != _configuredSeed.Y || seed.Z != _configuredSeed.Z)
			{
				_configuredSeed = seed;

				// Grey or black colors make an unusable theme, so fall back to the default.
				M3ColorMath.ToLch(seed, out var lightness, out var chroma, out _);
				_scheme = M3Scheme.FromSeed(lightness < 5f || chroma < 2f ? DefaultSeed : seed);
			}

			return _scheme;
		}
	}

	public static float ShapeExtraSmall => 4f * Scale;
	public static float ShapeSmall => 8f * Scale;
	public static float ShapeMedium => 12f * Scale;
	public static float ShapeLarge => 16f * Scale;
	public static float ShapeExtraLarge => 28f * Scale;
	public static float ShapeFull => 999f;

	public static float Space1 => 4f * Scale;
	public static float Space2 => 8f * Scale;
	public static float Space3 => 12f * Scale;

	public const float StateHover = 0.08f;
	public const float StatePressed = 0.10f;
	public const float DisabledContent = 0.38f;
	public const float DisabledContainer = 0.12f;

	public static ImFontPtr Body => FontManager.GetDefaultFont(TextScale);
	public static ImFontPtr HeadlineSmall => FontManager.GetFont(22f * TextScale);
	public static ImFontPtr TitleLarge => FontManager.GetFont(19f * TextScale);
	public static ImFontPtr TitleMedium => FontManager.GetFont(16f * TextScale);
	public static ImFontPtr LabelSmall => FontManager.GetFont(11f * TextScale);

	public static FontScope PushBody()
	{
		if (MathF.Abs(TextScale - 1f) < 0.005f)
		{
			return default;
		}

		ImGui.PushFont(Body);
		return new FontScope(true);
	}

	public static float FitText(float height, float padding)
	{
		return MathF.Max(height * Scale, ImGui.GetTextLineHeight() + (padding * 2f * Scale));
	}

	public static Vector4 Alpha(Vector4 color, float alpha)
	{
		return color with { W = alpha };
	}

	public static Vector4 StateLayer(Vector4 container, Vector4 content, bool hovered, bool active)
	{
		if (!hovered && !active)
		{
			return container;
		}

		var opacity = active ? StatePressed + StateHover : StateHover;
		var mixed = M3ColorMath.Mix(container, content, opacity);
		return mixed with { W = container.W };
	}

	public static Vector4 ContentOn(Vector4 fill)
	{
		M3ColorMath.ToLch(fill, out var lightness, out _, out _);
		return lightness > 60f ? Scheme.Surface : Scheme.OnSurface;
	}

	public static uint U32(Vector4 color)
	{
		return ImGui.GetColorU32(color);
	}

	public static uint U32(Vector4 color, float alpha)
	{
		return ImGui.GetColorU32(color with { W = alpha });
	}

	public static Vector4 Severity(M3Severity severity)
	{
		var scheme = Scheme;
		return severity switch
		{
			M3Severity.Error => scheme.Error,
			M3Severity.Warning => scheme.Warning,
			M3Severity.Success => scheme.Success,
			M3Severity.Info => scheme.Info,
			_ => scheme.Primary,
		};
	}

	public static Vector4 SeverityContainer(M3Severity severity)
	{
		var scheme = Scheme;
		return severity switch
		{
			M3Severity.Error => scheme.ErrorContainer,
			M3Severity.Warning => scheme.WarningContainer,
			M3Severity.Success => scheme.SuccessContainer,
			M3Severity.Info => scheme.SecondaryContainer,
			_ => scheme.PrimaryContainer,
		};
	}

	internal readonly struct WindowScaleScope(float previous) : IDisposable
	{
		public void Dispose()
		{
			// A default scope pushed nothing, so leave the scale alone.
			if (previous > 0f)
			{
				_windowScale = previous;
			}
		}
	}

	internal readonly struct FontScope(bool pushed) : IDisposable
	{
		public void Dispose()
		{
			if (pushed)
			{
				ImGui.PopFont();
			}
		}
	}
}

internal enum M3Severity
{
	Neutral,
	Info,
	Success,
	Warning,
	Error,
}
