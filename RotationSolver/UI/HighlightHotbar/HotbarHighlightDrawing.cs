using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Files;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightHotbar;

public class HotbarHighlightDrawing : IDisposable
{
    private static readonly Vector2 _uv1 = new(96 * 5 / 852f, 0),
        _uv2 = new((96 * 5 + 144) / 852f, 0.5f);

    private static IDalamudTextureWrap? _texture = null;

    /// <summary>
    /// The action ids that will be highlighted.
    /// </summary>
    public HashSet<HotbarID> HotbarIDs { get; } = [];

    /// <summary>
    /// The color of highlight.
    /// </summary>
    public Vector4 Color { get; set; } = Service.Config.TeachingModeColor;

    /// <summary>
    /// 
    /// </summary>
    public HotbarHighlightDrawing()
    {
        if (_texture != null) return;
        var tex = Svc.Data?.GetFile<TexFile>("ui/uld/icona_frame_hr1.tex");
        if (tex == null) return;
        byte[] imageData = tex.ImageData;
        byte[] array = new byte[imageData.Length];

        for (int i = 0; i < imageData.Length; i += 4)
        {
            array[i] = array[i + 1] = array[i + 2] = byte.MaxValue;
            array[i + 3] = imageData[i + 3];
        }

        _texture = Svc.Texture.CreateFromRaw(RawImageSpecification.Rgba32(tex.Header.Width, tex.Header.Height), array);

        HotbarHighlight._drawingElements.Add(this);
    }

    private static unsafe bool IsVisible(AtkUnitBase unit)
    {
        if (!unit.IsVisible) return false;
        if (unit.VisibilityFlags == 1) return false;

        return IsVisible(unit.RootNode);
    }

    private static unsafe bool IsVisible(AtkResNode* node)
    {
        while (node != null)
        {
            if (!node->IsVisible()) return false;
            node = node->ParentNode;
        }

        return true;
    }

    private protected unsafe IEnumerable<IDrawing2D> To2D()
    {
        if (_texture == null) return [];

        List<IDrawing2D> result = [];

        var hotBarIndex = 0;
        foreach (var intPtr in GetAddons<AddonActionBar>()
            .Union(GetAddons<AddonActionBarX>())
            .Union(GetAddons<AddonActionCross>())
            .Union(GetAddons<AddonActionDoubleCrossBase>()))
        {
            var actionBar = (AddonActionBarBase*)intPtr;
            if (actionBar != null && IsVisible(actionBar->AtkUnitBase))
            {
                var s = actionBar->AtkUnitBase.Scale;

                var isCrossBar = hotBarIndex > 9;
                if (isCrossBar)
                {
                    if (hotBarIndex == 10)
                    {
                        var actBar = (AddonActionCross*)intPtr;
                        hotBarIndex = actBar->RaptureHotbarId;
                    }
                    else
                    {
                        var actBar = (AddonActionDoubleCrossBase*)intPtr;
                        hotBarIndex = actBar->BarTarget;
                    }
                }
                var hotBar = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule()->Hotbars[hotBarIndex];

                var slotIndex = 0;
                foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
                {
                    var iconAddon = slot.Icon;
                    if ((nint)iconAddon != nint.Zero && IsVisible(&iconAddon->AtkResNode))
                    {
                        AtkResNode node = default;
                        HotbarSlot bar = hotBar.Slots[slotIndex];

                        if (isCrossBar)
                        {
                            var manager = slot.Icon->AtkResNode.ParentNode->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->GetAsAtkComponentNode()->Component->UldManager;

                            for (var i = 0; i < manager.NodeListCount; i++)
                            {
                                node = *manager.NodeList[i];
                                if (node.Width == 72) break;
                            }
                        }
                        else
                        {
                            node = *slot.Icon->AtkResNode.ParentNode->ParentNode;
                        }

                        if (IsActionSlotRight(slot, bar))
                        {
                            var pt1 = new Vector2(node.ScreenX, node.ScreenY);
                            var pt2 = pt1 + new Vector2(node.Width * s, node.Height * s);

                            result.Add(new ImageDrawing(_texture, pt1, pt2, _uv1, _uv2, ImGui.ColorConvertFloat4ToU32(Color)));
                        }
                    }

                    slotIndex++;
                }
            }

            hotBarIndex++;
        }

        return result;
    }

    private static unsafe IEnumerable<nint> GetAddons<T>() where T : struct
    {
        if (typeof(T).GetCustomAttribute<Addon>() is not Addon on) return [];

        return on.AddonIdentifiers
            .Select(str => Svc.GameGui.GetAddonByName(str, 1))
            .Where(ptr => ptr != nint.Zero);
    }

    private unsafe bool IsActionSlotRight(ActionBarSlot slot, HotbarSlot hot)
    {
        var actionId = ActionManager.Instance()->GetAdjustedActionId((uint)slot.ActionId);
        foreach (var hotbarId in HotbarIDs)
        {
            if (hot.OriginalApparentSlotType != hotbarId.SlotType) continue;
            if (hot.ApparentSlotType != hotbarId.SlotType) continue;
            if (actionId != hotbarId.Id) continue;

            return true;
        }

        return false;
    }

    private bool _disposed;

    /// <summary>
    /// If it is enabled.
    /// </summary>
    public virtual bool Enable { get; set; } = true;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        HotbarHighlight._drawingElements.Remove(this);

        GC.SuppressFinalize(this);
    }

    internal IEnumerable<IDrawing2D> To2DMain()
    {
        if (!Enable) return [];
        return To2D();
    }
}