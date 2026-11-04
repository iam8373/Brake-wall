using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BreakToEscape.Core.Json;

namespace BreakToEscape.Core.Save
{
    /// <summary>
    /// Converts <see cref="SaveData"/> to and from bytes. File layout (version 2):
    /// "BTES" magic (4 bytes) · format byte (2) · CRC-32 of the payload (4 bytes, little endian) ·
    /// gzip-compressed UTF-8 JSON payload. Files without the magic are treated as the legacy
    /// version 1 format (plain gzip of the first-draft JSON) and migrated.
    /// </summary>
    public static class SaveCodec
    {
        private static readonly byte[] Magic = { (byte)'B', (byte)'T', (byte)'E', (byte)'S' };
        private const byte FormatByte = 2;
        private const int HeaderLength = 9;

        /// <summary>Encodes a save. The data is sanitized first so only valid values are ever written.</summary>
        public static byte[] Encode(SaveData data)
        {
            data.Sanitize();
            string json = MiniJson.Serialize(ToNode(data));
            byte[] payload = Gzip(Encoding.UTF8.GetBytes(json));
            var bytes = new byte[HeaderLength + payload.Length];
            Array.Copy(Magic, bytes, 4);
            bytes[4] = FormatByte;
            uint crc = Crc32.Compute(payload, 0, payload.Length);
            bytes[5] = (byte)crc;
            bytes[6] = (byte)(crc >> 8);
            bytes[7] = (byte)(crc >> 16);
            bytes[8] = (byte)(crc >> 24);
            Array.Copy(payload, 0, bytes, HeaderLength, payload.Length);
            return bytes;
        }

        /// <summary>Decodes a save. Returns false (never throws) if the bytes are corrupt.</summary>
        public static bool TryDecode(byte[] bytes, out SaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                if (bytes == null || bytes.Length < 2) { error = "empty file"; return false; }
                if (bytes.Length >= HeaderLength && bytes[0] == Magic[0] && bytes[1] == Magic[1] && bytes[2] == Magic[2] && bytes[3] == Magic[3])
                {
                    if (bytes[4] != FormatByte) { error = "unknown format byte " + bytes[4]; return false; }
                    uint stored = (uint)(bytes[5] | (bytes[6] << 8) | (bytes[7] << 16) | (bytes[8] << 24));
                    uint actual = Crc32.Compute(bytes, HeaderLength, bytes.Length - HeaderLength);
                    if (stored != actual) { error = "checksum mismatch"; return false; }
                    string json = Encoding.UTF8.GetString(Gunzip(bytes, HeaderLength, bytes.Length - HeaderLength));
                    var node = JsonRead.Obj(MiniJson.Parse(json), "save");
                    int version = JsonRead.Int(node, "version", 0);
                    data = version <= 1 ? SaveMigration.FromVersion1(node) : FromNode(node);
                }
                else if (bytes[0] == 0x1f && bytes[1] == 0x8b)
                {
                    string json = Encoding.UTF8.GetString(Gunzip(bytes, 0, bytes.Length));
                    data = SaveMigration.FromVersion1(JsonRead.Obj(MiniJson.Parse(json), "legacy save"));
                }
                else
                {
                    error = "unrecognised file header";
                    return false;
                }
                data.Sanitize();
                return true;
            }
            catch (Exception e)
            {
                data = null;
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        private static byte[] Gzip(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                    gzip.Write(raw, 0, raw.Length);
                return output.ToArray();
            }
        }

        private static byte[] Gunzip(byte[] bytes, int offset, int count)
        {
            using (var input = new MemoryStream(bytes, offset, count))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        // ------------------------------------------------------------------ JSON mapping

        public static Dictionary<string, object> ToNode(SaveData d)
        {
            return new Dictionary<string, object>
            {
                { "version", d.version },
                { "savedAtUtcTicks", d.savedAtUtcTicks },
                { "cash", d.cash },
                { "lifetimeCashEarned", d.lifetimeCashEarned },
                { "day", d.day },
                { "dayTimer", d.dayTimer },
                { "runSeconds", d.runSeconds },
                { "cubesBroken", d.cubesBroken },
                { "upgradeLevels", Ints(d.upgradeLevels) },
                { "equipmentRestored", Bools(d.equipmentRestored) },
                { "partsFound", Bools(d.partsFound) },
                { "autoPunchUnlocked", d.autoPunchUnlocked },
                { "treasureDetectorUnlocked", d.treasureDetectorUnlocked },
                { "headlampLevel", d.headlampLevel },
                { "dynamiteStock", d.dynamiteStock },
                { "tankMaterials", Ints(d.tankMaterials) },
                { "tankValues", Ints(d.tankValues) },
                { "tankCounts", Ints(d.tankCounts) },
                { "hasPlayerPose", d.hasPlayerPose },
                { "playerX", d.playerX },
                { "playerY", d.playerY },
                { "playerZ", d.playerZ },
                { "playerYaw", d.playerYaw },
                { "playerPitch", d.playerPitch },
                { "fuelCellFound", d.fuelCellFound },
                { "generatorRepaired", d.generatorRepaired },
                { "winchCableRecovered", d.winchCableRecovered },
                { "logbooksFound", Strs(d.logbooksFound) },
                { "gateCodeEntry", Ints(d.gateCodeEntry) },
                { "liftGateOpened", d.liftGateOpened },
                { "escaped", d.escaped },
                { "usedInteractables", Strs(d.usedInteractables) },
                { "discoveredRooms", Strs(d.discoveredRooms) },
                { "treasuresCollected", Ints(d.treasuresCollected) },
                { "tutorialStep", d.tutorialStep },
                { "adsRemoved", d.adsRemoved },
                { "doubleCashRemaining", d.doubleCashRemaining },
                { "doubleCashCooldownRemaining", d.doubleCashCooldownRemaining },
                { "leftHanded", d.leftHanded },
                { "vibration", d.vibration },
                { "reduceFlashing", d.reduceFlashing },
                { "lookSensitivity", d.lookSensitivity },
                { "qualityOverride", d.qualityOverride },
                { "effectsIntensity", d.effectsIntensity },
                { "brokenVoxelRuns", Ints(d.brokenVoxelRuns) },
                { "damagedVoxelIndices", Ints(d.damagedVoxelIndices) },
                { "damagedVoxelAmounts", Floats(d.damagedVoxelAmounts) },
            };
        }

        public static SaveData FromNode(Dictionary<string, object> n)
        {
            var d = new SaveData
            {
                version = JsonRead.Int(n, "version", SaveData.CurrentVersion),
                savedAtUtcTicks = JsonRead.Long(n, "savedAtUtcTicks", 0),
                cash = JsonRead.Long(n, "cash", 0),
                lifetimeCashEarned = JsonRead.Long(n, "lifetimeCashEarned", 0),
                day = JsonRead.Int(n, "day", 1),
                dayTimer = (float)JsonRead.Num(n, "dayTimer", 0),
                runSeconds = (float)JsonRead.Num(n, "runSeconds", 0),
                cubesBroken = JsonRead.Int(n, "cubesBroken", 0),
                upgradeLevels = ReadInts(n, "upgradeLevels").ToArray(),
                equipmentRestored = ReadBools(n, "equipmentRestored"),
                partsFound = ReadBools(n, "partsFound"),
                autoPunchUnlocked = JsonRead.Bool(n, "autoPunchUnlocked", false),
                treasureDetectorUnlocked = JsonRead.Bool(n, "treasureDetectorUnlocked", false),
                headlampLevel = JsonRead.Int(n, "headlampLevel", 1),
                dynamiteStock = JsonRead.Int(n, "dynamiteStock", 3),
                tankMaterials = ReadInts(n, "tankMaterials"),
                tankValues = ReadInts(n, "tankValues"),
                tankCounts = ReadInts(n, "tankCounts"),
                hasPlayerPose = JsonRead.Bool(n, "hasPlayerPose", false),
                playerX = (float)JsonRead.Num(n, "playerX", 0),
                playerY = (float)JsonRead.Num(n, "playerY", 0),
                playerZ = (float)JsonRead.Num(n, "playerZ", 0),
                playerYaw = (float)JsonRead.Num(n, "playerYaw", 0),
                playerPitch = (float)JsonRead.Num(n, "playerPitch", 0),
                fuelCellFound = JsonRead.Bool(n, "fuelCellFound", false),
                generatorRepaired = JsonRead.Bool(n, "generatorRepaired", false),
                winchCableRecovered = JsonRead.Bool(n, "winchCableRecovered", false),
                logbooksFound = JsonRead.StrList(n, "logbooksFound"),
                gateCodeEntry = ReadInts(n, "gateCodeEntry").ToArray(),
                liftGateOpened = JsonRead.Bool(n, "liftGateOpened", false),
                escaped = JsonRead.Bool(n, "escaped", false),
                usedInteractables = JsonRead.StrList(n, "usedInteractables"),
                discoveredRooms = JsonRead.StrList(n, "discoveredRooms"),
                treasuresCollected = ReadInts(n, "treasuresCollected"),
                tutorialStep = JsonRead.Int(n, "tutorialStep", 0),
                adsRemoved = JsonRead.Bool(n, "adsRemoved", false),
                doubleCashRemaining = (float)JsonRead.Num(n, "doubleCashRemaining", 0),
                doubleCashCooldownRemaining = (float)JsonRead.Num(n, "doubleCashCooldownRemaining", 0),
                leftHanded = JsonRead.Bool(n, "leftHanded", false),
                vibration = JsonRead.Bool(n, "vibration", true),
                reduceFlashing = JsonRead.Bool(n, "reduceFlashing", false),
                lookSensitivity = (float)JsonRead.Num(n, "lookSensitivity", 0.16),
                qualityOverride = JsonRead.Int(n, "qualityOverride", -1),
                effectsIntensity = (float)JsonRead.Num(n, "effectsIntensity", 1),
                brokenVoxelRuns = ReadInts(n, "brokenVoxelRuns"),
                damagedVoxelIndices = ReadInts(n, "damagedVoxelIndices"),
                damagedVoxelAmounts = ReadFloats(n, "damagedVoxelAmounts"),
            };
            return d;
        }

        private static List<object> Ints(IEnumerable<int> values) { var l = new List<object>(); foreach (int v in values) l.Add(v); return l; }
        private static List<object> Floats(IEnumerable<float> values) { var l = new List<object>(); foreach (float v in values) l.Add(v); return l; }
        private static List<object> Bools(IEnumerable<bool> values) { var l = new List<object>(); foreach (bool v in values) l.Add(v); return l; }
        private static List<object> Strs(IEnumerable<string> values) { var l = new List<object>(); foreach (string v in values) l.Add(v); return l; }

        internal static List<int> ReadInts(Dictionary<string, object> n, string key)
        {
            var result = new List<int>();
            List<object> arr = JsonRead.OptArr(n, key);
            if (arr == null) return result;
            foreach (object o in arr) if (o is double d) result.Add((int)Math.Round(d));
            return result;
        }

        private static List<float> ReadFloats(Dictionary<string, object> n, string key)
        {
            var result = new List<float>();
            List<object> arr = JsonRead.OptArr(n, key);
            if (arr == null) return result;
            foreach (object o in arr) if (o is double d) result.Add((float)d);
            return result;
        }

        private static bool[] ReadBools(Dictionary<string, object> n, string key)
        {
            List<object> arr = JsonRead.OptArr(n, key);
            if (arr == null) return new bool[4];
            var result = new bool[arr.Count];
            for (int i = 0; i < arr.Count; i++) result[i] = arr[i] is bool b && b;
            return result;
        }
    }

    /// <summary>Upgrades older save formats to the current <see cref="SaveData"/>.</summary>
    public static class SaveMigration
    {
        /// <summary>
        /// Version 1 (first draft) stored: currentCash, currentDay, punchPowerLevel, punchPatternLevel,
        /// vacuumTierLevel (= Tank Capacity), crusherSpeedLevel, autoPunchUnlocked,
        /// treasureDetectorUnlocked, adsRemoved, dynamiteStock, currentTankCount, player position and
        /// yaw, discoveredRoomIds, collectedTreasureIds, editedVoxelFlatIndices and editedVoxelNewIds.
        /// The player pose and room ids used the old, wrong coordinates, and the tank stored only a
        /// count, so those are not carried over (see docs/DECISIONS.md).
        /// </summary>
        public static SaveData FromVersion1(Dictionary<string, object> v1)
        {
            var d = new SaveData
            {
                cash = JsonRead.Long(v1, "currentCash", 0),
                day = JsonRead.Int(v1, "currentDay", 1),
                autoPunchUnlocked = JsonRead.Bool(v1, "autoPunchUnlocked", false),
                treasureDetectorUnlocked = JsonRead.Bool(v1, "treasureDetectorUnlocked", false),
                adsRemoved = JsonRead.Bool(v1, "adsRemoved", false),
                dynamiteStock = JsonRead.Int(v1, "dynamiteStock", 3),
                hasPlayerPose = false,
            };
            d.lifetimeCashEarned = d.cash;
            d.upgradeLevels[0] = JsonRead.Int(v1, "punchPowerLevel", 1);
            d.upgradeLevels[2] = JsonRead.Int(v1, "punchPatternLevel", 1);
            d.upgradeLevels[4] = JsonRead.Int(v1, "vacuumTierLevel", 1);
            d.upgradeLevels[5] = JsonRead.Int(v1, "crusherSpeedLevel", 1);

            if (JsonRead.StrList(v1, "discoveredRoomIds").Contains("CentralCell")) d.discoveredRooms.Add("collapse_chamber");

            List<int> indices = SaveCodec.ReadInts(v1, "editedVoxelFlatIndices");
            List<int> ids = SaveCodec.ReadInts(v1, "editedVoxelNewIds");
            var broken = new List<int>();
            for (int i = 0; i < Math.Min(indices.Count, ids.Count); i++) if (ids[i] == 0) broken.Add(indices[i]);
            d.brokenVoxelRuns = SaveData.EncodeRuns(broken);
            d.cubesBroken = broken.Count;
            d.version = SaveData.CurrentVersion;
            return d;
        }
    }

    /// <summary>
    /// Crash-safe save file writing. A save is written to "&lt;file&gt;.tmp" first; the previous save
    /// becomes "&lt;file&gt;.bak"; then the temporary file is moved into place. Loading tries the main
    /// file, then the temporary file (a completed write interrupted before the final move), then the
    /// backup, and validates each by checksum, so a crash at any point leaves a loadable save.
    /// </summary>
    public sealed class SaveFileStore
    {
        public string PrimaryPath { get; }
        public string TempPath => PrimaryPath + ".tmp";
        public string BackupPath => PrimaryPath + ".bak";

        public SaveFileStore(string primaryPath)
        {
            PrimaryPath = primaryPath ?? throw new ArgumentNullException(nameof(primaryPath));
        }

        public void Write(byte[] bytes)
        {
            string dir = Path.GetDirectoryName(PrimaryPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
            if (File.Exists(PrimaryPath))
            {
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                File.Move(PrimaryPath, BackupPath);
            }
            File.Move(TempPath, PrimaryPath);
        }

        /// <summary>Loads the newest valid save. Returns null if none exists or all copies are corrupt.</summary>
        public SaveData Load(out string report)
        {
            var notes = new StringBuilder();
            foreach (string path in new[] { PrimaryPath, TempPath, BackupPath })
            {
                if (!File.Exists(path)) continue;
                byte[] bytes;
                try { bytes = File.ReadAllBytes(path); }
                catch (Exception e) { notes.Append(Path.GetFileName(path)).Append(": ").Append(e.Message).Append("; "); continue; }
                if (SaveCodec.TryDecode(bytes, out SaveData data, out string error))
                {
                    notes.Append("loaded ").Append(Path.GetFileName(path));
                    report = notes.ToString();
                    return data;
                }
                notes.Append(Path.GetFileName(path)).Append(" rejected (").Append(error).Append("); ");
            }
            report = notes.Length == 0 ? "no save found" : notes.ToString();
            return null;
        }

        public bool AnyExists => File.Exists(PrimaryPath) || File.Exists(TempPath) || File.Exists(BackupPath);

        public void DeleteAll()
        {
            foreach (string path in new[] { PrimaryPath, TempPath, BackupPath })
                if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>CRC-32 (IEEE 802.3), table-driven.</summary>
    public static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }

        public static uint Compute(byte[] data, int offset, int count)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = offset; i < offset + count; i++) crc = Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }
    }
}
