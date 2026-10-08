using Dalamud.Interface.Textures.TextureWraps;
using RebornMaterial;

namespace RotationSolver.UI;

// Feeds an RSR action into RebornMaterial's action icon, which only knows textures and recast times.
internal static class ActionIcon
{
	public static bool Draw(string id, IAction? action, float size, bool showCooldown, string? tooltip = null)
	{
		IDalamudTextureWrap? texture = null;
		if (action != null && action.GetTexture(out var wrap))
		{
			texture = wrap;
		}

		M3Cooldown? cooldown = action != null && showCooldown ? ToM3(action.Cooldown) : null;
		return M3ActionIcon.Draw(id, texture, size, cooldown, action?.EnoughLevel ?? true, tooltip ?? action?.Name);
	}

	private static M3Cooldown ToM3(ICooldown cooldown)
	{
		return new M3Cooldown(cooldown.IsCoolingDown, cooldown.RecastTimeOneChargeRaw, cooldown.RecastTimeElapsedRaw,
			cooldown.CurrentCharges, cooldown.MaxCharges);
	}
}
