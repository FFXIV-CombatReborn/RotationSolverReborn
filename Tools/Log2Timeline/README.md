# Log2Timeline

Turns an FFLogs pull into a draft timeline and CD plan for RSR's CD plan rotations: the Beiruta healers (AST, SCH, SGE, WHM), the melee and caster rotations (VPR, DRG, RPR, SAM, MNK, NIN, RDM, BLM, SMN, PCT, BRD, MCH, DNC) and the tank rotations (PLD, WAR, DRK, GNB).

## Quick start

1. Install **Python 3** from <https://www.python.org/downloads/> if you don't have it. Nothing else is needed (no `pip install`).
2. In VS Code, open `Tools/Log2Timeline/log2timeline.py`.
3. Fill in **YOUR SETTINGS** at the top of the file, or leave them empty and you'll be asked:

   ```python
   LOG_URL = "https://www.fflogs.com/reports/AbCd1234EfGh5678#fight=12"   # the pull to use
   JOB = "SGE"                    # SGE / WHM / AST / SCH / VPR / DRG / RPR / SAM / MNK / NIN / RDM / BLM / SMN / PCT / BRD / MCH / DNC / PLD / WAR / DRK / GNB, or "" for every one of them in the pull
   CLIENT_ID = ""                 # your FFLogs API client, see below ("" = asked once, then remembered)
   CLIENT_SECRET = ""
   INSTALL = None                 # None = ask, True = straight into RotationSolver's Timelines folder
   ```

4. Press **Run Python File** (▷, top right). This needs the Python extension; VS Code offers to install it when you open a `.py` file. Any remaining questions come up in the terminal.

### Where the files go, and when RSR sees them

RSR only reads `%APPDATA%\XIVLauncher\pluginConfigs\RotationSolver\Timelines\`.

| `INSTALL` / your answer | Saved to | To use it in game |
| --- | --- | --- |
| `True`, or **yes** when asked | the Timelines folder | Nothing to do. Enter the duty as that job. |
| `False`, or **no** when asked | `Tools\Log2Timeline\output\` | Copy the folder into the Timelines folder first. RSR never looks in `output\`. |

Once it's in Timelines, there's no restart or rebuild needed. Out of combat in that duty, chat says "... loaded (N rows)".

### When the duty already has a timeline

The script first looks for a timeline of the same duty in your Timelines folder, then in the plugin's built-in timelines (the installed plugin's, then the repo's `RotationSolver.Timeline\`). If it finds one, it keeps that timeline and only adds the plan files:
- Your own timeline: the plans go into its folder.
- A built-in timeline: the plans go into your Timelines folder under the same path (e.g. `Dawntrail\Ultimate\DancingMadUltimate`), without a `timeline.csv`. The plugin reads them next to the built-in timeline, and the built-in plans are never changed. Built-in plans stay the default; pick yours in the CD Plan picker.
- The plan times are moved onto that timeline's phases. The script plays the timeline's sync rows against the pull, the way the plugin does in game, to see where each phase's clock was.
- It prints a sync check: how many of the timeline's sync rows fired in the pull, the biggest drift per phase (when a sync fired vs when the timeline expected it), and rows that never fired. A `!` marks a phase more than 1.5s off; plan rows there may be off by about that much.
- If a phase of that timeline never synced in the pull, it says so; check the plan rows there.
- The output keeps the existing folder's name and place (no ` Test`), e.g. `Dawntrail\Ultimate\DancingMadUltimate` for a repo timeline. When the timeline came from the repo, a copy of it goes into the output folder too.

Use `--new-timeline` to make a separate timeline anyway. Two folders for the same duty compete in game, so move the other one out while you test.

### Which link to use

Open the report on fflogs.com and click the pull you want, then copy the address bar:

| Link | Pull used |
| --- | --- |
| `https://www.fflogs.com/reports/AbCd1234EfGh5678#fight=12` | pull 12 |
| `https://www.fflogs.com/reports/AbCd1234EfGh5678?fight=last` | the last pull in the report |
| `https://www.fflogs.com/reports/AbCd1234EfGh5678` | you pick from a list (the last kill is the default) |

Use a **kill**. Wipes cut the timeline short.

### FFLogs API client

The script needs a free API client to read logs. You only make it once.

1. Open <https://www.fflogs.com/api/clients> and log in.
2. Click **Create Client**.
   - Any name works, e.g. Log2Timeline.
   - Redirect URL: `https://localhost`.
   - Leave "Public Client" unticked.
3. Copy the **client ID** and **client secret**. The secret is only shown once.

You can paste them into `CLIENT_ID` / `CLIENT_SECRET`, or leave those empty. In that case the script asks the first time and can remember them in `fflogs_credentials.json`, which git ignores; delete that file to forget them.

**Don't commit or share `log2timeline.py` with your secret typed into it.**

## What you get

A folder named after the duty, inside expansion and category folders, e.g. `Endwalker\Ultimate\TheOmegaProtocolUltimate Test` or `Dawntrail\Savage\M10S The Xtremes Test` (the plugin finds timelines at any depth in its Timelines folder). It contains (when the duty has no timeline yet; see above otherwise):

**`timeline.csv`**
- The duty name, looked up from the log automatically.
- Sync rows: the start of each boss cast that has a cast bar, plus the boss becoming targetable or untargetable.
- In ultimates, the timeline is split into phases where FFLogs changes phase.

**`Default.<JOB>.plan.csv`** for each healer, **`M1.<JOB>.plan.csv`** / **`M2.<JOB>.plan.csv`** for each melee
- Melee are numbered by Feint: M1 is the melee with more Feints (the one keeping it on cooldown), M2 the other.

**`M1.Feint.plan.csv`** / **`M2.Feint.plan.csv`** when two melee used Feint
- Feint profiles: only the Feint rows of each melee slot. Every melee job uses them when "Feint from" is Auto, M1 or M2; the job plan's own Feint rows are then not used, everything else still comes from the job plan.
- With one melee (double caster), no profile is made; its Feint stays in its job plan (set "Feint from" to Job plan).

**`R2.Addle.plan.csv`** / **`FakeMelee.Addle.plan.csv`** for casters
- Addle profiles, used by every caster when "Addle from" is Auto, R2 or Fake melee. R2 is the caster with more Addles; with two casters, the other one is the Fake melee (Addle on a melee's Feint spots).
- Caster windows are wider (6s by default) because casters often can't weave for a while; they still open before the first hit they cover.
- Only the actions the built-in plans schedule, from what that player did in the log:
  - SCH: fairy, Soil, Expedient, Indom, Concitation, the Recitation/Adlo/Deploy chain, Seraph/Seraphism, Excog
  - SGE: Kerachole, Physis II, Panhaima, Holos, Zoe, Philosophia, Ixochole
  - WHM: Rapture, Plenary, Asylum, Temperance, Caress, Medica III, Bell, Cure III
  - AST: Star, CU, Helios Conjunction, Opposition, Horoscope, Sun Sign, Neutral Sect, Macrocosmos, Lightspeed
  - VPR, DRG: Feint
  - RPR: Feint, Arcane Crest
  - SAM: Feint, Tengentsu, Meditate
  - MNK: Feint, Mantra, Riddle of Earth
  - RDM: Addle, Magick Barrier
  - BLM: Addle, Ley Lines, Triplecast, Manaward
  - SMN: Addle
  - PCT: Addle, Tempera Grassa
  - BRD: Troubadour, Nature's Minne, the three songs
  - MCH: Tactician, Dismantle, Automaton Queen, Queen Overdrive
  - DNC: Shield Samba, Curing Waltz, Improvisation
  - PLD: Divine Veil, Holy Sheltron, Intervention, Guardian, Bulwark, Rampart, Reprisal; optional: Passage of Arms, Hallowed Ground, Provoke, Shirk, tank stance
  - WAR: Shake It Off, Damnation, Thrill of Battle, Bloodwhetting, Nascent Flash, Rampart, Reprisal; optional: Holmgang, Provoke, Shirk, tank stance
  - DRK: Dark Missionary, Shadowed Vigil, Dark Mind, The Blackest Night, Oblation, Rampart, Reprisal; optional: Living Dead, Provoke, Shirk, tank stance
  - GNB: Heart of Light, Great Nebula, Camouflage, Heart of Corundum, Rampart, Reprisal; optional: Superbolide, Provoke, Shirk, tank stance
  - Tank stance is written as "Tank Stance" rows with Target On / Off (from one stance press to the next): they say when the stance should be on or off, and the rotation only changes it when it isn't already.
  - Tank plans are named MT.<JOB> / OT.<JOB> (MT = the tank the boss auto-attacks first). Intervention, Nascent Flash, Heart of Corundum, The Blackest Night, Oblation and Shirk get Target Self / OtherTank / LowHP from who they hit in the log.
  - NIN: Feint, Shade Shift, and downtime mudras: a ninjutsu (Raiton, Suiton, ...) whose mudras went in before a boss came back and that went off right after. Its row opens 5s before the boss is targetable and closes 2s after; it needs "Downtime Mudra (experimental)" on.
- Mitigation is timed so that:
  - it's up before the first raidwide it covers,
  - it still lasts until the last one it covers,
  - and its window is longer than 2.5s.

  The `Note` column says which hits it covers.

It's a draft, so check it before use:
- Delete sync rows for casts that don't happen on every pull.
- Delete plan rows that were the player's own mistakes.

The ` Test` on the end of the folder name marks it as a draft.

Logs where only you were recorded, such as Duty Support, still work, but raidwides are guessed from your own hits.

## Command line (optional)

```
python log2timeline.py "<fflogs link>" [SGE|WHM|AST|SCH|VPR|DRG|RPR|SAM|MNK|NIN|RDM|BLM|SMN|PCT|BRD|MCH|DNC|PLD|WAR|DRK|GNB] [options]
```

Command-line values win over YOUR SETTINGS.

| Option | What it does |
| --- | --- |
| `--install` | Write straight into RotationSolver's Timelines folder |
| `--out <folder>` | Write somewhere else (default: `output\` next to the script) |
| `--force` | Replace existing files without asking |
| `--healer 2` | Pick the player by number in the pull's list of plan jobs (sorted by job, e.g. `1. SGE #1, 2. SGE #2, 3. VPR`) |
| `--duty "Name"` | Override the duty name if the lookup is wrong |
| `--suffix Test` | Text added to the folder name of a new timeline (`--suffix ""` for none) |
| `--new-timeline` | Make a separate timeline even when the duty already has one |
| `--min-cast 2.0` | Shortest boss cast bar that becomes a sync row (seconds) |
| `--window 3.0` | Plan window length (seconds, never below 2.5) |
| `--client-id` / `--client-secret` | FFLogs credentials (or set `FFLOGS_CLIENT_ID` / `FFLOGS_CLIENT_SECRET`) |

To change which actions are planned or how long each mitigation lasts, edit `PLANNED` and `MIT_DURATION` in `log2timeline.py`.
