using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.GameFunctions;
using ECommons.Logging;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using RotationSolver.Basic.Configuration;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace RotationSolver.Basic;

/// <summary>
/// Provides various services and utilities for the RotationSolver.
/// </summary>
internal class Service : IDisposable
{
    public const string COMMAND = "/rotation";
    public const string ALTCOMMAND = "/rsr";
    public const string AUTOCOMMAND = "/rotation Auto";
    public const string OFFCOMMAND = "/rotation Off";
    public const string USERNAME = "FFXIV-CombatReborn";
    public const string REPO = "RotationSolverReborn";

    [EzHook("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8", nameof(ActorVfxCreateDetour), true)]
    private readonly EzHook<ActorVfxCreateDelegate2> actorVfxCreateHook = null!;
    private unsafe delegate IntPtr ActorVfxCreateDelegate2(char* a1, nint a2, nint a3, float a4, char a5, ushort a6, char a7);

    // From https://GitHub.com/PunishXIV/Orbwalker/blame/master/Orbwalker/Memory.cs#L74-L76
    [Signature("F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7", ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Infallible)]
#pragma warning disable IDE0044 // cannot be made readonly
    private static IntPtr forceDisableMovementPtr = IntPtr.Zero;
#pragma warning restore IDE0044

    private static unsafe ref int ForceDisableMovement => ref *(int*)(forceDisableMovementPtr + 4);

    private static bool _canMove = true;

    /// <summary>
    /// Gets or sets a value indicating whether the player can move.
    /// </summary>
    internal static unsafe bool CanMove
    {
        get => ForceDisableMovement == 0;
        set
        {
            bool realCanMove = value || DataCenter.NoPoslock;
            if (_canMove == realCanMove)
            {
                return;
            }

            _canMove = realCanMove;

            if (!realCanMove)
            {
                ForceDisableMovement++;
            }
            else if (ForceDisableMovement > 0)
            {
                ForceDisableMovement--;
            }
        }
    }

    private unsafe IntPtr ActorVfxCreateDetour(char* a1, nint a2, nint a3, float a4, char a5, ushort a6, char a7)
    {
        try
        {
            string path = Dalamud.Memory.MemoryHelper.ReadString(new nint(a1), Encoding.ASCII, 256);
            if (Svc.Objects.CreateObjectReference(a2) is not IBattleChara battleChara || string.IsNullOrEmpty(path))
            {
                return actorVfxCreateHook!.Original(a1, a2, a3, a4, a5, a6, a7);
            }

            VfxNewData newVfx = new(battleChara.GameObjectId, path, battleChara.RemainingCastTime);
            DataCenter.VfxDataQueue.Enqueue(newVfx);
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to process VfxCreateDetour: {ex.Message}");
        }

        return actorVfxCreateHook!.Original(a1, a2, a3, a4, a5, a6, a7);
    }

    /// <summary>
    /// Gets the remaining countdown time.
    /// </summary>
    public static float CountDownTime => Countdown.TimeRemaining;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public static Configs Config { get; set; } = new Configs();

    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static Configs ConfigDefault { get; set; } = new Configs();

    /// <summary>
    /// Initializes a new instance of the <see cref="Service"/> class.
    /// </summary>
    public Service()
    {
        Svc.Hook.InitializeFromAttributes(this);
        EzSignatureHelper.Initialize(this);
    }

    /// <summary>
    /// Gets the adjusted action ID.
    /// </summary>
    /// <param name="id">The action ID.</param>
    /// <returns>The adjusted action ID.</returns>
    public static ActionID GetAdjustedActionId(ActionID id)
    {
        return (ActionID)GetAdjustedActionId((uint)id);
    }

    /// <summary>
    /// Gets the adjusted action ID.
    /// </summary>
    /// <param name="id">The action ID.</param>
    /// <returns>The adjusted action ID.</returns>
    public static unsafe uint GetAdjustedActionId(uint id)
    {
        return ActionManager.Instance()->GetAdjustedActionId(id);
    }

    private static readonly ConcurrentDictionary<Type, AddonAttribute?> AddonCache = new();

    /// <summary>
    /// Gets the addons of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the addon.</typeparam>
    /// <returns>A collection of addon pointers.</returns>
    public static IEnumerable<IntPtr> GetAddons<T>() where T : struct
    {
        AddonAttribute? addon = AddonCache.GetOrAdd(typeof(T), t => t.GetCustomAttribute<AddonAttribute>());

        if (addon is null)
        {
            return [];
        }

        int identifierCount = 0;
        foreach (var _ in addon.AddonIdentifiers)
        {
            identifierCount++;
        }
        if (identifierCount == 0)
        {
            return [];
        }

        // Use ArrayPool to minimize allocations
        var pool = ArrayPool<nint>.Shared;
        var buffer = pool.Rent(identifierCount);
        int count = 0;

        try
        {
            foreach (string str in addon.AddonIdentifiers)
            {
                nint ptr = Svc.GameGui.GetAddonByName(str, 1);
                if (ptr != IntPtr.Zero)
                {
                    buffer[count++] = ptr;
                }
            }

            var result = new nint[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = buffer[i];
            }
            return result;
        }
        finally
        {
            pool.Return(buffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<nint>());
        }
    }

    /// <summary>
    /// Gets the Excel sheet of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the Excel row.</typeparam>
    /// <returns>The Excel sheet.</returns>
    public static ExcelSheet<T> GetSheet<T>() where T : struct, IExcelRow<T>
    {
        return Svc.Data.GetExcelSheet<T>()!;
    }

    /// <summary>
    /// Releases unmanaged resources and performs other cleanup operations.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose the EzHook instance
            actorVfxCreateHook?.Disable();

            // Clean up ForceDisableMovement if necessary
            if (!_canMove && ForceDisableMovement > 0)
            {
                ForceDisableMovement--;
            }
        }
    }

    /// <summary>
    /// Releases unmanaged resources and performs other cleanup operations.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}