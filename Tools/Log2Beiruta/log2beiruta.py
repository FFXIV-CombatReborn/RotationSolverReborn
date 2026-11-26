#!/usr/bin/env python3
"""
log2beiruta.py - turn an FFLogs pull into a draft timeline.csv + healer plan for the Beiruta healers.

How to run: fill in YOUR SETTINGS below (or leave them empty to be asked), then press Run Python File
in VS Code. See README.md for the full guide.

What the draft contains:
- timeline.csv: CombatStart, a CastStart sync row for every enemy cast with a cast bar (>= --min-cast s),
  boss Targetable / Untargetable rows, split into phases on FFLogs' phase changes. No Damage rows.
- plan: only the actions the built-in plans schedule (raid mitigation, party heals, their set-up),
  taken from what the healer in the log did. No '# Set.' lines.
- Mitigation rows are timed so the action is up before the first party hit it covers, still lasts to the
  last hit it covers, and has a window longer than 2.5s. The Note column names the hits.
Only the standard library is used, so no pip install is needed.
"""

import argparse
import base64
import datetime
import getpass
import json
import math
import os
import re
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

# 2. Which healer's plan to make: "SGE", "WHM", "AST" or "SCH".
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

JOBS = {"SGE": "Sage", "WHM": "WhiteMage", "AST": "Astrologian", "SCH": "Scholar"}
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
}

# Mitigation / shield actions and how long their effect lasts (s). These get the
# "up before the first hit, still up for the last hit" window treatment.
MIT_DURATION = {
    "Sacred Soil": 15, "Expedient": 20, "Fey Illumination": 20, "Concitation": 30, "Consolation": 30,
    "Deployment Tactics": 30,
    "Kerachole": 15, "Panhaima": 15, "Holos": 20,
    "Temperance": 20, "Divine Caress": 10, "Liturgy of the Bell": 20,
    "Collective Unconscious": 5, "Sun Sign": 15, "Macrocosmos": 15,
}

# Group GCD heals that get Target 'Precast' when cast right before a party hit.
PRECAST_GCDS = {"Medica III", "Helios Conjunction", "Cure III"}

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
            headers={"Authorization": f"Basic {auth}", "User-Agent": "log2beiruta"})
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
            "Authorization": f"Bearer {self.token}", "Content-Type": "application/json", "User-Agent": "log2beiruta"})
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


def duty_finder_name(territory_id):
    """The duty's Duty Finder name (what the plugin matches on), from the game data on XIVAPI."""
    if not territory_id:
        return None
    url = f"https://v2.xivapi.com/api/sheet/TerritoryType/{int(territory_id)}?fields=ContentFinderCondition.Name"
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "log2beiruta"}), timeout=20) as r:
            name = json.load(r)["fields"]["ContentFinderCondition"]["fields"]["Name"]
        return name or None
    except Exception:
        return None


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
  2. Click "Create Client". Any name works (e.g. Log2Beiruta); for the redirect URL
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
        self.job = None  # set before building each healer's plan
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
    def party_hits(self):
        events = self.api.events(self.code, self.fight, "DamageTaken", "Friendlies")
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
    def sync_events(self):
        rows = []
        casts = self.api.events(self.code, self.fight, "Casts", "Enemies")
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

        targ = self.api.events(self.code, self.fight, "All", "Enemies", 'type = "targetabilityupdate"')
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

    # ---- the healer's casts
    def healers(self):
        """[(job, actor)] for every AST/SCH/SGE/WHM in the pull (narrowed by --healer)."""
        by_sub = {v: k for k, v in JOBS.items()}
        found = [(by_sub[a["subType"]], a) for a in self.actors.values()
                 if a.get("type") == "Player" and a.get("subType") in by_sub and a["id"] in self.players]
        found.sort(key=lambda h: (h[0], h[1]["id"]))
        if self.args.healer:
            if not 1 <= self.args.healer <= len(found):
                raise SystemExit(f"--healer {self.args.healer}: this pull has {len(found)} healer(s): {healer_list(found)}.")
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

            gcd_extra = 1.0 if ct > 0.5 else 0.0
            start, end = t, t + w + gcd_extra
            if name in MIT_DURATION:
                dur = MIT_DURATION[name]
                landed = t + ct
                covered = [h for h in hits if landed < h[0] <= landed + dur]
                if covered:
                    end = min(end, covered[0][0] - ct - margin)        # up before the first hit
                    start = min(start, end - max(w, min_w + 0.1))      # window over 2.5s
                    while covered and start + ct + dur < covered[-1][0] + margin:
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


def write_plan(path, folder, job, plan, b, code):
    lines = [f"# {folder} Log {job} draft ({short_code(code)} fight {b.fight['id']})",
             "Phase,Start,End,Action,Target,Note"]
    for r in plan:
        lines.append(",".join([r["phase"], fmt(r["start"]), fmt(r["end"]), r["action"], r["target"], clean(r["note"])]))
    with open(os.path.join(path, f"Default.{job}.plan.csv"), "w", encoding="utf-8", newline="\n") as fh:
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
        raise SystemExit(f"No {wanted_job} in this pull. Beiruta healers in it: {healer_list(found)}.")
    if not found:
        raise SystemExit("No AST, SCH, SGE or WHM in this pull, so there is nothing to make a plan from.")
    if not interactive or len(found) == 1:
        return found
    print("\nHealers in this pull:")
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
        print("Log2Beiruta: FFLogs pull -> timeline + healer plan for the Beiruta rotations\n")

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
                         "(or leave it \"\" to use every healer in the pull).")

    api = login(args, interactive)
    print("Reading the report...")
    report = api.report(code)
    fight = pick_fight(report, fight_id, interactive)
    duty, folder, guessed = duty_for(fight, args.duty)
    if args.suffix.strip():
        folder = f"{folder} {clean(args.suffix)}"
    print(f"\nPull {fight['id']}: {fight['name']} ({'kill' if fight.get('kill') else 'wipe'})")

    b = Builder(api, code, fight, report, args)
    healers = choose_healers(b.healers(), wanted_job, interactive)

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
    path = os.path.join(out, folder)
    files = ["timeline.csv"] + [f"Default.{job}.plan.csv" for job, _ in healers]
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
        plans.append((job, me, b.plan_rows(b.healer_casts(me), hits, origins)))

    os.makedirs(path, exist_ok=True)
    write_timeline(path, duty, b, sync, origins, code)
    for job, _, plan in plans:
        write_plan(path, folder, job, plan, b, code)

    print(f"\nDone! Saved in {path}")
    print(f"  timeline.csv: {len(sync) + 1} sync rows, {len(origins)} phase(s)")
    for job, _, plan in plans:
        print(f"  Default.{job}.plan.csv: {len(plan)} rows")
    notes = []
    if guessed:
        notes.append(f"Couldn't look up the duty name (XIVAPI unreachable?), so '# Duty: {duty}' is a guess. It must be"
                     " the Duty Finder name without spaces/symbols; fix line 1 of timeline.csv or rerun with --duty.")
    if not fight.get("kill"):
        notes.append("This pull was a wipe, so the timeline stops where it ended. A kill works better.")
    if b.few_players:
        notes.append(f"Only {len(b.players)} real player(s) in this log (the rest are NPCs or weren't recorded), so"
                     " raidwides are guessed from hits on them.")
    for n in notes:
        print(f"\nNOTE: {n}")
    if target and os.path.normcase(os.path.abspath(out)) == os.path.normcase(os.path.abspath(target)):
        print("\nNext: enter the duty as that healer. Out of combat the chat says '... loaded (N rows)'.")
        if args.suffix.strip():
            print("If the duty already has another timeline folder, that one wins: move it out while testing this one.")
    else:
        print(f"\nNext: copy the '{folder}' folder into"
              f" {target or '%APPDATA%/XIVLauncher/pluginConfigs/RotationSolver/Timelines'}.")
    print("It's a draft: delete sync rows for casts that don't happen every pull, and plan rows you don't want.")


def main():
    ap = argparse.ArgumentParser(
        description="FFLogs pull -> Beiruta timeline.csv + healer plan. Run with no arguments to be asked step by step.")
    ap.add_argument("url", nargs="?", help="FFLogs report link (#fight=N, fight=last, or none for the last kill)")
    ap.add_argument("job", nargs="?", help="SGE, WHM, AST or SCH (default: every one of them in the pull)")
    ap.add_argument("--client-id")
    ap.add_argument("--client-secret")
    ap.add_argument("--healer", type=int,
                    help="which healer to use, by number in the pull's healer list (sorted by job), e.g. 2")
    ap.add_argument("--duty", help="Duty Finder name, if the automatic lookup gets it wrong")
    ap.add_argument("--install", action="store_true", help="write straight into RotationSolver's Timelines folder")
    ap.add_argument("--out", help="output folder (default: output/ next to this script)")
    ap.add_argument("--min-cast", type=float, default=2.0, help="shortest enemy cast bar that becomes a sync row (s)")
    ap.add_argument("--window", type=float, default=3.0, help="plan window length (s), never below 2.5")
    ap.add_argument("--force", action="store_true", help="replace existing files without asking")
    ap.add_argument("--suffix", default="Test",
                    help="added to the folder and plan name so drafts stand out (--suffix \"\" for none)")
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
