namespace RotationSolver.UI.Material;

// Uses CIE LCh instead of Material's HCT to avoid a color library. Close enough for UI.
internal sealed class TonalPalette(float hue, float chroma)
{
	private readonly Dictionary<int, Vector4> _cache = [];

	public float Hue { get; } = hue;
	public float Chroma { get; } = chroma;

	public Vector4 this[float tone]
	{
		get
		{
			var key = (int)MathF.Round(tone * 10f);
			if (_cache.TryGetValue(key, out var cached))
			{
				return cached;
			}

			var color = M3ColorMath.FromLch(Math.Clamp(tone, 0f, 100f), Chroma, Hue);
			_cache[key] = color;
			return color;
		}
	}
}

internal sealed class CorePalette
{
	private const float BaseErrorHue = 25f;
	private const float MinimumErrorSeparation = 35f;

	private CorePalette(TonalPalette primary, TonalPalette secondary, TonalPalette tertiary, TonalPalette neutral, TonalPalette neutralVariant, TonalPalette error)
	{
		Primary = primary;
		Secondary = secondary;
		Tertiary = tertiary;
		Neutral = neutral;
		NeutralVariant = neutralVariant;
		Error = error;
	}

	public TonalPalette Primary { get; }
	public TonalPalette Secondary { get; }
	public TonalPalette Tertiary { get; }
	public TonalPalette Neutral { get; }
	public TonalPalette NeutralVariant { get; }
	public TonalPalette Error { get; }

	public static CorePalette FromSeed(Vector4 seed)
	{
		M3ColorMath.ToLch(seed, out _, out var chroma, out var hue);

		var primaryChroma = MathF.Max(chroma, 48f);
		return new CorePalette(
			primary: new TonalPalette(hue, primaryChroma),
			secondary: new TonalPalette(hue, MathF.Max(primaryChroma / 3f, 16f)),
			tertiary: new TonalPalette(M3ColorMath.WrapHue(hue + 60f), MathF.Max(primaryChroma / 2f, 24f)),
			neutral: new TonalPalette(hue, MathF.Min(chroma / 12f, 4f)),
			neutralVariant: new TonalPalette(hue, MathF.Min(chroma / 6f, 8f)),
			error: new TonalPalette(ResolveErrorHue(hue), 84f));
	}

	// Moves the error hue away from a red accent so errors stay easy to tell apart.
	private static float ResolveErrorHue(float seedHue)
	{
		var separation = MathF.Abs(M3ColorMath.WrapHue(BaseErrorHue - seedHue + 180f) - 180f);
		return separation >= MinimumErrorSeparation
			? BaseErrorHue
			: M3ColorMath.WrapHue(seedHue - MinimumErrorSeparation);
	}
}

internal sealed class M3Scheme
{
	private static readonly TonalPalette _warningPalette = new(75f, 80f);
	private static readonly TonalPalette _successPalette = new(145f, 50f);
	private static readonly TonalPalette _infoPalette = new(250f, 50f);

	private M3Scheme(CorePalette core)
	{
		var p = core.Primary;
		var s = core.Secondary;
		var t = core.Tertiary;
		var n = core.Neutral;
		var nv = core.NeutralVariant;
		var e = core.Error;

		Primary = p[80];
		OnPrimary = p[20];
		PrimaryContainer = p[30];
		OnPrimaryContainer = p[90];
		PrimaryFixedDim = p[70];
		InversePrimary = p[40];

		Secondary = s[80];
		SecondaryContainer = s[30];
		OnSecondaryContainer = s[90];

		Tertiary = t[80];
		TertiaryContainer = t[30];
		OnTertiaryContainer = t[90];

		Error = e[80];
		OnError = e[20];
		ErrorContainer = e[30];
		OnErrorContainer = e[90];

		Surface = n[6];
		OnSurface = n[90];
		SurfaceContainerLowest = n[4];
		SurfaceContainerLow = n[10];
		SurfaceContainer = n[12];
		SurfaceContainerHigh = n[17];
		SurfaceContainerHighest = n[22];
		OnSurfaceVariant = nv[80];
		InverseSurface = n[90];
		InverseOnSurface = n[20];

		Outline = nv[60];
		OutlineVariant = nv[30];
		Scrim = n[0];
		Shadow = n[0];

		Warning = _warningPalette[80];
		WarningContainer = _warningPalette[30];
		Success = _successPalette[80];
		SuccessContainer = _successPalette[30];
		Info = _infoPalette[80];
	}

	public Vector4 Primary { get; }
	public Vector4 OnPrimary { get; }
	public Vector4 PrimaryContainer { get; }
	public Vector4 OnPrimaryContainer { get; }
	public Vector4 PrimaryFixedDim { get; }

	public Vector4 InversePrimary { get; }

	public Vector4 Secondary { get; }
	public Vector4 SecondaryContainer { get; }
	public Vector4 OnSecondaryContainer { get; }

	public Vector4 Tertiary { get; }
	public Vector4 TertiaryContainer { get; }
	public Vector4 OnTertiaryContainer { get; }

	public Vector4 Error { get; }
	public Vector4 OnError { get; }
	public Vector4 ErrorContainer { get; }
	public Vector4 OnErrorContainer { get; }

	public Vector4 Surface { get; }
	public Vector4 OnSurface { get; }
	public Vector4 SurfaceContainerLowest { get; }
	public Vector4 SurfaceContainerLow { get; }
	public Vector4 SurfaceContainer { get; }
	public Vector4 SurfaceContainerHigh { get; }
	public Vector4 SurfaceContainerHighest { get; }
	public Vector4 OnSurfaceVariant { get; }
	public Vector4 InverseSurface { get; }
	public Vector4 InverseOnSurface { get; }

	public Vector4 Outline { get; }
	public Vector4 OutlineVariant { get; }
	public Vector4 Scrim { get; }
	public Vector4 Shadow { get; }

	public Vector4 Warning { get; }
	public Vector4 WarningContainer { get; }
	public Vector4 Success { get; }
	public Vector4 SuccessContainer { get; }
	public Vector4 Info { get; }

	public static M3Scheme FromSeed(Vector4 seed)
	{
		return new M3Scheme(CorePalette.FromSeed(seed));
	}
}

internal static class M3ColorMath
{
	private const float Epsilon = 216f / 24389f;
	private const float Kappa = 24389f / 27f;
	private const float WhiteX = 0.95047f;
	private const float WhiteY = 1.00000f;
	private const float WhiteZ = 1.08883f;

	public static float WrapHue(float hue)
	{
		hue %= 360f;
		return hue < 0f ? hue + 360f : hue;
	}

	public static Vector4 FromRgb(uint rgb)
	{
		return new Vector4(
			((rgb >> 16) & 0xFF) / 255f,
			((rgb >> 8) & 0xFF) / 255f,
			(rgb & 0xFF) / 255f,
			1f);
	}

	public static void ToLch(Vector4 color, out float lightness, out float chroma, out float hue)
	{
		ToLab(color, out lightness, out var a, out var b);
		chroma = MathF.Sqrt((a * a) + (b * b));
		hue = chroma < 0.0001f ? 0f : WrapHue(MathF.Atan2(b, a) * 180f / MathF.PI);
	}

	public static void ToLab(Vector4 color, out float lightness, out float a, out float b)
	{
		var r = Linearize(color.X);
		var g = Linearize(color.Y);
		var bl = Linearize(color.Z);

		var x = ((0.4124564f * r) + (0.3575761f * g) + (0.1804375f * bl)) / WhiteX;
		var y = ((0.2126729f * r) + (0.7151522f * g) + (0.0721750f * bl)) / WhiteY;
		var z = ((0.0193339f * r) + (0.1191920f * g) + (0.9503041f * bl)) / WhiteZ;

		var fx = LabF(x);
		var fy = LabF(y);
		var fz = LabF(z);

		lightness = (116f * fy) - 16f;
		a = 500f * (fx - fy);
		b = 200f * (fy - fz);
	}

	public static Vector4 FromLch(float lightness, float chroma, float hue)
	{
		if (chroma < 0.0001f)
		{
			return FromLab(lightness, 0f, 0f);
		}

		var radians = hue * MathF.PI / 180f;
		var cos = MathF.Cos(radians);
		var sin = MathF.Sin(radians);

		var low = 0f;
		var high = chroma;
		var best = FromLab(lightness, 0f, 0f);

		// 12 steps is accurate to well under one 8-bit color step.
		for (var i = 0; i < 12; i++)
		{
			var mid = (low + high) * 0.5f;
			var candidate = FromLabRaw(lightness, mid * cos, mid * sin, out var inGamut);
			if (inGamut)
			{
				best = candidate;
				low = mid;
			}
			else
			{
				high = mid;
			}
		}

		return best;
	}

	public static Vector4 FromLab(float lightness, float a, float b)
	{
		return FromLabRaw(lightness, a, b, out _);
	}

	public static Vector4 Mix(Vector4 from, Vector4 to, float amount)
	{
		amount = Math.Clamp(amount, 0f, 1f);
		return new Vector4(
			Delinearize(float.Lerp(Linearize(from.X), Linearize(to.X), amount)),
			Delinearize(float.Lerp(Linearize(from.Y), Linearize(to.Y), amount)),
			Delinearize(float.Lerp(Linearize(from.Z), Linearize(to.Z), amount)),
			float.Lerp(from.W, to.W, amount));
	}

	private static Vector4 FromLabRaw(float lightness, float a, float b, out bool inGamut)
	{
		var fy = (lightness + 16f) / 116f;
		var fx = fy + (a / 500f);
		var fz = fy - (b / 200f);

		var x = LabFInverse(fx) * WhiteX;
		var y = LabFInverse(fy) * WhiteY;
		var z = LabFInverse(fz) * WhiteZ;

		var r = (3.2404542f * x) - (1.5371385f * y) - (0.4985314f * z);
		var g = (-0.9692660f * x) + (1.8760108f * y) + (0.0415560f * z);
		var bl = (0.0556434f * x) - (0.2040259f * y) + (1.0572252f * z);

		const float tolerance = 0.0005f;
		inGamut = r >= -tolerance && r <= 1f + tolerance
			&& g >= -tolerance && g <= 1f + tolerance
			&& bl >= -tolerance && bl <= 1f + tolerance;

		return new Vector4(
			Math.Clamp(Delinearize(r), 0f, 1f),
			Math.Clamp(Delinearize(g), 0f, 1f),
			Math.Clamp(Delinearize(bl), 0f, 1f),
			1f);
	}

	private static float LabF(float t)
	{
		return t > Epsilon ? MathF.Cbrt(t) : ((Kappa * t) + 16f) / 116f;
	}

	private static float LabFInverse(float t)
	{
		var cubed = t * t * t;
		return cubed > Epsilon ? cubed : ((116f * t) - 16f) / Kappa;
	}

	private static float Linearize(float channel)
	{
		return channel <= 0.04045f ? channel / 12.92f : MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);
	}

	private static float Delinearize(float channel)
	{
		return channel <= 0.0031308f ? channel * 12.92f : (1.055f * MathF.Pow(channel, 1f / 2.4f)) - 0.055f;
	}
}
