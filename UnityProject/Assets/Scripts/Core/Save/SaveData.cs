using System;
using System.Collections.Generic;

namespace BreakToEscape.Core.Save
{
    /// <summary>
    /// Everything needed to restore a run. Version 2 is the current format. Version 1 was the
    /// first-draft format (four upgrade levels, no story state); it is upgraded by
    /// <see cref="SaveMigration"/>.
    /// </summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public long savedAtUtcTicks;

        // Economy
        public long cash;
        public long lifetimeCashEarned;
        public int day = 1;
        public float dayTimer;
        public float runSeconds;
        public int cubesBroken;
        public int[] upgradeLevels = { 1, 1, 1, 1, 1, 1, 1 };
        /// <summary>Indexed like EconomyState.EquipmentKeys: workGauntlets, pneumaticHammer, conveyor, rockDrill.</summary>
        public bool[] equipmentRestored = new bool[4];
        public bool[] partsFound = new bool[4];
        public bool autoPunchUnlocked;
        public bool treasureDetectorUnlocked;
        public int headlampLevel = 1;
        public int dynamiteStock = 3;

        // Vacuum tank contents as runs of (material, value, count)
        public List<int> tankMaterials = new List<int>();
        public List<int> tankValues = new List<int>();
        public List<int> tankCounts = new List<int>();

        // Player pose (metres, world space)
        public bool hasPlayerPose;
        public float playerX, playerY, playerZ, playerYaw, playerPitch;

        // Story and escape
        public bool fuelCellFound;
        public bool generatorRepaired;
        public bool winchCableRecovered;
        public List<string> logbooksFound = new List<string>();
        public int[] gateCodeEntry = { 0, 0, 0 };
        public bool liftGateOpened;
        public bool escaped;
        public List<string> usedInteractables = new List<string>();
        public List<string> discoveredRooms = new List<string>();
        public List<int> treasuresCollected = new List<int>();
        public int tutorialStep;

        // Monetization
        public bool adsRemoved;
        public float doubleCashRemaining;
        public float doubleCashCooldownRemaining;

        // Settings
        public bool leftHanded;
        public bool vibration = true;
        public bool reduceFlashing;
        public float lookSensitivity = 0.16f;
        public int qualityOverride = -1;
        public float effectsIntensity = 1f;

        // World: broken voxels as sorted runs of consecutive flat indices [start, length, start, length, ...]
        public List<int> brokenVoxelRuns = new List<int>();
        // World: partial damage on unbroken voxels
        public List<int> damagedVoxelIndices = new List<int>();
        public List<float> damagedVoxelAmounts = new List<float>();

        /// <summary>Encodes broken voxel flat indices as runs (indices need not be sorted or unique).</summary>
        public static List<int> EncodeRuns(IEnumerable<int> indices)
        {
            var sorted = new List<int>(indices);
            sorted.Sort();
            var runs = new List<int>();
            int i = 0;
            while (i < sorted.Count)
            {
                int start = sorted[i];
                int length = 1;
                i++;
                while (i < sorted.Count && sorted[i] <= start + length)
                {
                    if (sorted[i] == start + length) length++;
                    i++;
                }
                runs.Add(start);
                runs.Add(length);
            }
            return runs;
        }

        public static List<int> DecodeRuns(IList<int> runs, int maxIndexExclusive)
        {
            var result = new List<int>();
            for (int r = 0; r + 1 < runs.Count; r += 2)
            {
                int start = runs[r], length = runs[r + 1];
                if (start < 0 || length <= 0) continue;
                for (int k = 0; k < length; k++)
                {
                    int idx = start + k;
                    if (idx >= maxIndexExclusive) break;
                    result.Add(idx);
                }
            }
            return result;
        }

        /// <summary>Clamps every value into its valid range so bad or tampered data can never crash the game.</summary>
        public void Sanitize()
        {
            version = CurrentVersion;
            cash = Math.Max(0, cash);
            lifetimeCashEarned = Math.Max(cash, lifetimeCashEarned);
            day = Math.Max(1, day);
            dayTimer = Finite(dayTimer, 0f);
            runSeconds = Math.Max(0f, Finite(runSeconds, 0f));
            cubesBroken = Math.Max(0, cubesBroken);
            upgradeLevels = FixArray(upgradeLevels, 7, 1);
            for (int i = 0; i < upgradeLevels.Length; i++) upgradeLevels[i] = Math.Max(1, Math.Min(10, upgradeLevels[i]));
            equipmentRestored = FixBools(equipmentRestored, 4);
            partsFound = FixBools(partsFound, 4);
            headlampLevel = Math.Max(1, Math.Min(3, headlampLevel));
            dynamiteStock = Math.Max(0, Math.Min(999, dynamiteStock));
            gateCodeEntry = FixArray(gateCodeEntry, 3, 0);
            for (int i = 0; i < 3; i++) gateCodeEntry[i] = ((gateCodeEntry[i] % 100) + 100) % 100;
            tankMaterials = tankMaterials ?? new List<int>();
            tankValues = tankValues ?? new List<int>();
            tankCounts = tankCounts ?? new List<int>();
            logbooksFound = logbooksFound ?? new List<string>();
            usedInteractables = usedInteractables ?? new List<string>();
            discoveredRooms = discoveredRooms ?? new List<string>();
            treasuresCollected = treasuresCollected ?? new List<int>();
            brokenVoxelRuns = brokenVoxelRuns ?? new List<int>();
            damagedVoxelIndices = damagedVoxelIndices ?? new List<int>();
            damagedVoxelAmounts = damagedVoxelAmounts ?? new List<float>();
            doubleCashRemaining = Math.Max(0f, Finite(doubleCashRemaining, 0f));
            doubleCashCooldownRemaining = Math.Max(0f, Finite(doubleCashCooldownRemaining, 0f));
            lookSensitivity = Math.Max(0.05f, Math.Min(0.45f, Finite(lookSensitivity, 0.16f)));
            effectsIntensity = Math.Max(0f, Math.Min(1f, Finite(effectsIntensity, 1f)));
            qualityOverride = Math.Max(-1, Math.Min(2, qualityOverride));
            if (hasPlayerPose && (!IsFinite(playerX) || !IsFinite(playerY) || !IsFinite(playerZ))) hasPlayerPose = false;
            tutorialStep = Math.Max(0, Math.Min(5, tutorialStep));
        }

        private static bool IsFinite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        private static float Finite(float f, float fallback) => IsFinite(f) ? f : fallback;

        private static int[] FixArray(int[] a, int length, int fill)
        {
            var r = new int[length];
            for (int i = 0; i < length; i++) r[i] = a != null && i < a.Length ? a[i] : fill;
            return r;
        }

        private static bool[] FixBools(bool[] a, int length)
        {
            var r = new bool[length];
            for (int i = 0; i < length; i++) r[i] = a != null && i < a.Length && a[i];
            return r;
        }
    }
}
