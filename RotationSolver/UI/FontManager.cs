using ECommons.DalamudServices;

namespace RotationSolver.UI
{
	public static class FontManager
	{
		private static readonly Lock _lock = new();
		private static readonly Dictionary<int, Dalamud.Interface.ManagedFontAtlas.IFontHandle> _handles = [];

		public static unsafe ImFontPtr GetFont(float size)
		{
			// Round to a stable integer key to avoid excessive variants.
			int key = Math.Max(1, (int)MathF.Round(size));

			Dalamud.Interface.ManagedFontAtlas.IFontHandle? handle;
			lock (_lock)
			{
				if (!_handles.TryGetValue(key, out handle) || handle == null)
				{
					var style = new Dalamud.Interface.GameFonts.GameFontStyle(
						Dalamud.Interface.GameFonts.GameFontStyle.GetRecommendedFamilyAndSize(
							Dalamud.Interface.GameFonts.GameFontFamily.Axis, key));

					handle = Svc.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(style);
					_handles[key] = handle;
				}
			}

			try
			{
				using var locked = handle.Lock();
				var font = locked.ImFont;
				return font.IsLoaded() ? font : ImGui.GetFont();
			}
			catch (Exception)
			{
				return ImGui.GetFont();
			}
		}

		/// <summary>
		/// Dispose all cached font handles. Call on plugin shutdown.
		/// </summary>
		public static void DisposeAll()
		{
			lock (_lock)
			{
				foreach (var h in _handles.Values)
				{
					h.Dispose();
				}
				_handles.Clear();
			}
		}
	}
}