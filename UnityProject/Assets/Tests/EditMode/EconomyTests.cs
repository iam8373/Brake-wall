using System;
using BreakToEscape.Core.Economy;
using NUnit.Framework;

namespace BreakToEscape.Tests
{
    public class EconomyTests
    {
        // Cumulative cost to reach each level (levels 1..10), copied from docs/economy/ECONOMY_TABLES.md
        // (rounded to whole dollars there).
        private static readonly double[][] TableCumulative =
        {
            new double[] { 0, 150, 480, 1206, 2803, 6317, 14047, 31054, 68470, 150784 },
            new double[] { 0, 200, 640, 1608, 3738, 8423, 18730, 41406, 91293, 201045 },
            new double[] { 0, 250, 800, 2010, 4672, 10528, 23412, 51757, 114116, 251306 },
            new double[] { 0, 175, 560, 1407, 3270, 7370, 16389, 36230, 79881, 175914 },
            new double[] { 0, 125, 400, 1005, 2336, 5264, 11706, 25879, 57058, 125653 },
            new double[] { 0, 225, 720, 1809, 4205, 9476, 21071, 46582, 102705, 226175 },
            new double[] { 0, 375, 1200, 3015, 7008, 15793, 35119, 77636, 171175, 376959 },
        };

        // Effects per level from ECONOMY_TABLES.md (one decimal place there).
        private static readonly double[][] TableEffects =
        {
            new[] { 10, 14.5, 21, 30.5, 44.2, 64.1, 92.9, 134.8, 195.4, 283.3 },
            new[] { 1.5, 1.75, 2, 2.25, 2.5, 2.75, 3, 3.25, 3.5, 3.75 },
            new double[] { 5, 9, 13, 21, 29, 37, 49, 61, 77, 97 },
            new[] { 6, 7.8, 10.1, 13.2, 17.1, 22.3, 29, 37.6, 48.9, 63.6 },
            new double[] { 60, 90, 130, 190, 270, 380, 530, 740, 1030, 1450 },
            new[] { 8, 10.4, 13.5, 17.6, 22.8, 29.7, 38.6, 50.2, 65.3, 84.8 },
            new[] { 1, 1.08, 1.16, 1.24, 1.32, 1.4, 1.48, 1.56, 1.64, 1.72 },
        };

        [Test]
        public void CostFormulaMatchesSpreadsheetCumulativeColumn_AllLinesAllLevels()
        {
            EconomyData data = TestData.Economy;
            for (int line = 0; line < UpgradeLines.Count; line++)
            {
                UpgradeDefinition def = data.upgrades[line];
                double cumulative = 0;
                for (int level = 1; level <= 10; level++)
                {
                    Assert.AreEqual(def.cumulativeCost[level - 1], cumulative, 1e-6, def.displayName + " level " + level);
                    Assert.AreEqual(TableCumulative[line][level - 1], cumulative, 0.51, def.displayName + " level " + level + " vs ECONOMY_TABLES.md");
                    cumulative += def.ExactCostToNext(level);
                }
                Assert.AreEqual(0, def.ExactCostToNext(10), "no cost beyond level 10");
            }
        }

        [Test]
        public void EffectsMatchEconomyTables_AllLinesAllLevels()
        {
            EconomyData data = TestData.Economy;
            for (int line = 0; line < UpgradeLines.Count; line++)
                for (int level = 1; level <= 10; level++)
                    Assert.AreEqual(TableEffects[line][level - 1], data.upgrades[line].EffectAt(level), 0.051,
                        data.upgrades[line].displayName + " level " + level);
        }

        [Test]
        public void EffectsFollowTheSpreadsheetFormulas()
        {
            EconomyData data = TestData.Economy;
            for (int level = 1; level <= 10; level++)
            {
                Assert.AreEqual(10 * Math.Pow(1.45, level - 1), data.Upgrade(UpgradeLine.PunchPower).EffectAt(level), 1e-6);
                Assert.AreEqual(1.5 + 0.25 * (level - 1), data.Upgrade(UpgradeLine.PunchSpeed).EffectAt(level), 1e-9);
                Assert.AreEqual(6 * Math.Pow(1.3, level - 1), data.Upgrade(UpgradeLine.VacuumSuction).EffectAt(level), 1e-6);
                Assert.AreEqual(8 * Math.Pow(1.3, level - 1), data.Upgrade(UpgradeLine.CrusherSpeed).EffectAt(level), 1e-6);
                Assert.AreEqual(1 + 0.08 * (level - 1), data.Upgrade(UpgradeLine.OreSense).EffectAt(level), 1e-9);
            }
        }

        [Test]
        public void BaseCostsAndGrowthMatchDesign()
        {
            EconomyData data = TestData.Economy;
            double[] bases = { 150, 200, 250, 175, 125, 225, 375 };
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(bases[i], data.upgrades[i].baseCost);
                Assert.AreEqual(2.2, data.upgrades[i].costGrowth, 1e-12);
                Assert.AreEqual((long)bases[i], data.upgrades[i].PriceToNext(1));
            }
        }

        [Test]
        public void BandsMatchSpreadsheet()
        {
            EconomyData data = TestData.Economy;
            float[] hp = { 10, 25, 60, 140, 260, 420 };
            float[] value = { 2, 3.5f, 6, 10, 30, 70 };
            int[] cubes = { 850, 1500, 2300, 3500, 5000, 7000 };
            float[] overhead = { 45, 60, 70, 80, 90, 100 };
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i + 1, data.bands[i].id);
                Assert.AreEqual(hp[i], data.bands[i].hp);
                Assert.AreEqual(value[i], data.bands[i].value);
                Assert.AreEqual(cubes[i], data.bands[i].cubesOnPath);
                Assert.AreEqual(overhead[i], data.bands[i].overheadSeconds);
            }
        }

        [Test]
        public void PacingModelReproducesSpreadsheet()
        {
            EconomyData data = TestData.Economy;
            BandPacing[] bands = PacingModel.Run(data);
            double[] tableMinutes = { 7.2, 10.2, 12.7, 13.9, 15.4, 10.0 };
            int[] tablePunches = { 1, 2, 3, 4, 5, 4 };
            for (int b = 0; b < 6; b++)
            {
                Assert.AreEqual(data.pacing.expectedTimeInBandMinutes[b], bands[b].timeInBandMinutes, 1e-6, "time in band B" + (b + 1));
                Assert.AreEqual(tableMinutes[b], bands[b].timeInBandMinutes, 0.051, "ECONOMY_TABLES.md time B" + (b + 1));
                Assert.AreEqual(data.pacing.expectedCashEarnedInBand[b], bands[b].cashEarned, 1e-6, "cash earned B" + (b + 1));
                Assert.AreEqual(data.pacing.expectedCashNeeded[b], bands[b].cashNeeded, 1e-4, "cash needed B" + (b + 1));
                Assert.AreEqual(tablePunches[b], bands[b].punchesToBreak);
                Assert.AreEqual("Punching", bands[b].bottleneck);
            }
            Assert.AreEqual(69.4, PacingModel.TotalMinutes(bands), 0.05);
            Assert.AreEqual(data.pacing.expectedTotalRunMinutes, PacingModel.TotalMinutes(bands), 1e-6);
            Assert.AreEqual(data.pacing.expectedCashToMaxEverything, PacingModel.CashToMaxEverything(data), 1e-3);
            double total = 0;
            foreach (BandPacing b in bands) total += b.cashEarned;
            Assert.AreEqual(841008.6, total, 0.01);
        }

        [Test]
        public void EconomyStateBuysAndAppliesLevels()
        {
            var state = new EconomyState(TestData.Economy);
            Assert.AreEqual(PurchaseResult.NotEnoughCash, state.TryBuyUpgrade(UpgradeLine.TankCapacity));
            state.Earn(125);
            Assert.AreEqual(PurchaseResult.Success, state.TryBuyUpgrade(UpgradeLine.TankCapacity));
            Assert.AreEqual(0, state.Cash);
            Assert.AreEqual(2, state.Level(UpgradeLine.TankCapacity));
            Assert.AreEqual(90, state.TankCapacity);
            Assert.AreEqual(275, state.PriceToNext(UpgradeLine.TankCapacity));
            for (int i = 0; i < 20; i++) { state.Earn(1000000); state.TryBuyUpgrade(UpgradeLine.TankCapacity); }
            Assert.AreEqual(10, state.Level(UpgradeLine.TankCapacity));
            Assert.AreEqual(PurchaseResult.AlreadyMaxed, state.TryBuyUpgrade(UpgradeLine.TankCapacity));
        }

        [Test]
        public void EquipmentNeedsPartAndCash_GauntletsOnlyFromLocker()
        {
            var state = new EconomyState(TestData.Economy);
            Assert.IsFalse(state.IsRestored(EconomyState.WorkGauntlets));
            state.Earn(10000000);
            for (int i = 0; i < 9; i++) state.TryBuyUpgrade(UpgradeLine.PunchPower);
            Assert.IsFalse(state.IsRestored(EconomyState.WorkGauntlets), "no automatic grant from upgrade levels");
            Assert.AreEqual(PurchaseResult.Unavailable, state.TryRepair(EconomyState.WorkGauntlets));
            float before = state.PunchDamage;
            Assert.IsTrue(state.RestoreFromLocker(EconomyState.WorkGauntlets));
            Assert.AreEqual(before * 1.15f, state.PunchDamage, 1e-3);

            Assert.AreEqual(PurchaseResult.MissingPart, state.TryRepair(EconomyState.RockDrill));
            Assert.AreEqual(1, state.PunchDepthLayers);
            state.FindPart(EconomyState.RockDrill);
            Assert.AreEqual(PurchaseResult.Success, state.TryRepair(EconomyState.RockDrill));
            Assert.AreEqual(3, state.PunchDepthLayers);
            Assert.AreEqual(PurchaseResult.AlreadyOwned, state.TryRepair(EconomyState.RockDrill));

            var poor = new EconomyState(TestData.Economy);
            poor.FindPart(EconomyState.Conveyor);
            Assert.AreEqual(PurchaseResult.NotEnoughCash, poor.TryRepair(EconomyState.Conveyor));
            Assert.IsFalse(poor.ConveyorActive);
        }

        [Test]
        public void FirstUpgradeAffordableWellWithinTwoMinutes_ModelEstimate()
        {
            // Rule from the design: first cash and first upgrade within 2 minutes. Estimate with the
            // enforced level-1 rates: punch → vacuum → one trip → crush enough band-1 cubes for the
            // cheapest upgrade (Tank Capacity, $125).
            EconomyData data = TestData.Economy;
            double cheapest = double.MaxValue;
            foreach (UpgradeDefinition def in data.upgrades) cheapest = Math.Min(cheapest, def.PriceToNext(1));
            double cubes = Math.Ceiling(cheapest / data.bands[0].value);
            double punch = 5 * 1.5 * data.pacing.patternEfficiency;
            double seconds = cubes / punch + cubes / 6.0 + data.pacing.tripSeconds + cubes / 8.0;
            Assert.Less(seconds, 90.0, "estimated seconds to first upgrade");
        }
    }
}
