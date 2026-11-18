using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SuperwhisperWoW {
    internal sealed class Companion : IDisposable {
        readonly string root;
        readonly Control queue = new Control();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly Desktop.Keyboard keyboard;
        readonly Desktop.Focus focus;
        IntPtr hook, foregroundHook, target;
        uint pid;
        Shortcut hold, toggle;
        bool down, released, pasted, submitted;
        long pressTicks, releasedAt, nextPrefs, pasteTime;
        readonly Stopwatch clock = Stopwatch.StartNew();
        ClipboardLease clipboard;
        Recording recording;

        internal Companion(string directory) {
            root = directory;
            IntPtr owner = queue.Handle;
            keyboard = OnKey;
            focus = delegate(IntPtr h,uint e,IntPtr hwnd,int o,int c,uint t,uint ms) {
                if (target != IntPtr.Zero && target != hwnd) Cancel();
            };
            LoadPrefs();
            hook = Desktop.SetWindowsHookEx(13,keyboard,Desktop.GetModuleHandle(null),0);
            foregroundHook = Desktop.SetWinEventHook(3,3,IntPtr.Zero,focus,0,0,0);
            if (hook==IntPtr.Zero || foregroundHook==IntPtr.Zero) {
                Dispose();
                throw new InvalidOperationException("Windows input observation unavailable.");
            }
            timer.Interval=50;
            timer.Tick += delegate { Tick(); };
            timer.Start();
        }

        void LoadPrefs() {
            var prefs=Json.Read(Path.Combine(root,"preferences.json"));
            hold=Shortcut.ReadHold(prefs);
            toggle=Shortcut.Parse(Json.Text(prefs,"toggleRecordingShortcut") ?? "Control+Space");
            nextPrefs=clock.ElapsedMilliseconds+1500;
        }
        IntPtr OnKey(int code,IntPtr msg,IntPtr data) {
            if (code>=0 && hold!=null) {
                var edge=(Desktop.KeyEvent)Marshal.PtrToStructure(data,typeof(Desktop.KeyEvent));
                int vk=(int)edge.key;
                if (edge.extra!=Desktop.Stamp && hold.MatchesKey(vk)) {
                    bool up=(edge.flags & 128)!=0;
                    if (!up && !down && hold.Matches(vk,Desktop.Mods())) {
                        down=true;
                        IntPtr hwnd=Desktop.GetForegroundWindow();
                        long press=Recording.Epoch(), ticks=clock.ElapsedMilliseconds;
                        queue.BeginInvoke((Action)delegate { Arm(hwnd,press,ticks); });
                    } else if (up && down) {
                        down=false;
                        long durationEnd=clock.ElapsedMilliseconds, end=Recording.Epoch();
                        queue.BeginInvoke((Action)delegate {
                            if (recording==null) return;
                            if (durationEnd-pressTicks<1000) Cancel();
                            else { released=true; releasedAt=end; }
                        });
                    }
                }
            }
            return Desktop.CallNextHookEx(IntPtr.Zero,code,msg,data);
        }

        bool Collision(int key, int mods) {
            return (hold!=null && hold.Matches(key,mods)) || (toggle!=null && toggle.Matches(key,mods));
        }

        void Arm(IntPtr hwnd,long press,long ticks) {
            // A second press may stop an older recording. Never claim its output as a new hold.
            if (recording!=null) { Cancel(); return; }
            pid=Desktop.Game(hwnd);
            if (pid==0) return;
            int mods=Desktop.Mods();
            if (Collision(123,mods|3) || ((mods&1)==0 && Collision(17,mods)) ||
                ((mods&2)==0 && Collision(16,mods|1))) return;
            target=hwnd;
            pressTicks=ticks;
            try {
                clipboard=ClipboardLease.Capture(queue.Handle);
                if (clipboard==null) { Cancel(); return; }
                recording=new Recording(Path.Combine(root,"recordings"),press);
                if (!Desktop.Chord(target,pid,123,17,16)) Cancel();
            } catch (IOException) { Cancel(); } catch (UnauthorizedAccessException) { Cancel(); }
        }

        void Tick() {
            if (recording==null) {
                if (!down && clock.ElapsedMilliseconds>=nextPrefs) LoadPrefs();
                return;
            }
            if (!Desktop.Same(target,pid) || clock.ElapsedMilliseconds-pressTicks>300000) { Cancel(); return; }
            if (!released) return;
            if (pasted) {
                long elapsed=clock.ElapsedMilliseconds-pasteTime;
                if (submitted) { if (elapsed>=700) Cancel(); return; }
                if (!clipboard.IsCurrent || Desktop.Mods()!=0 || Collision(13,0)) { Cancel(); return; }
                if (elapsed>=150) {
                    submitted=Desktop.Chord(target,pid,13);
                    if (!submitted) Cancel();
                }
                return;
            }
            if (Recording.Epoch()-releasedAt>90) { Cancel(); return; }
            // Wait for physical chord modifiers to come up before synthesizing delivery.
            if (Desktop.Mods()!=0) return;
            try {
                bool reject;
                string text=recording.Result(releasedAt,out reject);
                if (reject) { Cancel(); return; }
                if (text==null) return;
                bool insert=!Collision(16,0) && !Collision(45,2);
                if ((!insert && (Collision(17,0) || Collision(86,1))) || Collision(13,0) ||
                    !Desktop.Same(target,pid) || !clipboard.Install(text)) { Cancel(); return; }
                pasted=insert ? Desktop.Chord(target,pid,45,16) : Desktop.Chord(target,pid,86,17);
                if (!pasted) { Cancel(); return; }
                pasteTime=clock.ElapsedMilliseconds;
            } catch (IOException) { Cancel(); } catch (UnauthorizedAccessException) { Cancel(); }
        }
        void Cancel() {
            target=IntPtr.Zero;
            recording=null;
            released=pasted=submitted=false;
            if (clipboard!=null) { clipboard.Dispose(); clipboard=null; }
        }
        public void Dispose() {
            timer.Stop();
            if (foregroundHook!=IntPtr.Zero) Desktop.UnhookWinEvent(foregroundHook);
            if (hook!=IntPtr.Zero) Desktop.UnhookWindowsHookEx(hook);
            Cancel();
            timer.Dispose();
            queue.Dispose();
        }
        [STAThread]
        internal static void Main() {
            bool first;
            using (var mutex=new Mutex(true,@"Local\SuperwhisperWoW.Companion",out first)) {
                if (!first) return;
                string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"com.superwhisper.app");
                using (var companion=new Companion(root)) Application.Run();
            }
        }
    }
}
