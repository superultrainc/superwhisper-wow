import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

import wow


class Delivery(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.app = wow.Companion(Path(directory.name))
        self.app.target = ("0xabc", 42)
        self.app.session = 7
        self.app.next_scan = float("inf")

    def finish(self, text, session=7, target=None):
        status = {"session_id": session, "phase": "idle", "last": {"text": text}}
        with (patch.object(wow, "sw", return_value=status),
              patch.object(wow, "focused", return_value=target or self.app.target),
              patch.object(wow, "key", return_value=True) as key,
              patch.object(wow.subprocess, "run") as copy,
              patch.object(wow.time, "sleep")):
            self.app.tick()
            self.app.tick()
            return copy, key

    def test_one_message(self):
        copy, key = self.finish("hello\nthere")
        self.assertEqual(copy.call_args.kwargs["input"], "hello there")
        self.assertEqual([call.args[1] for call in key.call_args_list], ["v", "Return"])

    def test_journal_omits_message_and_whisper_recipient(self):
        with self.assertLogs(wow.LOG, "INFO") as journal:
            self.finish("Whisper PrivateName private body")
        self.assertEqual(journal.output, [
            "INFO:superwhisper-wow:Channel parsed: whisper",
            "INFO:superwhisper-wow:Delivery dispatched (paste and Enter)",
        ])

    def test_channel_prefix_boundaries(self):
        for text, channel in [("Raid Warning: move", "raid warning"), ("Party, ready", "party"),
                              ("reply now", "reply"), ("partying tonight", "current")]:
            with self.subTest(channel=channel):
                self.assertEqual(wow.requested_channel(text), channel)

    def test_clipboard_failure_is_redacted(self):
        with (patch.object(wow, "sw", return_value={"phase": "idle", "session_id": 7,
                                                    "last": {"text": "a private message"}}),
              patch.object(wow, "focused", return_value=self.app.target),
              patch.object(wow.subprocess, "run", side_effect=OSError("a private message")),
              self.assertLogs(wow.LOG, "ERROR") as journal):
            self.app.tick()
        self.assertEqual(journal.output, ["ERROR:superwhisper-wow:Delivery failed (clipboard: OSError)"])

    def test_focus_and_capture_lifecycle_logs(self):
        self.app.target = self.app.session = None
        target = ("0xabc", 42)
        responses = {
            ("shortcuts", "show"): {"shortcuts": {"hold": "RightAlt"}},
            ("audio", "show"): {"config": {"showRecordingIndicator": False}},
            ("status",): {"ready": True, "phase": "idle"},
        }
        recorder = Mock(returncode=0)
        with (patch.object(wow, "binds", side_effect=[list(wow.ORIGINAL), list(wow.OWNED), list(wow.OWNED)]),
              patch.object(wow, "sw", side_effect=lambda *args: responses[args]),
              patch.object(wow, "STATE", self.app.edges.parent),
              patch.object(wow, "backup"), patch.object(wow, "lua", side_effect=["bound", "restored"]),
              patch.object(wow, "focused", return_value=target), patch.object(wow, "key", return_value=True),
              patch.object(wow.subprocess, "Popen", return_value=recorder),
              patch.object(self.app.vocab, "clear"), self.assertLogs(wow.LOG, "INFO") as journal):
            self.app.enter(target)
            self.app.press()
            self.app.leave("focus lost")
        self.assertEqual([record.getMessage() for record in journal.records], [
            "WoW focused", "PTT taken over", "Recording started", "Leaving WoW context (focus lost)",
            "Recording stopped", "PTT restored",
        ])

    def test_other_window(self):
        copy, key = self.finish("hello", target=("0xaaa", 43))
        copy.assert_not_called()
        key.assert_not_called()

    def test_other_session(self):
        copy, key = self.finish("old dictation", 8)
        copy.assert_not_called()
        key.assert_not_called()

    def test_empty_command_and_overlong(self):
        for raw in ("", " /cast Hearthstone", "é" * 128):
            self.assertEqual(wow.chat_text(raw), "")


if __name__ == "__main__":
    unittest.main()
