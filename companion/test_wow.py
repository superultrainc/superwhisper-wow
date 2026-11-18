import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

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
        self.assertEqual(copy.call_args.args[0][-1], "hello there")
        self.assertEqual([call.args[1] for call in key.call_args_list], ["v", "Return"])

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
