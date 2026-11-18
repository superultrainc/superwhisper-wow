#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")"

forever=${WOW_FOREVER_DIR:-"$HOME/Games/battlenet/drive_c/Program Files (x86)/World of Warcraft/_forever_"}
[[ -f "$forever/Wow.exe" ]] || { echo "WoW Forever's Wow.exe is missing" >&2; exit 1; }
data=${XDG_DATA_HOME:-"$HOME/.local/share"}/superwhisper-wow
addon="$forever/Interface/AddOns/SuperwhisperWoW"
for tool in python3 superwhisper hyprctl parecord wl-copy; do
    command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; exit 1; }
done
install -d "$data" "$addon"
install -m 644 SuperwhisperWoW/* "$addon/"
install -m 644 companion/{wow.py,vocabulary.py,hold.lua} "$data/"
printf 'Installed the addon in %s\nRun the companion with python3 "%s/wow.py"\n' "$addon" "$data"
