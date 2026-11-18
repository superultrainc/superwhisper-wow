# Superwhisper for WoW

Hold Right Alt to dictate one chat message in World of Warcraft Forever. The chat box shows a pulsing microphone while you speak. Release to transcribe, paste and send. Outside WoW, Right Alt returns to Omarchy dictation.

## Install on Omarchy

Install Superwhisper, select it as the Omarchy dictation provider, and configure Right Alt as its hold shortcut. This version supports the stock Omarchy press and release bindings on Hyprland with Lua configuration. It uses Python 3, PulseAudio's `parecord` and `wl-copy`.

Run `./install.sh` from this checkout. The installer copies the addon into the Forever Wine prefix under `~/Games/battlenet` and prints the command to start the companion. For a different Forever installation, export `WOW_FOREVER_DIR` before installing and running.

Enable **Superwhisper** in WoW's AddOns list and reload the UI. Under **Key Bindings → Superwhisper**, check that **Dictate chat** uses Ctrl+Shift+F12. The addon assigns Ctrl+Shift+F12 for the session if that key is free. Start the companion in your desktop session and leave it running. Ctrl+C stops it and restores dictation settings.

## Speak a message

Start with **party**, **raid**, **raid warning**, **guild**, **officer**, **say**, **yell**, **whisper Name**, or **reply** to pick the channel. For example, “party, ready when you are” sends “ready when you are” to party chat. With no prefix, WoW uses the current channel. Start with the chat box closed.

Type `/sww sync` in game to collect character names and reload the UI. The companion watches the most recently saved account vocabulary and combines it with the addon's core terms while WoW is focused. It removes terms it introduced when you switch away. The original Superwhisper configuration backup is under `~/.local/state/superwhisper-wow/original-config`.

## Development

Run `make test` with Python 3 and Lua 5.1 installed. Run `make run` from a Hyprland desktop session to use the companion from the checkout.
