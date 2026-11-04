using System;
using System.Collections.Generic;

namespace BreakToEscape.Core.World
{
    public enum InteractableType
    {
        StoreTerminal,
        CrewLocker,
        Logbook,
        HardHat,
        WallNote,
        RepairPart,
        FuelCell,
        Generator,
        WinchCable,
        DynamiteCrate,
        LiftGate
    }

    /// <summary>A room or nook on the map, in voxel coordinates (max exclusive).</summary>
    [Serializable]
    public sealed class RoomDefinition
    {
        public string id;
        public string title;
        /// <summary>Short map label shown once discovered (for example "dead end").</summary>
        public string mapLabel;
        public VoxelBox bounds;
        public int band;
        public bool isHiddenNook;
        public bool startsDiscovered;
    }

    /// <summary>An interactable object placed by the generator. Positions are in voxel units, on the floor.</summary>
    [Serializable]
    public sealed class InteractableSpot
    {
        public string id;
        public InteractableType type;
        /// <summary>Floor point in voxel units (x/z cell centre, y = floor height).</summary>
        public Float3 position;
        public string roomId;
        /// <summary>Type-specific data: logbook id, note id, equipment key, crew name, sticks.</summary>
        public string payload;
    }

    /// <summary>A named showcase treasure buried inside a rock voxel.</summary>
    [Serializable]
    public sealed class TreasureSpot
    {
        public int id;
        public string name;
        public int cashValue;
        public Int3 voxel;
    }

    /// <summary>
    /// The layout contract every mine generator must produce. All placement in the scene
    /// (player, crusher, store, lift gate, rooms, interactables, treasure) is computed from
    /// this object after the world is generated; nothing in the scene builder is hard-coded.
    /// All coordinates are voxel units; multiply by the voxel size and add the world origin
    /// to get metres.
    /// </summary>
    public sealed class MineLayout
    {
        public Int3 worldSize;
        public int seed;
        public bool isProcedural;

        public Int3 chamberCenter;
        /// <summary>Player feet position on the chamber floor.</summary>
        public Float3 spawnPoint;
        /// <summary>Horizontal direction the player faces at spawn (towards a wall, crusher in front).</summary>
        public Float3 spawnLookDirection;
        /// <summary>Crusher 07 floor hatch centre.</summary>
        public Float3 crusherPoint;
        /// <summary>Main Shaft lift gate floor centre, and the direction its front faces.</summary>
        public Float3 liftGatePoint;
        public Int3 liftGateFacing;

        public readonly List<RoomDefinition> rooms = new List<RoomDefinition>();
        public readonly List<InteractableSpot> interactables = new List<InteractableSpot>();
        public readonly List<TreasureSpot> treasures = new List<TreasureSpot>();
        /// <summary>Procedural mines only: cave-in plugs of rock that block the old tunnels (dig through them).</summary>
        public readonly List<VoxelBox> collapsedPlugs = new List<VoxelBox>();

        public RoomDefinition Room(string id)
        {
            foreach (RoomDefinition r in rooms) if (r.id == id) return r;
            return null;
        }

        public InteractableSpot FirstOfType(InteractableType type)
        {
            foreach (InteractableSpot s in interactables) if (s.type == type) return s;
            return null;
        }

        public int CountOfType(InteractableType type)
        {
            int n = 0;
            foreach (InteractableSpot s in interactables) if (s.type == type) n++;
            return n;
        }

        public InteractableSpot Find(InteractableType type, string payload)
        {
            foreach (InteractableSpot s in interactables) if (s.type == type && s.payload == payload) return s;
            return null;
        }
    }

    /// <summary>Checks that a generated mine satisfies the layout contract. Used by tests and by the generators.</summary>
    public static class MineLayoutValidator
    {
        public static readonly string[] RequiredLogbooks = { "quill", "pike", "bellamy" };
        public static readonly string[] RequiredParts = { "pneumaticHammer", "conveyor", "rockDrill" };

        public static List<string> Validate(VoxelGrid grid, MineLayout layout)
        {
            var problems = new List<string>();

            CheckStanding(grid, layout.spawnPoint, "spawn point", problems);
            CheckStanding(grid, layout.crusherPoint, "crusher", problems);
            CheckStanding(grid, layout.liftGatePoint, "lift gate", problems);

            RoomDefinition chamber = layout.Room("collapse_chamber");
            if (chamber == null) problems.Add("missing room collapse_chamber");
            else
            {
                if (!chamber.bounds.Contains(Floor(layout.spawnPoint))) problems.Add("spawn is not inside the Collapse Chamber");
                if (!chamber.bounds.Contains(Floor(layout.crusherPoint))) problems.Add("crusher is not inside the Collapse Chamber");
            }
            if (layout.Room("main_shaft") == null) problems.Add("missing room main_shaft");
            else if (!layout.Room("main_shaft").bounds.Contains(Floor(layout.liftGatePoint))) problems.Add("lift gate is not inside the Main Shaft");

            // Spawn must look at a wall with the crusher in front of the player.
            Float3 look = layout.spawnLookDirection.Normalized;
            Float3 toCrusher = (layout.crusherPoint - layout.spawnPoint).Normalized;
            if (Float3.Dot(look, toCrusher) < 0.7f) problems.Add("crusher is not in front of the spawn view");
            GridHit wall = GridRaycast.Cast(grid, layout.spawnPoint + new Float3(0f, 3.2f, 0f), look, 40f);
            if (!wall.hit) problems.Add("spawn view does not face a wall");

            Require(layout, InteractableType.FuelCell, 1, problems);
            Require(layout, InteractableType.Generator, 1, problems);
            Require(layout, InteractableType.WinchCable, 1, problems);
            Require(layout, InteractableType.LiftGate, 1, problems);
            Require(layout, InteractableType.CrewLocker, 1, problems);
            Require(layout, InteractableType.StoreTerminal, 1, problems);
            foreach (string log in RequiredLogbooks)
            {
                bool inLocker = false;
                foreach (InteractableSpot s in layout.interactables)
                    if (s.type == InteractableType.CrewLocker && s.payload != null && s.payload.Contains(log)) inLocker = true;
                if (!inLocker && layout.Find(InteractableType.Logbook, log) == null) problems.Add("missing logbook " + log);
            }
            foreach (string part in RequiredParts)
                if (layout.Find(InteractableType.RepairPart, part) == null) problems.Add("missing repair part for " + part);

            foreach (InteractableSpot s in layout.interactables)
                CheckStanding(grid, s.position, "interactable " + s.id, problems);

            var seen = new HashSet<string>();
            foreach (InteractableSpot s in layout.interactables)
                if (!seen.Add(s.id)) problems.Add("duplicate interactable id " + s.id);

            foreach (TreasureSpot t in layout.treasures)
            {
                if (grid.IsBoundary(t.voxel.x, t.voxel.y, t.voxel.z)) problems.Add("treasure " + t.name + " is in the unbreakable shell");
                else if (!grid.IsSolid(t.voxel.x, t.voxel.y, t.voxel.z)) problems.Add("treasure " + t.name + " is not buried");
            }
            return problems;
        }

        private static void Require(MineLayout layout, InteractableType type, int count, List<string> problems)
        {
            if (layout.CountOfType(type) < count) problems.Add("missing " + type);
        }

        public static Int3 Floor(Float3 p) => new Int3((int)Math.Floor(p.x), (int)Math.Floor(p.y), (int)Math.Floor(p.z));

        /// <summary>A point is standable when its cell and the cell above are air and the cell below is solid.</summary>
        private static void CheckStanding(VoxelGrid grid, Float3 point, string what, List<string> problems)
        {
            Int3 c = Floor(point);
            if (grid.IsBoundary(c.x, c.y, c.z)) { problems.Add(what + " is outside the playable interior"); return; }
            if (grid.IsSolid(c.x, c.y, c.z)) problems.Add(what + " is inside solid rock at " + c);
            if (grid.IsSolid(c.x, c.y + 1, c.z)) problems.Add(what + " has no headroom at " + c);
            if (!grid.IsSolid(c.x, c.y - 1, c.z)) problems.Add(what + " has no floor below " + c);
        }
    }
}
