
# [![](https://raw.githubusercontent.com/FFXIV-CombatReborn/RebornAssets/main/IconAssets/RSR_Icon.png)](https://github.com/FFXIV-CombatReborn/RotationSolverReborn)

**RotationSolverReborn — jkleinne fork**

![Github License](https://img.shields.io/github/license/FFXIV-CombatReborn/RotationSolverReborn.svg?label=License&style=for-the-badge)

This is a personal fork of [FFXIV-CombatReborn/RotationSolverReborn](https://github.com/FFXIV-CombatReborn/RotationSolverReborn). It tracks upstream and layers one experimental feature on top: a scoring-based PvP hostile target selector (`PvPSmart`). PvE behavior is identical to upstream.

If you don't specifically want the `PvPSmart` targeting mode, install upstream RSR instead. This fork does not publish its own Dalamud plugin repository; it exists primarily as a development branch for the PvP targeting work.

## What this fork adds

A new `TargetingType.PvPSmart` mode that replaces the role-blind `Auto(LowHP)` cycle in PvP with a scoring-based selector. For each candidate hostile, the scorer composes a weighted scalar over pure factors and picks the argmax:

- **Invuln short-circuit** — Guard, Hallowed Ground, Living Dead, Holmgang, Superbolide are skipped outright
- **Role value** — Healer / Ranged DPS weighted above Melee / Tank
- **Effective HP & finish** — current HP scaled by active mitigation statuses, with a finish-kill bias when a candidate is within burst range
- **Mitigation penalty** — heavy DR cooldowns deprioritize a target during the window they're active
- **Distance penalty** — soft falloff as targets approach the effective range edge
- **Hysteresis** — small sticky bonus for the previous target to prevent GCD-to-GCD oscillation between near-equal candidates
- **Crystal carrier awareness** *(Crystalline Conflict)* — the hostile holding the crystal gains a bonus
- **LB cast awareness** — hostiles mid-cast on a Limit Break gain a bonus (interrupt priority)
- **Isolation factor** — sigmoid bonus the further a hostile is from its nearest ally (catches stragglers)
- **Threat factor** — bonus when a hostile is targeting a low-HP ally or a party healer (peel priority)

Two preset weight profiles (Casual, Ranked) are bundled, plus a Custom preset for hand-tuned weights. A toggleable debug overlay renders the full per-target score breakdown in real time for tuning.

The existing `PvPHealers` / `PvPDPS` / `PvPTanks` modes remain as explicit role overrides.

### Status & caveats

- Starting weights are conservative seeds. Empirical tuning across Ranked CC matches is pending.
- Crystal-carrier `StatusID` and the `PvPLBs.json` database are stubs awaiting in-game observation; the carrier and LB factors evaluate to zero until those are populated.

## Upstream features

Everything below is inherited unchanged from upstream RSR:

- **Dynamic Rotation Guidance (Training Mode)** — real-time rotation suggestions tailored to the in-game situation
- **Customizable Settings** — adjust rotations per preference, encounter, and boss mechanics
- **Comprehensive Database** — extensive class ability coverage for accurate rotation
- **User-Friendly Interface** — clean ImGui surface
- **Regular Updates** — upstream tracks game patches and class changes; this fork periodically pulls from it

## Installing

This fork is not published to a Dalamud plugin repository. To run it you need to build from source against a Windows + Dalamud dev environment. If you want the official binary distribution, use upstream's instructions:

- Enter `/xlsettings` in chat and open the Experimental tab
- Scroll past DevPlugins to the Custom Plugin Repositories section
- Paste the upstream repo URL into a free text input:

```
https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json
```

- Click `+`, tick the new entry's checkbox, and save

CN and KR plugin master files for upstream:

```
https://raw.githubusercontent.com/FFXIV-CombatReborn/RotationSolverReborn/refs/heads/main/pluginmasterCN.json
https://raw.githubusercontent.com/FFXIV-CombatReborn/RotationSolverReborn/refs/heads/main/pluginmasterKR.json
```

## Contributing

PvP targeting work goes here. Anything else should be contributed upstream:

- For PvP scoring changes (factors, weights, debug overlay): fork this repo, branch from `main`, open a PR against `jkleinne/RotationSolverReborn:main`
- For everything else (rotations, PvE behavior, core engine): contribute to [upstream RSR](https://github.com/FFXIV-CombatReborn/RotationSolverReborn) instead — changes there flow into this fork on the next sync

Combat rotation changes should be validated against [Stone, Sky, Sea](https://ffxiv.consolegameswiki.com/wiki/Stone,_Sky,_Sea) per expansion before submission.

## Links

- Upstream rotation definitions: [`RotationSolver/RebornRotations`](https://github.com/FFXIV-CombatReborn/RotationSolverReborn/tree/main/RotationSolver/RebornRotations)
- Upstream Discord: [https://discord.gg/p54TZMPnC9](https://discord.gg/p54TZMPnC9)
