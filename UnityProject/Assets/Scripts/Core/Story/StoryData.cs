using System;
using System.Collections.Generic;
using BreakToEscape.Core.Json;

namespace BreakToEscape.Core.Story
{
    public sealed class LogbookEntry
    {
        public string id;
        public string owner;
        public string title;
        public string body;
        /// <summary>Which gate-code dial (0, 1 or 2) this logbook gives the number for.</summary>
        public int codeSlot;
    }

    public sealed class WallNote
    {
        public string id;
        public string title;
        public string body;
    }

    /// <summary>All story text, loaded from Resources/Data/story.json. No story text lives in code.</summary>
    public sealed class StoryData
    {
        public readonly int[] gateCode = new int[3];
        public readonly Dictionary<string, LogbookEntry> logbooks = new Dictionary<string, LogbookEntry>();
        public readonly Dictionary<string, WallNote> notes = new Dictionary<string, WallNote>();
        public readonly Dictionary<string, string> dispatch = new Dictionary<string, string>();
        public readonly Dictionary<string, string> hardHats = new Dictionary<string, string>();
        public readonly Dictionary<string, string> interactPrompts = new Dictionary<string, string>();
        public string endingTitle = "";
        public readonly List<string> endingLines = new List<string>();
        public string newShiftTeaser = "";
        public readonly List<string> credits = new List<string>();

        /// <summary>Dispatch line for a key, or the key in brackets if the line is missing (visible in testing).</summary>
        public string Line(string key)
        {
            if (key != null && dispatch.TryGetValue(key, out string line)) return line;
            return "[" + key + "]";
        }

        public string Prompt(string key, string fallback)
        {
            if (key != null && interactPrompts.TryGetValue(key, out string line)) return line;
            return fallback;
        }

        public static StoryData FromJson(string json)
        {
            var root = JsonRead.Obj(MiniJson.Parse(json), "story.json");
            var s = new StoryData();

            var code = JsonRead.Obj(root, "gateCode");
            s.gateCode[0] = JsonRead.Int(code, "quill", 14);
            s.gateCode[1] = JsonRead.Int(code, "pike", 3);
            s.gateCode[2] = JsonRead.Int(code, "bellamy", 96);

            foreach (object o in JsonRead.Arr(root, "logbooks"))
            {
                var n = JsonRead.Obj(o, "logbook");
                var entry = new LogbookEntry
                {
                    id = JsonRead.Str(n, "id", ""),
                    owner = JsonRead.Str(n, "owner", ""),
                    title = JsonRead.Str(n, "title", ""),
                    body = JsonRead.Str(n, "body", ""),
                    codeSlot = JsonRead.Int(n, "codeSlot", 0),
                };
                s.logbooks[entry.id] = entry;
            }

            foreach (object o in JsonRead.Arr(root, "notes"))
            {
                var n = JsonRead.Obj(o, "note");
                var note = new WallNote { id = JsonRead.Str(n, "id", ""), title = JsonRead.Str(n, "title", ""), body = JsonRead.Str(n, "body", "") };
                s.notes[note.id] = note;
            }

            CopyStrings(JsonRead.Obj(root, "dispatch"), s.dispatch);
            CopyStrings(JsonRead.OptObj(root, "hardHats"), s.hardHats);
            CopyStrings(JsonRead.OptObj(root, "interactPrompts"), s.interactPrompts);

            var ending = JsonRead.Obj(root, "ending");
            s.endingTitle = JsonRead.Str(ending, "title", "");
            s.endingLines.AddRange(JsonRead.StrList(ending, "lines"));
            s.newShiftTeaser = JsonRead.Str(ending, "newShiftTeaser", "");
            s.credits.AddRange(JsonRead.StrList(root, "credits"));
            return s;
        }

        private static void CopyStrings(Dictionary<string, object> node, Dictionary<string, string> target)
        {
            if (node == null) return;
            foreach (var kv in node)
                if (kv.Value is string text) target[kv.Key] = text;
        }
    }

    public enum GateResult
    {
        Opened,
        AlreadyOpen,
        NeedPower,
        NeedCable,
        WrongCode
    }

    public enum GeneratorResult
    {
        Repaired,
        AlreadyRepaired,
        NeedFuelCell
    }

    /// <summary>
    /// The escape chain: fuel cell → generator (power), winch cable, three logbook numbers →
    /// lift gate → ending. Unity-independent and fully saved.
    /// </summary>
    public sealed class EscapeStateMachine
    {
        private readonly int[] correctCode;
        private readonly int[] dials = new int[3];
        private readonly HashSet<string> logbooks = new HashSet<string>();

        public bool FuelCellFound { get; private set; }
        public bool GeneratorRepaired { get; private set; }
        public bool CableRecovered { get; private set; }
        public bool GateOpened { get; private set; }
        public bool Escaped { get; private set; }

        public event Action Changed;

        public EscapeStateMachine(int[] correctCode)
        {
            if (correctCode == null || correctCode.Length != 3) throw new ArgumentException("Gate code must have three numbers");
            this.correctCode = (int[])correctCode.Clone();
        }

        public int Dial(int slot) => dials[slot];
        public IEnumerable<string> LogbooksFound => logbooks;
        public int LogbookCount => logbooks.Count;
        public bool HasLogbook(string id) => logbooks.Contains(id);

        public bool FindFuelCell()
        {
            if (FuelCellFound) return false;
            FuelCellFound = true;
            Changed?.Invoke();
            return true;
        }

        public GeneratorResult TryRepairGenerator()
        {
            if (GeneratorRepaired) return GeneratorResult.AlreadyRepaired;
            if (!FuelCellFound) return GeneratorResult.NeedFuelCell;
            GeneratorRepaired = true;
            Changed?.Invoke();
            return GeneratorResult.Repaired;
        }

        public bool RecoverCable()
        {
            if (CableRecovered) return false;
            CableRecovered = true;
            Changed?.Invoke();
            return true;
        }

        public bool FindLogbook(string id)
        {
            if (string.IsNullOrEmpty(id) || !logbooks.Add(id)) return false;
            Changed?.Invoke();
            return true;
        }

        public void SetDial(int slot, int value)
        {
            dials[slot] = ((value % 100) + 100) % 100;
            Changed?.Invoke();
        }

        public void CycleDial(int slot, int delta) => SetDial(slot, dials[slot] + delta);

        public bool CodeMatches => dials[0] == correctCode[0] && dials[1] == correctCode[1] && dials[2] == correctCode[2];

        public GateResult TryOpenGate()
        {
            if (GateOpened) return GateResult.AlreadyOpen;
            if (!GeneratorRepaired) return GateResult.NeedPower;
            if (!CableRecovered) return GateResult.NeedCable;
            if (!CodeMatches) return GateResult.WrongCode;
            GateOpened = true;
            Changed?.Invoke();
            return GateResult.Opened;
        }

        public void MarkEscaped()
        {
            if (!GateOpened || Escaped) return;
            Escaped = true;
            Changed?.Invoke();
        }

        public void Restore(bool fuelCell, bool generator, bool cable, IEnumerable<string> foundLogbooks, int[] dialValues, bool gateOpened, bool escaped)
        {
            FuelCellFound = fuelCell || generator;
            GeneratorRepaired = generator;
            CableRecovered = cable;
            logbooks.Clear();
            if (foundLogbooks != null) foreach (string id in foundLogbooks) if (!string.IsNullOrEmpty(id)) logbooks.Add(id);
            for (int i = 0; i < 3; i++) dials[i] = dialValues != null && i < dialValues.Length ? ((dialValues[i] % 100) + 100) % 100 : 0;
            GateOpened = gateOpened && generator && cable;
            Escaped = escaped && GateOpened;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The five-step Dispatch tutorial. Each step advances only when the player actually does
    /// the thing. Steps: 1 punch the wall, 2 vacuum cubes, 3 feed the crusher, 4 the store is open,
    /// 5 dig deeper. <see cref="StepCaptionKey"/> gives the story.json dispatch key for a step.
    /// </summary>
    public sealed class TutorialSequence
    {
        public const int Done = 5;

        /// <summary>Number of the caption last shown (0 = not started, 5 = finished).</summary>
        public int Step { get; private set; }
        public float FirstCashTime { get; private set; } = -1f;
        public float FirstUpgradeTime { get; private set; } = -1f;

        public event Action<int> StepShown;

        public static string StepCaptionKey(int step) => "tutorial_" + step;

        public void Begin()
        {
            if (Step == 0) Show(1);
        }

        public void OnCubeBroken() { if (Step == 1) Show(2); }
        public void OnCubeVacuumed() { if (Step == 2) Show(3); }

        public void OnCashEarned(float runSeconds)
        {
            if (FirstCashTime < 0f) FirstCashTime = runSeconds;
            if (Step >= 1 && Step <= 3) Show(4);
        }

        public void OnUpgradeBought(float runSeconds)
        {
            if (FirstUpgradeTime < 0f) FirstUpgradeTime = runSeconds;
            if (Step >= 1 && Step < Done) Show(Done);
        }

        public void Restore(int step) => Step = Math.Max(0, Math.Min(Done, step));

        private void Show(int step)
        {
            Step = step;
            StepShown?.Invoke(step);
        }
    }
}
