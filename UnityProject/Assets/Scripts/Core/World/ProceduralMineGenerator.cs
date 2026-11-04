using System;
using System.Collections.Generic;

namespace BreakToEscape.Core.World
{
    /// <summary>
    /// "New Shift" seeded mine generator (experimental, behind VoxelWorldManager.useProceduralMine,
    /// default off). Produces the same <see cref="MineLayout"/> contract as the hand-built mine:
    /// chamber in the centre, Old Gallery, Equipment Bay, Pump Room, Ore Veins store and Main Shaft
    /// at increasing distances, hidden nooks, all interactables and the escape chain.
    /// Rooms are joined to the chamber by old tunnels, each blocked by a short cave-in plug of rock
    /// so the player still has to dig. Same seed → same mine.
    /// </summary>
    public static class ProceduralMineGenerator
    {
        public const int Floor = 2;
        private const int MaxAttempts = 200;

        private struct RoomSpec
        {
            public string id, title, label;
            public int minDist, maxDist, sizeX, sizeZ, height;
        }

        private static readonly RoomSpec[] Specs =
        {
            new RoomSpec { id = "old_gallery", title = "Old Gallery", label = "dead end", minDist = 17, maxDist = 21, sizeX = 10, sizeZ = 8, height = 5 },
            new RoomSpec { id = "equipment_bay", title = "Equipment Bay", label = "broken machines", minDist = 22, maxDist = 26, sizeX = 10, sizeZ = 10, height = 6 },
            new RoomSpec { id = "pump_room", title = "Pump Room", label = "generator", minDist = 27, maxDist = 31, sizeX = 10, sizeZ = 10, height = 6 },
            new RoomSpec { id = "ore_veins_store", title = "Ore Veins Store", label = "cable drum", minDist = 32, maxDist = 36, sizeX = 7, sizeZ = 7, height = 4 },
            new RoomSpec { id = "main_shaft", title = "Main Shaft", label = "locked - lift gate", minDist = 38, maxDist = 41, sizeX = 10, sizeZ = 8, height = 10 },
        };

        /// <summary>Concentric band rings by distance from the chamber (procedural layouts have no fixed directions).</summary>
        public static byte BandAt(int x, int z, int cx, int cz, int sizeX, int sizeZ)
        {
            int d = Math.Max(Math.Abs(x - cx), Math.Abs(z - cz));
            if (d <= 10) return 1;
            if (d <= 20) return 2;
            if (d <= 26) return 3;
            if (d <= 31) return 4;
            if (d <= 37) return 5;
            return 6;
        }

        public static MineLayout Generate(VoxelGrid grid, int seed, int treasureOneInVoxels, int nookClusterSize)
        {
            var rng = new DeterministicRandom(seed);
            int cx = grid.sizeX / 2, cz = grid.sizeZ / 2;
            var layout = new MineLayout
            {
                worldSize = new Int3(grid.sizeX, grid.sizeY, grid.sizeZ),
                seed = seed,
                isProcedural = true,
                chamberCenter = new Int3(cx, Floor, cz),
            };

            for (int x = 0; x < grid.sizeX; x++)
                for (int z = 0; z < grid.sizeZ; z++)
                {
                    byte band = BandAt(x, z, cx, cz, grid.sizeX, grid.sizeZ);
                    for (int y = 0; y < grid.sizeY; y++) grid.SetGenerated(x, y, z, band);
                }
            if (treasureOneInVoxels > 0)
            {
                for (int x = 1; x < grid.sizeX - 1; x++)
                    for (int z = 1; z < grid.sizeZ - 1; z++)
                        for (int y = 1; y < grid.sizeY - 1; y++)
                        {
                            if (Math.Max(Math.Abs(x - cx), Math.Abs(z - cz)) <= 8) continue;
                            if (DeterministicRandom.Hash(x, y, z, seed) % (uint)treasureOneInVoxels != 0) continue;
                            byte t = grid.materials.TreasureForBand(grid.Get(x, y, z));
                            if (t != 0) grid.SetGenerated(x, y, z, t);
                        }
            }

            VoxelBox chamber = new VoxelBox(cx - 7, Floor, cz - 7, cx + 7, Floor + 7, cz + 7);
            var placed = new List<VoxelBox> { chamber };
            AddRoom(grid, layout, "collapse_chamber", "Collapse Chamber", "you are here", chamber, 1, false, true);

            var roomBoxes = new Dictionary<string, VoxelBox>();
            foreach (RoomSpec spec in Specs)
            {
                VoxelBox box = PlaceRoom(grid, rng, cx, cz, spec, placed);
                placed.Add(box);
                roomBoxes[spec.id] = box;
                int band = BandAt(box.FloorCenter.x, box.FloorCenter.z, cx, cz, grid.sizeX, grid.sizeZ);
                AddRoom(grid, layout, spec.id, spec.title, spec.label, box, band, false, false);
                CarveTunnelWithPlug(grid, layout, chamber.FloorCenter, box, rng);
            }

            // Hidden nooks with treasure, notes and dynamite.
            string[] nookIds = { "nook_1", "nook_2", "nook_3", "fuel_stash", "drill_stash", "nook_4" };
            var nookBoxes = new Dictionary<string, VoxelBox>();
            for (int i = 0; i < nookIds.Length; i++)
            {
                var spec = new RoomSpec { id = nookIds[i], minDist = 12 + i * 4, maxDist = 16 + i * 4, sizeX = 3, sizeZ = 3, height = 3 };
                if (nookIds[i] == "fuel_stash") { spec.minDist = 24; spec.maxDist = 33; }
                VoxelBox box = PlaceRoom(grid, rng, cx, cz, spec, placed);
                placed.Add(box);
                nookBoxes[nookIds[i]] = box;
                int band = BandAt(box.FloorCenter.x, box.FloorCenter.z, cx, cz, grid.sizeX, grid.sizeZ);
                AddRoom(grid, layout, nookIds[i], "Hidden Nook", "?", box, band, true, false);
                byte treasure = grid.materials.TreasureForBand(band);
                int count = 0;
                for (int x = box.min.x; x < box.max.x && count < nookClusterSize; x++)
                    for (int y = box.min.y; y < box.max.y && count < nookClusterSize; y++)
                        if (treasure != 0 && !grid.IsBoundary(x, y, box.max.z) && grid.IsSolid(x, y, box.max.z)) { grid.SetGenerated(x, y, box.max.z, treasure); count++; }
            }

            // Key points.
            layout.spawnPoint = Cell(cx, cz - 4);
            layout.spawnLookDirection = new Float3(0f, 0f, 1f);
            layout.crusherPoint = Cell(cx, cz + 2);
            VoxelBox shaft = roomBoxes["main_shaft"];
            layout.liftGatePoint = Cell(shaft.FloorCenter.x, shaft.FloorCenter.z);
            layout.liftGateFacing = new Int3(0, 0, -1);

            Add(layout, "store_terminal", InteractableType.StoreTerminal, Cell(cx + 6, cz), "collapse_chamber", "");
            Add(layout, "note_chamber", InteractableType.WallNote, Cell(cx - 6, cz + 5), "collapse_chamber", "note_01");
            AddIn(layout, rng, "quill_locker", InteractableType.CrewLocker, roomBoxes["old_gallery"], "old_gallery", "workGauntlets|quill");
            AddIn(layout, rng, "hardhat_quill", InteractableType.HardHat, roomBoxes["old_gallery"], "old_gallery", "Quill");
            AddIn(layout, rng, "logbook_pike", InteractableType.Logbook, roomBoxes["equipment_bay"], "equipment_bay", "pike");
            AddIn(layout, rng, "part_hammer", InteractableType.RepairPart, roomBoxes["equipment_bay"], "equipment_bay", "pneumaticHammer");
            AddIn(layout, rng, "part_conveyor", InteractableType.RepairPart, roomBoxes["equipment_bay"], "equipment_bay", "conveyor");
            AddIn(layout, rng, "hardhat_pike", InteractableType.HardHat, roomBoxes["equipment_bay"], "equipment_bay", "Pike");
            AddIn(layout, rng, "generator", InteractableType.Generator, roomBoxes["pump_room"], "pump_room", "");
            AddIn(layout, rng, "logbook_bellamy", InteractableType.Logbook, roomBoxes["pump_room"], "pump_room", "bellamy");
            AddIn(layout, rng, "hardhat_bellamy", InteractableType.HardHat, roomBoxes["pump_room"], "pump_room", "Bellamy");
            AddIn(layout, rng, "fuel_cell", InteractableType.FuelCell, nookBoxes["fuel_stash"], "fuel_stash", "");
            AddIn(layout, rng, "part_drill", InteractableType.RepairPart, nookBoxes["drill_stash"], "drill_stash", "rockDrill");
            AddIn(layout, rng, "winch_cable", InteractableType.WinchCable, roomBoxes["ore_veins_store"], "ore_veins_store", "");
            Add(layout, "lift_gate", InteractableType.LiftGate, layout.liftGatePoint, "main_shaft", "");
            AddIn(layout, rng, "crate_1", InteractableType.DynamiteCrate, nookBoxes["nook_1"], "nook_1", "2");
            AddIn(layout, rng, "crate_2", InteractableType.DynamiteCrate, nookBoxes["nook_3"], "nook_3", "3");
            string[] noteRooms = { "old_gallery", "equipment_bay", "pump_room", "ore_veins_store", "main_shaft", "nook_2", "nook_4" };
            for (int i = 0; i < noteRooms.Length; i++)
            {
                VoxelBox box = roomBoxes.ContainsKey(noteRooms[i]) ? roomBoxes[noteRooms[i]] : nookBoxes[noteRooms[i]];
                AddIn(layout, rng, "note_" + (i + 2), InteractableType.WallNote, box, noteRooms[i], "note_" + (i + 2).ToString("00"));
            }

            // One named treasure buried next to each of three rooms.
            string[] treasureRooms = { "old_gallery", "equipment_bay", "ore_veins_store" };
            string[] names = { "Shift Bell", "Surveyor's Brass Level", "Singing Geode" };
            int[] values = { 900, 2400, 30000 };
            for (int i = 0; i < treasureRooms.Length; i++)
            {
                VoxelBox b = roomBoxes[treasureRooms[i]];
                var voxel = new Int3(b.max.x, Floor + 1, b.min.z + 1);
                if (!grid.IsSolid(voxel.x, voxel.y, voxel.z) || grid.IsBoundary(voxel.x, voxel.y, voxel.z)) voxel = new Int3(b.min.x - 1, Floor + 1, b.min.z + 1);
                if (grid.IsSolid(voxel.x, voxel.y, voxel.z) && !grid.IsBoundary(voxel.x, voxel.y, voxel.z))
                    layout.treasures.Add(new TreasureSpot { id = i + 1, name = names[i], cashValue = values[i], voxel = voxel });
            }
            return layout;
        }

        private static VoxelBox PlaceRoom(VoxelGrid grid, DeterministicRandom rng, int cx, int cz, RoomSpec spec, List<VoxelBox> placed)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                double angle = rng.NextFloat() * Math.PI * 2.0;
                int dist = rng.Range(spec.minDist, spec.maxDist + 1);
                int mx = cx + (int)Math.Round(Math.Cos(angle) * dist);
                int mz = cz + (int)Math.Round(Math.Sin(angle) * dist);
                var box = new VoxelBox(mx - spec.sizeX / 2, Floor, mz - spec.sizeZ / 2, mx - spec.sizeX / 2 + spec.sizeX, Floor + spec.height, mz - spec.sizeZ / 2 + spec.sizeZ);
                if (box.min.x < 2 || box.min.z < 2 || box.max.x > grid.sizeX - 2 || box.max.z > grid.sizeZ - 2) continue;
                bool clear = true;
                foreach (VoxelBox other in placed) if (box.Expanded(2).Intersects(other)) { clear = false; break; }
                if (clear) return box;
            }
            throw new InvalidOperationException("Procedural mine: could not place room " + spec.id + " (seed too crowded)");
        }

        /// <summary>L-shaped 2 × 2 tunnel from the chamber floor to a room, with a 3-voxel cave-in plug near the chamber.</summary>
        private static void CarveTunnelWithPlug(VoxelGrid grid, MineLayout layout, Int3 from, VoxelBox room, DeterministicRandom rng)
        {
            Int3 to = room.FloorCenter;
            bool xFirst = rng.Range(0, 2) == 0;
            Int3 corner = xFirst ? new Int3(to.x, Floor, from.z) : new Int3(from.x, Floor, to.z);
            CarveLine(grid, from, corner);
            CarveLine(grid, corner, to);

            // The plug sits where the tunnel leaves the chamber shell (8 to 10 voxels out).
            // The first leg leaves the chamber along x when xFirst (and it actually moves in x), otherwise along z.
            bool firstLegX = xFirst ? to.x != from.x : to.z == from.z;
            Int3 dir = firstLegX ? new Int3(Math.Sign(to.x - from.x), 0, 0) : new Int3(0, 0, Math.Sign(to.z - from.z));
            Int3 a = from + dir * 8, b = from + dir * 10;
            VoxelBox plug = dir.x != 0
                ? new VoxelBox(Math.Min(a.x, b.x), Floor, from.z, Math.Max(a.x, b.x) + 1, Floor + 2, from.z + 2)
                : new VoxelBox(from.x, Floor, Math.Min(a.z, b.z), from.x + 2, Floor + 2, Math.Max(a.z, b.z) + 1);
            for (int x = plug.min.x; x < plug.max.x; x++)
                for (int y = plug.min.y; y < plug.max.y; y++)
                    for (int z = plug.min.z; z < plug.max.z; z++)
                        if (!grid.IsSolid(x, y, z)) grid.SetGenerated(x, y, z, BandAt(x, z, from.x, from.z, grid.sizeX, grid.sizeZ));
            layout.collapsedPlugs.Add(plug);
        }

        private static void CarveLine(VoxelGrid grid, Int3 a, Int3 b)
        {
            int minX = Math.Min(a.x, b.x), maxX = Math.Max(a.x, b.x);
            int minZ = Math.Min(a.z, b.z), maxZ = Math.Max(a.z, b.z);
            grid.CarveGenerated(new VoxelBox(minX, Floor, minZ, maxX + 2, Floor + 2, maxZ + 2));
        }

        private static Float3 Cell(int x, int z) => new Float3(x + 0.5f, Floor, z + 0.5f);

        private static void AddRoom(VoxelGrid grid, MineLayout layout, string id, string title, string label, VoxelBox box, int band, bool nook, bool discovered)
        {
            grid.CarveGenerated(box);
            layout.rooms.Add(new RoomDefinition { id = id, title = title, mapLabel = label, bounds = box, band = band, isHiddenNook = nook, startsDiscovered = discovered });
        }

        private static void Add(MineLayout layout, string id, InteractableType type, Float3 pos, string room, string payload)
        {
            layout.interactables.Add(new InteractableSpot { id = id, type = type, position = pos, roomId = room, payload = payload });
        }

        /// <summary>Places an interactable on a free floor cell inside a room (inset one cell from the walls).</summary>
        private static void AddIn(MineLayout layout, DeterministicRandom rng, string id, InteractableType type, VoxelBox box, string room, string payload)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = rng.Range(box.min.x + 1, Math.Max(box.min.x + 2, box.max.x - 1));
                int z = rng.Range(box.min.z + 1, Math.Max(box.min.z + 2, box.max.z - 1));
                var pos = new Float3(x + 0.5f, Floor, z + 0.5f);
                bool taken = false;
                foreach (InteractableSpot s in layout.interactables)
                    if ((int)s.position.x == x && (int)s.position.z == z) { taken = true; break; }
                if (taken) continue;
                Add(layout, id, type, pos, room, payload);
                return;
            }
            Add(layout, id, type, new Float3(box.FloorCenter.x + 0.5f, Floor, box.FloorCenter.z + 0.5f), room, payload);
        }

        /// <summary>
        /// Breadth-first search over air (and cave-in plugs) on the floor layer pair from the spawn.
        /// Returns the ids of rooms whose floor centre is reachable. Used by tests to prove every room
        /// can be reached by walking and digging only the plugs.
        /// </summary>
        public static HashSet<string> ReachableRooms(VoxelGrid grid, MineLayout layout)
        {
            var visited = new bool[grid.sizeX, grid.sizeZ];
            var queue = new Queue<Int3>();
            Int3 start = MineLayoutValidator.Floor(layout.spawnPoint);
            queue.Enqueue(start);
            visited[start.x, start.z] = true;
            while (queue.Count > 0)
            {
                Int3 p = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + (k == 0 ? 1 : k == 1 ? -1 : 0);
                    int nz = p.z + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx < 1 || nz < 1 || nx >= grid.sizeX - 1 || nz >= grid.sizeZ - 1 || visited[nx, nz]) continue;
                    if (!Passable(grid, layout, nx, Floor, nz) || !Passable(grid, layout, nx, Floor + 1, nz)) continue;
                    visited[nx, nz] = true;
                    queue.Enqueue(new Int3(nx, Floor, nz));
                }
            }
            var reachable = new HashSet<string>();
            foreach (RoomDefinition r in layout.rooms)
            {
                Int3 c = r.bounds.FloorCenter;
                if (visited[c.x, c.z]) reachable.Add(r.id);
            }
            return reachable;
        }

        private static bool Passable(VoxelGrid grid, MineLayout layout, int x, int y, int z)
        {
            if (!grid.IsSolid(x, y, z)) return true;
            foreach (VoxelBox plug in layout.collapsedPlugs) if (plug.Contains(new Int3(x, y, z))) return true;
            return false;
        }
    }
}
