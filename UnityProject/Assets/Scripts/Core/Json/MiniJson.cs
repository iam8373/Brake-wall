using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BreakToEscape.Core.Json
{
    /// <summary>
    /// Minimal JSON reader and writer used for game data files and saves.
    /// It has no Unity dependency so the same loader runs in the game and in the
    /// automated tests. Objects become Dictionary&lt;string, object&gt;, arrays become
    /// List&lt;object&gt;, numbers become double, and true/false/null map to bool/null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var parser = new Parser(json);
            object value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw new FormatException("Unexpected trailing characters in JSON at " + parser.Position);
            return value;
        }

        public static string Serialize(object value, bool pretty = false)
        {
            var sb = new StringBuilder(256);
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        private sealed class Parser
        {
            private readonly string text;
            private int index;

            public Parser(string text) { this.text = text; }
            public bool AtEnd => index >= text.Length;
            public int Position => index;

            public void SkipWhitespace()
            {
                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw new FormatException("Unexpected end of JSON");
                char c = text[index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw new FormatException("Unexpected character '" + c + "' at " + index);
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(text, index, word, 0, word.Length) != 0)
                    throw new FormatException("Expected '" + word + "' at " + index);
                index += word.Length;
            }

            private Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>();
                index++;
                SkipWhitespace();
                if (!AtEnd && text[index] == '}') { index++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || text[index] != '"') throw new FormatException("Expected property name at " + index);
                    string key = ParseString();
                    SkipWhitespace();
                    if (AtEnd || text[index] != ':') throw new FormatException("Expected ':' at " + index);
                    index++;
                    result[key] = ParseValue();
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("Unterminated object");
                    if (text[index] == ',') { index++; continue; }
                    if (text[index] == '}') { index++; return result; }
                    throw new FormatException("Expected ',' or '}' at " + index);
                }
            }

            private List<object> ParseArray()
            {
                var result = new List<object>();
                index++;
                SkipWhitespace();
                if (!AtEnd && text[index] == ']') { index++; return result; }
                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("Unterminated array");
                    if (text[index] == ',') { index++; continue; }
                    if (text[index] == ']') { index++; return result; }
                    throw new FormatException("Expected ',' or ']' at " + index);
                }
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                index++;
                while (true)
                {
                    if (AtEnd) throw new FormatException("Unterminated string");
                    char c = text[index++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw new FormatException("Unterminated escape");
                    char e = text[index++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 > text.Length) throw new FormatException("Bad unicode escape");
                            sb.Append((char)int.Parse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            index += 4;
                            break;
                        default: throw new FormatException("Bad escape '\\" + e + "'");
                    }
                }
            }

            private double ParseNumber()
            {
                int start = index;
                if (text[index] == '-') index++;
                while (index < text.Length)
                {
                    char c = text[index];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') index++;
                    else break;
                }
                return double.Parse(text.Substring(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        private static void Write(StringBuilder sb, object value, bool pretty, int depth)
        {
            switch (value)
            {
                case null: sb.Append("null"); return;
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case double d: WriteNumber(sb, d); return;
                case float f: WriteNumber(sb, f); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case byte by: sb.Append(by.ToString(CultureInfo.InvariantCulture)); return;
                case IDictionary<string, object> dict:
                    {
                        sb.Append('{');
                        bool first = true;
                        foreach (var kv in dict)
                        {
                            if (!first) sb.Append(',');
                            first = false;
                            NewLine(sb, pretty, depth + 1);
                            WriteString(sb, kv.Key);
                            sb.Append(pretty ? ": " : ":");
                            Write(sb, kv.Value, pretty, depth + 1);
                        }
                        if (!first) NewLine(sb, pretty, depth);
                        sb.Append('}');
                        return;
                    }
                case System.Collections.IEnumerable list:
                    {
                        sb.Append('[');
                        bool first = true;
                        foreach (object item in list)
                        {
                            if (!first) sb.Append(',');
                            first = false;
                            if (pretty && (item is IDictionary<string, object>)) NewLine(sb, true, depth + 1);
                            Write(sb, item, pretty, depth + 1);
                        }
                        sb.Append(']');
                        return;
                    }
                default:
                    throw new ArgumentException("MiniJson cannot serialize type " + value.GetType().Name);
            }
        }

        /// <summary>JSON has no NaN or infinity; they are written as null and read back as the field's default.</summary>
        private static void WriteNumber(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) sb.Append("null");
            else sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void NewLine(StringBuilder sb, bool pretty, int depth)
        {
            if (!pretty) return;
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    /// <summary>Typed helpers for reading values out of a parsed JSON tree.</summary>
    public static class JsonRead
    {
        public static Dictionary<string, object> Obj(object node, string context)
        {
            if (node is Dictionary<string, object> d) return d;
            throw new FormatException("Expected a JSON object for " + context);
        }

        public static Dictionary<string, object> Obj(Dictionary<string, object> parent, string key)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is Dictionary<string, object> d) return d;
            throw new FormatException("Missing object '" + key + "'");
        }

        public static Dictionary<string, object> OptObj(Dictionary<string, object> parent, string key)
        {
            if (parent != null && parent.TryGetValue(key, out object v)) return v as Dictionary<string, object>;
            return null;
        }

        public static List<object> Arr(Dictionary<string, object> parent, string key)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is List<object> l) return l;
            throw new FormatException("Missing array '" + key + "'");
        }

        public static List<object> OptArr(Dictionary<string, object> parent, string key)
        {
            if (parent != null && parent.TryGetValue(key, out object v)) return v as List<object>;
            return null;
        }

        public static double Num(Dictionary<string, object> parent, string key)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is double d) return d;
            throw new FormatException("Missing number '" + key + "'");
        }

        public static double Num(Dictionary<string, object> parent, string key, double fallback)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is double d) return d;
            return fallback;
        }

        public static int Int(Dictionary<string, object> parent, string key, int fallback)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is double d) return (int)Math.Round(d);
            return fallback;
        }

        public static long Long(Dictionary<string, object> parent, string key, long fallback)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is double d) return (long)Math.Round(d);
            return fallback;
        }

        public static bool Bool(Dictionary<string, object> parent, string key, bool fallback)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is bool b) return b;
            return fallback;
        }

        public static string Str(Dictionary<string, object> parent, string key, string fallback)
        {
            if (parent != null && parent.TryGetValue(key, out object v) && v is string s) return s;
            return fallback;
        }

        public static double[] NumArray(Dictionary<string, object> parent, string key)
        {
            List<object> list = Arr(parent, key);
            var result = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (!(list[i] is double d)) throw new FormatException("Array '" + key + "' must contain numbers");
                result[i] = d;
            }
            return result;
        }

        public static List<string> StrList(Dictionary<string, object> parent, string key)
        {
            var result = new List<string>();
            List<object> list = OptArr(parent, key);
            if (list == null) return result;
            foreach (object o in list) if (o is string s) result.Add(s);
            return result;
        }
    }
}
