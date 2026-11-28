#!/usr/bin/env python3
"""
log2timeline.py - turn an FFLogs pull into a draft timeline.csv + CD plan for RSR's CD plan rotations (healers, DPS and tanks).

How to run: fill in YOUR SETTINGS below (or leave them empty to be asked), then press Run Python File
in VS Code. See README.md for the full guide.

What the draft contains:
- timeline.csv: CombatStart, a CastStart sync row for every enemy cast with a cast bar (>= --min-cast s),
  boss Targetable / Untargetable rows, split into phases on FFLogs' phase changes. No Damage rows.
- plan: only the actions the built-in plans schedule (raid mitigation, party heals, their set-up),
  taken from what that player in the log did (melee: Feint and their own mitigation). No '# Set.' lines.
- Mitigation rows are timed so the action is up before the first party hit it covers, still lasts to the
  last hit it covers, and has a window longer than 2.5s. The Note column names the hits.
Only the standard library is used, so no pip install is needed.
"""

import argparse
import base64
import csv
import datetime
import getpass
import json
import math
import os
import re
import shutil
import sys
import urllib.error
import urllib.parse
import urllib.request

# ================================================================ YOUR SETTINGS
# Fill these in, then press Run. Anything left empty ("") is asked for in the terminal.

# 1. The FFLogs link of the pull to use. Copy it from your browser's address bar.
#    - With a pull number:   https://www.fflogs.com/reports/AbCd1234EfGh5678#fight=12  -> uses pull 12
#    - Last pull of the log: https://www.fflogs.com/reports/AbCd1234EfGh5678?fight=last
#    - No pull number:       https://www.fflogs.com/reports/AbCd1234EfGh5678  -> you pick from a list
LOG_URL = ""

# 2. Which job's plan to make: "SGE", "WHM", "AST", "SCH", "VPR", "DRG", "RPR", "SAM", "MNK" or "NIN".
#    Leave it "" to make a plan for every one of those jobs in the pull.
JOB = ""

# 3. Your FFLogs API client (free, see README.md "FFLogs API client"). Leave both "" to be asked once
#    and have them remembered on this PC (in fflogs_credentials.json, which git ignores).
#    !! If you type them here, don't commit or share this file with them filled in. !!
CLIENT_ID = ""
CLIENT_SECRET = ""

# 4. Put the result straight into RotationSolver's Timelines folder?
#    None = ask each time, True = always, False = never (it goes to the output folder next to this file).
INSTALL = None
# ================================================================ end of settings

API_URL = "https://www.fflogs.com/api/v2/client"
TOKEN_URL = "https://www.fflogs.com/oauth/token"
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CRED_FILE = os.path.join(SCRIPT_DIR, "fflogs_credentials.json")
# The default timelines that ship with the plugin source.
REPO_TIMELINES = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "..", "RotationSolver.Timeline"))
# Same as the plugin: a sync row is only looked for this many rows past the last one that fired.
SYNC_LOOKAHEAD = 4

JOBS = {"SGE": "Sage", "WHM": "WhiteMage", "AST": "Astrologian", "SCH": "Scholar",
        "VPR": "Viper", "DRG": "Dragoon", "RPR": "Reaper", "SAM": "Samurai", "MNK": "Monk", "NIN": "Ninja",
        "RDM": "RedMage", "BLM": "BlackMage", "SMN": "Summoner", "PCT": "Pictomancer",
        "BRD": "Bard", "MCH": "Machinist", "DNC": "Dancer",
        "PLD": "Paladin", "WAR": "Warrior", "DRK": "DarkKnight", "GNB": "Gunbreaker"}
TANK_JOBS = {"PLD", "WAR", "DRK", "GNB"}
# Actions that can go on the co-tank (or anyone): the plan row's Target says who, from the log.
# Tank stance: plan rows say when it should be on / off ("Tank Stance", Target On / Off), not presses.
STANCE_ON = {"Iron Will", "Defiance", "Grit", "Royal Guard"}
STANCE_OFF = {"Release Iron Will", "Release Defiance", "Release Grit", "Release Royal Guard"}
PARTNER_ACTIONS = {"Intervention", "Nascent Flash", "Heart of Corundum", "The Blackest Night", "Oblation", "Shirk"}
MELEE_JOBS = {"VPR", "DRG", "RPR", "SAM", "MNK", "NIN"}
CASTER_JOBS = {"RDM", "BLM", "SMN", "PCT"}
RANGED_JOBS = {"BRD", "MCH", "DNC"}
# Shared mitigation profiles (<slot>.<action>.plan.csv): the player with more uses gets the first slot.
FEINT_SLOTS = ["M1", "M2"]
ADDLE_SLOTS = ["R2", "FakeMelee"]
# Casters often can't weave for a while, so their windows are wider (still up before the first hit they cover).
CASTER_WINDOW = 6.0
TANKS = {"Paladin", "Warrior", "DarkKnight", "Gunbreaker"}

# Actions the built-in plans schedule, per job (everything else is left to the rotation).
PLANNED = {
    "SCH": ["Whispering Dawn", "Fey Illumination", "Fey Blessing", "Consolation", "Summon Seraph", "Seraphism",
            "Sacred Soil", "Expedient", "Indomitability", "Concitation", "Recitation", "Protraction",
            "Adloquium", "Deployment Tactics", "Excogitation"],
    "SGE": ["Kerachole", "Physis II", "Panhaima", "Holos", "Zoe", "Philosophia", "Ixochole"],
    "WHM": ["Afflatus Rapture", "Plenary Indulgence", "Asylum", "Temperance", "Divine Caress", "Medica III",
            "Liturgy of the Bell", "Cure III"],
    "AST": ["Earthly Star", "Collective Unconscious", "Helios Conjunction", "Celestial Opposition", "Horoscope",
            "Sun Sign", "Neutral Sect", "Macrocosmos", "Lightspeed"],
    "VPR": ["Feint"],
    "DRG": ["Feint"],
    "RPR": ["Feint", "Arcane Crest"],
    "SAM": ["Feint", "Tengentsu", "Meditate"],
    "MNK": ["Feint", "Mantra", "Riddle of Earth"],
    "NIN": ["Feint", "Shade Shift"],
    "RDM": ["Addle", "Magick Barrier"],
    "BLM": ["Addle", "Ley Lines", "Triplecast", "Manaward"],
    "SMN": ["Addle"],
    "PCT": ["Addle", "Tempera Grassa"],
    "BRD": ["Troubadour", "Nature's Minne", "Mage's Ballad", "Army's Paeon", "The Wanderer's Minuet"],
    "MCH": ["Tactician", "Dismantle", "Automaton Queen", "Queen Overdrive"],
    "DNC": ["Shield Samba", "Curing Waltz", "Improvisation"],
    # Tanks: mitigation, then the optional ones (invuln, Provoke / Shirk, tank stance; PLD Passage of Arms).
    "PLD": ["Divine Veil", "Holy Sheltron", "Intervention", "Guardian", "Bulwark", "Rampart", "Reprisal",
            "Passage of Arms", "Hallowed Ground", "Provoke", "Shirk"],
    "WAR": ["Shake It Off", "Damnation", "Thrill of Battle", "Bloodwhetting", "Nascent Flash", "Rampart", "Reprisal",
            "Holmgang", "Provoke", "Shirk"],
    "DRK": ["Dark Missionary", "Shadowed Vigil", "Dark Mind", "The Blackest Night", "Oblation", "Rampart", "Reprisal",
            "Living Dead", "Provoke", "Shirk"],
    "GNB": ["Heart of Light", "Great Nebula", "Camouflage", "Heart of Corundum", "Rampart", "Reprisal",
            "Superbolide", "Provoke", "Shirk"],
}

# Mitigation / shield actions and how long their effect lasts (s). These get the
# "up before the first hit, still up for the last hit" window treatment.
MIT_DURATION = {
    "Sacred Soil": 15, "Expedient": 20, "Fey Illumination": 20, "Concitation": 30, "Consolation": 30,
    "Deployment Tactics": 30,
    "Kerachole": 15, "Panhaima": 15, "Holos": 20,
    "Temperance": 20, "Divine Caress": 10, "Liturgy of the Bell": 20,
    "Collective Unconscious": 5, "Sun Sign": 15, "Macrocosmos": 15,
    "Feint": 15, "Riddle of Earth": 10, "Tengentsu": 4, "Shade Shift": 20, "Arcane Crest": 5,
    "Addle": 15, "Magick Barrier": 10, "Manaward": 20, "Tempera Grassa": 10,
    "Troubadour": 15, "Shield Samba": 15, "Tactician": 15, "Dismantle": 10,
    "Divine Veil": 30, "Shake It Off": 30, "Dark Missionary": 15, "Heart of Light": 15, "Reprisal": 15,
}

# Group GCD heals that get Target 'Precast' when cast right before a party hit.
PRECAST_GCDS = {"Medica III", "Helios Conjunction", "Cure III"}

# NIN downtime mudra rows: a ninjutsu prepared before a boss turns targetable. The row opens this long before
# it turns targetable and stays open this long after (the ninjutsu itself has to go off within that).
NINJUTSU = {"Fuma Shuriken", "Katon", "Raiton", "Hyoton", "Huton", "Doton", "Suiton", "Goka Mekkyaku", "Hyosho Ranryu"}
MUDRAS = {"Ten", "Chi", "Jin"}
DOWNTIME_MUDRA_LEAD = 5.0
DOWNTIME_NINJUTSU_LATE = 2.0

# Same action logged again within this many seconds = one press (bell procs, pet copies, ...).
REPEAT_GAP = {"Liturgy of the Bell": 25.0, "Earthly Star": 9.0}
DEFAULT_REPEAT_GAP = 1.5

# Known duties: (keyword in the FFLogs fight name, savage only?) -> (in-game duty name for '# Duty:', folder)
KNOWN_DUTIES = [
    ("vamp fatale", True, "AACHeavyweightM1Savage", "M9S Vamp Fatale"),
    ("red hot", True, "AACHeavyweightM2Savage", "M10S The Xtremes"),
    ("deep blue", True, "AACHeavyweightM2Savage", "M10S The Xtremes"),
    ("tyrant", True, "AACHeavyweightM3Savage", "M11S The Tyrant"),
    ("lindwurm ii", True, "AACHeavyweightM4Savage", "M12SP2 Lindwurm II"),
    ("lindwurm", True, "AACHeavyweightM4Savage", "M12SP1 Lindwurm"),
    ("futures rewritten", False, "FuturesRewrittenUltimate", "FuturesRewrittenUltimate"),
    ("dancing mad", False, "DancingMadUltimate", "DancingMadUltimate"),
    ("omega protocol", False, "TheOmegaProtocolUltimate", "TheOmegaProtocolUltimate"),
    ("dragonsong", False, "DragonsongsRepriseUltimate", "DragonsongsRepriseUltimate"),
    ("epic of alexander", False, "TheEpicofAlexanderUltimate", "TheEpicofAlexanderUltimate"),
    ("weapon's refrain", False, "TheWeaponsRefrainUltimate", "TheWeaponsRefrainUltimate"),
    ("unending coil", False, "TheUnendingCoilofBahamutUltimate", "TheUnendingCoilofBahamutUltimate"),
]

# Enemy IDs this high are invisible helper objects; the game never shows them as casters you can sync on.
HELPER_ID_FROM = 1_000_000
SAVAGE_DIFFICULTY = 101


# ---------------------------------------------------------------- FFLogs API

class LoginError(Exception):
    pass


class FFLogs:
    def __init__(self, client_id, client_secret):
        auth = base64.b64encode(f"{client_id}:{client_secret}".encode()).decode()
        req = urllib.request.Request(
            TOKEN_URL, data=urllib.parse.urlencode({"grant_type": "client_credentials"}).encode(),
            headers={"Authorization": f"Basic {auth}", "User-Agent": "log2timeline"})
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                self.token = json.load(r)["access_token"]
        except urllib.error.HTTPError as e:
            if e.code in (400, 401, 403):
                raise LoginError() from None
            raise SystemExit(f"FFLogs login failed ({e.code}). Try again in a minute.") from None

    def query(self, q, variables):
        body = json.dumps({"query": q, "variables": variables}).encode()
        req = urllib.request.Request(API_URL, data=body, headers={
            "Authorization": f"Bearer {self.token}", "Content-Type": "application/json", "User-Agent": "log2timeline"})
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                res = json.load(r)
        except urllib.error.HTTPError as e:
            raise SystemExit(f"FFLogs request failed ({e.code}): {e.read().decode(errors='replace')[:300]}") from None
        if res.get("errors"):
            raise SystemExit("FFLogs error: " + "; ".join(err.get("message", "?") for err in res["errors"]))
        return res["data"]

    def report(self, code):
        q = """query($code: String!) { reportData { report(code: $code) {
            title
            zone { name }
            fights { id name encounterID difficulty kill fightPercentage startTime endTime friendlyPlayers gameZone { id }
                     phaseTransitions { id startTime } }
            masterData(translate: true) {
                npcs: actors(type: "NPC") { id name type subType gameID petOwner }
                players: actors(type: "Player") { id type subType gameID }
                pets: actors(type: "Pet") { id type subType gameID petOwner }
                abilities { gameID name }
            }
        } } }"""
        rep = self.query(q, {"code": code})["reportData"]["report"]
        if rep is None:
            raise SystemExit("Report not found (or it is private).")
        return rep

    def events(self, code, fight, data_type, hostility, filter_expr=None):
        q = """query($code: String!, $fight: Int!, $start: Float!, $end: Float!, $filter: String) {
            reportData { report(code: $code) {
                events(fightIDs: [$fight], startTime: $start, endTime: $end, dataType: %s,
                       hostilityType: %s, limit: 10000, translate: true, filterExpression: $filter) {
                    data nextPageTimestamp
                } } } }""" % (data_type, hostility)
        out, start = [], fight["startTime"]
        while start is not None:
            ev = self.query(q, {"code": code, "fight": fight["id"], "start": start, "end": fight["endTime"],
                                "filter": filter_expr})["reportData"]["report"]["events"]
            out.extend(ev["data"])
            start = ev.get("nextPageTimestamp")
        return out


# ---------------------------------------------------------------- helpers

def normalize(name):
    return "".join(ch for ch in name if ch.isalnum())


def clean(text):
    # CSV has no quoting in the plugin's reader: keep commas out of names.
    return re.sub(r"\s+", " ", str(text).replace(",", " ")).strip()


def parse_url(url):
    m = re.search(r"reports/([A-Za-z0-9]+)", url) or re.fullmatch(r"\s*([A-Za-z0-9]{12,})\s*", url)
    if not m:
        raise SystemExit("That does not look like an FFLogs report link.")
    f = re.search(r"fight=(\d+|last)", url)
    return m.group(1), (f.group(1) if f else None)


def pick_fight(report, wanted, interactive=False):
    fights = [f for f in report["fights"] if f.get("encounterID")]
    if not fights:
        raise SystemExit("No boss fights in this report.")
    if wanted == "last":
        return fights[-1]
    if wanted:
        for f in fights:
            if str(f["id"]) == wanted:
                return f
        raise SystemExit(f"Fight {wanted} not found in the report.")
    kills = [f for f in fights if f.get("kill")]
    default = (kills or fights)[-1]
    if not interactive or len(fights) == 1:
        return default
    print("\nPulls in this report (a kill works best):")
    for f in fights:
        mins, secs = divmod(int((f["endTime"] - f["startTime"]) / 1000), 60)
        result = "KILL" if f.get("kill") else f"wipe at {f.get('fightPercentage', 0) or 0:.0f}%"
        print(f"  {f['id']:>3}  {f['name']:<32} {mins}:{secs:02d}  {result}")
    while True:
        pick = ask("Pull number", str(default["id"]))
        for f in fights:
            if str(f["id"]) == pick:
                return f
        print("No pull with that number.")


ULTIMATE_CONTENT_TYPE = 28
CATEGORY_TAGS = ("Savage", "Extreme", "Unreal", "Chaotic")
_duty_info = {}


def duty_info(territory_id):
    """(Duty Finder name, expansion, category) from the game data on XIVAPI, or None. The category is named like the
    plugin's CD Plan picker: Ultimate, Savage, Extreme, Unreal, Chaotic, otherwise the content type."""
    if not territory_id:
        return None
    if territory_id in _duty_info:
        return _duty_info[territory_id]
    url = (f"https://v2.xivapi.com/api/sheet/TerritoryType/{int(territory_id)}?fields=ContentFinderCondition.Name,"
           "ContentFinderCondition.ContentType.Name,ExVersion.Name")
    info = None
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "log2timeline"}), timeout=20) as r:
            f = json.load(r)["fields"]
        cfc = f["ContentFinderCondition"]
        name = cfc["fields"]["Name"]
        ctype = cfc["fields"].get("ContentType") or {}
        if name:
            if ctype.get("row_id") == ULTIMATE_CONTENT_TYPE:
                category = "Ultimate"
            else:
                category = next((t for t in CATEGORY_TAGS if name.lower().endswith(f"({t.lower()})")),
                                (ctype.get("fields") or {}).get("Name") or "Other")
            expansion = ((f.get("ExVersion") or {}).get("fields") or {}).get("Name") or "Other"
            info = (name, clean(expansion), clean(category))
    except Exception:
        info = None
    _duty_info[territory_id] = info
    return info


def duty_finder_name(territory_id):
    """The duty's Duty Finder name (what the plugin matches on)."""
    info = duty_info(territory_id)
    return info[0] if info else None


def duty_group(fight):
    """(expansion, category) folders a new timeline goes in, e.g. ("Dawntrail", "Savage")."""
    info = duty_info((fight.get("gameZone") or {}).get("id"))
    return (info[1], info[2]) if info else ("Other", "Other")


def duty_for(fight, override):
    if override:
        return normalize(override), clean(override), False
    name = fight["name"].lower()
    known = next((k for k in KNOWN_DUTIES
                  if k[0] in name and (not k[1] or fight.get("difficulty") == SAVAGE_DIFFICULTY)), None)
    finder = duty_finder_name((fight.get("gameZone") or {}).get("id"))
    if finder:
        if known:
            folder = known[3]
        else:
            folder = clean(finder[:1].upper() + finder[1:])
            # e.g. a duty with two bosses fought as separate pulls: keep them in separate folders
            if normalize(fight["name"]).lower() not in normalize(finder).lower():
                folder += f" - {clean(fight['name'])}"
        return normalize(finder), folder, False
    if known:
        return known[2], known[3], False
    # The report's zone can belong to other pulls in the same report, so guess from the fight alone.
    savage = " Savage" if fight.get("difficulty") == SAVAGE_DIFFICULTY else ""
    return normalize(fight["name"] + savage), clean(fight["name"]), True


def fmt(x):
    return f"{x:.1f}"


def ask(question, default=None):
    """Prompt; Enter gives the default."""
    hint = f" [{default}]" if default not in (None, "") else ""
    answer = input(f"{question}{hint}: ").strip()
    return answer or (default or "")


def yes(question, default=True):
    answer = input(f"{question} [{'Y/n' if default else 'y/N'}]: ").strip().lower()
    return default if not answer else answer.startswith("y")


CRED_HELP = """
First time setup: this needs a free FFLogs API client (takes a minute).
  1. Open https://www.fflogs.com/api/clients and log in.
  2. Click "Create Client". Any name works (e.g. Log2Timeline); for the redirect URL
     put https://localhost. Leave "Public Client" unticked.
  3. Copy the client ID and the client secret it shows you.
"""


def login(args, interactive):
    """Returns a logged-in FFLogs client, asking for (and saving) credentials if needed."""
    cid = args.client_id or CLIENT_ID.strip() or os.environ.get("FFLOGS_CLIENT_ID")
    secret = args.client_secret or CLIENT_SECRET.strip() or os.environ.get("FFLOGS_CLIENT_SECRET")
    given = bool(cid and secret)  # typed in the settings / command line / environment: don't offer to save
    saved = False
    if (not cid or not secret) and os.path.exists(CRED_FILE):
        try:
            with open(CRED_FILE, encoding="utf-8") as fh:
                data = json.load(fh)
            cid, secret, saved = cid or data.get("client_id"), secret or data.get("client_secret"), True
        except (OSError, ValueError):
            pass
    while True:
        if not cid or not secret:
            if not interactive:
                raise SystemExit("FFLogs client ID/secret missing: fill in CLIENT_ID / CLIENT_SECRET at the top of the "
                                 "script, pass --client-id/--client-secret, or run it once without arguments to set up.")
            print(CRED_HELP)
            cid = ask("Client ID")
            secret = getpass.getpass("Client secret (typing is hidden, paste and press Enter): ").strip()
            saved = False
        try:
            api = FFLogs(cid, secret)
        except LoginError:
            where = " (the saved ones)" if saved else " (the ones in YOUR SETTINGS / command line)" if given else ""
            if not interactive:
                raise SystemExit(f"FFLogs rejected the client ID/secret{where}.")
            print(f"\nFFLogs rejected that client ID/secret{where}. Let's try again.")
            cid = secret = None
            given = False
            continue
        if interactive and not saved and not given:
            if yes("Remember these on this PC so you don't have to enter them again?"):
                with open(CRED_FILE, "w", encoding="utf-8") as fh:
                    json.dump({"client_id": cid, "client_secret": secret}, fh)
                print(f"Saved to {CRED_FILE} (delete that file to forget them).")
        return api


def timelines_dir():
    appdata = os.environ.get("APPDATA")
    path = os.path.join(appdata, "XIVLauncher", "pluginConfigs", "RotationSolver", "Timelines") if appdata else ""
    return path if path and os.path.isdir(os.path.dirname(path)) else None


# ---------------------------------------------------------------- build

class Builder:
    def __init__(self, api, code, fight, report, args):
        self.api, self.code, self.fight, self.report, self.args = api, code, fight, report, args
        self.job = None  # set before building each player's plan
        self.few_players = False
        self.t0 = fight["startTime"]
        # Player and pet names are never downloaded, so they can't end up in the files or the console.
        md = report["masterData"]
        self.actors = {a["id"]: a for a in (md.get("npcs") or []) + (md.get("players") or []) + (md.get("pets") or [])}
        self.abilities = {a["gameID"]: a["name"] for a in report["masterData"]["abilities"]}
        # Real party members only (FFLogs also lists a "Multiple Players" limit break actor).
        self.players = {i for i in (fight.get("friendlyPlayers") or [])
                        if self.actors.get(i, {}).get("subType") not in (None, "LimitBreak", "Unknown")}

    def sec(self, ts):
        return (ts - self.t0) / 1000.0

    def ability(self, gid):
        name = self.abilities.get(gid) or f"Unknown {gid}"
        return clean(name)

    # ---- party hits (only used to time mitigation rows; not written as Damage rows)
    def damage_taken(self):
        if getattr(self, "_damage_taken", None) is None:
            self._damage_taken = self.api.events(self.code, self.fight, "DamageTaken", "Friendlies")
        return self._damage_taken

    def tank_slots(self):
        """{actor id: "MT" / "OT"}: MT is the tank the boss auto-attacks first in the pull."""
        tanks = [a["id"] for a in self.actors.values() if a.get("type") == "Player" and a["id"] in self.players
                 and a.get("subType") in TANKS]
        if not tanks:
            return {}
        first = next((e.get("targetID") for e in sorted(self.damage_taken(), key=lambda x: x["timestamp"])
                      if e.get("targetID") in tanks and self.ability(e.get("abilityGameID", 0)).lower() == "attack"), tanks[0])
        return {t: "MT" if t == first else "OT" for t in tanks}

    def party_hits(self):
        events = self.damage_taken()
        if len(self.players) >= 4:
            need = math.ceil(len(self.players) * 0.5)
        else:
            # Only the logger (and maybe a friend) are real players, e.g. Duty Support or a partial log:
            # treat any hit on them as a party hit.
            need = 1
            self.few_players = True
        by_ability = {}
        for e in events:
            if e.get("type") not in ("damage", "calculateddamage") or e.get("tick"):
                continue
            if e.get("targetID") not in self.players:
                continue
            name = self.ability(e.get("abilityGameID", 0))
            if name.lower() == "attack":
                continue
            by_ability.setdefault(e["abilityGameID"], []).append((self.sec(e["timestamp"]), e["targetID"]))
        hits = []
        for gid, evs in by_ability.items():
            evs.sort()
            cluster = []
            for t, target in evs + [(float("inf"), None)]:
                if cluster and t - cluster[0][0] > 2.0:
                    if len({c[1] for c in cluster}) >= need:
                        hits.append((cluster[0][0], self.ability(gid)))
                    cluster = []
                if target is not None:
                    cluster.append((t, target))
        hits.sort()
        return hits

    # ---- sync rows
    def enemy_casts(self):
        if getattr(self, "_enemy_casts", None) is None:
            self._enemy_casts = self.api.events(self.code, self.fight, "Casts", "Enemies")
        return self._enemy_casts

    def enemy_targetability(self):
        if getattr(self, "_enemy_targ", None) is None:
            self._enemy_targ = self.api.events(self.code, self.fight, "All", "Enemies", 'type = "targetabilityupdate"')
        return self._enemy_targ

    def replay_events(self):
        """[(t, kind, oid, ability)] in time order: enemy cast starts, targetable / untargetable, deaths, and
        "gone" at each enemy's last cast (a boss that leaves often has no death or untargetable event)."""
        ev, last_cast = [], {}
        for e in self.enemy_casts():
            a = self.actors.get(e.get("sourceID"))
            if not a or not a.get("gameID"):
                continue
            t = self.sec(e["timestamp"])
            last_cast[e["sourceID"]] = (t, a["gameID"])
            if e.get("type") == "begincast":
                ev.append((t, "cast", a["gameID"], e.get("abilityGameID", 0)))
        for t, oid in last_cast.values():
            ev.append((t + 0.1, "gone", oid, 0))
        for e in self.enemy_targetability():
            a = self.actors.get(e.get("sourceID"))
            if a and a.get("gameID"):
                ev.append((self.sec(e["timestamp"]), "targetable" if e.get("targetable") else "untargetable", a["gameID"], 0))
        for e in self.api.events(self.code, self.fight, "Deaths", "Enemies"):
            a = self.actors.get(e.get("targetID"))
            if a and a.get("gameID"):
                ev.append((self.sec(e["timestamp"]), "death", a["gameID"], 0))
        ev.sort(key=lambda x: x[0])
        return ev

    def sync_events(self):
        rows = []
        casts = self.enemy_casts()
        last_seen = {}
        for e in casts:
            if e.get("type") != "begincast" or e.get("duration", 0) < self.args.min_cast * 1000:
                continue
            actor = self.actors.get(e.get("sourceID"))
            if not actor or not actor.get("gameID") or actor["gameID"] >= HELPER_ID_FROM or actor.get("type") == "Pet":
                continue
            t, gid = self.sec(e["timestamp"]), e.get("abilityGameID", 0)
            name = self.ability(gid)
            if name in last_seen and t - last_seen[name] < 1.0:
                continue  # several copies of the boss (or a pair of bosses) starting the same cast
            last_seen[name] = t
            rows.append({"t": t, "name": name, "watch": "CastStart", "caster": clean(actor["name"]),
                         "oid": actor["gameID"], "ids": str(gid)})

        targ = self.enemy_targetability()
        last_state = {}
        for e in targ:
            actor = self.actors.get(e.get("sourceID"))
            if not actor or actor.get("subType") != "Boss" or not actor.get("gameID"):
                continue
            t = self.sec(e["timestamp"])
            state = "Targetable" if e.get("targetable") else "Untargetable"
            key = clean(actor["name"])  # by name, so a boss pair (two OIDs) gives one row
            prev = last_state.get(key)
            if (prev and prev[0] == state and t - prev[1] < 1.0) or (state == "Targetable" and t < 1.0):
                continue
            last_state[key] = (state, t)
            word = "appears" if state == "Targetable" else "disappears"
            rows.append({"t": t, "name": f"{clean(actor['name'])} {word}", "watch": state,
                         "caster": clean(actor["name"]), "oid": actor["gameID"], "ids": ""})
        rows.sort(key=lambda r: r["t"])
        return rows

    def boss(self, sync):
        bosses = [a for a in self.actors.values() if a.get("subType") == "Boss" and a.get("gameID")]
        for r in sync:
            for b in bosses:
                if b["gameID"] == r["oid"]:
                    return clean(b["name"]), b["gameID"]
        if bosses:
            return clean(bosses[0]["name"]), bosses[0]["gameID"]
        return (sync[0]["caster"], sync[0]["oid"]) if sync else ("", 0)

    # ---- phases: P1 starts at the pull; later phases start at their first sync row
    def phases(self, sync):
        trans = sorted(self.fight.get("phaseTransitions") or [], key=lambda p: p["startTime"])
        bounds = [self.sec(p["startTime"]) for p in trans[1:]]
        origins = [("P1", 0.0)]
        for i, start in enumerate(bounds):
            end = bounds[i + 1] if i + 1 < len(bounds) else float("inf")
            first = next((r for r in sync if start <= r["t"] < end), None)
            if first:
                origins.append((f"P{len(origins) + 1}", first["t"]))
        return origins

    @staticmethod
    def phase_of(origins, t):
        label, origin = origins[0]
        for lab, o in origins:
            if t >= o:
                label, origin = lab, o
        return label, origin

    # ---- the player's casts
    def healers(self):
        """[(job, actor)] for every plan job in the pull (narrowed by --healer)."""
        by_sub = {v: k for k, v in JOBS.items()}
        found = [(by_sub[a["subType"]], a) for a in self.actors.values()
                 if a.get("type") == "Player" and a.get("subType") in by_sub and a["id"] in self.players]
        found.sort(key=lambda h: (h[0], h[1]["id"]))
        if self.args.healer:
            if not 1 <= self.args.healer <= len(found):
                raise SystemExit(f"--healer {self.args.healer}: this pull has {len(found)} player(s) with a plan job: {healer_list(found)}.")
            found = [found[self.args.healer - 1]]
        return found

    def healer_casts(self, me):
        ids = {me["id"]} | {a["id"] for a in self.actors.values() if a.get("petOwner") == me["id"]}
        wanted = set(PLANNED[self.job])
        if getattr(self, "_friendly_casts", None) is None:
            self._friendly_casts = self.api.events(self.code, self.fight, "Casts", "Friendlies")
        events = self._friendly_casts
        begins, casts, last = {}, [], {}
        for e in sorted(events, key=lambda x: x["timestamp"]):
            if e.get("sourceID") not in ids:
                continue
            name = self.ability(e.get("abilityGameID", 0))
            if name not in wanted:
                continue
            t = self.sec(e["timestamp"])
            if e.get("type") == "begincast":
                begins[name] = t
                continue
            if e.get("type") != "cast":
                continue
            gap = REPEAT_GAP.get(name, DEFAULT_REPEAT_GAP)
            if name in last and t - last[name] < gap:
                continue
            last[name] = t
            start = begins.pop(name, t)
            if t - start > 5:
                start = t
            target = self.actors.get(e.get("targetID"))
            casts.append({"name": name, "t": start, "ct": t - start,
                          "self": e.get("targetID") == me["id"],
                          "tank": bool(target and target.get("subType") in TANKS)})
        return casts

    def plan_rows(self, casts, hits, origins):
        w, min_w, margin = self.args.window, 2.5, 0.6
        if self.job in CASTER_JOBS:
            w = max(w, CASTER_WINDOW)
        rows = []
        for i, c in enumerate(casts):
            name, t, ct = c["name"], c["t"], c["ct"]
            if t < 0:
                continue  # countdown actions are handled by the opener settings
            target, note = "", ""
            if name == "Adloquium":
                if not (c["self"] and any(d["name"] == "Deployment Tactics" and 0 <= d["t"] - t <= 8 for d in casts)):
                    continue  # single-target heal, not part of a deployment
                target = "Self"
            elif name == "Protraction":
                if not c["self"]:
                    continue  # tank help, the rotation handles it
                target = "Self"
            elif name == "Deployment Tactics":
                target = "Self" if c["self"] else ""
            elif name == "Excogitation":
                if not c["tank"]:
                    continue
                target = "TankWithAggro"
            elif name in PARTNER_ACTIONS:
                target = "Self" if c["self"] else "OtherTank" if c["tank"] else "LowHP"

            gcd_extra = 1.0 if ct > 0.5 else 0.0
            start, end = t, t + w + gcd_extra
            if name in MIT_DURATION:
                dur = MIT_DURATION[name]
                landed = t + ct
                covered = [h for h in hits if landed < h[0] <= landed + dur]
                if covered:
                    end = min(end, covered[0][0] - ct - margin)        # up before the first hit
                    start = min(start, end - max(w, min_w + 0.1))      # window over 2.5s
                    while covered and end - (min_w + 0.1) + ct + dur < covered[-1][0] + margin:
                        covered.pop()                                  # can't reach that late hit any more
                    if covered:
                        start = max(start, covered[-1][0] + margin - ct - dur)
                        if end - start <= min_w:
                            start = end - (min_w + 0.1)
                    note = self.hit_note(covered)
                else:
                    note = "no party hit while active"
            else:
                before = [h for h in hits if t - 10 <= h[0] <= t]
                after = [h for h in hits if t < h[0] <= t + ct + 8]
                if name in PRECAST_GCDS and after:
                    target, note = "Precast", f"before {after[0][1]}"
                elif before:
                    note = f"after {before[-1][1]}"
                elif after:
                    note = f"before {after[0][1]}"

            phase, origin = self.phase_of(origins, max(start, 0.0))
            rows.append({"phase": phase, "start": max(0.0, start - origin), "end": end - origin,
                         "action": name, "target": target, "note": note, "orig": t})

        # Recitation / Protraction / Adloquium follow their Deployment if it was moved earlier.
        for d in [r for r in rows if r["action"] == "Deployment Tactics"]:
            shift = d["start"] - (d["orig"] - self.phase_of(origins, d["orig"])[1])
            if shift < 0:
                for r in rows:
                    if r["action"] in ("Recitation", "Protraction", "Adloquium") and r["phase"] == d["phase"] \
                            and 0 <= d["orig"] - r["orig"] <= 12:
                        r["start"], r["end"] = max(0.0, r["start"] + shift), r["end"] + shift
        rows.sort(key=lambda r: (int(r["phase"][1:]), r["start"]))
        return rows

    def friendly_casts(self):
        if getattr(self, "_friendly_casts", None) is None:
            self._friendly_casts = self.api.events(self.code, self.fight, "Casts", "Friendlies")
        return self._friendly_casts

    def dps_slots(self, action, jobs, names):
        """{actor id: slot} for the players of these jobs who used this action (Feint: M1 / M2, Addle: R2 / FakeMelee):
        the first slot is the one with more uses (keeping it on cooldown), ties go to whoever used it first."""
        players = {a["id"] for a in self.actors.values() if a.get("type") == "Player" and a["id"] in self.players
                   and any(JOBS[j] == a.get("subType") for j in jobs)}
        first, count = {}, {}
        for e in sorted(self.friendly_casts(), key=lambda x: x["timestamp"]):
            src = e.get("sourceID")
            if src in players and e.get("type") == "cast" and self.ability(e.get("abilityGameID", 0)) == action:
                first.setdefault(src, e["timestamp"])
                count[src] = count.get(src, 0) + 1
        order = sorted(first, key=lambda src: (-count[src], first[src]))
        return {src: names[i] if i < len(names) else f"{names[-1]}{i}" for i, src in enumerate(order)}

    def stance_rows(self, me, origins):
        """Tank: "Tank Stance" On / Off rows from one stance press to the next, split at phase changes."""
        presses = sorted((self.sec(e["timestamp"]), self.ability(e.get("abilityGameID", 0)) in STANCE_ON)
                         for e in self.friendly_casts()
                         if e.get("sourceID") == me["id"] and e.get("type") == "cast"
                         and self.ability(e.get("abilityGameID", 0)) in STANCE_ON | STANCE_OFF)
        end_of_fight = self.sec(self.fight["endTime"])
        bounds = [o for _, o in origins] + [end_of_fight]
        rows = []
        for i, (t, on) in enumerate(presses):
            t_next = presses[i + 1][0] - 0.5 if i + 1 < len(presses) else end_of_fight
            for k, (label, origin) in enumerate(origins):
                a, z = max(t, origin), min(t_next, bounds[k + 1])
                if z - a >= 1.0:
                    rows.append({"phase": label, "start": a - origin, "end": z - origin, "action": "Tank Stance",
                                 "target": "On" if on else "Off", "note": "stance " + ("on" if on else "off"), "orig": a})
        return rows

    def downtime_ninjutsu_rows(self, me, sync, origins):
        """NIN: ninjutsu whose mudras went in before a boss came back and that went off right after it did."""
        appear = [(r["t"], r["caster"]) for r in sync if r["watch"] == "Targetable"]
        mine = [(self.sec(e["timestamp"]), self.ability(e.get("abilityGameID", 0)))
                for e in sorted(self.friendly_casts(), key=lambda x: x["timestamp"])
                if e.get("sourceID") == me["id"] and e.get("type") == "cast"]
        rows = []
        for i, (t, name) in enumerate(mine):
            if name not in NINJUTSU:
                continue
            back = next(((a, who) for a, who in appear if 0 <= t - a <= DOWNTIME_NINJUTSU_LATE), None)
            if back is None:
                continue
            first, j = None, i - 1
            while j >= 0 and mine[j][1] in MUDRAS and t - mine[j][0] <= 8:
                first, j = mine[j][0], j - 1
            if first is None or first >= back[0]:
                continue  # not prepared during the downtime (or a Ten Chi Jin)
            start, end = back[0] - DOWNTIME_MUDRA_LEAD, back[0] + DOWNTIME_NINJUTSU_LATE
            phase, origin = self.phase_of(origins, max(start, 0.0))
            rows.append({"phase": phase, "start": max(0.0, start - origin), "end": end - origin,
                         "action": name, "target": "", "note": f"downtime mudra, before {back[1]} appears", "orig": t})
        return rows

    @staticmethod
    def hit_note(covered):
        if not covered:
            return "too early for the next hit"
        names = []
        for _, n in covered:
            if names and names[-1][0] == n:
                names[-1][1] += 1
            else:
                names.append([n, 1])
        return "covers " + " + ".join(n if c == 1 else f"{n} x{c}" for n, c in names)




# ---------------------------------------------------------------- existing timelines

def timeline_duty(path):
    with open(path, encoding="utf-8-sig") as fh:
        for line in fh:
            if not line.startswith("#"):
                break
            key, sep, value = line[1:].partition(":")
            if sep and key.strip().lower() == "duty":
                return value.strip()
    return ""


def installed_builtin_dir():
    """The built-in timelines of the installed plugin (newest installed version), or None."""
    appdata = os.environ.get("APPDATA")
    base = os.path.join(appdata, "XIVLauncher", "installedPlugins", "RotationSolver") if appdata else ""
    if not base or not os.path.isdir(base):
        return None
    dirs = [os.path.join(base, d, "RotationSolver.Timeline") for d in os.listdir(base)]
    dirs = [d for d in dirs if os.path.isdir(d)]
    return max(dirs, key=os.path.getmtime) if dirs else None


def existing_timelines(duty):
    """[(folder, where, root)] of the timelines for this duty, at any depth: your Timelines folder first, then the
    plugin's built-in ones (installed plugin, then the repo)."""
    found = []
    for root, where in ((timelines_dir(), "your Timelines folder"),
                        (installed_builtin_dir(), "the plugin's built-in timelines"),
                        (REPO_TIMELINES, "the repo's RotationSolver.Timeline")):
        if not root or not os.path.isdir(root):
            continue
        hits = []
        for folder, _, files in os.walk(root):
            if "timeline.csv" in files and folder != root:
                tl = os.path.join(folder, "timeline.csv")
                if duty.lower() in (os.path.basename(folder).lower(), timeline_duty(tl).lower()):
                    hits.append(folder)
        found += [(h, where, root) for h in sorted(hits, key=str.lower)]
    return found


def read_sync_rows(path):
    """The sync rows of a timeline.csv (no Damage / Downtime rows), in file order."""
    rows = []
    with open(path, encoding="utf-8-sig", newline="") as fh:
        for c in csv.reader(line for line in fh if not line.startswith("#")):
            c = [x.strip() for x in c]
            if len(c) < 8 or c[0] == "Phase" or c[2] in ("Damage", "Downtime"):
                continue
            try:
                oid = int(c[4], 16) if c[4].lower().startswith("0x") else int(c[4] or 0)
                pt = float(c[7])
            except ValueError:
                continue
            ids = {int(x) for x in c[5].split("|") if x.strip().isdigit()}
            rows.append({"phase": c[0], "name": c[1], "watch": c[2], "oid": oid, "ids": ids, "pt": pt})
    return rows


def replay(rows, events):
    """Plays the timeline's sync rows against the log like the plugin does:
    [(log time, phase, phase time, row index, event kind)]."""
    if not rows:
        return []
    start = next((i for i, r in enumerate(rows) if r["watch"] == "CombatStart"), 0)
    last, phase = start, rows[start]["phase"]
    syncs = [(0.0, phase, rows[start]["pt"], start, "pull")]

    def phase_first(i):
        return i > 0 and rows[i]["phase"] != rows[i - 1]["phase"]

    def fires(r, kind, oid, ability):
        if r["oid"] != oid:
            return False
        w = r["watch"]
        return ((w == "CastStart" and kind == "cast" and ability in r["ids"]) or
                (w in ("Targetable", "CombatStart") and kind == "targetable") or
                (w == "Untargetable" and kind == "untargetable") or
                (w == "Gone" and kind in ("untargetable", "death", "gone")))

    for t, kind, oid, ability in events:
        for i in range(last + 1, len(rows)):
            r = rows[i]
            if i > last + SYNC_LOOKAHEAD and not (r["phase"] != phase and phase_first(i)):
                continue
            if fires(r, kind, oid, ability):
                last, phase = i, r["phase"]
                syncs.append((t, phase, r["pt"], i, kind))
                break
    return syncs


def to_timeline_clock(syncs, t):
    """(phase, phase time) the timeline's clock shows at log time t. The phase is the one running at t; the time
    is measured from the nearest sync of that phase, before or after t (the timeline's own spacing beats a
    loosely timed sync, e.g. a "Gone" guessed from the boss's last cast)."""
    i = 0
    while i + 1 < len(syncs) and syncs[i + 1][0] <= t:
        i += 1
    at = syncs[i]
    nxt = syncs[i + 1] if i + 1 < len(syncs) else None
    if nxt and nxt[1] == at[1] and nxt[0] - t < t - at[0]:
        return at[1], nxt[2] - (nxt[0] - t)
    return at[1], at[2] + (t - at[0])


def sync_report(rows, syncs, warn_at=1.5):
    """Lines saying how well the timeline fits this pull: rows that fired, the biggest drift per phase, rows that
    never fired. Drift = when a sync fired vs when the timeline expected it from the sync before it."""
    lines = [f"Sync check: {len(syncs)} of {len(rows)} timeline sync rows fired in this pull."]
    worst = {}
    for prev, cur in zip(syncs, syncs[1:]):
        if prev[1] != cur[1]:
            continue
        drift = (cur[0] - prev[0]) - (cur[2] - prev[2])
        if cur[1] not in worst or abs(drift) > abs(worst[cur[1]][0]):
            worst[cur[1]] = (drift, rows[cur[3]]["name"], prev[4] == "gone")
    for phase in dict.fromkeys(r["phase"] for r in rows):
        if phase not in worst:
            continue
        drift, name, after_guess = worst[phase]
        mark = "!" if abs(drift) > warn_at and not after_guess else " "
        why = " (the sync before it was a boss leaving, guessed from its last cast)" if after_guess else ""
        lines.append(f" {mark} {phase}: off by up to {drift:+.1f}s at '{name}'{why}")
    fired = {s[3] for s in syncs}
    missed = [r for i, r in enumerate(rows) if i not in fired]
    if missed:
        names = ", ".join(f"{r['phase']} '{r['name']}'" for r in missed[:8])
        lines.append(f"   Never fired: {names}{' ...' if len(missed) > 8 else ''}")
    return lines


def convert_rows(rows, origins, syncs):
    """Moves plan rows from the log's own phases onto an existing timeline's phases."""
    origin_of = dict(origins)
    out = []
    for r in rows:
        a0, a1 = origin_of[r["phase"]] + r["start"], origin_of[r["phase"]] + r["end"]
        phase, start = to_timeline_clock(syncs, a0)
        out.append({**r, "phase": phase, "start": max(0.0, start), "end": start + (a1 - a0), "abs": a0})
    out.sort(key=lambda r: r["abs"])
    return out


# ---------------------------------------------------------------- output

def write_timeline(path, duty, b, sync, origins, code):
    kill = "kill" if b.fight.get("kill") else "wipe"
    boss_name, boss_oid = b.boss(sync)
    origin_of = dict(origins)
    lines = [f"# Duty: {duty}",
             f"# Draft from FFLogs report {short_code(code)} fight {b.fight['id']} ({clean(b.fight['name'])} {kill})"
             f" made {datetime.date.today().isoformat()}",
             "Phase,Sync,Watch,Caster,OID,ActionIDs,Occurrence,PhaseTime",
             "# ===== P1 =====",
             f"P1,Combat starts,CombatStart,{boss_name},0x{boss_oid:X},,1,0.0"]
    seen, current = {}, "P1"
    for r in sync:
        phase, origin = Builder.phase_of(origins, r["t"])
        if phase != current:
            lines.append(f"# ===== {phase} =====")
            current, seen = phase, {}
        seen[r["name"]] = seen.get(r["name"], 0) + 1
        lines.append(",".join([phase, r["name"], r["watch"], r["caster"], f"0x{r['oid']:X}", r["ids"],
                               str(seen[r["name"]]), fmt(r["t"] - origin_of[phase])]))
    with open(os.path.join(path, "timeline.csv"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(lines) + "\n")


def short_code(code):
    """Last 4 characters of the report code: enough to tell your own drafts apart, not enough to open the log
    (the full code would show every player's name)."""
    return f"...{code[-4:]}" if code else ""


def plan_file(job, slot):
    return f"{slot or 'Default'}.{job}.plan.csv"


def profile_file(slot, action):
    return f"{slot}.{action}.plan.csv"


def write_profile(path, folder, slot, action, rows, b, code):
    lines = [f"# {folder} Log {slot} {action} profile ({short_code(code)} fight {b.fight['id']})",
             "Phase,Start,End,Action,Target,Note"]
    for r in rows:
        lines.append(",".join([r["phase"], fmt(r["start"]), fmt(r["end"]), r["action"], r["target"], clean(r["note"])]))
    with open(os.path.join(path, profile_file(slot, action)), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(lines) + "\n")


def write_plan(path, folder, job, slot, plan, b, code):
    lines = [f"# {folder} Log {slot + ' ' if slot else ''}{job} draft ({short_code(code)} fight {b.fight['id']})",
             "Phase,Start,End,Action,Target,Note"]
    for r in plan:
        lines.append(",".join([r["phase"], fmt(r["start"]), fmt(r["end"]), r["action"], r["target"], clean(r["note"])]))
    with open(os.path.join(path, plan_file(job, slot)), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(lines) + "\n")


# ---------------------------------------------------------------- main

def healer_labels(found):
    """'SGE', or 'SGE #1' / 'SGE #2' when the pull has two of the same job. No character names."""
    counts, seen, labels = {}, {}, []
    for job, _ in found:
        counts[job] = counts.get(job, 0) + 1
    for job, _ in found:
        seen[job] = seen.get(job, 0) + 1
        labels.append(f"{job} #{seen[job]}" if counts[job] > 1 else job)
    return labels


def healer_list(found):
    return ", ".join(f"{i}. {label}" for i, label in enumerate(healer_labels(found), 1)) or "none"


def choose_healers(found, wanted_job, interactive):
    if wanted_job:
        picked = [h for h in found if h[0] == wanted_job]
        if picked:
            return picked[:1]
        raise SystemExit(f"No {wanted_job} in this pull. Plan jobs in it: {healer_list(found)}.")
    if not found:
        raise SystemExit("No job with a CD plan rotation in this pull, so there is nothing to make a plan from.")
    if not interactive or len(found) == 1:
        return found
    print("\nPlan jobs in this pull:")
    for i, label in enumerate(healer_labels(found), 1):
        print(f"  {i}. {label}")
    while True:
        pick = ask("Make plans for which? Type numbers (e.g. 1 2) or jobs (e.g. SGE), or just press Enter for all"
                   ).upper().replace(",", " ").split()
        if not pick:
            return found
        chosen = [h for i, h in enumerate(found, 1) if str(i) in pick or h[0] in pick]
        if chosen:
            return chosen
        print("Didn't recognise that, try again.")


def run(args, interactive):
    if interactive:
        print("Log2Timeline: FFLogs pull -> timeline + CD plan for RSR\n")

    url = args.url or LOG_URL.strip()
    while True:
        if not url:
            if not interactive:
                raise SystemExit("No FFLogs link given.")
            print("Paste the FFLogs link of the pull (from the browser's address bar), e.g.\n"
                  "  https://www.fflogs.com/reports/AbCd1234EfGh5678#fight=12\n"
                  "(tip: set LOG_URL at the top of the script to skip this question)")
            url = ask("FFLogs link")
        try:
            code, fight_id = parse_url(url)
            break
        except SystemExit as e:
            if not interactive:
                raise
            print(f"{e}\n")
            url = None

    wanted_job = (args.job or JOB or "").strip().upper() or None
    if wanted_job and wanted_job not in JOBS:
        raise SystemExit(f"Job '{wanted_job}' isn't one of {', '.join(JOBS)}. Fix JOB at the top of the script "
                         "(or leave it \"\" to use every one of them in the pull).")

    api = login(args, interactive)
    print("Reading the report...")
    report = api.report(code)
    fight = pick_fight(report, fight_id, interactive)
    duty, folder, guessed = duty_for(fight, args.duty)
    if args.suffix.strip():
        folder = f"{folder} {clean(args.suffix)}"
    print(f"\nPull {fight['id']}: {fight['name']} ({'kill' if fight.get('kill') else 'wipe'})")

    found = existing_timelines(duty)
    own_base = False
    base, base_where, base_root = (found[0] if found and not args.new_timeline else (None, None, None))
    if base:
        folder = os.path.basename(base)
        rel = os.path.relpath(base, base_root)
        own_base = bool(timelines_dir()) and os.path.normcase(os.path.abspath(base_root)) == \
            os.path.normcase(os.path.abspath(timelines_dir()))
        print(f"\nThis duty already has a timeline: {base} (in {base_where}).\n"
              "The plans are added to it, with their times moved onto its phases. "
              "Use --new-timeline to make a separate timeline instead.")
        if not own_base:
            print(f"It's a built-in one, so the plans go into your own Timelines folder under {rel}, without a"
                  " timeline.csv: the plugin reads them next to the built-in timeline, and never changes the built-in plans.")
        for other, where, _ in found[1:]:
            print(f"  (also found: {other}, in {where})")
    else:
        rel = os.path.join(*duty_group(fight), folder)

    b = Builder(api, code, fight, report, args)
    healers = choose_healers(b.healers(), wanted_job, interactive)
    feint_slots = b.dps_slots("Feint", MELEE_JOBS, FEINT_SLOTS) if any(job in MELEE_JOBS for job, _ in healers) else {}
    addle_slots = b.dps_slots("Addle", CASTER_JOBS, ADDLE_SLOTS) if any(job in CASTER_JOBS for job, _ in healers) else {}
    tank_slots = b.tank_slots() if any(job in TANK_JOBS for job, _ in healers) else {}
    slots = {**feint_slots, **addle_slots, **tank_slots}
    # Job plan file names: tanks keep their MT / OT slot; melee / casters are "Default" (their Feint / Addle slot is in
    # the shared M1 / M2 / R2 / FakeMelee plan), unless two players of the same job need telling apart.
    job_count = {}
    for job_, _ in healers:
        job_count[job_] = job_count.get(job_, 0) + 1

    def job_slot(job_, me_):
        return slots.get(me_["id"]) if job_ in TANK_JOBS or job_count[job_] > 1 else None
    by_sub = {v: k for k, v in JOBS.items()}
    # Feint profiles need both melee (a lone melee = double caster comp); Addle: R2, plus Fake melee with two casters.
    profile_players = ([("Feint", by_sub[b.actors[i]["subType"]], b.actors[i]) for i in feint_slots] if len(feint_slots) == 2 else []) + \
        [("Addle", by_sub[b.actors[i]["subType"]], b.actors[i]) for i in addle_slots if addle_slots[i] in ADDLE_SLOTS]

    # Where to put it
    out = args.out
    target = timelines_dir()
    if args.install or (INSTALL is True and not out):
        if not target:
            raise SystemExit("Could not find RotationSolver's config folder (is the plugin installed?). Use --out.")
        out = target
    elif not out:
        out = os.path.join(SCRIPT_DIR, "output")
        if INSTALL is None and interactive and target and yes("\nPut it straight into RotationSolver's Timelines folder, so the rotation "
                                          "uses it next time you're in the duty?"):
            out = target
    path = os.path.join(out, rel)
    files = ([] if base else ["timeline.csv"]) + [plan_file(job, job_slot(job, me)) for job, me in healers] + \
        [profile_file(slots[me["id"]], action) for action, _, me in profile_players]
    existing = [f for f in files if os.path.exists(os.path.join(path, f))]
    if existing and not args.force:
        if not (interactive and yes(f"{path} already has {', '.join(existing)}. Replace?", default=False)):
            raise SystemExit("Stopped, nothing was changed (use --force to replace existing files).")

    print("Reading boss casts...")
    sync = b.sync_events()
    origins = b.phases(sync)
    print("Reading damage taken...")
    hits = b.party_hits()
    plans = []
    for job, me in healers:
        print(f"Reading the {job}'s casts...")
        b.job = job
        rows = b.plan_rows(b.healer_casts(me), hits, origins)
        if job == "NIN":
            rows += b.downtime_ninjutsu_rows(me, sync, origins)
            rows.sort(key=lambda r: (int(r["phase"][1:]), r["start"]))
        if job in TANK_JOBS:
            rows += b.stance_rows(me, origins)
            rows.sort(key=lambda r: (int(r["phase"][1:]), r["start"]))
        plans.append((job, me, rows))
    profiles = []
    for action, job, me in profile_players:
        b.job = job
        rows = [r for r in b.plan_rows(b.healer_casts(me), hits, origins) if r["action"] == action]
        profiles.append((slots[me["id"]], action, job, rows))
    profiles.sort()
    # Feint / Addle written to a shared plan for this player: the job plan doesn't repeat them.
    profiled = {(me["id"], action) for action, _, me in profile_players}
    plans = [(job, me, [r for r in rows if (me["id"], r["action"]) not in profiled]) for job, me, rows in plans]

    unreached = []
    if base:
        print("Matching the pull to the existing timeline...")
        base_rows = read_sync_rows(os.path.join(base, "timeline.csv"))
        syncs = replay(base_rows, b.replay_events())
        synced = {s[1] for s in syncs}
        unreached = [ph for ph in dict.fromkeys(r["phase"] for r in base_rows) if ph not in synced]
        plans = [(job, me, convert_rows(rows, origins, syncs)) for job, me, rows in plans]
        profiles = [(slot, action, job, convert_rows(rows, origins, syncs)) for slot, action, job, rows in profiles]
        for line in sync_report(base_rows, syncs):
            print(line)

    os.makedirs(path, exist_ok=True)
    if not base:
        write_timeline(path, duty, b, sync, origins, code)
    elif own_base and not os.path.exists(os.path.join(path, "timeline.csv")):
        shutil.copyfile(os.path.join(base, "timeline.csv"), os.path.join(path, "timeline.csv"))
    for job, me, plan in plans:
        if plan:
            write_plan(path, folder, job, job_slot(job, me), plan, b, code)
    for slot, action, _, rows in profiles:
        write_profile(path, folder, slot, action, rows, b, code)

    print(f"\nDone! Saved in {path}")
    if not base:
        print(f"  timeline.csv: {len(sync) + 1} sync rows, {len(origins)} phase(s)")
    elif not own_base:
        print("  timeline.csv: none, the built-in one is used")
    elif os.path.normcase(os.path.abspath(path)) == os.path.normcase(os.path.abspath(base)):
        print("  timeline.csv: kept as it was")
    else:
        print(f"  timeline.csv: copied from {base}")
    for job, me, plan in plans:
        print(f"  {plan_file(job, job_slot(job, me))}: {len(plan)} rows" if plan else
              f"  {plan_file(job, job_slot(job, me))}: not written (nothing left besides the shared Feint / Addle plan)")
    for slot, action, job, rows in profiles:
        print(f"  {profile_file(slot, action)}: {len(rows)} {action} rows (from the {job})")
    notes = []
    if len(feint_slots) == 1:
        notes.append("Only one melee used Feint in this pull (double caster?), so no M1 / M2 Feint profile was made."
                     " Its Feint stays in its job plan: set 'Feint from' to Job plan to use it.")
    if guessed:
        notes.append(f"Couldn't look up the duty name (XIVAPI unreachable?), so '# Duty: {duty}' is a guess. It must be"
                     " the Duty Finder name without spaces/symbols; fix line 1 of timeline.csv or rerun with --duty.")
    if not fight.get("kill"):
        notes.append("This pull was a wipe, so the timeline stops where it ended. A kill works better.")
    if unreached:
        notes.append(f"The timeline's {', '.join(unreached)} never synced in this pull, so plan rows there are timed"
                     " from the phase before it. Check them, or use a pull that reaches it.")
    if not base and found:
        notes.append(f"This duty also has {found[0][0]}. If both are in your Timelines folder, the plugin may load"
                     " that one instead: move it out while testing this one.")
    if base and own_base and os.path.normcase(os.path.abspath(path)) != os.path.normcase(os.path.abspath(base)) and target and \
            os.path.normcase(os.path.abspath(out)) == os.path.normcase(os.path.abspath(target)):
        notes.append(f"Your Timelines folder now has this duty twice ({path} and {base}); the plugin loads only one.")
    if b.few_players:
        notes.append(f"Only {len(b.players)} real player(s) in this log (the rest are NPCs or weren't recorded), so"
                     " raidwides are guessed from hits on them.")
    for n in notes:
        print(f"\nNOTE: {n}")
    if target and os.path.normcase(os.path.abspath(out)) == os.path.normcase(os.path.abspath(target)):
        print("\nNext: enter the duty as that job. Out of combat the chat says '... loaded (N rows)'.")
    else:
        print(f"\nNext: copy the '{rel}' folder into"
              f" {target or '%APPDATA%/XIVLauncher/pluginConfigs/RotationSolver/Timelines'} (keep the folders above it, or not:"
              " the plugin finds it at any depth).")
    print("It's a draft: " + ("" if base else "delete sync rows for casts that don't happen every pull, and ")
          + "delete plan rows you don't want.")


def main():
    ap = argparse.ArgumentParser(
        description="FFLogs pull -> timeline.csv + CD plan (healers, DPS and tanks). Run with no arguments to be asked step by step.")
    ap.add_argument("url", nargs="?", help="FFLogs report link (#fight=N, fight=last, or none for the last kill)")
    ap.add_argument("job", nargs="?", help="SGE, WHM, AST, SCH, VPR, DRG, RPR, SAM, MNK or NIN (default: every one of them in the pull)")
    ap.add_argument("--client-id")
    ap.add_argument("--client-secret")
    ap.add_argument("--healer", type=int,
                    help="which player to use, by number in the pull's list of plan jobs (sorted by job), e.g. 2")
    ap.add_argument("--duty", help="Duty Finder name, if the automatic lookup gets it wrong")
    ap.add_argument("--install", action="store_true", help="write straight into RotationSolver's Timelines folder")
    ap.add_argument("--out", help="output folder (default: output/ next to this script)")
    ap.add_argument("--min-cast", type=float, default=2.0, help="shortest enemy cast bar that becomes a sync row (s)")
    ap.add_argument("--window", type=float, default=3.0, help="plan window length (s), never below 2.5")
    ap.add_argument("--force", action="store_true", help="replace existing files without asking")
    ap.add_argument("--suffix", default="Test",
                    help="added to the folder and plan name so drafts stand out (--suffix \"\" for none)")
    ap.add_argument("--new-timeline", action="store_true",
                    help="make a separate timeline even when the duty already has one")
    args = ap.parse_args()
    args.window = max(args.window, 2.6)

    interactive = not args.url and sys.stdin.isatty()
    code = 0
    try:
        run(args, interactive)
    except KeyboardInterrupt:
        print("\nCancelled.")
        code = 1
    except SystemExit as e:
        if e.code not in (None, 0):
            print(f"\n{e.code}")
            code = 1
    except urllib.error.URLError as e:
        print(f"\nCouldn't reach FFLogs ({e.reason}). Check your internet connection and try again.")
        code = 1
    except Exception:
        import traceback
        traceback.print_exc()
        print("\nSomething went wrong. Please send the text above (and the log link) to whoever gave you this tool.")
        code = 1
    sys.exit(code)


if __name__ == "__main__":
    main()
