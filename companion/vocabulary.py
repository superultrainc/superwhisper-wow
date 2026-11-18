"""Track only vocabulary terms introduced for the focused WoW client."""
import csv
import tempfile
import json
import os
from pathlib import Path
import re

DEFAULT_FOREVER = Path.home() / "Games/battlenet/drive_c/Program Files (x86)/World of Warcraft/_forever_"
FOREVER = Path(os.environ.get("WOW_FOREVER_DIR", DEFAULT_FOREVER))
TERM = r'"([^"\\\x00-\x1f]{1,128})"'
ENTRY = re.compile(r"\s*(?:\[\d+\]\s*=\s*)?" + TERM + r"\s*[,;]?")
COMMENTS = re.compile(r"--[^\n]*")
SAVED = re.compile(r'\s*SuperwhisperWoWDB\s*=\s*\{\s*(?:\["names"\]|names)\s*=\s*\{([^{}]*)\}\s*,?\s*\}\s*', re.S)


def terms(body):
    words = set()
    pos = 0
    while body[pos:].strip():
        item = ENTRY.match(body, pos)
        if not item:
            raise ValueError("Unrecognized vocabulary entry")
        words.add(item.group(1))
        pos = item.end()
    return words


def read_names(text):
    table = SAVED.fullmatch(COMMENTS.sub("", text))
    if not table:
        raise ValueError("Incomplete SavedVariables table")
    return terms(table[1])


def read_core(addon):
    text = (addon / "Core.lua").read_text()
    table = re.search(r"addon\.core\s*=\s*\{([^{}]*)\}", text)
    if not table:
        raise ValueError("Missing core vocabulary")
    return terms(table[1])


class Vocabulary:
    def __init__(self, state, cli):
        self.file = state / "terms.json"
        self.cli = cli
        self.owned = set(json.loads(self.file.read_text())) if self.file.exists() else set()
        self.signature = None

    def save(self):
        if self.owned:
            tmp = self.file.with_suffix(".tmp")
            tmp.write_text(json.dumps(sorted(self.owned)))
            tmp.replace(self.file)
        else:
            self.file.unlink(missing_ok=True)

    def reconcile(self, wanted):
        config = self.cli("vocab", "show")["config"]
        current = config["terms"]
        pronounced = {item["word"].casefold() for item in config.get("pronunciations", [])}
        existing = {term.casefold() for term in current} | pronounced
        desired = {term.casefold(): term for term in sorted(wanted)}
        for term in sorted(self.owned):
            if term.casefold() not in desired:
                if term in current and term.casefold() not in pronounced:
                    self.cli("vocab", "remove", term)
                self.owned.remove(term)
                self.save()
        additions = [desired[key] for key in sorted(desired.keys() - existing)]
        if additions:
            self.owned.update(additions)
            self.save()
            with tempfile.NamedTemporaryFile(mode="w", newline="", encoding="utf-8") as batch:
                writer = csv.writer(batch)
                writer.writerow(["word"])
                writer.writerows((term,) for term in additions)
                batch.flush()
                self.cli("vocab", "import", batch.name)

    def refresh(self, addon, forever=FOREVER):
        files = (forever / "WTF/Account").glob("*/SavedVariables/SuperwhisperWoW.lua")
        latest = max(files, key=lambda path: path.stat().st_mtime_ns, default=None)
        signature = (latest, latest.stat().st_mtime_ns if latest else None,
                     (addon / "Core.lua").stat().st_mtime_ns)
        if signature == self.signature:
            return
        wanted = read_core(addon)
        if latest:
            wanted.update(read_names(latest.read_text(encoding="utf-8-sig")))
        self.reconcile(wanted)
        self.signature = signature

    def clear(self):
        if self.owned:
            self.reconcile(set())
        self.signature = None
