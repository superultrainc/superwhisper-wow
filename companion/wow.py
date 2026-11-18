#!/usr/bin/env python3
"""Focus-scoped WoW dictation for Omarchy."""
import fcntl
import json
import os
from pathlib import Path
import re
import select
import shutil
import signal
import socket
import subprocess
import sys
import tempfile
import time

from vocabulary import FOREVER, Vocabulary

HERE = Path(__file__).resolve().parent
RUNTIME = Path(os.environ["XDG_RUNTIME_DIR"])
STATE = Path(os.environ.get("XDG_STATE_HOME", Path.home() / ".local/state")) / "superwhisper-wow"
ORIGINAL = {"Start dictation (push-to-talk)", "Stop dictation (push-to-talk)"}
OWNED = {"Superwhisper WoW press", "Superwhisper WoW release"}


def run(*args):
    return subprocess.check_output(args, text=True, timeout=10).strip()


def sw(*args):
    result = json.loads(run("superwhisper", *args))
    if isinstance(result, dict) and result.get("error"):
        raise RuntimeError(result["error"])
    return result


def lua(script):
    return run("hyprctl", "repl", script)


def literal(value):
    return json.dumps(str(value), ensure_ascii=False)


def binds():
    return [b["description"] for b in json.loads(run("hyprctl", "-j", "binds"))
            if b["key"] == "Alt_R" and b["modmask"] == 8 and not b["submap"]]


def restore():
    current = binds()
    if len(current) == 2 and set(current) == OWNED:
        reply = lua('hl.unbind("ALT + Alt_R"); o.bind_hold("ALT + Alt_R", '
                    '"Start dictation (push-to-talk)", "omarchy-dictation start", '
                    '"Stop dictation (push-to-talk)", "omarchy-dictation stop", '
                    '{ignore_mods=true}); return "restored"')
        if reply != "restored":
            raise RuntimeError(reply)
    journal = STATE / "indicator.json"
    if journal.exists():
        if json.loads(journal.read_text()) and not sw("audio", "show")["config"]["showRecordingIndicator"]:
            sw("audio", "set", "showRecordingIndicator", "true")
        journal.unlink()


def backup():
    dest = STATE / "original-config"
    if not dest.exists():
        with tempfile.TemporaryDirectory(dir=STATE) as directory:
            copy = Path(directory) / "config"
            shutil.copytree(Path.home() / ".config/superwhisper", copy, symlinks=True)
            copy.rename(dest)


def focused():
    window = json.loads(run("hyprctl", "-j", "activewindow"))
    address = window.get("address", "")
    if not window.get("xwayland") or not re.fullmatch(r"0x[0-9a-f]+", address):
        return None
    pid = window["pid"]
    try:
        executable = Path(f"/proc/{pid}/cmdline").read_bytes().split(b"\0")[0]
    except (FileNotFoundError, PermissionError):
        return None
    if re.split(br"[/\\]", executable)[-1].lower() == b"wow.exe":
        return address, pid
    return None


def key(target, name, control=False, shift=False):
    address, pid = target
    edges = [(name, "down"), (name, "up")]
    if shift:
        edges = [("Shift_L", "down"), *edges, ("Shift_L", "up")]
    if control:
        # Wine tracks the modifier key edges for a paste into its XWayland window.
        edges = [("Control_L", "down"), *edges, ("Control_L", "up")]
    sequence = ",".join("{" + literal(k) + "," + literal(state) + "}" for k, state in edges)
    script = f"""
local w = hl.get_active_window()
if not w or w.address ~= '{address}' or w.pid ~= {pid} then return 'blur' end
for _,edge in ipairs({{{sequence}}}) do
    local r = hl.dispatch(hl.dsp.send_key_state({{mods='',key=edge[1],
        state=edge[2],window='address:{address}'}}))
    if not r or not r.ok then return 'failed' end
end
return 'sent'
"""
    return lua(script) == "sent"


def chat_text(raw):
    text = " ".join(raw.split())
    if not text or text.startswith("/") or len(text.encode()) > 255:
        return ""
    return text


class Companion:
    def __init__(self, path):
        self.edges = path / "edges"
        self.edges.touch(mode=0o600)
        self.wav = path / "hold.wav"
        self.target = self.recorder = self.session = None
        self.device = None
        self.vocab = Vocabulary(STATE, sw)
        self.next_scan = 0.0

    def enter(self, target):
        current = binds()
        if len(current) != 2 or set(current) != ORIGINAL:
            raise RuntimeError("Expected the Omarchy Right Alt hold binding")
        if sw("shortcuts", "show")["shortcuts"]["hold"] != "RightAlt":
            raise RuntimeError("Expected RightAlt as the Superwhisper hold key")
        backup()
        audio = sw("audio", "show")
        self.device = audio.get("effectiveDevice")
        if audio["config"]["showRecordingIndicator"]:
            journal = STATE / "indicator.tmp"
            journal.write_text("true")
            journal.replace(STATE / "indicator.json")
            sw("audio", "set", "showRecordingIndicator", "false")
        self.target = target
        script = (HERE / "hold.lua").read_text()
        result = lua(f"(function(...) {script} end)({literal(self.edges)}); return 'bound'")
        if result != "bound" or set(binds()) != OWNED:
            raise RuntimeError("Could not install WoW hold binding")

    def leave(self):
        self.target = self.session = None
        try:
            if self.recorder:
                self.stop_capture()
            self.vocab.clear()
        finally:
            restore()

    def stop_capture(self):
        process, self.recorder = self.recorder, None
        process.send_signal(signal.SIGINT)
        try:
            process.wait(timeout=2)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()

    def press(self):
        if self.recorder or self.session is not None or self.target != focused():
            return
        status = sw("status")
        if not (status["ready"] or status.get("idle_sleeping")) or status["phase"] not in ("idle", "cancelled", "failed"):
            return
        capture = ["parecord", "--file-format=wav", "--channels=1", "--rate=16000"]
        if self.device:
            capture.extend(["--device", self.device])
        capture.append("--format=s16le")
        self.wav.unlink(missing_ok=True)
        self.recorder = subprocess.Popen([*capture, str(self.wav)])
        if not key(self.target, "F12", control=True, shift=True):
            self.stop_capture()

    def release(self):
        if not self.recorder:
            return
        self.stop_capture()
        if self.target == focused() and self.wav.is_file() and self.wav.stat().st_size > 44:
            self.session = sw("transcribe", str(self.wav))["session_id"]

    def tick(self):
        if time.monotonic() >= self.next_scan:
            self.next_scan = time.monotonic() + 1
            try:
                self.vocab.refresh(FOREVER / "Interface/AddOns/SuperwhisperWoW")
            except (FileNotFoundError, ValueError):
                # A reload can briefly leave the SavedVariables file incomplete.
                pass
        if self.session is None:
            return
        status = sw("status")
        if status["session_id"] != self.session or status["phase"] in ("failed", "cancelled"):
            self.session = None
        elif status["phase"] == "idle":
            self.session = None
            text = chat_text((status.get("last") or {}).get("text", ""))
            if text and self.target == focused():
                subprocess.run(["wl-copy", "--type", "text/plain;charset=utf-8", "--", text],
                               check=True, stdout=subprocess.DEVNULL, timeout=3)
                if key(self.target, "v", control=True):
                    time.sleep(0.12)
                    key(self.target, "Return")


def serve(app):
    instance = os.environ["HYPRLAND_INSTANCE_SIGNATURE"]
    with socket.socket(socket.AF_UNIX) as events, app.edges.open() as edges:
        events.connect(str(RUNTIME / "hypr" / instance / ".socket2.sock"))
        refresh = True
        pending = b""
        while True:
            if refresh:
                target = focused()
                if target != app.target:
                    if app.target:
                        app.leave()
                    edges.seek(0, 2)
                    if target:
                        app.enter(target)
            if app.target:
                for line in edges.readlines():
                    if line == "press\n":
                        app.press()
                    elif line == "release\n":
                        app.release()
                app.tick()
            refresh = False
            if select.select([events], [], [], 0.05 if app.target else None)[0]:
                data = events.recv(65536)
                if not data:
                    return
                lines = (pending + data).split(b"\n")
                pending = lines.pop()
                for line in lines:
                    if line.startswith(b"activewindowv2>>"):
                        address = line.split(b">>", 1)[1].decode()
                        if app.target and app.target[0] != "0x" + address:
                            app.leave()
                        refresh = True
                    elif line.startswith(b"configreloaded>>"):
                        if app.target:
                            app.leave()
                        refresh = True


def main():
    os.umask(0o077)
    STATE.mkdir(parents=True, exist_ok=True)
    with (RUNTIME / "superwhisper-wow.lock").open("w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        restore()
        with tempfile.TemporaryDirectory(dir=RUNTIME) as directory:
            app = Companion(Path(directory))
            app.vocab.clear()
            signal.signal(signal.SIGTERM, lambda *_: sys.exit(0))
            try:
                serve(app)
            finally:
                app.leave()


if __name__ == "__main__":
    main()
