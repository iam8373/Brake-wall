using System;
using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.World;
using NUnit.Framework;

namespace BreakToEscape.Tests
{
    public class PunchPatternTests
    {
        [Test]
        public void EachLevelHasTheDesignedCubeCount()
        {
            int[] expected = { 5, 9, 13, 21, 29, 37, 49, 61, 77, 97 };
            for (int level = 1; level <= 10; level++)
                Assert.AreEqual(expected[level - 1], PunchPatterns.Cells(level).Length, "level " + level);
        }

        [Test]
        public void PatternCountsMatchTheEconomyData()
        {
            var data = TestData.Economy;
            for (int level = 1; level <= 10; level++)
                Assert.AreEqual((int)data.Upgrade(Core.Economy.UpgradeLine.PunchPattern).EffectAt(level), PunchPatterns.Cells(level).Length);
        }

        [Test]
        public void ShapesAreSymmetricAndUnique()
        {
            for (int level = 1; level <= 10; level++)
            {
                var set = new HashSet<Int2>(PunchPatterns.Cells(level));
                Assert.AreEqual(PunchPatterns.Cells(level).Length, set.Count, "duplicates at level " + level);
                foreach (Int2 c in set)
                {
                    Assert.IsTrue(set.Contains(new Int2(-c.x, c.y)), "mirror x at level " + level);
                    Assert.IsTrue(set.Contains(new Int2(c.x, -c.y)), "mirror y at level " + level);
                    Assert.IsTrue(set.Contains(new Int2(c.y, c.x)), "diagonal symmetry at level " + level);
                }
            }
        }

        [Test]
        public void FirstCellIsTheAimedCube_AndEveryShapeIncludesThePlusSign()
        {
            // Note: the designed shapes are not strictly nested (the level 8 diamond does not contain
            // the corners of the level 7 square); only the counts and symmetry are design rules.
            var plus = new HashSet<Int2>(PunchPatterns.Cells(1));
            for (int level = 1; level <= 10; level++)
            {
                Assert.AreEqual(new Int2(0, 0), PunchPatterns.Cells(level)[0]);
                Assert.IsTrue(plus.IsSubsetOf(new HashSet<Int2>(PunchPatterns.Cells(level))), "level " + level);
            }
        }

        [Test]
        public void ChargedLevelAddsUpToTwoAndCapsAtTen()
        {
            Assert.AreEqual(1, PunchPatterns.ChargedLevel(1, 0f, 2));
            Assert.AreEqual(1, PunchPatterns.ChargedLevel(1, 0.49f, 2));
            Assert.AreEqual(2, PunchPatterns.ChargedLevel(1, 0.5f, 2));
            Assert.AreEqual(3, PunchPatterns.ChargedLevel(1, 1f, 2));
            Assert.AreEqual(10, PunchPatterns.ChargedLevel(9, 1f, 2));
            Assert.AreEqual(10, PunchPatterns.ChargedLevel(10, 1f, 2));
            Assert.AreEqual(10, PunchPatterns.ChargedLevel(10, 5f, 2));
            Assert.AreEqual(1f, PunchPatterns.ChargedDamageMultiplier(0f, 2f), 1e-6);
            Assert.AreEqual(1.5f, PunchPatterns.ChargedDamageMultiplier(0.5f, 2f), 1e-6);
            Assert.AreEqual(2f, PunchPatterns.ChargedDamageMultiplier(1f, 2f), 1e-6);
        }

        [Test]
        public void NormalPunchBreaksTheWholePatternOnAFlatWall()
        {
            for (int level = 1; level <= 10; level++)
            {
                VoxelGrid grid = TestData.SmallGrid(32, 1);
                for (int z = 0; z < 16; z++)
                    for (int x = 1; x < 31; x++)
                        for (int y = 1; y < 31; y++)
                            grid.SetGenerated(x, y, z, 0);
                var broken = new List<BrokenVoxel>();
                PunchResolver.ApplyPunch(grid, new Int3(16, 16, 16), new Int3(0, 0, -1), level, 1, 10f, new List<Int3>(), broken);
                Assert.AreEqual(PunchPatterns.DesignedCounts[level - 1], broken.Count, "level " + level);
            }
        }

        [Test]
        public void RockDrillDamagesEveryDepthLayerIndependently()
        {
            VoxelGrid grid = TestData.SmallGrid(32, 1);
            var broken = new List<BrokenVoxel>();
            PunchResolver.ApplyPunch(grid, new Int3(16, 16, 10), new Int3(0, 0, -1), 1, 3, 10f, new List<Int3>(), broken);
            Assert.AreEqual(15, broken.Count, "5 cells x 3 layers");
            Assert.IsFalse(grid.IsSolid(16, 16, 10));
            Assert.IsFalse(grid.IsSolid(16, 16, 11));
            Assert.IsFalse(grid.IsSolid(16, 16, 12));
            Assert.IsTrue(grid.IsSolid(16, 16, 13));
        }
    }

    public class VoxelGridTests
    {
        [Test]
        public void DamageAccumulatesUntilTheBreakThreshold()
        {
            VoxelGrid grid = TestData.SmallGrid(16, 2); // band 2, 25 HP
            Assert.AreEqual(DamageOutcome.Damaged, grid.ApplyDamage(5, 5, 5, 10f, out _));
            Assert.AreEqual(DamageOutcome.Damaged, grid.ApplyDamage(5, 5, 5, 10f, out _));
            Assert.AreEqual(20f, grid.DamageAt(5, 5, 5), 1e-4);
            Assert.AreEqual(DamageOutcome.Broken, grid.ApplyDamage(5, 5, 5, 5f, out byte mat));
            Assert.AreEqual(2, mat);
            Assert.AreEqual(0, grid.Get(5, 5, 5));
            Assert.AreEqual(1, grid.BrokenCount);
            Assert.AreEqual(DamageOutcome.Ignored, grid.ApplyDamage(5, 5, 5, 100f, out _), "air takes no damage");
        }

        [Test]
        public void BoundaryShellIsUnbreakable()
        {
            VoxelGrid grid = TestData.SmallGrid(16, 1);
            Assert.AreEqual(DamageOutcome.Ignored, grid.ApplyDamage(0, 5, 5, 1000f, out _));
            Assert.AreEqual(DamageOutcome.Ignored, grid.ApplyDamage(15, 5, 5, 1000f, out _));
            Assert.AreEqual(DamageOutcome.Ignored, grid.ApplyDamage(5, 0, 5, 1000f, out _));
            Assert.AreEqual(DamageOutcome.Ignored, grid.ApplyDamage(5, 5, 15, 1000f, out _));
            Assert.IsTrue(grid.IsSolid(-1, 5, 5), "outside the grid counts as solid");
        }

        [Test]
        public void BreakingOnAChunkEdgeDirtiesTheNeighbourChunk()
        {
            VoxelGrid grid = TestData.SmallGrid(32, 1);
            var dirty = new List<int>();
            grid.ApplyDamage(15, 5, 5, 100f, out _);
            grid.DrainDirtyChunks(dirty);
            Assert.That(dirty, Does.Contain(grid.ChunkIndex(0, 0, 0)));
            Assert.That(dirty, Does.Contain(grid.ChunkIndex(1, 0, 0)));
            Assert.AreEqual(2, dirty.Count);

            dirty.Clear();
            grid.ApplyDamage(8, 8, 8, 100f, out _);
            grid.DrainDirtyChunks(dirty);
            Assert.AreEqual(1, dirty.Count, "interior voxel dirties only its own chunk");
            Assert.IsFalse(grid.HasDirtyChunks);
        }

        [Test]
        public void EditsAndPartialDamageAreRecordedAndRestoredOnAFreshGrid()
        {
            VoxelGrid grid = TestData.SmallGrid(16, 3); // 60 HP
            grid.ApplyDamage(4, 4, 4, 100f, out _);
            grid.ApplyDamage(4, 4, 5, 100f, out _);
            grid.ApplyDamage(6, 6, 6, 30f, out _);

            var idx = new List<int>();
            var ids = new List<byte>();
            grid.GetEdits(idx, ids);
            var dIdx = new List<int>();
            var dAmt = new List<float>();
            grid.GetPartialDamage(dIdx, dAmt);
            Assert.AreEqual(2, idx.Count);
            Assert.AreEqual(1, dIdx.Count);

            VoxelGrid fresh = TestData.SmallGrid(16, 3);
            Assert.AreEqual(2, fresh.ApplySavedEdits(idx, ids, grid.BrokenCount));
            Assert.AreEqual(1, fresh.ApplySavedDamage(dIdx, dAmt));
            Assert.IsFalse(fresh.IsSolid(4, 4, 4));
            Assert.IsFalse(fresh.IsSolid(4, 4, 5));
            Assert.AreEqual(30f, fresh.DamageAt(6, 6, 6), 1e-4);
            Assert.AreEqual(2, fresh.BrokenCount);
            Assert.AreEqual(DamageOutcome.Broken, fresh.ApplyDamage(6, 6, 6, 30f, out _), "restored damage counts toward breaking");
        }

        [Test]
        public void SavedEditsInTheShellOrOutOfRangeAreIgnored()
        {
            VoxelGrid grid = TestData.SmallGrid(16, 1);
            int applied = grid.ApplySavedEdits(new List<int> { -5, 999999, grid.Index(0, 3, 3), grid.Index(3, 3, 3) }, new List<byte> { 0, 0, 0, 0 }, 0);
            Assert.AreEqual(1, applied);
            Assert.IsTrue(grid.IsSolid(0, 3, 3));
        }

        [Test]
        public void FlatIndexRoundTrips()
        {
            VoxelGrid grid = TestData.NewGrid();
            foreach (var p in new[] { new Int3(0, 0, 0), new Int3(95, 31, 95), new Int3(12, 7, 80) })
                Assert.AreEqual(p, grid.FromIndex(grid.Index(p.x, p.y, p.z)));
            Assert.AreEqual(5 * 32 * 96 + 3 * 96 + 7, grid.Index(5, 3, 7), "same flat layout as the first save format");
        }
    }

    public class GridRaycastTests
    {
        private static VoxelGrid EmptyWithWallAtZ(int wallZ)
        {
            VoxelGrid grid = TestData.SmallGrid(32, 0);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    grid.SetGenerated(x, y, wallZ, 1);
            return grid;
        }

        [Test]
        public void AxisAlignedRayHitsTheWallFaceWithCorrectNormalAndDistance()
        {
            VoxelGrid grid = EmptyWithWallAtZ(20);
            GridHit hit = GridRaycast.Cast(grid, new Float3(10.5f, 10.5f, 10.5f), new Float3(0, 0, 1), 50f);
            Assert.IsTrue(hit.hit);
            Assert.AreEqual(new Int3(10, 10, 20), hit.voxel);
            Assert.AreEqual(new Int3(0, 0, -1), hit.normal);
            Assert.AreEqual(9.5f, hit.distance, 1e-4);
            Assert.IsFalse(hit.startedInside);
        }

        [Test]
        public void RayMissesBeyondMaxDistance()
        {
            VoxelGrid grid = EmptyWithWallAtZ(20);
            GridHit hit = GridRaycast.Cast(grid, new Float3(10.5f, 10.5f, 10.5f), new Float3(0, 0, 1), 5f);
            Assert.IsFalse(hit.hit);
        }

        [Test]
        public void NegativeDirectionsAndOtherAxes()
        {
            VoxelGrid grid = TestData.SmallGrid(32, 0);
            grid.SetGenerated(3, 10, 10, 1);
            GridHit hit = GridRaycast.Cast(grid, new Float3(10.2f, 10.5f, 10.5f), new Float3(-1, 0, 0), 50f);
            Assert.IsTrue(hit.hit);
            Assert.AreEqual(new Int3(3, 10, 10), hit.voxel);
            Assert.AreEqual(new Int3(1, 0, 0), hit.normal);
            Assert.AreEqual(6.2f, hit.distance, 1e-4);

            grid.SetGenerated(10, 2, 10, 1);
            GridHit down = GridRaycast.Cast(grid, new Float3(10.5f, 10.5f, 10.5f), new Float3(0, -1, 0), 50f);
            Assert.AreEqual(new Int3(10, 2, 10), down.voxel);
            Assert.AreEqual(new Int3(0, 1, 0), down.normal);
        }

        [Test]
        public void DiagonalRayVisitsCellsInOrder()
        {
            VoxelGrid grid = TestData.SmallGrid(32, 0);
            grid.SetGenerated(14, 14, 10, 1);
            GridHit hit = GridRaycast.Cast(grid, new Float3(10.5f, 10.5f, 10.5f), new Float3(1, 1, 0), 50f);
            Assert.IsTrue(hit.hit);
            Assert.AreEqual(new Int3(14, 14, 10), hit.voxel);
            Assert.AreEqual(Math.Sqrt(2) * 3.5, hit.distance, 1e-3);
            Assert.IsTrue(hit.normal == new Int3(-1, 0, 0) || hit.normal == new Int3(0, -1, 0));
        }

        [Test]
        public void StartingInsideSolidReportsTheStartVoxel()
        {
            VoxelGrid grid = TestData.SmallGrid(32, 1);
            GridHit hit = GridRaycast.Cast(grid, new Float3(5.5f, 5.5f, 5.5f), new Float3(0, 0, 1), 10f);
            Assert.IsTrue(hit.hit);
            Assert.IsTrue(hit.startedInside);
            Assert.AreEqual(new Int3(5, 5, 5), hit.voxel);
            Assert.AreEqual(0f, hit.distance);
            Assert.AreEqual(new Int3(0, 0, -1), hit.normal);
        }

        [Test]
        public void RayStartingOnACellBoundaryAndZeroDirection()
        {
            VoxelGrid grid = EmptyWithWallAtZ(20);
            GridHit hit = GridRaycast.Cast(grid, new Float3(10f, 10f, 19f), new Float3(0, 0, 1), 5f);
            Assert.IsTrue(hit.hit);
            Assert.AreEqual(20, hit.voxel.z);
            Assert.AreEqual(1f, hit.distance, 1e-4);
            Assert.IsFalse(GridRaycast.Cast(grid, new Float3(10, 10, 10), new Float3(0, 0, 0), 5f).hit);
        }

        [Test]
        public void AimAssistFindsANearbyCubeWhenTheCentreRayMisses()
        {
            VoxelGrid grid = TestData.SmallGrid(32, 0);
            grid.SetGenerated(11, 10, 14, 1);
            var origin = new Float3(10.5f, 10.5f, 10.5f);
            Assert.IsFalse(GridRaycast.Cast(grid, origin, new Float3(0, 0, 1), 6f).hit);
            GridHit assisted = GridRaycast.CastWithAssist(grid, origin, new Float3(0, 0, 1), new Float3(1, 0, 0), new Float3(0, 1, 0), 6f, 0.25f, 8);
            Assert.IsTrue(assisted.hit);
            Assert.AreEqual(new Int3(11, 10, 14), assisted.voxel);
        }
    }

    public class MineLayoutTests
    {
        [Test]
        public void HandBuiltMineSatisfiesTheLayoutContract()
        {
            VoxelGrid grid = TestData.NewHandBuiltMine(out MineLayout layout);
            List<string> problems = MineLayoutValidator.Validate(grid, layout);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void RoomsAreInTheDesignedDirectionsAndBands()
        {
            VoxelGrid grid = TestData.NewHandBuiltMine(out MineLayout layout);
            Int3 c = layout.chamberCenter;
            Assert.Less(layout.Room("old_gallery").bounds.Center.x, c.x, "Old Gallery is west");
            Assert.Greater(layout.Room("equipment_bay").bounds.Center.x, c.x, "Equipment Bay is east");
            Assert.Less(layout.Room("pump_room").bounds.Center.z, c.z, "Pump Room is south");
            Assert.Greater(layout.Room("main_shaft").bounds.Center.z, c.z, "Main Shaft is north");
            Assert.AreEqual(2, HandBuiltMineGenerator.BandAt(22, 48, c.x, c.z));
            Assert.AreEqual(3, HandBuiltMineGenerator.BandAt(74, 48, c.x, c.z));
            Assert.AreEqual(4, HandBuiltMineGenerator.BandAt(48, 22, c.x, c.z));
            Assert.AreEqual(5, HandBuiltMineGenerator.BandAt(48, 72, c.x, c.z));
            Assert.AreEqual(6, HandBuiltMineGenerator.BandAt(48, 90, c.x, c.z));
            Assert.AreEqual(1, HandBuiltMineGenerator.BandAt(50, 50, c.x, c.z));
            // Every band 1..6 is present in the solid rock.
            var seen = new HashSet<byte>();
            for (int x = 1; x < grid.sizeX - 1; x++)
                for (int z = 1; z < grid.sizeZ - 1; z++)
                {
                    byte id = grid.Get(x, 1, z);
                    if (id >= 1 && id <= 6) seen.Add(id);
                }
            Assert.AreEqual(6, seen.Count);
        }

        [Test]
        public void GenerationIsDeterministic()
        {
            VoxelGrid a = TestData.NewHandBuiltMine(out _);
            VoxelGrid b = TestData.NewHandBuiltMine(out _);
            Assert.AreEqual(a.RawIds, b.RawIds);
        }

        [Test]
        public void TreasureModel_ReportsExtraCashNotInTheSpreadsheet()
        {
            // The spreadsheet does not model treasure. This counts treasure voxels and named
            // treasures so their payout can be compared with band earnings (see docs/DECISIONS.md).
            VoxelGrid grid = TestData.NewHandBuiltMine(out MineLayout layout);
            double voxelTreasure = 0;
            int voxelCount = 0;
            for (int i = 0; i < grid.VoxelCount; i++)
            {
                byte id = grid.RawIds[i];
                if (id >= 7 && id <= 9) { voxelCount++; voxelTreasure += grid.materials.Get(id).value; }
            }
            double named = 0;
            foreach (TreasureSpot t in layout.treasures) named += t.cashValue;
            double spreadsheetRunCash = TestData.Economy.pacing.expectedTotalCashEarned;
            TestContext.WriteLine("Treasure voxels in the whole mine: " + voxelCount + ", total value $" + voxelTreasure);
            TestContext.WriteLine("Named treasures: $" + named + "; spreadsheet run cash $" + spreadsheetRunCash);
            Assert.Greater(voxelCount, 0);
            // A player only digs a fraction of the mine; even if every treasure in the whole world were
            // collected it must stay well below the modelled run income so the pacing still holds.
            Assert.Less(voxelTreasure + named, spreadsheetRunCash * 0.5);
        }
    }
}
