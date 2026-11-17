# Heal-to-full Doom investigation

## Existing encounter handling

The project already handles individual boss mechanics. `Basic/DataCenter.cs` combines territory IDs with cast action IDs, for example M11S Charybdistopia (46117), Orbonne Agrias's Judgment Blade (14423), and Lakshmi's raid-wide attacks. `Updaters/StateUpdater.cs` uses these checks to decide when to heal or mitigate. The shared rotation also prevents healing spells during M9S Hell in a Cell.

Doom healing is status based, rather than tied to a particular boss cast. `Basic/Helpers/StatusHelper.cs` already lists `Doom_1769` and `Doom_5473`, whose descriptions explicitly say that the effect dissipates once fully healed. Other statuses named Doom can require Esuna or an encounter mechanic, so they must not all be treated as heal-to-full Doom.

Existing healing support included single/area healing requests in `StateUpdater`, a synthetic low health ratio in `ObjectHelper`, and Scholar Emergency Tactics handling. WHM, SCH, AST, and SGE all consume the shared healing requests and targeting logic.

## Problems and changes

| Problem | Result after this change |
| --- | --- |
| Doom detection only checked the status, including full-HP or dead targets. | Only living targets with actual HP below maximum request Doom healing. A lingering status is monitored again if HP drops. |
| Party HP prediction could report full HP before a heal landed. | Doom targets use actual HP in party health calculations, keeping the healing request active until HP is restored. |
| Combat state and time-to-kill could suppress healing while Doom remained active. | Doom healing can continue between combat phases and near the end of a fight, while respecting the automatic healing setting and permitted role. |
| Per-action healing thresholds and self/healer/tank preferences could exclude or displace a Doom target. | Eligible Doom targets bypass ordinary healing thresholds and take priority. Multiple targets are ordered by remaining countdown, then actual HP ratio. |
| Full-HP Doom targets inflated the number requiring area healing. | Only injured, healable Doom targets count toward the existing single/area requests. |

The fix uses the existing job rotations and available healing actions. Action availability, range, MP, casting restrictions, healing blocks, and existing encounter safety checks still apply. Monitoring a status cannot guarantee survival when healing cannot be performed.

## Validation

- Release plugin build succeeded with zero warnings and zero errors, using Dalamud API 15 and Roslyn 5.9. The installed .NET 10.0.100 compiler was too old for the upstream source generator, so the matching compiler toolset was imported for this local build without changing project dependencies.
- A temporary regression harness extracted the production Doom detection, HP prediction, healing-state, and healing-target methods. Native game interfaces were stubbed. All 31 scenarios passed on the fix; 17 failed against upstream commit `866988a7`.
- Scenarios covered both supported Doom statuses, one HP missing with an active HoT, pending healing predicting full HP, full/dead/invalid targets, repeated damage while the status remains, multiple countdowns, target handoff after healing, action thresholds, automatic healing disabled, non-healer restrictions, and the M9S spell restriction.
- `git diff --check` passed.

This has not been validated inside a live game encounter. To verify in game, enable automatic healing and test WHM/SCH/AST/SGE against an encounter applying status 1769 or 5473. Confirm that injured affected players receive healing even above the usual HP threshold, several affected players trigger area/single healing as appropriate, targets stop requesting Doom healing at actual full HP, and damage before status removal triggers healing again. Also verify that ordinary Doom requiring Esuna continues through the existing dispel logic.
