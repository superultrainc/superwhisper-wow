using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SuperwhisperWoW {
    internal static class Desktop {
        internal delegate IntPtr Keyboard(int code, IntPtr message, IntPtr data);
        internal delegate void Focus(IntPtr hook, uint evt, IntPtr hwnd, int obj, int child, uint thread, uint time);
        [DllImport("user32",SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int id, Keyboard callback, IntPtr module, uint thread);
        [DllImport("user32")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr msg,IntPtr data);
        [DllImport("user32")] internal static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,Focus callback,uint pid,uint thread,uint flags);
        [DllImport("user32")] internal static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
        [DllImport("user32")] static extern short GetAsyncKeyState(int key);
        [DllImport("kernel32",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32")] static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
        [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h,uint flags,StringBuilder name,ref int size);
        [DllImport("user32",SetLastError=true)] static extern uint SendInput(uint n, Input[] data,int size);

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyEvent { internal uint key,scan,flags,time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        struct Input { internal uint type; internal Union data; }
        [StructLayout(LayoutKind.Explicit)]
        struct Union {
            [FieldOffset(0)] internal KeyInput key;
            [FieldOffset(0)] internal MouseInput mouse;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct KeyInput { internal ushort vk,scan; internal uint flags,time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        struct MouseInput { internal int x,y; internal uint data,flags,time; internal UIntPtr extra; }
        internal static readonly UIntPtr Stamp = new UIntPtr(0x53575731);
        internal static bool Down(int vk) { return GetAsyncKeyState(vk)<0; }
        internal static int Mods() {
            return (Down(17)?1:0) | (Down(16)?2:0) | (Down(18)?4:0) | (Down(91)||Down(92)?8:0);
        }
        internal static bool GameName(string exe) {
            return String.Equals(Path.GetFileName(exe),"Wow.exe",StringComparison.OrdinalIgnoreCase);
        }
        internal static uint Game(IntPtr hwnd) {
            uint pid;
            if (hwnd == IntPtr.Zero || hwnd != GetForegroundWindow() || GetWindowThreadProcessId(hwnd,out pid)==0) return 0;
            IntPtr proc = OpenProcess(0x1000,false,pid);
            if (proc==IntPtr.Zero) return 0;
            try {
                int size=32768;
                var name = new StringBuilder(size);
                return QueryFullProcessImageName(proc,0,name,ref size) && GameName(name.ToString()) ? pid : 0;
            } finally { CloseHandle(proc); }
        }
        internal static bool Same(IntPtr hwnd,uint pid) {
            uint actual;
            return pid != 0 && hwnd==GetForegroundWindow() && GetWindowThreadProcessId(hwnd,out actual)!=0 && actual==pid;
        }
        static Input Edge(int vk,bool up) {
            var input=new Input {type=1};
            input.data.key.vk=(ushort)vk;
            input.data.key.flags=(up?2u:0u) | (vk==45?1u:0u);
            input.data.key.extra=Stamp;
            return input;
        }
        internal static bool Chord(IntPtr hwnd,uint pid,int key,params int[] modifiers) {
            if (!Same(hwnd,pid) || Down(key)) return false;
            var events=new List<Input>();
            var acquired=new Stack<int>();
            foreach (int mod in modifiers) {
                if (Down(mod)) continue;
                acquired.Push(mod);
                events.Add(Edge(mod,false));
            }
            events.Add(Edge(key,false));
            events.Add(Edge(key,true));
            foreach (int mod in acquired) events.Add(Edge(mod,true));
            if (!Same(hwnd,pid)) return false;
            return SendInput((uint)events.Count,events.ToArray(),Marshal.SizeOf(typeof(Input)))==events.Count;
        }
    }
}
