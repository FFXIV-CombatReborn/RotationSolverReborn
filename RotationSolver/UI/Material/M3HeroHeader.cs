using Dalamud.Interface.Textures.TextureWraps;

namespace RotationSolver.UI.Material;

// Holds its own stars: keep one per window and call Reset when it closes.
internal sealed class M3HeroHeader
{
	private const int StaticStarCount = 48;
	private const int MaxParticles = 36;
	private const float SpawnInterval = 0.3f;
	private const float ShootingStarsPerSecond = 0.12f;
	private const float EdgeFade = 28f;
	private const float TwinkleFade = 2.5f;
	private const int TrailSegments = 12;
	private const int GradientBands = 8;

	private const float MaxWindowShare = 0.4f;

	private enum Kind : byte
	{
		Twinkle,
		Shooting,
	}

	private enum Tone : byte
	{
		Primary,
		Tertiary,
		Neutral,
	}

	private struct Particle
	{
		public Kind Kind;
		public Tone Tone;
		public Vector2 Position;
		public Vector2 Velocity;
		public float Life;
		public float MaxLife;
		public float Size;
		public float Depth;
		public float Phase;
	}

	private readonly List<Particle> _particles = [];
	private readonly Vector3[] _staticStars = new Vector3[StaticStarCount];
	private readonly Random _random = new();
	private float _spawnTimer;
	private bool _warm;

	private float _imageAspect;

	public M3HeroHeader()
	{
		var seeded = new Random(42);
		for (var i = 0; i < _staticStars.Length; i++)
		{
			_staticStars[i] = new Vector3(seeded.NextSingle(), seeded.NextSingle(), 0.25f + (seeded.NextSingle() * 0.4f));
		}
	}

	public float Height { get; set; } = 168f;

	public float MaxImageHeight { get; set; } = 300f;

	public Vector2 ImageFocus { get; set; } = new(0.5f, 0.5f);

	public float ImageBlend { get; set; } = 0.1f;

	public float FadeHeight { get; set; } = 40f;

	public bool Animated { get; set; } = true;

	public (Vector2 Min, Vector2 Max) Draw(IDalamudTextureWrap? image = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var windowPos = ImGui.GetWindowPos();
		var windowSize = ImGui.GetWindowSize();

		if (image is { Width: > 0, Height: > 0 })
		{
			_imageAspect = image.Width / (float)image.Height;
		}

		var height = _imageAspect > 0f
			? MathF.Min(windowSize.X / _imageAspect, MaxImageHeight * scale)
			: Height * scale;
		height = MathF.Min(height, windowSize.Y * MaxWindowShare);

		var min = windowPos;
		var max = new Vector2(windowPos.X + windowSize.X, windowPos.Y + height);
		var drawList = ImGui.GetWindowDrawList();

		drawList.PushClipRect(windowPos, windowPos + windowSize, false);
		try
		{
			var rounding = MathF.Min(ImGui.GetStyle().WindowRounding, height * 0.5f);
			if (image != null && _imageAspect > 0f)
			{
				DrawImage(drawList, image, min, max, rounding);
			}
			else
			{
				DrawGradient(drawList, s, min, max, rounding);
			}
		}
		finally
		{
			drawList.PopClipRect();
		}

		return (min, max);
	}

	public void Reset()
	{
		_particles.Clear();
		_spawnTimer = 0f;
		_warm = false;
	}

	private void DrawImage(ImDrawListPtr drawList, IDalamudTextureWrap image, Vector2 min, Vector2 max, float rounding)
	{
		var boxAspect = (max.X - min.X) / MathF.Max(1f, max.Y - min.Y);
		var uvMin = Vector2.Zero;
		var uvMax = Vector2.One;
		if (boxAspect > _imageAspect)
		{
			var visible = _imageAspect / boxAspect;
			uvMin.Y = (1f - visible) * Math.Clamp(ImageFocus.Y, 0f, 1f);
			uvMax.Y = uvMin.Y + visible;
		}
		else
		{
			var visible = boxAspect / _imageAspect;
			uvMin.X = (1f - visible) * Math.Clamp(ImageFocus.X, 0f, 1f);
			uvMax.X = uvMin.X + visible;
		}

		drawList.AddImageRounded(image.Handle, min, max, uvMin, uvMax, M3.U32(Vector4.One), rounding, ImDrawFlags.RoundCornersTop);

		var surface = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.WindowBg));
		var blendTop = max.Y - ((max.Y - min.Y) * Math.Clamp(ImageBlend, 0f, 1f));
		EasedGradient(drawList, new Vector2(min.X, blendTop), max, surface with { W = 0f }, surface);
	}

	private void DrawGradient(ImDrawListPtr drawList, M3Scheme s, Vector2 min, Vector2 max, float rounding)
	{
		var top = M3ColorMath.Mix(s.SurfaceContainerLowest, s.PrimaryContainer, 0.55f);
		var bottom = M3ColorMath.Mix(s.Surface, s.PrimaryContainer, 0.28f);

		// Multi-color rects can't be rounded, so a solid strip draws the rounded top corners.
		if (rounding > 0f)
		{
			drawList.AddRectFilled(min, new Vector2(max.X, min.Y + (rounding * 2f)), M3.U32(top), rounding, ImDrawFlags.RoundCornersTop);
		}

		M3Draw.VerticalGradient(drawList, new Vector2(min.X, min.Y + rounding), max, top, bottom);
		EasedGradient(drawList, new Vector2(min.X, max.Y), new Vector2(max.X, max.Y + (FadeHeight * M3.Scale)), bottom, bottom with { W = 0f });

		drawList.PushClipRect(min, max, true);
		DrawStaticStars(drawList, min, max - min, s.OnSurface);
		if (Animated)
		{
			DrawParticles(drawList, min, max - min, s);
		}

		drawList.PopClipRect();
	}

	private static void EasedGradient(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 from, Vector4 to)
	{
		var height = max.Y - min.Y;
		if (height <= 0f)
		{
			return;
		}

		for (var band = 0; band < GradientBands; band++)
		{
			var near = (float)band / GradientBands;
			var far = (float)(band + 1) / GradientBands;
			var nearColor = M3.U32(Vector4.Lerp(from, to, near * near));
			var farColor = M3.U32(Vector4.Lerp(from, to, far * far));
			drawList.AddRectFilledMultiColor(
				new Vector2(min.X, min.Y + (height * near)),
				new Vector2(max.X, min.Y + (height * far)),
				nearColor, nearColor, farColor, farColor);
		}
	}

	private void DrawStaticStars(ImDrawListPtr drawList, Vector2 origin, Vector2 size, Vector4 color)
	{
		var radius = 0.9f * M3.Scale;
		foreach (var star in _staticStars)
		{
			var position = origin + new Vector2(star.X * size.X, star.Y * size.Y);
			drawList.AddCircleFilled(position, radius, M3.U32(color, star.Z * 0.6f), 8);
		}
	}

	private void DrawParticles(ImDrawListPtr drawList, Vector2 origin, Vector2 size, M3Scheme s)
	{
		var delta = Math.Clamp(ImGui.GetIO().DeltaTime, 0f, 0.1f);
		var scale = M3.Scale;

		if (!_warm)
		{
			_warm = true;
			for (var i = 0; i < MaxParticles / 2; i++)
			{
				SpawnTwinkle(size, prewarmed: true);
			}
		}

		_spawnTimer += delta;
		if (_spawnTimer >= SpawnInterval && _particles.Count < MaxParticles)
		{
			_spawnTimer = 0f;
			SpawnTwinkle(size, prewarmed: false);
		}

		if (_random.NextSingle() < ShootingStarsPerSecond * delta)
		{
			SpawnShootingStar(size);
		}

		for (var i = _particles.Count - 1; i >= 0; i--)
		{
			var particle = _particles[i];
			particle.Position += particle.Velocity * delta;
			particle.Life -= delta;

			if (particle.Life <= 0f || (particle.Kind == Kind.Shooting && OutOfBounds(particle.Position, size, 120f * scale)))
			{
				_particles.RemoveAt(i);
				continue;
			}

			var color = particle.Tone switch
			{
				Tone.Primary => s.Primary,
				Tone.Tertiary => s.Tertiary,
				_ => s.OnSurface,
			};

			if (particle.Kind == Kind.Twinkle)
			{
				Bounce(ref particle, size);
				particle.Phase += delta * (1.2f + particle.Depth);

				var age = particle.MaxLife - particle.Life;
				var alpha = MathF.Min(MathF.Min(age, particle.Life) / TwinkleFade, 1f)
					* EdgeAlpha(particle.Position, size, EdgeFade * scale)
					* (0.6f + (0.4f * MathF.Sin(particle.Phase)));

				var center = origin + particle.Position;
				drawList.AddCircleFilled(center, particle.Size * (1.8f + (particle.Depth * 0.4f)), M3.U32(color, alpha * 0.22f), 12);
				drawList.AddCircleFilled(center, particle.Size, M3.U32(color, alpha), 12);
			}
			else
			{
				var age = particle.MaxLife - particle.Life;
				var alpha = MathF.Min(MathF.Min(age / 0.2f, particle.Life / 0.4f), 1f);
				DrawShootingStar(drawList, origin + particle.Position, particle.Velocity, color, alpha, scale);
			}

			_particles[i] = particle;
		}
	}

	private static void DrawShootingStar(ImDrawListPtr drawList, Vector2 head, Vector2 velocity, Vector4 color, float alpha, float scale)
	{
		var speed = velocity.Length();
		if (speed <= 0.001f)
		{
			return;
		}

		var tail = head - (velocity / speed * (80f * scale));
		for (var segment = 0; segment < TrailSegments; segment++)
		{
			var near = (float)segment / TrailSegments;
			var far = (float)(segment + 1) / TrailSegments;
			var from = Vector2.Lerp(head, tail, near);
			var to = Vector2.Lerp(head, tail, far);
			var fade = (1f - near) * alpha;
			var width = float.Lerp(2.5f, 0.5f, near) * scale;

			drawList.AddLine(from, to, M3.U32(color, fade * 0.3f), width + (3f * scale));
			drawList.AddLine(from, to, M3.U32(color, fade), width);
		}

		drawList.AddCircleFilled(head, 1.8f * scale, M3.U32(color, alpha), 10);
	}

	private void SpawnTwinkle(Vector2 size, bool prewarmed)
	{
		var scale = M3.Scale;
		var depth = _random.Next(3) switch
		{
			0 => 0.5f,
			1 => 1f,
			_ => 1.5f,
		};

		var drift = 4f * depth * scale;
		var maxLife = 20f + (_random.NextSingle() * 20f);
		var roll = _random.NextSingle();

		_particles.Add(new Particle
		{
			Kind = Kind.Twinkle,
			Tone = roll < 0.4f ? Tone.Primary : roll < 0.7f ? Tone.Tertiary : Tone.Neutral,
			Position = new Vector2(_random.NextSingle() * size.X, _random.NextSingle() * size.Y),
			Velocity = new Vector2((_random.NextSingle() - 0.5f) * drift, (_random.NextSingle() - 0.5f) * drift),
			MaxLife = maxLife,

			Life = prewarmed ? maxLife * (0.3f + (_random.NextSingle() * 0.7f)) : maxLife,
			Size = (0.6f + (_random.NextSingle() * 1.4f)) * depth * scale,
			Depth = depth,
			Phase = _random.NextSingle() * MathF.Tau,
		});
	}

	private void SpawnShootingStar(Vector2 size)
	{
		var scale = M3.Scale;
		var maxLife = 1.4f + (_random.NextSingle() * 0.6f);

		_particles.Add(new Particle
		{
			Kind = Kind.Shooting,
			Tone = _random.NextSingle() < 0.6f ? Tone.Primary : Tone.Neutral,
			Position = new Vector2(size.X * (0.35f + (_random.NextSingle() * 0.6f)), -8f * scale),
			Velocity = new Vector2(-160f - (_random.NextSingle() * 80f), 90f + (_random.NextSingle() * 60f)) * scale,
			MaxLife = maxLife,
			Life = maxLife,
			Size = 1f,
			Depth = 1f,
		});
	}

	private static void Bounce(ref Particle particle, Vector2 size)
	{
		if ((particle.Position.X < 0f && particle.Velocity.X < 0f) || (particle.Position.X > size.X && particle.Velocity.X > 0f))
		{
			particle.Velocity.X = -particle.Velocity.X;
		}

		if ((particle.Position.Y < 0f && particle.Velocity.Y < 0f) || (particle.Position.Y > size.Y && particle.Velocity.Y > 0f))
		{
			particle.Velocity.Y = -particle.Velocity.Y;
		}
	}

	private static bool OutOfBounds(Vector2 position, Vector2 size, float margin)
	{
		return position.X < -margin || position.X > size.X + margin || position.Y > size.Y + margin;
	}

	private static float EdgeAlpha(Vector2 position, Vector2 size, float distance)
	{
		if (distance <= 0f)
		{
			return 1f;
		}

		var nearest = MathF.Min(MathF.Min(position.X, size.X - position.X), MathF.Min(position.Y, size.Y - position.Y));
		return Math.Clamp(nearest / distance, 0f, 1f);
	}
}
