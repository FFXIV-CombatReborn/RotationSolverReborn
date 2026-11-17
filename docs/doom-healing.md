# Heal-to-full Doom investigation

## Existing encounter handling

The project already handles individual boss mechanics. `Basic/DataCenter.cs` combines territory IDs with cast action IDs, for example M11S Charybdistopia (46117), Orbonne Agrias's Judgment Blade (14423), and Lakshmi's raid-wide attacks. `Updaters/StateUpdater.cs` uses these checks to decide when to heal or mitigate. The shared rotation also prevents healing spells during M9S Hell in a Cell.

Doom healing is status based, rather than tied to a particular boss cast. `Basic/Helpers/StatusHelper.cs` already lists `Doom_1769` and `Doom_5473`, whose descriptions explicitly say that the effect dissipates once fully healed. Other statuses named Doom can require Esuna or an encounter mechanic, so they must not all be treated as heal-to-full Doom.

Existing healing support included single/area healing requests in `StateUpdater`, a synthetic low health ratio in `ObjectHelper`, and Scholar Emergency Tactics handling. WHM, SCH, AST, and SGE all consume the shared healing requests and targeting logic.

## Problems and changes

| Problem | Result after this change |
| --- | --- |
| Doom detection only checked the status, including full-HP or dead targets. | Only living targets with actual HP below maximum request Doom healing. A lingering status is monitored again if HP drops. |
| Party HP prediction could report full HP before a heal landed. | Doom and emergency targets use actual HP in party health calculations, keeping the healing request active until HP is restored. |
| Combat state and time-to-kill could suppress healing while Doom remained active. | Doom healing can continue between combat phases and near the end of a fight, while respecting the automatic healing setting and permitted role. |
| Per-action healing thresholds could exclude urgent targets, and always prioritizing Doom could delay saving a dying tank. | Emergency targets take priority by actual HP ratio, followed by Doom targets ordered by remaining countdown and actual HP ratio. Both bypass ordinary action healing thresholds. |
| Full-HP Doom targets inflated the number requiring area healing. | Only injured, healable Doom targets count toward the existing single/area requests. |

The fix uses the existing job rotations and available healing actions. Action availability, range, MP, casting restrictions, healing blocks, and existing encounter safety checks still apply. Monitoring a status cannot guarantee survival when healing cannot be performed.

## Emergency protection

All living, healable targets at or below 30% actual HP qualify for emergency healing. The existing tank, healer, and self priority settings can extend that threshold (defaults: tank 45%, healer/self 40%). The 30% floor remains even if a role setting is lower. Among eligible emergencies, the lowest actual HP percentage goes first; Doom's synthetic 1% health value does not influence this comparison. As soon as the emergency target recovers above its threshold, normal Doom countdown priority resumes.

Automatic area healing must cover the most critical eligible party member within the normal 30-yalm single-heal range and line of sight. Targeted area heals prefer anchors covering that member before maximizing the number affected. If a self-centered or ground-targeted group heal misses that member, the action is rejected so the rotation can try another area heal or fall back to single-target healing. A critical target outside the single-heal range or behind an obstruction does not block otherwise useful group healing. Explicit targeting overrides are respected. This checks coverage, not whether a particular heal's potency or cast time guarantees survival; job-specific action selection remains in the existing rotation.

## Validation

- Release plugin build succeeded with zero warnings and zero errors, using Dalamud API 15 and Roslyn 5.9. The installed .NET 10.0.100 compiler was too old for the upstream source generator, so the matching compiler toolset was imported for this local build without changing project dependencies.
- `tests/doom-healing/verify.py` extracts the production Doom detection, HP prediction, healing-state, healing-target, area-coverage, and area-anchor methods. Native game interfaces and geometry are stubbed. All 64 scenarios pass. The original 31-scenario harness had 17 failures against upstream commit `866988a7` before the first fix.
- Scenarios covered both supported Doom statuses, one HP missing with an active HoT, pending healing predicting full HP, full/dead/invalid targets, repeated damage while the status remains, multiple countdowns, target handoff after healing, action thresholds, automatic healing disabled, non-healer restrictions, and the M9S spell restriction.
- `git diff --check` passed.

Run the regression harness from the repository root with `python tests/doom-healing/verify.py` (requires Python 3 and .NET 10). It builds in a temporary directory without changing project dependencies.

This has not been validated inside a live game encounter. To verify in game, enable automatic healing and test WHM/SCH/AST/SGE against an encounter applying status 1769 or 5473. Confirm that injured affected players receive healing even above the usual HP threshold, several affected players trigger area/single healing as appropriate, targets stop requesting Doom healing at actual full HP, and damage before status removal triggers healing again. Also verify that ordinary Doom requiring Esuna continues through the existing dispel logic. With a low-HP tank and a nearly full Doom target, check that the tank is rescued first and that healing returns to Doom afterward; repeat with the tank outside the group heal's coverage.
