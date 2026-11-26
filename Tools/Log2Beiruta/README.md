# Log2Beiruta

Turns an FFLogs pull into a draft timeline and healer plan for the Beiruta rotations (AST, SCH, SGE, WHM).

## Quick start

1. Install **Python 3** from <https://www.python.org/downloads/> if you don't have it. Nothing else is needed (no `pip install`).
2. In VS Code, open `Tools/Log2Beiruta/log2beiruta.py`.
3. Fill in **YOUR SETTINGS** at the top of the file, or leave them empty and you'll be asked:

   ```python
   LOG_URL = "https://www.fflogs.com/reports/AbCd1234EfGh5678#fight=12"   # the pull to use
   JOB = "SGE"                    # SGE / WHM / AST / SCH, or "" for every healer in the pull
   CLIENT_ID = ""                 # your FFLogs API client, see below ("" = asked once, then remembered)
   CLIENT_SECRET = ""
   INSTALL = None                 # None = ask, True = straight into RotationSolver's Timelines folder
   ```

4. Press **Run Python File** (▷, top right). This needs the Python extension; VS Code offers to install it when you open a `.py` file. Any remaining questions come up in the terminal.

### Where the files go, and when Beiruta sees them

Beiruta only reads `%APPDATA%\XIVLauncher\pluginConfigs\RotationSolver\Timelines\`.

| `INSTALL` / your answer | Saved to | To use it in game |
| --- | --- | --- |
| `True`, or **yes** when asked | the Timelines folder | Nothing to do. Enter the duty as that healer. |
| `False`, or **no** when asked | `Tools\Log2Beiruta\output\` | Copy the folder into the Timelines folder first. Beiruta never looks in `output\`. |

Once it's in Timelines, there's no restart or rebuild needed. Out of combat in that duty, chat says "... loaded (N rows)". If nothing shows up, the duty already has another timeline folder, which is used instead; move that one out while you test.

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
   - Any name works, e.g. Log2Beiruta.
   - Redirect URL: `https://localhost`.
   - Leave "Public Client" unticked.
3. Copy the **client ID** and **client secret**. The secret is only shown once.

You can paste them into `CLIENT_ID` / `CLIENT_SECRET`, or leave those empty. In that case the script asks the first time and can remember them in `fflogs_credentials.json`, which git ignores; delete that file to forget them.

**Don't commit or share `log2beiruta.py` with your secret typed into it.**

## What you get

A folder named after the duty, e.g. `TheOmegaProtocolUltimate Test`, containing:

**`timeline.csv`**
- The duty name, looked up from the log automatically.
- Sync rows: the start of each boss cast that has a cast bar, plus the boss becoming targetable or untargetable.
- In ultimates, the timeline is split into phases where FFLogs changes phase.

**`Default.<JOB>.plan.csv`** for each healer
- Only the actions the built-in plans schedule, from what that player did in the log:
  - SCH: fairy, Soil, Expedient, Indom, Concitation, the Recitation/Adlo/Deploy chain, Seraph/Seraphism, Excog
  - SGE: Kerachole, Physis II, Panhaima, Holos, Zoe, Philosophia, Ixochole
  - WHM: Rapture, Plenary, Asylum, Temperance, Caress, Medica III, Bell, Cure III
  - AST: Star, CU, Helios Conjunction, Opposition, Horoscope, Sun Sign, Neutral Sect, Macrocosmos, Lightspeed
- Mitigation is timed so that:
  - it's up before the first raidwide it covers,
  - it still lasts until the last one it covers,
  - and its window is longer than 2.5s.

  The `Note` column says which hits it covers.

It's a draft, so check it before use:
- Delete sync rows for casts that don't happen on every pull.
- Delete plan rows that were the player's own mistakes.

The ` Test` on the end of the folder name marks it as a draft. If that duty already has another timeline folder, the other one is used, so move it out while you test.

Logs where only you were recorded, such as Duty Support, still work, but raidwides are guessed from your own hits.

## Command line (optional)

```
python log2beiruta.py "<fflogs link>" [SGE|WHM|AST|SCH] [options]
```

Command-line values win over YOUR SETTINGS.

| Option | What it does |
| --- | --- |
| `--install` | Write straight into RotationSolver's Timelines folder |
| `--out <folder>` | Write somewhere else (default: `output\` next to the script) |
| `--force` | Replace existing files without asking |
| `--healer 2` | Pick the healer by number in the pull's healer list (sorted by job, e.g. `1. SGE #1, 2. SGE #2`) |
| `--duty "Name"` | Override the duty name if the lookup is wrong |
| `--suffix Test` | Text added to the folder name (`--suffix ""` for none) |
| `--min-cast 2.0` | Shortest boss cast bar that becomes a sync row (seconds) |
| `--window 3.0` | Plan window length (seconds, never below 2.5) |
| `--client-id` / `--client-secret` | FFLogs credentials (or set `FFLOGS_CLIENT_ID` / `FFLOGS_CLIENT_SECRET`) |

To change which actions are planned or how long each mitigation lasts, edit `PLANNED` and `MIT_DURATION` in `log2beiruta.py`.
