using System;

namespace BreakToEscape.Core.Economy
{
    /// <summary>Per-band result of the pacing model.</summary>
    public struct BandPacing
    {
        public int bandIndex;
        public int punchesToBreak;
        public double punchThroughput;
        public double vacuumThroughput;
        public double crusherThroughput;
        public double overallThroughput;
        public string bottleneck;
        public double crusherTrips;
        public double timeInBandMinutes;
        public double cumulativeMinutes;
        public double cashEarned;
        public double cashNeeded;
        public double earnedOverNeeded;
    }

    /// <summary>
    /// Re-implementation of the spreadsheet's Pacing sheet, driven by economy.json.
    /// The rates it assumes (punches per second, vacuum cubes per second, crusher cubes
    /// per second) are the same table values the game enforces with its limiters, so this
    /// is the model of the game as coded. Tests compare its output with the spreadsheet.
    /// </summary>
    public static class PacingModel
    {
        public static BandPacing[] Run(EconomyData data)
        {
            var result = new BandPacing[6];
            double cumulative = 0;
            var p = data.pacing;
            for (int b = 0; b < 6; b++)
            {
                BandDefinition band = data.bands[b];
                double dmg = Effect(data, UpgradeLine.PunchPower, p.plannedLevels[0][b]);
                double pps = Effect(data, UpgradeLine.PunchSpeed, p.plannedLevels[1][b]);
                double cubesPerPunch = Effect(data, UpgradeLine.PunchPattern, p.plannedLevels[2][b]);
                double vac = Effect(data, UpgradeLine.VacuumSuction, p.plannedLevels[3][b]);
                double tank = Effect(data, UpgradeLine.TankCapacity, p.plannedLevels[4][b]);
                double crusher = Effect(data, UpgradeLine.CrusherSpeed, p.plannedLevels[5][b]);
                double mult = Effect(data, UpgradeLine.OreSense, p.plannedLevels[6][b]);

                int ptb = (int)Math.Ceiling(band.hp / dmg - 1e-9);
                double punchTp = cubesPerPunch * pps * p.patternEfficiency / ptb;
                double overall = Math.Min(punchTp, Math.Min(vac, crusher));
                string bottleneck = overall == punchTp ? "Punching" : (overall == vac ? "Vacuum" : "Crusher");
                double trips = band.cubesOnPath / tank;
                double minutes = (band.cubesOnPath / overall + trips * p.tripSeconds + band.overheadSeconds) / 60.0;
                cumulative += minutes;
                double earned = band.cubesOnPath * band.value * mult * p.collectionEfficiency;
                double needed = 0;
                for (int line = 0; line < UpgradeLines.Count; line++)
                {
                    UpgradeDefinition def = data.upgrades[line];
                    int from = p.plannedLevels[line][b];
                    int to = p.plannedLevels[line][b + 1];
                    needed += def.cumulativeCost[to - 1] - def.cumulativeCost[from - 1];
                }

                result[b] = new BandPacing
                {
                    bandIndex = b,
                    punchesToBreak = ptb,
                    punchThroughput = punchTp,
                    vacuumThroughput = vac,
                    crusherThroughput = crusher,
                    overallThroughput = overall,
                    bottleneck = bottleneck,
                    crusherTrips = trips,
                    timeInBandMinutes = minutes,
                    cumulativeMinutes = cumulative,
                    cashEarned = earned,
                    cashNeeded = needed,
                    earnedOverNeeded = needed > 0 ? earned / needed : 0,
                };
            }
            return result;
        }

        public static double TotalMinutes(BandPacing[] bands) => bands[bands.Length - 1].cumulativeMinutes;

        /// <summary>
        /// Sum of the cumulative-cost column at level 10 for every line (the spreadsheet's
        /// "cash to max everything").
        /// </summary>
        public static double CashToMaxEverything(EconomyData data)
        {
            double total = 0;
            foreach (UpgradeDefinition def in data.upgrades) total += def.cumulativeCost[UpgradeLines.MaxLevel - 1];
            return total;
        }

        private static double Effect(EconomyData data, UpgradeLine line, int level) => data.Upgrade(line).EffectAt(level);
    }
}
