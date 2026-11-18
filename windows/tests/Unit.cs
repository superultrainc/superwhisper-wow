using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SuperwhisperWoW {
    class Unit {
        static void Check(bool ok,string name) { if (!ok) throw new Exception(name); }
        [STAThread] public static void Main() {
            var prefs=new Dictionary<string,object>{{"p2t",false},{"pushToTalkShortcut","AltRight"},{"toggleRecordingShortcut","Control+Space"}};
            Check(Shortcut.ReadHold(prefs)==null,"disabled");
            prefs["p2t"]=true;
            var hold=Shortcut.ReadHold(prefs);
            Check(hold.Matches(165,4) && !hold.Matches(164,4),"side");
            Check(Shortcut.Parse("Control+Space").Matches(32,1),"combined");
            Check(!Shortcut.Parse("Control+Space").Matches(32,3),"extra modifier");
            Check(Shortcut.Parse("ShiftLeft").MatchesInjected(16,0),"synthetic Shift triggers left Shift");
            Check(!Shortcut.Parse("ControlRight").MatchesInjected(17,0),"synthetic Control leaves right Control alone");
            Check(Shortcut.Parse("Control+ShiftLeft").MatchesInjected(16,1),"modifier chord collision");
            Check(Shortcut.Parse("Bogus")==null,"unknown shortcut");
            Check(Desktop.GameName(@"C:\Games\WOW.exe") && !Desktop.GameName(@"C:\Wow.exe\Other.exe"),"foreground executable");
            string root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(root,"100"));
            try {
                var watcher=new Recording(root,100);
                File.WriteAllText(Path.Combine(root,"100","meta.json"),"{\"result\":\"old\",\"modeName\":\"WoW\"}");
                bool reject;
                Check(watcher.Result(101,out reject)==null && !reject,"stale");
                var dir=Directory.CreateDirectory(Path.Combine(root,"101")).FullName;
                var meta=Path.Combine(dir,"meta.json");
                File.WriteAllText(meta,"{\"result\":");
                Check(watcher.Result(101,out reject)==null && !reject,"partial");
                File.WriteAllText(meta,"{\"result\":\"raw\",\"llmResult\":\"Party, hello\",\"modeName\":\"WoW\"}");
                Check(watcher.Result(101,out reject)=="Party, hello","addon receives intact prefix");
                File.WriteAllText(meta,"{\"result\":\"raw\",\"modeName\":\"WoW\"}");
                Check(watcher.Result(101,out reject)=="raw","result fallback");
                File.WriteAllText(meta,"{\"result\":\"/reload\",\"modeName\":\"WoW\"}");
                Check(watcher.Result(101,out reject)==null && reject,"command rejected");
                File.WriteAllText(meta,"{\"result\":\"other\",\"modeName\":\"Email\"}");
                Check(watcher.Result(101,out reject)==null && reject,"wrong mode");
                Directory.CreateDirectory(Path.Combine(root,"102"));
                Check(watcher.Result(102,out reject)==null && reject,"ambiguous");
            } finally { Directory.Delete(root,true); }
            using (var window=new Control()) {
                Clipboard.SetText("original");
                using(var lease=ClipboardLease.Capture(window.Handle)) {
                    Check(lease!=null && lease.Install("hello"),"clipboard install");
                    Check(Clipboard.GetText()=="hello","clipboard content");
                }
                Check(Clipboard.GetText()=="original","restore");
                using(var lease=ClipboardLease.Capture(window.Handle)) {
                    Clipboard.SetText("hello");
                    Check(lease.Install("hello"),"Superwhisper completion copied text");
                    Clipboard.SetText("new user copy");
                }
                Check(Clipboard.GetText()=="new user copy","concurrent user copy");
            }
            Console.WriteLine("Windows unit checks passed");
        }
    }
}
