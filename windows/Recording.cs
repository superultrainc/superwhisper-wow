using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SuperwhisperWoW {
    internal static class Json {
        internal static Dictionary<string, object> Read(string file) {
            try {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream)) {
                    return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                }
            } catch (IOException) {} catch (UnauthorizedAccessException) {}
              catch (ArgumentException) {} catch (InvalidOperationException) {}
            return null;
        }

        internal static string Text(Dictionary<string, object> doc, string field) {
            object value;
            return doc != null && doc.TryGetValue(field, out value) ? value as string : null;
        }
    }

    internal sealed class Shortcut {
        internal int Key, Mods;
        static readonly Dictionary<string, Keys> Names = new Dictionary<string, Keys>(StringComparer.OrdinalIgnoreCase) {
            {"Control",Keys.ControlKey},{"Ctrl",Keys.ControlKey},{"Ctl",Keys.ControlKey},
            {"ControlLeft",Keys.LControlKey},{"ControlRight",Keys.RControlKey},
            {"Alt",Keys.Menu},{"Option",Keys.Menu},{"AltLeft",Keys.LMenu},{"AltRight",Keys.RMenu},
            {"Shift",Keys.ShiftKey},{"ShiftLeft",Keys.LShiftKey},{"ShiftRight",Keys.RShiftKey},
            {"Meta",Keys.LWin},{"Super",Keys.LWin},{"Win",Keys.LWin},{"Windows",Keys.LWin},
            {"Command",Keys.LWin},{"Cmd",Keys.LWin},{"MetaLeft",Keys.LWin},{"MetaRight",Keys.RWin},
            {"ArrowUp",Keys.Up},{"ArrowDown",Keys.Down},{"ArrowLeft",Keys.Left},{"ArrowRight",Keys.Right},
            {"Backspace",Keys.Back},{"Esc",Keys.Escape},{"Ins",Keys.Insert},{"Del",Keys.Delete},
            {"PgUp",Keys.PageUp},{"PgDn",Keys.PageDown},{"NumpadEnter",Keys.Enter},
            {"Backquote",Keys.Oemtilde},{"Minus",Keys.OemMinus},{"Equal",Keys.Oemplus},
            {"BracketLeft",Keys.OemOpenBrackets},{"BracketRight",Keys.OemCloseBrackets},
            {"Backslash",Keys.OemPipe},{"IntlBackslash",Keys.OemBackslash},{"IntlYen",Keys.OemPipe},
            {"IntlRo",Keys.OemBackslash},{"Semicolon",Keys.OemSemicolon},{"Quote",Keys.OemQuotes},
            {"Comma",Keys.Oemcomma},{"Period",Keys.OemPeriod},{"Slash",Keys.OemQuestion},
            {"NumpadAdd",Keys.Add},{"NumpadSubtract",Keys.Subtract},{"NumpadMultiply",Keys.Multiply},
            {"NumpadDivide",Keys.Divide},{"NumpadDecimal",Keys.Decimal},{"ContextMenu",Keys.Apps},
            {"AudioVolumeMute",Keys.VolumeMute},{"AudioMuteMute",Keys.VolumeMute},{"AudioVolumeDown",Keys.VolumeDown},
            {"AudioVolumeUp",Keys.VolumeUp},{"NonConvert",Keys.IMENonconvert},{"Convert",Keys.IMEConvert}
        };

        internal static int Modifier(int k) {
            if (k == 17 || k == 162 || k == 163) return 1;
            if (k == 16 || k == 160 || k == 161) return 2;
            if (k == 18 || k == 164 || k == 165) return 4;
            return k == 91 || k == 92 ? 8 : 0;
        }

        internal static Shortcut Parse(string spec) {
            if (String.IsNullOrWhiteSpace(spec)) return null;
            int mask = 0, trigger = 0, lastMod = 0;
            foreach (var raw in spec.Split('+')) {
                string name = raw.Trim();
                Keys key;
                if (!Names.TryGetValue(name, out key)) {
                    if (name.StartsWith("Key", StringComparison.OrdinalIgnoreCase) && name.Length == 4) name = name.Substring(3);
                    if (name.StartsWith("Digit", StringComparison.OrdinalIgnoreCase) && name.Length == 6) name = name.Substring(5);
                    if (name.Length == 1 && name[0] >= '0' && name[0] <= '9') key = (Keys)name[0];
                    else if (!Enum.TryParse(name, true, out key) || !Enum.IsDefined(typeof(Keys), key) ||
                             name.Contains(",") || key <= 0 || (int)key > 254 || Char.IsDigit(name[0])) return null;
                }
                int mod = Modifier((int)key);
                if (mod != 0) { mask |= mod; lastMod = (int)key; }
                else if (trigger == 0) trigger = (int)key;
                else return null;
            }
            return new Shortcut { Key = trigger == 0 ? lastMod : trigger, Mods = trigger == 0 ? 0 : mask };
        }

        internal bool MatchesKey(int vk) {
            return vk == Key || ((Key == 16 || Key == 17 || Key == 18) && Modifier(Key) == Modifier(vk));
        }

        internal bool Matches(int vk, int mask) {
            return MatchesKey(vk) && (mask & ~Modifier(vk)) == Mods;
        }
        internal static Shortcut ReadHold(Dictionary<string,object> prefs) {
            object enabled;
            if (prefs == null || !prefs.TryGetValue("p2t", out enabled) || !Object.Equals(enabled, true)) return null;
            return Parse(Json.Text(prefs, "pushToTalkShortcut") ?? "Control+Space");
        }
    }

    internal sealed class Recording {
        readonly string root;
        readonly HashSet<string> old;
        readonly long start;
        internal static long Epoch() { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
        internal Recording(string directory, long press) {
            root = directory;
            start = press;
            old = new HashSet<string>(Directory.Exists(root) ? Directory.GetDirectories(root) : new string[0]);
        }
        internal string Result(long release, out bool reject) {
            reject = false;
            if (!Directory.Exists(root)) return null;
            var folders = Directory.GetDirectories(root).Where(path => {
                long seconds;
                return !old.Contains(path) && Int64.TryParse(Path.GetFileName(path), out seconds) &&
                    seconds >= start && seconds <= release + 10;
            }).Take(2).ToArray();
            if (folders.Length != 1) { reject = folders.Length > 1; return null; }
            var meta = Json.Read(Path.Combine(folders[0], "meta.json"));
            if (meta == null) return null;
            string result = Json.Text(meta, "llmResult");
            if (String.IsNullOrWhiteSpace(result)) result = Json.Text(meta, "result");
            if (String.IsNullOrWhiteSpace(result)) return null;
            if (!String.Equals(Json.Text(meta,"modeName"), "WoW", StringComparison.OrdinalIgnoreCase)) {
                reject = true;
                return null;
            }
            // The addon owns prefix parsing. Never pass a raw game command or multiple lines.
            result = String.Join(" ", result.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            if (result.StartsWith("/") || result.Any(Char.IsControl) || Encoding.UTF8.GetByteCount(result) > 255) {
                reject = true;
                return null;
            }
            return result;
        }
    }
}
