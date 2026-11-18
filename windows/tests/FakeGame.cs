using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// A real foreground HWND, edit control and message loop. Only the test executable is named Wow.exe.
class FakeGame : Form {
    readonly TextBox edit = new TextBox();
    readonly Label status = new Label();
    readonly string output;
    int arms, enters, pasted;
    [DllImport("user32")] static extern bool SetForegroundWindow(IntPtr hwnd);
    FakeGame(string dir) {
        output=dir;
        Text="WoW chat integration fixture";
        ClientSize=new Size(560,115);
        StartPosition=FormStartPosition.CenterScreen;
        KeyPreview=true;
        status.SetBounds(16,16,525,35);
        status.Text="Waiting for the addon binding";
        edit.SetBounds(16,62,525,28);
        Controls.Add(status);
        Controls.Add(edit);
        Shown+=delegate {
            Activate();
            SetForegroundWindow(Handle);
            edit.Focus();
            File.WriteAllText(Path.Combine(output,"window"),Handle.ToInt64().ToString());
        };
        edit.TextChanged+=delegate {
            if (arms==1 && edit.Text=="party microphone check") pasted++;
        };
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keys) {
        if (keys==(Keys.Control|Keys.Shift|Keys.F12)) {
            arms++;
            edit.Clear();
            status.Text="Listening…";
            File.WriteAllText(Path.Combine(output,"armed"),arms.ToString());
            return true;
        }
        if (keys==Keys.Enter) {
            enters++;
            status.Text="Enter received";
            File.WriteAllLines(Path.Combine(output,"result"), new[]{arms.ToString(),pasted.ToString(),enters.ToString(),edit.Text});
            using (var image=new Bitmap(Width,Height)) {
                DrawToBitmap(image,new Rectangle(0,0,Width,Height));
                image.Save(Path.Combine(output,"windows-chat.png"));
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg,keys);
    }
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles();
        Application.Run(new FakeGame(args[0]));
    }
}
