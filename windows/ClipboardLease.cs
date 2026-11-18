using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace SuperwhisperWoW {
    // Hold independent copies: Superwhisper itself writes the clipboard before saving meta.json.
    internal sealed class ClipboardLease : IDisposable {
        [DllImport("user32")] static extern bool OpenClipboard(IntPtr owner);
        [DllImport("user32")] static extern bool CloseClipboard();
        [DllImport("user32")] static extern bool EmptyClipboard();
        [DllImport("user32", SetLastError=true)] static extern uint EnumClipboardFormats(uint last);
        [DllImport("user32")] static extern IntPtr GetClipboardData(uint format);
        [DllImport("user32")] static extern IntPtr SetClipboardData(uint format, IntPtr data);
        [DllImport("user32")] internal static extern uint GetClipboardSequenceNumber();
        [DllImport("ole32")] static extern IntPtr OleDuplicateData(IntPtr data, uint format, uint flags);
        [DllImport("kernel32")] static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
        [DllImport("kernel32")] static extern IntPtr GlobalLock(IntPtr memory);
        [DllImport("kernel32")] static extern bool GlobalUnlock(IntPtr memory);
        [DllImport("kernel32")] static extern IntPtr GlobalFree(IntPtr memory);
        [DllImport("gdi32")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32")] static extern bool DeleteEnhMetaFile(IntPtr obj);
        readonly Dictionary<uint,IntPtr> saved = new Dictionary<uint,IntPtr>();
        readonly IntPtr owner;
        uint original, installed;
        bool owns;
        ClipboardLease(IntPtr window) { owner = window; }

        static bool Open(IntPtr owner) {
            for (int i=0; i<5; i++) {
                if (OpenClipboard(owner)) return true;
                Thread.Sleep(5);
            }
            return false;
        }

        internal static ClipboardLease Capture(IntPtr owner) {
            if (!Open(owner)) return null;
            var lease = new ClipboardLease(owner);
            try {
                uint format = 0;
                while ((format = EnumClipboardFormats(format)) != 0) {
                    // Owner-display, metafile pictures and private handles have non-generic lifetimes.
                    if (format == 3 || (format >= 0x80 && format < 0xC000)) return null;
                    IntPtr source = GetClipboardData(format);
                    if (source == IntPtr.Zero) return null;
                    IntPtr copy = OleDuplicateData(source, format, 2);
                    if (copy == IntPtr.Zero) return null;
                    lease.saved.Add(format,copy);
                }
                if (Marshal.GetLastWin32Error() != 0) return null;
                lease.original = GetClipboardSequenceNumber();
                var result = lease;
                lease = null;
                return result;
            } finally {
                CloseClipboard();
                if (lease != null) lease.Dispose();
            }
        }

        internal bool Install(string text) {
            if (!Open(owner)) return false;
            IntPtr memory = IntPtr.Zero;
            try {
                if (GetClipboardSequenceNumber() != original) {
                    IntPtr handle = GetClipboardData(13);
                    IntPtr ptr = handle == IntPtr.Zero ? IntPtr.Zero : GlobalLock(handle);
                    if (ptr == IntPtr.Zero) return false;
                    try {
                        string now = Marshal.PtrToStringUni(ptr);
                        if (String.Join(" ",now.Split((char[])null,StringSplitOptions.RemoveEmptyEntries)) != text) return false;
                    } finally { GlobalUnlock(handle); }
                }
                char[] contents = (text + "\0").ToCharArray();
                memory = GlobalAlloc(2, (UIntPtr)(contents.Length * 2));
                if (memory == IntPtr.Zero) return false;
                IntPtr dest = GlobalLock(memory);
                if (dest == IntPtr.Zero) return false;
                try { Marshal.Copy(contents,0,dest,contents.Length); } finally { GlobalUnlock(memory); }
                if (!EmptyClipboard()) return false;
                if (SetClipboardData(13,memory) == IntPtr.Zero) { PutBack(); return false; }
                memory = IntPtr.Zero;
                installed = GetClipboardSequenceNumber();
                owns = true;
                return true;
            } finally {
                CloseClipboard();
                if (memory != IntPtr.Zero) GlobalFree(memory);
            }
        }

        internal bool IsCurrent { get { return owns && GetClipboardSequenceNumber() == installed; } }
        void PutBack() {
            foreach (var format in new List<uint>(saved.Keys))
                if (SetClipboardData(format,saved[format]) != IntPtr.Zero) saved.Remove(format);
        }
        public void Dispose() {
            if (owns && Open(owner)) {
                try { if (IsCurrent && EmptyClipboard()) PutBack(); } finally { CloseClipboard(); }
            }
            owns = false;
            foreach (var pair in saved) {
                if (pair.Key == 2 || pair.Key == 9) DeleteObject(pair.Value);
                else if (pair.Key == 14) DeleteEnhMetaFile(pair.Value);
                else GlobalFree(pair.Value);
            }
            saved.Clear();
        }
    }
}
