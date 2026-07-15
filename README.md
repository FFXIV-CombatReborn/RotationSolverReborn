
# [![](https://raw.githubusercontent.com/FFXIV-CombatReborn/RebornAssets/main/IconAssets/RSR_Icon.png)](https://github.com/FFXIV-CombatReborn/RotationSolverReborn)

**RotationSolverReborn — Training Mode fork**

![Github License](https://img.shields.io/github/license/FFXIV-CombatReborn/RotationSolverReborn.svg?label=License&style=for-the-badge)
[![](https://dcbadge.limes.pink/api/server/p54TZMPnC9)](https://discord.gg/p54TZMPnC9)

This is a personal fork of [RotationSolverReborn](https://github.com/FFXIV-CombatReborn/RotationSolverReborn), locked down to **highlight-only training mode**. It computes and visually highlights the recommended next action on your hotbar — it never presses actions, switches targets, runs macros, cancels casts, restricts movement, or lets an external plugin drive any of that on your behalf. You press your own buttons; this just tells you which one.

## What's different from upstream

Everything about auto-execution and auto-behavior is gated behind a single flag, `TrainingModeGate.ExecutionLocked` (`RotationSolver\TrainingModeGate.cs` and its mirror `RotationSolver.Basic\TrainingModeGate.cs`), hardcoded `true` in this fork. Gated rather than deleted, so the diff against upstream stays small and localized — job rotation decision logic, per-job tuning, and most settings are untouched. Concretely, this fork never:

- Presses an action or item on your behalf (`BaseAction.Use()`/`BaseItem.Use()`, the actual execution primitives, refuse to fire)
- Auto-selects or auto-switches your target, including the "target freely" nearest-enemy behavior
- Runs in-game macros automatically (duty-start/end or action-use triggers)
- Cancels your casts or forcibly locks your movement
- Lets external plugins (via IPC) or chat commands flip it into an auto-executing state — chat only accepts `/rotation off`

What's kept, because it only ever affects *what gets recommended*, never what happens in-game: per-job rotation tuning, the internal target-resolution logic each job's recommendations use (e.g. "heal the lowest-HP party member"), and the manual Special-Command bias buttons (Heal/Defense/Burst/Move/etc. — your own clicks, not automatic).

The Auto/Manual/AutoDuty/Henched/PvP mode machinery, the Debug/AutoDuty/Duty/Target settings tabs, and the targeting-priority UI are hidden — there's just a single "Training Mode: On/Off" toggle now.

## Updating from upstream

`main` is kept as a pure, untouched mirror of upstream `FFXIV-CombatReborn/RotationSolverReborn`'s `main` branch. All of this fork's changes live on the `training-mode` branch (the one to actually build/run).

To pick up a game-patch update from upstream:
```
git fetch upstream
git checkout main && git merge --ff-only upstream/main && git push origin main
git checkout training-mode && git merge main
```
Since this fork's edits are small, additive gate-checks scattered across a couple dozen files rather than large rewrites, conflicts should be rare and localized even when upstream touches the same files.

## Building / running

This fork isn't published to a plugin repository — build it locally and dev-load it via Dalamud (`/xlsettings` → Experimental → Dev Plugin Locations, pointing at this repo's build output), rather than installing RotationSolverReborn from the Combat Reborn repo, which would get you the unmodified upstream plugin instead of this one.

```
dotnet build RotationSolver.sln -c Debug
```

## Links

- Upstream project: https://github.com/FFXIV-CombatReborn/RotationSolverReborn
- Job rotation definitions: [`RotationSolver/RebornRotations`](RotationSolver/RebornRotations)
