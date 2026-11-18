# Superwhisper for WoW

Hold your Superwhisper shortcut to dictate one chat message in World of Warcraft Forever. The chat box shows a pulsing microphone while you speak. Release to transcribe, paste and send. Outside WoW, use Superwhisper normally.

## Install on Windows x64

1. In Superwhisper preferences, open **Modes → Create mode**. Select the **Voice** preset and name the mode **WoW**.
2. Under **Activate for apps**, select World of Warcraft. If it is missing, search for it, select **Add from file…**, and choose the game version's `Wow.exe`. Expand **Advanced settings → Auto paste** and choose **Off** explicitly.
3. Under **Configuration → Keyboard Shortcuts → Push to Talk**, assign your hold shortcut. **Control+Space** can also be the toggle shortcut. Hold it for at least one second and release the entire chord. Leave Ctrl+Shift+F12 free for the addon. An Alt hold also needs Alt+Ctrl+Shift+F12 free.
4. Download the **SuperwhisperWoW-windows-x64** artifact from a successful [Windows Actions run](https://github.com/superultrainc/superwhisper-wow/actions/workflows/windows.yml). Extract the entire ZIP. Open PowerShell in the extracted folder and run the installer with the directory containing your actual `Wow.exe`.

   ```powershell
   .\install.ps1 -WowDirectory 'C:\Games\World of Warcraft\_forever_'
   ```

   The installer copies the addon, starts the companion, and registers it for sign-in on your Windows account. The executable uses the .NET Framework included in Windows 10 and 11. No Python install is needed.
5. Enable **Superwhisper** in WoW's AddOns list and reload the UI. Start with chat closed and the game focused. Run the game and companion as a regular user. The companion reads the completed WoW-mode recording and sends it to the same focused client. Moving to another window cancels delivery.
6. To import the core terms, open **Vocabulary → Import vocabulary from CSV** in Superwhisper and select `wow-vocabulary.csv` from the ZIP or `%LOCALAPPDATA%\SuperwhisperWoW`. This is a one-time, global import. Names collected by `/sww sync` do not reach Superwhisper on Windows.

To uninstall, run the following command in PowerShell. It stops the companion and removes its sign-in entry and installed files. The mode and imported words can be removed separately in Superwhisper.

```powershell
& "$env:LOCALAPPDATA\SuperwhisperWoW\uninstall.ps1"
```

## Install on Omarchy

Install Superwhisper, select it as the Omarchy dictation provider, and configure Right Alt as its hold shortcut. This version supports the stock Omarchy press and release bindings on Hyprland with Lua configuration. It uses Python 3, PulseAudio's `parecord` and `wl-copy`.

Run `./install.sh` from this checkout. The installer copies the addon into the Forever Wine prefix under `~/Games/battlenet` and enables and starts the systemd user service for your graphical session. For a different Forever installation, export `WOW_FOREVER_DIR` before installing.

Enable **Superwhisper** in WoW's AddOns list and reload the UI. Under **Key Bindings → Superwhisper**, check that **Dictate chat** uses Ctrl+Shift+F12. The addon assigns Ctrl+Shift+F12 for the session if that key is free. Use `systemctl --user status superwhisper-wow.service` to check the companion and `journalctl --user -fu superwhisper-wow.service` to follow its log. Delivery entries report key dispatch to the focused client. The journal includes the spoken channel prefix and omits message text and recipient names. To stop the companion and restore dictation settings, run `systemctl --user stop superwhisper-wow.service`.

## Speak a message

Start with **party**, **raid**, **raid warning**, **guild**, **officer**, **say**, **yell**, **whisper Name**, or **reply** to pick the channel. For example, “party, ready when you are” sends “ready when you are” to party chat. With no prefix, WoW uses the current channel. Start with the chat box closed.

On Linux, type `/sww sync` in game to collect character names and reload the UI. The companion watches the most recently saved account vocabulary and combines it with the addon's core terms while WoW is focused. It removes terms it introduced when you switch away. The original Superwhisper configuration backup is under `~/.local/state/superwhisper-wow/original-config`.

## Development

Run `make test` with Python 3 and Lua 5.1 installed. Stop the installed service before running `make run` from a Hyprland desktop session to use the companion from the checkout.

For a Windows build, run `windows/build.ps1` from PowerShell on Windows x64. Run `windows/tests/unit.ps1` for unit tests and `windows/tests/desktop.ps1` under a fresh Windows account for the desktop fixture. The fixture refuses to use an existing Superwhisper data directory. The Windows workflow also installs and uninstalls the package.
