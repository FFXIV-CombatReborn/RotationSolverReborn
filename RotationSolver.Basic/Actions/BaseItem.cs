using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The item usage.
/// </summary>
public class BaseItem : IBaseItem
{
    private readonly struct ItemCooldown(uint id) : ICooldown
    {
        unsafe float ICooldown.RecastTimeOneChargeRaw => ActionManager.Instance()->GetRecastTime(ActionType.Item, id);

        unsafe float ICooldown.RecastTimeElapsedRaw => ActionManager.Instance()->GetRecastTimeElapsed(ActionType.Item, id);

        unsafe bool ICooldown.IsCoolingDown => ActionManager.Instance()->IsRecastTimerActive(ActionType.Item, id);

        ushort ICooldown.MaxCharges => 0;

        ushort ICooldown.CurrentCharges => 0;
    }

    private protected readonly Item _item;

    /// <inheritdoc/>
    public uint A4 { get; set; } = 0;

    /// <summary>
    /// Item Id.
    /// </summary>
    public uint ID => _item.RowId;

    /// <summary>
    /// Item Id
    /// </summary>
    public uint AdjustedID => ID;

    /// <summary>
    /// The check about this item.
    /// </summary>
    public Func<bool>? ItemCheck { get; set; }

    /// <inheritdoc/>
    public unsafe bool HasIt => InventoryManager.Instance()->GetInventoryItemCount(ID, false) > 0
        || InventoryManager.Instance()->GetInventoryItemCount(ID, true) > 0;

    /// <summary>
    /// Icon Id.
    /// </summary>
    public uint IconID { get; }

    /// <summary>
    /// Item name.
    /// </summary>
    public string Name => _item.Name.ExtractText();

    /// <summary>
    /// The item configs.
    /// </summary>
    public ItemConfig Config
    {
        get
        {
            if (!Service.Config.RotationItemConfig.TryGetValue(ID, out ItemConfig? value))
            {
                Service.Config.RotationItemConfig[ID] = value = new();
            }
            return value;
        }
    }

    /// <summary>
    /// Is item enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => Config.IsEnabled;
        set => Config.IsEnabled = value;
    }

    /// <summary>
    ///
    /// </summary>
    public bool IsIntercepted
    {
        get => Config.IsIntercepted;
        set => Config.IsIntercepted = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsRestrictedDOT
    {
        get => Config.IsRestrictedDOT;
        set => Config.IsRestrictedDOT = value;
    }

    /// <summary>
    /// Is the item in the cd window.
    /// </summary>
    public bool IsOnCooldownWindow
    {
        get => Config.IsOnCooldownWindow;
        set => Config.IsOnCooldownWindow = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this action is restricted by min HP.
    /// </summary>
    public bool MinHPFeature
    {
        get => Config.MinHPFeature;
        set => Config.MinHPFeature = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public float MinHPPercent
    {
        get => Config.MinHPPercent;
        set => Config.MinHPPercent = value;
    }

    /// <summary>
    /// Description about this item.
    /// </summary>
    public string Description => string.Empty;


    /// <summary>
    /// Get the enough level for using this item.
    /// </summary>
    public bool EnoughLevel => true;

    /// <summary>
    /// The level to use this item.
    /// </summary>
    public byte Level => 0;

    /// <summary>
    /// Sort the item key.
    /// </summary>
    public uint SortKey { get; }

    /// <summary>
    /// Is this action in action sequencer.
    /// </summary>
    public virtual bool IsActionSequencer => false;

    /// <summary>
    /// Can I use this item.
    /// </summary>
    protected virtual bool CanUseThis => true;

    /// <inheritdoc/>
    public ICooldown Cooldown { get; }

    /// <summary>
    /// Create by row.
    /// </summary>
    /// <param name="row"></param>
    public unsafe BaseItem(uint row)
        : this(Service.GetSheet<Item>().GetRow(row)!)
    {
    }

    /// <summary>
    /// Create by item.
    /// </summary>
    /// <param name="item"></param>
    public unsafe BaseItem(Item item)
    {
        _item = item;
        IconID = _item.Icon;
        A4 = item.RowId switch
        {
            36109 => 196625,
            _ => 65535, //TODO: better A4!
        };
        SortKey = (uint)ActionManager.Instance()->GetRecastGroup((int)ActionType.Item, ID);
        Cooldown = new ItemCooldown(ID);
    }

    /// <summary>
    /// Can Use this item.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="clippingCheck"></param>
    /// <returns></returns>
    public virtual unsafe bool CanUse(out IAction item, bool clippingCheck = false)
    {
        item = this;
        if (_item.RowId == 0)
        {
            return false; // Check if the item is uninitialized
        }

        if (!CanUseThis)
        {
            return false;
        }

        if (!IsEnabled)
        {
            return false;
        }

        if (ConfigurationHelper.BadStatus.Contains(ActionManager.Instance()->GetActionStatus(ActionType.Item, ID))
            && ConfigurationHelper.BadStatus.Contains(ActionManager.Instance()->GetActionStatus(ActionType.Item, ID + 1000000)))
        {
            return false;
        }

        float remain = Cooldown.RecastTimeOneChargeRaw - Cooldown.RecastTimeElapsedRaw;

        return remain <= DataCenter.DefaultGCDRemain && (ItemCheck == null || ItemCheck()) && HasIt;
    }

    /// <summary>
    /// Use this item.
    /// </summary>
    /// <returns></returns>
    public unsafe bool Use()
    {
		if (Player.Object == null)
		{
			return false;
		}

		if (_item.RowId == 0)
        {
            return false; // Check if the item is uninitialized
        }

        if (_item.RowId == 4570 && DataCenter.DeathTarget != null)
        {
            if (InventoryManager.Instance()->GetInventoryItemCount(ID, true) > 0)
            {
                return ActionManager.Instance()->UseAction(ActionType.Item, ID + 1000000, DataCenter.DeathTarget.GameObjectId, 65535);
            }
            else
            {
                return ActionManager.Instance()->UseAction(ActionType.Item, ID, DataCenter.DeathTarget.GameObjectId, 65535);
            }
        }

        if (InventoryManager.Instance()->GetInventoryItemCount(ID, true) > 0)
        {
            return ActionManager.Instance()->UseAction(ActionType.Item, ID + 1000000, Player.Object.GameObjectId, A4);
        }
        else
        {
            return ActionManager.Instance()->UseAction(ActionType.Item, ID, Player.Object.GameObjectId, A4);
        }
    }

    /// <summary>
    /// To String.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Name;
    }
}
