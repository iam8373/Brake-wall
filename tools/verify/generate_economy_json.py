#!/usr/bin/env python3
"""Generate UnityProject/Assets/Resources/Data/economy.json from the economy spreadsheet.

The spreadsheet (docs/economy/Break_to_Escape_Economy.xlsx) is the source of truth for
upgrade effects, upgrade costs, band numbers and the pacing model's settings. This
script reads the cached cell values with the Python standard library only (an .xlsx
file is a zip of XML documents), so no extra packages are needed.

Values that are not in the spreadsheet (treasure, store items, equipment, monetization
rules, device tiers) come from tools/verify/economy_extras.json and are merged in.

Usage:
    python3 tools/verify/generate_economy_json.py          # write the JSON
    python3 tools/verify/generate_economy_json.py --check  # fail if the JSON is out of date
"""
import json
import os
import re
import sys
import zipfile
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
XLSX = os.path.join(ROOT, "docs", "economy", "Break_to_Escape_Economy.xlsx")
EXTRAS = os.path.join(ROOT, "tools", "verify", "economy_extras.json")
OUT = os.path.join(ROOT, "UnityProject", "Assets", "Resources", "Data", "economy.json")

NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"

UPGRADE_KEYS = [
    # (json key, row on the Upgrades sheet, effect column on the effect table, unit)
    ("punchPower", 5, "B", "damage"),
    ("punchSpeed", 6, "C", "punches/s"),
    ("punchPattern", 7, "D", "cubes"),
    ("vacuumSuction", 8, "E", "cubes/s"),
    ("tankCapacity", 9, "F", "cubes"),
    ("crusherSpeed", 10, "G", "cubes/s"),
    ("oreSense", 11, "H", "x cash"),
]
CUMULATIVE_COLUMNS = ["J", "K", "L", "M", "N", "O", "P"]
PACING_ROWS = {"punchPower": 7, "punchSpeed": 8, "punchPattern": 9, "vacuumSuction": 10,
               "tankCapacity": 11, "crusherSpeed": 12, "oreSense": 13}


def read_workbook(path):
    z = zipfile.ZipFile(path)
    shared = []
    if "xl/sharedStrings.xml" in z.namelist():
        root = ET.fromstring(z.read("xl/sharedStrings.xml"))
        for si in root.findall("m:si", NS):
            shared.append("".join(t.text or "" for t in si.iter("{%s}t" % NS["m"])))
    wb = ET.fromstring(z.read("xl/workbook.xml"))
    rels = ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))
    targets = {r.get("Id"): r.get("Target") for r in rels}
    sheets = {}
    for s in wb.find("m:sheets", NS):
        rid = s.get("{%s}id" % REL_NS)
        target = targets[rid].lstrip("/")
        if not target.startswith("xl/"):
            target = "xl/" + target
        cells = {}
        for c in ET.fromstring(z.read(target)).iter("{%s}c" % NS["m"]):
            v = c.find("m:v", NS)
            if v is None:
                continue
            value = v.text
            if c.get("t") == "s":
                value = shared[int(value)]
            elif c.get("t") not in ("str", "inlineStr"):
                try:
                    value = float(value)
                except (TypeError, ValueError):
                    pass
            cells[c.get("r")] = value
        sheets[s.get("name")] = cells
    return sheets


def num(cells, ref):
    value = cells.get(ref)
    if not isinstance(value, float):
        raise ValueError("Expected a number in cell %s, found %r" % (ref, value))
    return clean(value)


def clean(value):
    """Round away binary noise from cached formula results (e.g. 91293.1225600001)."""
    rounded = round(value, 9)
    return int(rounded) if rounded == int(rounded) else rounded


def build(sheets, extras):
    up = sheets["Upgrades"]
    bands_sheet = sheets["Bands"]
    pacing = sheets["Pacing"]

    upgrades = {}
    for index, (key, row, column, unit) in enumerate(UPGRADE_KEYS):
        effects = [num(up, "%s%d" % (column, 15 + level)) for level in range(10)]
        cumulative = [num(up, "%s%d" % (CUMULATIVE_COLUMNS[index], 15 + level)) for level in range(10)]
        upgrades[key] = {
            "displayName": up["A%d" % row],
            "description": up["F%d" % row],
            "unit": unit,
            "baseCost": num(up, "B%d" % row),
            "costGrowth": num(up, "C%d" % row),
            "effects": effects,
            "cumulativeCost": cumulative,
        }

    bands = []
    for i in range(6):
        r = 5 + i
        bands.append({
            "id": i + 1,
            "code": bands_sheet["A%d" % r],
            "name": bands_sheet["B%d" % r],
            "look": bands_sheet["C%d" % r],
            "hp": num(bands_sheet, "D%d" % r),
            "value": num(bands_sheet, "E%d" % r),
            "cubesOnPath": num(bands_sheet, "F%d" % r),
            "overheadSeconds": num(bands_sheet, "G%d" % r),
            "notes": bands_sheet["H%d" % r],
            "colorHex": extras["bandColors"][i],
        })

    planned = {}
    for key, row in PACING_ROWS.items():
        planned[key] = [int(num(pacing, "%s%d" % (col, row))) for col in "BCDEFGH"]

    expected = {
        "timeInBandMinutes": [num(pacing, "%s33" % col) for col in "BCDEFG"],
        "cashEarnedInBand": [num(pacing, "%s35" % col) for col in "BCDEFG"],
        "cashNeededForNextUpgrades": [num(pacing, "%s36" % col) for col in "BCDEFG"],
        "totalRunMinutes": num(pacing, "B41"),
        "totalCashEarned": num(pacing, "B43"),
        "cashToMaxEverything": num(pacing, "B44"),
    }

    data = {
        "_generatedBy": "tools/verify/generate_economy_json.py from docs/economy/Break_to_Escape_Economy.xlsx and tools/verify/economy_extras.json. Do not edit by hand; edit the sources and re-run the script.",
        "version": 1,
        "upgrades": upgrades,
        "bands": bands,
        "pacingModel": {
            "patternEfficiency": num(bands_sheet, "D14"),
            "tripSeconds": num(bands_sheet, "D15"),
            "collectionEfficiency": num(bands_sheet, "D16"),
            "targetMinMinutes": num(bands_sheet, "D17"),
            "targetMaxMinutes": num(bands_sheet, "D18"),
            "healthyRatioLow": num(bands_sheet, "D19"),
            "healthyRatioHigh": num(bands_sheet, "D20"),
            "plannedLevelsAtBandStart": planned,
            "spreadsheetResults": expected,
        },
    }
    for key, value in extras.items():
        if key.startswith("_") or key == "bandColors":
            continue
        data[key] = value
    return data


def main():
    sheets = read_workbook(XLSX)
    with open(EXTRAS, encoding="utf-8") as f:
        extras = json.load(f)
    text = json.dumps(build(sheets, extras), indent=2, ensure_ascii=False) + "\n"
    if "--check" in sys.argv:
        with open(OUT, encoding="utf-8") as f:
            current = f.read()
        if current != text:
            print("economy.json is OUT OF DATE. Run tools/verify/generate_economy_json.py")
            return 1
        print("economy.json matches the spreadsheet and extras.")
        return 0
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write(text)
    print("Wrote", os.path.relpath(OUT, ROOT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
