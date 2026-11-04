using System;
using System.Collections.Generic;

namespace BreakToEscape.Core.World
{
    /// <summary>
    /// Version 1 hand-built mine (Greywater Deep), following the design map:
    /// Collapse Chamber in the centre; Old Gallery to the west (band 2); Equipment Bay to the
    /// east (band 3); Pump Room to the south (band 4, with stashes into band 5); the Ore Veins
    /// store and the Crystal Grotto to the north (bands 5 and 6); the Main Shaft at the north end.
    /// Bands are laid out by direction from the chamber, so each route has a fixed difficulty.
    /// The world is 96 × 32 × 96 voxels of 0.5 m (the design's 100 × 50 × 100 is reduced for the
    /// prototype; see docs/DECISIONS.md).
    /// </summary>
    public static class HandBuiltMineGenerator
    {
        public const int Floor = 2;
        private const int TreasureSeed = 10492;

        public static MineLayout Generate(VoxelGrid grid, int treasureOneInVoxels, int nookClusterSize)
        {
            int cx = grid.sizeX / 2, cz = grid.sizeZ / 2;
            var layout = new MineLayout
            {
                worldSize = new Int3(grid.sizeX, grid.sizeY, grid.sizeZ),
                seed = TreasureSeed,
                isProcedural = false,
                chamberCenter = new Int3(cx, Floor, cz),
            };

            FillBands(grid, cx, cz);
            ScatterTreasure(grid, cx, cz, treasureOneInVoxels);

            // ---------------------------------------------------------------- rooms
            AddRoom(grid, layout, "collapse_chamber", "Collapse Chamber", "you are here", Box(cx - 7, cz - 7, cx + 7, cz + 7, 7), 1, false, true);
            AddRoom(grid, layout, "old_gallery", "Old Gallery", "dead end", Box(cx - 32, cz - 4, cx - 20, cz + 4, 5), 2, false, false);
            AddRoom(grid, layout, "equipment_bay", "Equipment Bay", "broken machines", Box(cx + 20, cz - 6, cx + 32, cz + 6, 6), 3, false, false);
            AddRoom(grid, layout, "pump_room", "Pump Room", "generator", Box(cx - 6, cz - 32, cx + 6, cz - 20, 6), 4, false, false);
            AddRoom(grid, layout, "ore_veins_store", "Ore Veins Store", "cable drum", Box(cx - 4, cz + 20, cx + 4, cz + 28, 4), 5, false, false);
            AddRoom(grid, layout, "crystal_grotto", "Crystal Grotto", "humming light", Box(cx - 6, cz + 32, cx + 6, cz + 38, 6), 6, false, false);
            AddRoom(grid, layout, "main_shaft", "Main Shaft", "locked - lift gate", Box(cx - 6, cz + 39, cx + 6, cz + 47, 10), 6, false, false);

            // Dead-end tunnels left by the old crews (pre-carved, narrow, lead nowhere).
            grid.CarveGenerated(Box(cx - 40, cz - 1, cx - 32, cz + 1, 2));
            grid.CarveGenerated(Box(cx + 32, cz + 2, cx + 40, cz + 4, 2));
            grid.CarveGenerated(Box(cx + 4, cz - 40, cx + 6, cz - 32, 2));

            // Hidden nooks: one or more per band, each with buried treasure in its walls.
            AddNook(grid, layout, "nook_b1", "Tool Cupboard", Box(cx - 11, cz - 2, cx - 8, cz + 1, 3), 1, nookClusterSize);
            AddNook(grid, layout, "nook_b2", "Timber Hollow", Box(cx - 28, cz + 8, cx - 25, cz + 11, 3), 2, nookClusterSize);
            AddNook(grid, layout, "nook_b3", "Rail Siding", Box(cx + 36, cz - 8, cx + 39, cz - 5, 3), 3, nookClusterSize);
            AddNook(grid, layout, "fuel_stash", "Fuel Stash", Box(cx + 8, cz - 28, cx + 11, cz - 25, 3), 4, nookClusterSize);
            AddNook(grid, layout, "nook_b4", "Granite Pocket", Box(cx - 12, cz - 26, cx - 9, cz - 23, 3), 4, nookClusterSize);
            AddNook(grid, layout, "drill_stash", "Drill Stash", Box(cx - 2, cz - 42, cx + 2, cz - 38, 3), 5, nookClusterSize);
            AddNook(grid, layout, "nook_b5", "Copper Crawl", Box(cx - 18, cz + 22, cx - 15, cz + 25, 3), 5, nookClusterSize);
            AddNook(grid, layout, "nook_b6", "Geode Bubble", Box(cx + 14, cz + 34, cx + 17, cz + 37, 3), 6, nookClusterSize);

            // ---------------------------------------------------------------- key points
            layout.spawnPoint = Cell(cx, Floor, cz - 4);
            layout.spawnLookDirection = new Float3(0f, 0f, 1f);
            layout.crusherPoint = Cell(cx, Floor, cz + 2);
            layout.liftGatePoint = Cell(cx, Floor, cz + 45);
            layout.liftGateFacing = new Int3(0, 0, -1);

            // ---------------------------------------------------------------- interactables
            Add(layout, "store_terminal", InteractableType.StoreTerminal, Cell(cx + 6, Floor, cz), "collapse_chamber", "");
            Add(layout, "note_chamber", InteractableType.WallNote, Cell(cx - 6, Floor, cz + 5), "collapse_chamber", "note_01");

            Add(layout, "quill_locker", InteractableType.CrewLocker, Cell(cx - 31, Floor, cz - 3), "old_gallery", "workGauntlets|quill");
            Add(layout, "hardhat_quill", InteractableType.HardHat, Cell(cx - 24, Floor, cz + 3), "old_gallery", "Quill");
            Add(layout, "note_gallery", InteractableType.WallNote, Cell(cx - 30, Floor, cz + 3), "old_gallery", "note_02");

            Add(layout, "logbook_pike", InteractableType.Logbook, Cell(cx + 22, Floor, cz - 5), "equipment_bay", "pike");
            Add(layout, "part_hammer", InteractableType.RepairPart, Cell(cx + 27, Floor, cz + 4), "equipment_bay", "pneumaticHammer");
            Add(layout, "part_conveyor", InteractableType.RepairPart, Cell(cx + 30, Floor, cz - 2), "equipment_bay", "conveyor");
            Add(layout, "hardhat_pike", InteractableType.HardHat, Cell(cx + 24, Floor, cz + 3), "equipment_bay", "Pike");
            Add(layout, "note_bay", InteractableType.WallNote, Cell(cx + 31, Floor, cz + 5), "equipment_bay", "note_03");

            Add(layout, "generator", InteractableType.Generator, Cell(cx, Floor, cz - 30), "pump_room", "");
            Add(layout, "logbook_bellamy", InteractableType.Logbook, Cell(cx - 4, Floor, cz - 22), "pump_room", "bellamy");
            Add(layout, "hardhat_bellamy", InteractableType.HardHat, Cell(cx + 4, Floor, cz - 23), "pump_room", "Bellamy");
            Add(layout, "note_pump", InteractableType.WallNote, Cell(cx + 5, Floor, cz - 31), "pump_room", "note_04");
            Add(layout, "fuel_cell", InteractableType.FuelCell, Cell(cx + 9, Floor, cz - 27), "fuel_stash", "");

            Add(layout, "part_drill", InteractableType.RepairPart, Cell(cx, Floor, cz - 40), "drill_stash", "rockDrill");
            Add(layout, "winch_cable", InteractableType.WinchCable, Cell(cx, Floor, cz + 24), "ore_veins_store", "");
            Add(layout, "note_veins", InteractableType.WallNote, Cell(cx - 3, Floor, cz + 27), "ore_veins_store", "note_05");
            Add(layout, "note_grotto", InteractableType.WallNote, Cell(cx + 5, Floor, cz + 33), "crystal_grotto", "note_06");
            Add(layout, "lift_gate", InteractableType.LiftGate, layout.liftGatePoint, "main_shaft", "");
            Add(layout, "note_shaft", InteractableType.WallNote, Cell(cx - 5, Floor, cz + 40), "main_shaft", "note_07");

            Add(layout, "crate_b1", InteractableType.DynamiteCrate, Cell(cx - 10, Floor, cz - 1), "nook_b1", "2");
            Add(layout, "note_b2", InteractableType.WallNote, Cell(cx - 27, Floor, cz + 9), "nook_b2", "note_08");
            Add(layout, "crate_b3", InteractableType.DynamiteCrate, Cell(cx + 37, Floor, cz - 7), "nook_b3", "3");
            Add(layout, "note_b4", InteractableType.WallNote, Cell(cx - 11, Floor, cz - 25), "nook_b4", "note_09");
            Add(layout, "crate_b5", InteractableType.DynamiteCrate, Cell(cx - 17, Floor, cz + 23), "nook_b5", "3");
            Add(layout, "note_b6", InteractableType.WallNote, Cell(cx + 15, Floor, cz + 35), "nook_b6", "note_10");

            // ---------------------------------------------------------------- showcase treasure (buried objects)
            AddTreasure(grid, layout, 1, "Shift Bell", 900, new Int3(cx - 19, Floor + 1, cz + 6));
            AddTreasure(grid, layout, 2, "Surveyor's Brass Level", 2400, new Int3(cx + 34, Floor + 1, cz + 7));
            AddTreasure(grid, layout, 3, "Founder's Medal", 7500, new Int3(cx - 8, Floor + 1, cz - 34));
            AddTreasure(grid, layout, 4, "Singing Geode", 30000, new Int3(cx + 8, Floor + 1, cz + 36));

            return layout;
        }

        /// <summary>Directional band for a column, by the dominant axis from the chamber centre.</summary>
        public static byte BandAt(int x, int z, int cx, int cz)
        {
            int dx = x - cx, dz = z - cz;
            int d = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (d <= 10) return 1;
            if (Math.Abs(dx) > Math.Abs(dz))
            {
                if (dx < 0) return 2;
                return (byte)(d <= 17 ? 2 : 3);
            }
            if (dz < 0) return (byte)(d <= 17 ? 3 : (d <= 31 ? 4 : 5));
            return (byte)(d <= 17 ? 4 : (d <= 29 ? 5 : 6));
        }

        private static void FillBands(VoxelGrid grid, int cx, int cz)
        {
            for (int x = 0; x < grid.sizeX; x++)
                for (int z = 0; z < grid.sizeZ; z++)
                {
                    byte band = BandAt(x, z, cx, cz);
                    for (int y = 0; y < grid.sizeY; y++) grid.SetGenerated(x, y, z, band);
                }
        }

        private static void ScatterTreasure(VoxelGrid grid, int cx, int cz, int oneIn)
        {
            if (oneIn <= 0) return;
            for (int x = 1; x < grid.sizeX - 1; x++)
                for (int z = 1; z < grid.sizeZ - 1; z++)
                {
                    int d = Math.Max(Math.Abs(x - cx), Math.Abs(z - cz));
                    if (d <= 8) continue;
                    for (int y = 1; y < grid.sizeY - 1; y++)
                    {
                        if (DeterministicRandom.Hash(x, y, z, TreasureSeed) % (uint)oneIn != 0) continue;
                        byte band = grid.Get(x, y, z);
                        byte treasure = grid.materials.TreasureForBand(band);
                        if (treasure != 0) grid.SetGenerated(x, y, z, treasure);
                    }
                }
        }

        private static VoxelBox Box(int minX, int minZ, int maxX, int maxZ, int height) =>
            new VoxelBox(minX, Floor, minZ, maxX, Floor + height, maxZ);

        private static Float3 Cell(int x, int y, int z) => new Float3(x + 0.5f, y, z + 0.5f);

        private static void AddRoom(VoxelGrid grid, MineLayout layout, string id, string title, string label, VoxelBox box, int band, bool nook, bool discovered)
        {
            grid.CarveGenerated(box);
            layout.rooms.Add(new RoomDefinition
            {
                id = id, title = title, mapLabel = label, bounds = box, band = band, isHiddenNook = nook, startsDiscovered = discovered
            });
        }

        private static void AddNook(VoxelGrid grid, MineLayout layout, string id, string title, VoxelBox box, int band, int clusterSize)
        {
            AddRoom(grid, layout, id, title, "?", box, band, true, false);
            byte treasure = grid.materials.TreasureForBand(band);
            if (treasure == 0 || clusterSize <= 0) return;
            // Treasure seam embedded in the north (+z) wall of the nook, visible once the nook is opened.
            int placed = 0;
            int wallZ = box.max.z;
            for (int y = box.min.y; y < box.max.y && placed < clusterSize; y++)
                for (int x = box.min.x; x < box.max.x && placed < clusterSize; x++)
                {
                    if (grid.IsBoundary(x, y, wallZ)) continue;
                    grid.SetGenerated(x, y, wallZ, treasure);
                    placed++;
                }
        }

        private static void Add(MineLayout layout, string id, InteractableType type, Float3 pos, string room, string payload)
        {
            layout.interactables.Add(new InteractableSpot { id = id, type = type, position = pos, roomId = room, payload = payload });
        }

        private static void AddTreasure(VoxelGrid grid, MineLayout layout, int id, string name, int value, Int3 voxel)
        {
            if (!grid.IsSolid(voxel.x, voxel.y, voxel.z)) throw new InvalidOperationException("Showcase treasure " + name + " must be buried in rock");
            layout.treasures.Add(new TreasureSpot { id = id, name = name, cashValue = value, voxel = voxel });
        }
    }
}
