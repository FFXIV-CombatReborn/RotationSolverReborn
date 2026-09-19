using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Files;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightTeachingMode;

/// <summary> 
/// The hotbar highlight drawing. 
/// </summary>
public class DrawingHighlightHotbar : DrawingHighlightHotbarBase
{
	/// <summary> </summary>
	/// <param name="color"> Color </param>
	/// <param name="ids">   action ids </param>
	public DrawingHighlightHotbar(Vector4 color, params HotbarID[] ids)
		: this()
	{
		Color = color;
		HotbarIDs = [.. ids];
	}

	/// <summary> </summary>
	public DrawingHighlightHotbar()
	{
		if (_texture != null)
		{
			return;
		}

		var tex = Svc.Data?.GetFile<TexFile>("ui/uld/icona_frame_hr1.tex");
		if (tex == null)
		{
			return;
		}

		var imageData = tex.ImageData;
		var array = new byte[imageData.Length];

		for (var i = 0; i < imageData.Length; i += 4)
		{
			array[i] = array[i + 1] = array[i + 2] = byte.MaxValue;
			array[i + 3] = imageData[i + 3];
		}

		_texture = Svc.Texture.CreateFromRaw(RawImageSpecification.Rgba32(tex.Header.Width, tex.Header.Height), array);
	}

	/// <summary> The color of highlight. </summary>
	public Vector4 Color { get; set; } = new Vector4(0.8f, 0.5f, 0.3f, 1);

	/// <summary> The action ids that </summary>
	public HashSet<HotbarID> HotbarIDs { get; } = [];

	/// <summary>
	/// The ids the drawings are currently built from. <see cref="HotbarHighlightManager"/> sets this from
	/// the decision being shown, which can lag <see cref="HotbarIDs"/> so a short-lived choice stays visible.
	/// </summary>
	private readonly HashSet<HotbarID> _displayIds = [];

	internal void SetDisplayIds(IEnumerable<HotbarID> ids)
	{
		_displayIds.Clear();
		foreach (var id in ids)
		{
			_ = _displayIds.Add(id);
		}
	}

	private protected override unsafe IEnumerable<IDrawing2D> To2D()
	{
		if (_texture == null || _displayIds.Count == 0)
		{
			return [];
		}

		var framework = Framework.Instance();
		var uiModule = framework == null ? null : framework->GetUIModule();
		var raptureModule = uiModule == null ? null : uiModule->GetRaptureHotbarModule();
		var actionManager = ActionManager.Instance();
		if (raptureModule == null || actionManager == null)
		{
			return [];
		}

		List<IDrawing2D> result = [];
		var color = ImGui.ColorConvertFloat4ToU32(Color);

		HotbarAddonHelper.GetHotbarAddons(_addons);

		var hotBarIndex = -1;
		foreach (var intPtr in _addons)
		{
			hotBarIndex++;

			var actionBar = (AddonActionBarBase*)intPtr;
			if (!HotbarAddonHelper.IsVisible(&actionBar->AtkUnitBase))
			{
				continue;
			}

			var s = actionBar->AtkUnitBase.Scale;

			// Resolve the RaptureHotbarModule index separately so the addon counter stays intact.
			var isCrossBar = hotBarIndex > 9;
			var resolvedHotbarIndex = hotBarIndex;
			if (isCrossBar)
			{
				resolvedHotbarIndex = hotBarIndex == 10
					? ((AddonActionCross*)intPtr)->RaptureHotbarId
					: ((AddonActionDoubleCrossBase*)intPtr)->BarTarget;
			}

			if (resolvedHotbarIndex < 0 || resolvedHotbarIndex >= raptureModule->Hotbars.Length)
			{
				continue;
			}

			var hotBar = raptureModule->Hotbars[resolvedHotbarIndex];

			var slotIndex = -1;
			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				slotIndex++;

				var iconAddon = slot.Icon;
				if (iconAddon == null || (uint)slotIndex >= hotBar.Slots.Length
					|| !IsActionSlotRight(actionManager, slot, hotBar.Slots[slotIndex])
					|| !HotbarAddonHelper.IsVisible(&iconAddon->AtkResNode))
				{
					continue;
				}

				var node = isCrossBar ? FindCrossBarFrameNode(iconAddon) : GetGrandParent(iconAddon);
				if (node == null)
				{
					continue;
				}

				Vector2 pt1 = new(node->ScreenX, node->ScreenY);
				var pt2 = pt1 + new Vector2(node->Width * s, node->Height * s);

				result.Add(new ImageDrawing(_texture, pt1, pt2, _uv1, _uv2, color));
			}
		}

		return result;
	}

	private static unsafe AtkResNode* GetGrandParent(AtkComponentNode* iconAddon)
	{
		var parent = iconAddon->AtkResNode.ParentNode;
		return parent == null ? null : parent->ParentNode;
	}

	private static unsafe AtkResNode* FindCrossBarFrameNode(AtkComponentNode* iconAddon)
	{
		var parent = iconAddon->AtkResNode.ParentNode;
		var parentComponent = parent == null ? null : parent->GetAsAtkComponentNode();
		if (parentComponent == null || parentComponent->Component == null)
		{
			return null;
		}

		var parentUld = parentComponent->Component->UldManager;
		if (parentUld.NodeListCount <= 2 || parentUld.NodeList[2] == null)
		{
			return null;
		}

		var frameComponent = parentUld.NodeList[2]->GetAsAtkComponentNode();
		if (frameComponent == null || frameComponent->Component == null)
		{
			return null;
		}

		var manager = frameComponent->Component->UldManager;
		AtkResNode* node = null;
		for (var i = 0; i < manager.NodeListCount; i++)
		{
			node = manager.NodeList[i];
			if (node != null && node->Width == 72)
			{
				break;
			}
		}

		return node;
	}

	/// <inheritdoc />
	protected override void UpdateOnFrame()
	{
		return;
	}

	private static readonly Vector2 _uv1 = new(96 * 5 / 852f, 0),
		_uv2 = new(((96 * 5) + 144) / 852f, 0.5f);

	private static IDalamudTextureWrap? _texture = null;

	// Own buffer, only used from the framework thread via HotbarHighlightManager.UpdateElements.
	private readonly List<nint> _addons = [];

	/// <summary>
	/// Releases the shared highlight texture. Call on plugin shutdown.
	/// </summary>
	internal static void DisposeTexture()
	{
		_texture?.Dispose();
		_texture = null;
	}
	private unsafe bool IsActionSlotRight(ActionManager* actionManager, ActionBarSlot slot, HotbarSlot hot)
	{
		var actionId = actionManager->GetAdjustedActionId((uint)slot.ActionId);
		foreach (var hotbarId in _displayIds)
		{
			if (hot.OriginalApparentSlotType != hotbarId.SlotType)
			{
				continue;
			}

			if (hot.ApparentSlotType != hotbarId.SlotType)
			{
				continue;
			}

			if (actionId != hotbarId.Id)
			{
				continue;
			}

			return true;
		}

		return false;
	}
}

/// <summary> 
/// The Hot bar ID 
/// </summary>
public readonly record struct HotbarID(HotbarSlotType SlotType, uint Id)
{
	///// <summary>
	///// Convert from a action id.
	///// </summary>
	///// <param name="actionId"></param>
	//public static implicit operator HotbarID(uint actionId) => new(HotbarSlotType.Action, actionId);
}

/// <summary> Polyline drawing draws the actual border lines on the overlay window. </summary>
/// <remarks> </remarks>
/// <param name="pts">       </param>
/// <param name="color">     </param>
/// <param name="thickness"> </param>
public readonly struct PolylineDrawing(Vector2[] pts, uint color, float thickness) : IDrawing2D
{
	/// <summary> Draw on the <seealso cref="ImGui" /> </summary>
	public void Draw()
	{
		if (_pts == null || _pts.Length < 2)
		{
			return;
		}

		foreach (var pt in _pts)
		{
			ImGui.GetWindowDrawList().PathLineTo(pt);
		}

		if (_thickness == 0)
		{
			ImGui.GetWindowDrawList().PathFillConvex(_color);
		}
		else if (_thickness < 0)
		{
			ImGui.GetWindowDrawList().PathStroke(_color, ImDrawFlags.RoundCornersAll, -_thickness);
		}
		else
		{
			ImGui.GetWindowDrawList().PathStroke(_color, ImDrawFlags.Closed | ImDrawFlags.RoundCornersAll, _thickness);
		}
	}

	internal readonly float _thickness = thickness;
	private readonly uint _color = color;
	private readonly Vector2[] _pts = pts;
}

/// <summary> 
/// 2D drawing element. 
/// </summary>
public interface IDrawing2D
{
	/// <summary> Draw on the <seealso cref="ImGui" /> </summary>
	void Draw();
}

/// <summary> Drawing the image. </summary>
/// <remarks> </remarks>
/// <param name="texture"> </param>
/// <param name="pt1">     </param>
/// <param name="pt2">     </param>
/// <param name="col">     </param>
public readonly struct ImageDrawing(IDalamudTextureWrap texture, Vector2 pt1, Vector2 pt2, uint col = uint.MaxValue) : IDrawing2D
{
	/// <summary> </summary>
	/// <param name="texture"> </param>
	/// <param name="pt1">     </param>
	/// <param name="pt2">     </param>
	/// <param name="uv1">     </param>
	/// <param name="uv2">     </param>
	/// <param name="col">     </param>
	public ImageDrawing(IDalamudTextureWrap texture, Vector2 pt1, Vector2 pt2,
		Vector2 uv1, Vector2 uv2, uint col = uint.MaxValue)
		: this(texture, pt1, pt2, col)
	{
		_uv1 = uv1;
		_uv2 = uv2;
	}

	/// <summary> Draw on the <seealso cref="ImGui" /> </summary>
	public void Draw()
	{
		ImGui.GetWindowDrawList().AddImage(_texture.Handle, _pt1, _pt2, _uv1, _uv2, _col);
	}

	private readonly uint _col = col;
	private readonly Vector2 _pt1 = pt1, _pt2 = pt2, _uv1 = default, _uv2 = Vector2.One;
	private readonly IDalamudTextureWrap _texture = texture;
}