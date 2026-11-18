#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")"

forever=${WOW_FOREVER_DIR:-"$HOME/Games/battlenet/drive_c/Program Files (x86)/World of Warcraft/_forever_"}
[[ -f "$forever/Wow.exe" ]] || { echo "WoW Forever's Wow.exe is missing" >&2; exit 1; }
data=${XDG_DATA_HOME:-"$HOME/.local/share"}/superwhisper-wow
addon="$forever/Interface/AddOns/SuperwhisperWoW"
for tool in python3 superwhisper hyprctl parecord wl-copy systemctl; do
    command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; exit 1; }
done
install -d "$data" "$addon"
install -m 644 SuperwhisperWoW/* "$addon/"
install -m 644 companion/{wow.py,vocabulary.py,hold.lua} "$data/"
unit_dir=${XDG_CONFIG_HOME:-"$HOME/.config"}/systemd/user
install -d "$unit_dir"
python3 - "$data/wow.py" "$forever" "$unit_dir/superwhisper-wow.service" <<'PYUNIT'
import os
from pathlib import Path
import sys

def quote(value):
    return '"' + value.replace('\\', '\\\\').replace('"', '\\"').replace('%', '%%').replace('\n', '\\n') + '"'

script, game, destination = sys.argv[1:]
unit = Path("superwhisper-wow.service.in").read_text().splitlines()
for index, line in enumerate(unit):
    if "@COMPANION@" in line:
        unit[index] = line.replace("@COMPANION@", quote(os.path.abspath(script)).replace("$", "$$"))
    elif "@GAME_ENV@" in line:
        unit[index] = line.replace("@GAME_ENV@", quote("WOW_FOREVER_DIR=" + os.path.abspath(game)))
Path(destination).write_text("\n".join(unit) + "\n")
PYUNIT
systemctl --user daemon-reload
if [[ $(systemctl --user show superwhisper-wow-live.service -p Transient --value) == yes ]]; then
    systemctl --user stop superwhisper-wow-live.service
fi
systemctl --user enable superwhisper-wow.service
systemctl --user restart superwhisper-wow.service
printf 'Installed the addon in %s and started superwhisper-wow.service\n' "$addon"
