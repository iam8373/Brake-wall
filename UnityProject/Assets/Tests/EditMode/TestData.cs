using System;
using System.IO;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Story;
using BreakToEscape.Core.World;

namespace BreakToEscape.Tests
{
    /// <summary>
    /// Finds the game data files from both test runners: Unity's Test Runner (working directory =
    /// the Unity project folder) and `dotnet test` in tools/tests (walks up to the repository root).
    /// </summary>
    public static class TestData
    {
        private static EconomyData economy;
        private static StoryData story;

        public static string ReadDataFile(string fileName)
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                string dir = start;
                for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
                {
                    foreach (string candidate in new[]
                    {
                        Path.Combine(dir, "Assets", "Resources", "Data", fileName),
                        Path.Combine(dir, "UnityProject", "Assets", "Resources", "Data", fileName),
                    })
                    {
                        if (File.Exists(candidate)) return File.ReadAllText(candidate);
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }
            throw new FileNotFoundException("Could not locate data file " + fileName);
        }

        public static EconomyData Economy => economy ?? (economy = EconomyData.FromJson(ReadDataFile("economy.json")));
        public static StoryData Story => story ?? (story = StoryData.FromJson(ReadDataFile("story.json")));

        public static VoxelGrid NewGrid()
        {
            EconomyData e = Economy;
            return new VoxelGrid(e.worldSizeVoxels.x, e.worldSizeVoxels.y, e.worldSizeVoxels.z, new VoxelMaterialTable(e));
        }

        public static VoxelGrid NewHandBuiltMine(out MineLayout layout)
        {
            VoxelGrid grid = NewGrid();
            layout = HandBuiltMineGenerator.Generate(grid, Economy.treasureOneInVoxels, Economy.nookTreasureClusterSize);
            return grid;
        }

        /// <summary>A small grid filled with band-1 rock (10 HP), or air.</summary>
        public static VoxelGrid SmallGrid(int size, byte fill)
        {
            var grid = new VoxelGrid(size, size, size, new VoxelMaterialTable(Economy));
            grid.FillGenerated(fill);
            return grid;
        }
    }
}
