using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BreakToEscape.Core.Json;
using BreakToEscape.Core.Save;
using NUnit.Framework;

namespace BreakToEscape.Tests
{
    public class SaveTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "bte_save_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        private static SaveData Sample()
        {
            var d = new SaveData
            {
                cash = 12345,
                lifetimeCashEarned = 50000,
                day = 4,
                runSeconds = 1234.5f,
                cubesBroken = 777,
                upgradeLevels = new[] { 3, 2, 4, 1, 5, 2, 1 },
                equipmentRestored = new[] { true, false, true, false },
                partsFound = new[] { true, true, true, false },
                autoPunchUnlocked = true,
                treasureDetectorUnlocked = true,
                headlampLevel = 2,
                dynamiteStock = 7,
                hasPlayerPose = true,
                playerX = 24.25f, playerY = 1.0f, playerZ = 22.5f, playerYaw = 90f, playerPitch = -10f,
                fuelCellFound = true,
                generatorRepaired = true,
                winchCableRecovered = false,
                gateCodeEntry = new[] { 14, 3, 0 },
                tutorialStep = 5,
                adsRemoved = true,
                doubleCashRemaining = 120f,
                doubleCashCooldownRemaining = 300f,
                leftHanded = true,
                vibration = false,
                lookSensitivity = 0.2f,
                qualityOverride = 1,
            };
            d.tankMaterials.AddRange(new[] { 1, 2 });
            d.tankValues.AddRange(new[] { 2, 4 });
            d.tankCounts.AddRange(new[] { 10, 3 });
            d.logbooksFound.AddRange(new[] { "quill", "pike" });
            d.usedInteractables.AddRange(new[] { "fuel_cell", "quill_locker" });
            d.discoveredRooms.AddRange(new[] { "collapse_chamber", "old_gallery" });
            d.treasuresCollected.Add(2);
            d.brokenVoxelRuns = SaveData.EncodeRuns(new[] { 100, 101, 102, 500, 501, 9000 });
            d.damagedVoxelIndices.Add(42);
            d.damagedVoxelAmounts.Add(12.5f);
            return d;
        }

        [Test]
        public void RoundTripKeepsEveryField()
        {
            SaveData original = Sample();
            Assert.IsTrue(SaveCodec.TryDecode(SaveCodec.Encode(original), out SaveData copy, out string error), error);
            Assert.AreEqual(MiniJson.Serialize(SaveCodec.ToNode(original)), MiniJson.Serialize(SaveCodec.ToNode(copy)));
            Assert.AreEqual(new List<int> { 100, 101, 102, 500, 501, 9000 }, SaveData.DecodeRuns(copy.brokenVoxelRuns, int.MaxValue));
        }

        [Test]
        public void RunEncodingHandlesDuplicatesAndOrder()
        {
            List<int> runs = SaveData.EncodeRuns(new[] { 5, 3, 4, 4, 10 });
            Assert.AreEqual(new List<int> { 3, 3, 10, 1 }, runs);
            Assert.AreEqual(new List<int> { 3, 4, 5 }, SaveData.DecodeRuns(runs, 6));
        }

        [Test]
        public void AtomicWriteKeepsABackupAndRecoversFromCorruption()
        {
            var store = new SaveFileStore(Path.Combine(dir, "save.dat"));
            SaveData first = Sample();
            first.cash = 1;
            store.Write(SaveCodec.Encode(first));
            SaveData second = Sample();
            second.cash = 2;
            store.Write(SaveCodec.Encode(second));
            Assert.IsTrue(File.Exists(store.BackupPath));
            Assert.IsFalse(File.Exists(store.TempPath));
            Assert.AreEqual(2, store.Load(out _).cash);

            // Corrupt the primary (flip a byte in the payload): the checksum rejects it, the backup loads.
            byte[] bytes = File.ReadAllBytes(store.PrimaryPath);
            bytes[bytes.Length - 3] ^= 0x5A;
            File.WriteAllBytes(store.PrimaryPath, bytes);
            SaveData recovered = store.Load(out string report);
            Assert.IsNotNull(recovered, report);
            Assert.AreEqual(1, recovered.cash);
            StringAssert.Contains("rejected", report);
        }

        [Test]
        public void InterruptedWriteBeforeFinalMoveLoadsTheTempFile()
        {
            var store = new SaveFileStore(Path.Combine(dir, "save.dat"));
            SaveData older = Sample();
            older.cash = 10;
            store.Write(SaveCodec.Encode(older));
            // Simulate: new data fully written to .tmp, primary already moved to .bak, then the app died.
            SaveData newer = Sample();
            newer.cash = 20;
            File.WriteAllBytes(store.TempPath, SaveCodec.Encode(newer));
            File.Move(store.PrimaryPath, store.BackupPath);
            Assert.AreEqual(20, store.Load(out _).cash);
        }

        [Test]
        public void TruncatedOrGarbageFilesNeverThrow()
        {
            var store = new SaveFileStore(Path.Combine(dir, "save.dat"));
            File.WriteAllBytes(store.PrimaryPath, new byte[] { 1, 2, 3 });
            Assert.IsNull(store.Load(out _));
            byte[] good = SaveCodec.Encode(Sample());
            File.WriteAllBytes(store.PrimaryPath, good.AsSpan(0, good.Length / 2).ToArray());
            Assert.IsNull(store.Load(out _));
            Assert.IsFalse(SaveCodec.TryDecode(null, out _, out _));
            Assert.IsFalse(SaveCodec.TryDecode(Encoding.UTF8.GetBytes("{\"cash\":5}"), out _, out _));
        }

        [Test]
        public void BadValuesAreClamped()
        {
            SaveData d = Sample();
            d.cash = -50;
            d.upgradeLevels = new[] { 0, 99 };
            d.gateCodeEntry = new[] { -1, 250 };
            d.lookSensitivity = float.NaN;
            d.playerX = float.PositiveInfinity;
            Assert.IsTrue(SaveCodec.TryDecode(SaveCodec.Encode(d), out SaveData c, out _));
            Assert.AreEqual(0, c.cash);
            Assert.AreEqual(new[] { 1, 10, 1, 1, 1, 1, 1 }, c.upgradeLevels);
            Assert.AreEqual(new[] { 99, 50, 0 }, c.gateCodeEntry);
            Assert.AreEqual(0.16f, c.lookSensitivity, 1e-6);
            Assert.IsFalse(c.hasPlayerPose);
        }

        [Test]
        public void Version1SaveMigratesToCurrent()
        {
            // The first-draft format: gzip of a flat JSON object written by JsonUtility.
            string v1 = "{\"version\":1,\"seed\":10492,\"currentCash\":900,\"currentDay\":3,\"punchPowerLevel\":3," +
                        "\"punchPatternLevel\":2,\"vacuumTierLevel\":4,\"crusherSpeedLevel\":2,\"autoPunchUnlocked\":true," +
                        "\"treasureDetectorUnlocked\":false,\"adsRemoved\":false,\"dynamiteStock\":5,\"currentTankCount\":40," +
                        "\"playerX\":7.0,\"playerY\":1.5,\"playerZ\":2.0,\"playerYaw\":0.0,\"discoveredRoomIds\":[\"CentralCell\",\"CrewRoom1\"]," +
                        "\"collectedTreasureIds\":[1],\"editedVoxelFlatIndices\":[10,11,12,40],\"editedVoxelNewIds\":[0,0,0,0]}";
            byte[] gz;
            using (var ms = new MemoryStream())
            {
                using (var g = new GZipStream(ms, CompressionMode.Compress))
                {
                    byte[] raw = Encoding.UTF8.GetBytes(v1);
                    g.Write(raw, 0, raw.Length);
                }
                gz = ms.ToArray();
            }
            Assert.IsTrue(SaveCodec.TryDecode(gz, out SaveData d, out string error), error);
            Assert.AreEqual(SaveData.CurrentVersion, d.version);
            Assert.AreEqual(900, d.cash);
            Assert.AreEqual(3, d.day);
            Assert.AreEqual(new[] { 3, 1, 2, 1, 4, 2, 1 }, d.upgradeLevels);
            Assert.IsTrue(d.autoPunchUnlocked);
            Assert.AreEqual(5, d.dynamiteStock);
            Assert.IsFalse(d.hasPlayerPose, "old coordinates are not trusted");
            Assert.AreEqual(new List<string> { "collapse_chamber" }, d.discoveredRooms);
            Assert.AreEqual(new List<int> { 10, 11, 12, 40 }, SaveData.DecodeRuns(d.brokenVoxelRuns, int.MaxValue));
            Assert.AreEqual(4, d.cubesBroken);
            Assert.AreEqual(0, d.tankCounts.Count, "tank contents were never stored in version 1");
        }

        [Test]
        public void MiniJsonRoundTripsEscapesAndNumbers()
        {
            var node = new Dictionary<string, object> { { "s", "a \"quoted\"\nline\\" }, { "n", -12.75 }, { "b", true }, { "z", null }, { "l", new List<object> { 1, 2.5, "x" } } };
            string json = MiniJson.Serialize(node, pretty: true);
            var back = (Dictionary<string, object>)MiniJson.Parse(json);
            Assert.AreEqual("a \"quoted\"\nline\\", back["s"]);
            Assert.AreEqual(-12.75, back["n"]);
            Assert.AreEqual(true, back["b"]);
            Assert.IsNull(back["z"]);
            Assert.AreEqual(3, ((List<object>)back["l"]).Count);
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"a\":1,}"));
            Assert.Throws<FormatException>(() => MiniJson.Parse("[1 2]"));
        }
    }
}
