import csv
import tempfile
import unittest
from pathlib import Path

from vocabulary import Vocabulary, read_names, read_core


class VocabularyTests(unittest.TestCase):
    def test_windows_import_matches_addon_core(self):
        repo = Path(__file__).resolve().parents[1]
        with (repo / "windows/wow-vocabulary.csv").open(newline="") as stream:
            rows = list(csv.DictReader(stream))
        self.assertEqual({row["word"] for row in rows}, read_core(repo / "SuperwhisperWoW"))
        self.assertTrue(all(row["replacement"] == "" for row in rows))

    def test_saved_variables(self):
        src = 'SuperwhisperWoWDB = { ["names"] = {\n"Thráll", -- [1]\n[2]="Jaina-Realm", }, }'
        self.assertEqual(read_names(src), {"Thráll", "Jaina-Realm"})
        for invalid in (src[:-2], src + "run()", src.replace('"Thráll"', 'f()')):
            with self.assertRaises(ValueError):
                read_names(invalid)

    def test_swap_and_recovery(self):
        words = {"dps", "Personal"}

        def cli(_, cmd, *args):
            if cmd == "show":
                return {"config": {"terms": list(words)}}
            if cmd == "import":
                with open(args[0]) as batch:
                    words.update(row["word"] for row in csv.DictReader(batch))
            else:
                words.remove(args[0])

        with tempfile.TemporaryDirectory() as state:
            vocab = Vocabulary(Path(state), cli)
            vocab.reconcile({"DPS", "Jaina", "Tank"})
            words.add("New personal word")
            vocab.reconcile({"Tank", "Thrall"})
            self.assertEqual(words, {"dps", "Personal", "Tank", "Thrall", "New personal word"})
            Vocabulary(Path(state), cli).clear()
            self.assertEqual(words, {"dps", "Personal", "New personal word"})
