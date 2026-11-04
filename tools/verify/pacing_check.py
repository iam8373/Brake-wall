#!/usr/bin/env python3
"""Reproduce the economy spreadsheet's Pacing sheet from economy.json and compare.

The game enforces the same rates the spreadsheet assumes (punch cooldown from Punch Speed,
vacuum token bucket from Vacuum Suction, crusher queue from Crusher Speed), so the model
below is the model of the game as coded. The script prints per-band time, cash earned,
cash needed and the earned/needed ratio, and checks every number against the values the
spreadsheet itself computed (stored in economy.json under pacingModel.spreadsheetResults).

It also reports the treasure payout that the spreadsheet does not model.

Usage: python3 tools/verify/pacing_check.py      (exit code 1 on any mismatch)
"""
import json
import math
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DATA = os.path.join(ROOT, "UnityProject", "Assets", "Resources", "Data", "economy.json")
LINES = ["punchPower", "punchSpeed", "punchPattern", "vacuumSuction", "tankCapacity", "crusherSpeed", "oreSense"]


def main():
    with open(DATA, encoding="utf-8") as f:
        d = json.load(f)
    up = d["upgrades"]
    pm = d["pacingModel"]
    planned = pm["plannedLevelsAtBandStart"]
    expected = pm["spreadsheetResults"]

    def effect(line, level):
        return up[line]["effects"][level - 1]

    rows = []
    cumulative = 0.0
    ok = True
    for b, band in enumerate(d["bands"]):
        lv = {k: planned[k][b] for k in LINES}
        nxt = {k: planned[k][b + 1] for k in LINES}
        dmg = effect("punchPower", lv["punchPower"])
        pps = effect("punchSpeed", lv["punchSpeed"])
        cubes = effect("punchPattern", lv["punchPattern"])
        vac = effect("vacuumSuction", lv["vacuumSuction"])
        tank = effect("tankCapacity", lv["tankCapacity"])
        crusher = effect("crusherSpeed", lv["crusherSpeed"])
        mult = effect("oreSense", lv["oreSense"])
        ptb = math.ceil(band["hp"] / dmg - 1e-9)
        punch_tp = cubes * pps * pm["patternEfficiency"] / ptb
        overall = min(punch_tp, vac, crusher)
        bottleneck = "Punching" if overall == punch_tp else ("Vacuum" if overall == vac else "Crusher")
        trips = band["cubesOnPath"] / tank
        minutes = (band["cubesOnPath"] / overall + trips * pm["tripSeconds"] + band["overheadSeconds"]) / 60.0
        cumulative += minutes
        earned = band["cubesOnPath"] * band["value"] * mult * pm["collectionEfficiency"]
        needed = sum(up[k]["cumulativeCost"][nxt[k] - 1] - up[k]["cumulativeCost"][lv[k] - 1] for k in LINES)
        ratio = earned / needed if needed else 0.0
        rows.append((band["code"], ptb, punch_tp, overall, bottleneck, minutes, cumulative, earned, needed, ratio))
        for name, mine, theirs in (("time", minutes, expected["timeInBandMinutes"][b]),
                                   ("earned", earned, expected["cashEarnedInBand"][b]),
                                   ("needed", needed, expected["cashNeededForNextUpgrades"][b])):
            if abs(mine - theirs) > 1e-6 * max(1.0, abs(theirs)):
                print("MISMATCH %s %s: model %.6f, spreadsheet %.6f" % (band["code"], name, mine, theirs))
                ok = False

    print("Band | punches/cube | punch cubes/s | overall cubes/s | bottleneck | min in band | cumulative min | cash earned | cash needed | earned/needed")
    for r in rows:
        print("%-4s | %12d | %13.2f | %15.2f | %-10s | %11.1f | %14.1f | %11.0f | %11.0f | %13.2f" % r)
    total = rows[-1][6]
    print("Total run: %.1f min (spreadsheet %.1f). Target %d-%d min." % (total, expected["totalRunMinutes"], pm["targetMinMinutes"], pm["targetMaxMinutes"]))
    if abs(total - expected["totalRunMinutes"]) > 1e-6:
        ok = False

    # Treasure (not in the spreadsheet): expected count of treasure voxels per 1,000 cubes dug.
    one_in = d.get("treasureDensity", {}).get("oneInVoxels", 0)
    if one_in:
        print("\nTreasure (not modelled by the spreadsheet): about 1 in %d rock cubes is a treasure seam." % one_in)
        for t in d.get("treasureMaterials", []):
            for band_id in t["bands"]:
                band = d["bands"][band_id - 1]
                extra = band["cubesOnPath"] / one_in * t["value"]
                share = extra / (band["cubesOnPath"] * band["value"])
                print("  %s in %s: ~%.0f cubes on the path, +$%.0f (%.0f%% of the band's rock value)" %
                      (t["name"], band["code"], band["cubesOnPath"] / one_in, extra, share * 100))
    print("\nRESULT:", "MATCH" if ok else "MISMATCH")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
