using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SuperwhisperWoW {
    class DesktopTest {
        [DllImport("user32")] static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
        [DllImport("user32")] static extern bool SetForegroundWindow(IntPtr hwnd);
        static void Edge(byte vk,bool down) { keybd_event(vk,0,down?0u:2u,UIntPtr.Zero); }
        static void Wait(Func<bool> ready,string step) {
            var clock=Stopwatch.StartNew();
            while (!ready()) {
                if (clock.ElapsedMilliseconds>10000) throw new Exception("Timed out: "+step);
                Thread.Sleep(50);
            }
        }
        static void Stop(Process process) {
            if (process!=null && !process.HasExited) { process.Kill(); process.WaitForExit(); }
        }
        [STAThread] static void Main(string[] args) {
            string bin=Path.GetFullPath(args[0]), output=Path.Combine(bin,"proof");
            string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"com.superwhisper.app");
            if (Directory.Exists(data)) throw new Exception("Run the fixture under an unused Windows account");
            Directory.CreateDirectory(output);
            string recordings=Path.Combine(data,"recordings");
            Directory.CreateDirectory(recordings);
            File.WriteAllText(Path.Combine(data,"preferences.json"),"{\"p2t\":true,\"pushToTalkShortcut\":\"Control+Space\",\"toggleRecordingShortcut\":\"Control+Space\"}");
            Process game=null, companion=null;
            try {
                Clipboard.SetText("clipboard before recording");
                game=Process.Start(Path.Combine(bin,"Wow.exe"),"\""+output+"\"");
                Wait(()=>File.Exists(Path.Combine(output,"window")),"fake game window");
                IntPtr hwnd=new IntPtr(long.Parse(File.ReadAllText(Path.Combine(output,"window"))));
                SetForegroundWindow(hwnd);
                Wait(()=>Desktop.GetForegroundWindow()==hwnd,"foreground Wow.exe");
                companion=Process.Start(Path.Combine(bin,"SuperwhisperWoW.exe"));
                if (!companion.WaitForInputIdle(5000)) throw new Exception("No companion message loop");
                Thread.Sleep(350);
                Edge(17,true);
                Edge(32,true);
                try {
                    Wait(()=>File.Exists(Path.Combine(output,"armed")),"addon key");
                    Thread.Sleep(1100);
                } finally { Edge(32,false); Edge(17,false); }
                string dir=Path.Combine(recordings,Recording.Epoch().ToString());
                Directory.CreateDirectory(dir);
                string meta=Path.Combine(dir,"meta.json");
                File.WriteAllText(meta,"{\"rawResult\":");
                Thread.Sleep(250);
                Clipboard.SetText("party microphone check");
                File.WriteAllText(meta,"{\"modeName\":\"WoW\",\"rawResult\":\"wrong\",\"result\":\"wrong\",\"llmResult\":\"party microphone check\"}");
                Wait(()=>File.Exists(Path.Combine(output,"windows-chat.png")),"paste and Enter");
                Wait(()=>Clipboard.GetText()=="clipboard before recording","clipboard restoration");
                var received=File.ReadAllLines(Path.Combine(output,"result"));
                if (!received.SequenceEqual(new[]{"1","1","1","party microphone check"})) throw new Exception("Unexpected edit box events");
                Console.WriteLine("Verified addon key, native paste, one Enter, exact text and clipboard restoration.");
            } finally {
                Stop(companion); Stop(game); Directory.Delete(data,true);
            }
        }
    }
}
