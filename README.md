<h1 align="center">Superwhisper for WoW</h1>

<p align="center">Hold a key, say the channel and your message, and release to send it in World of Warcraft Forever.</p>

<p align="center">
  <a href="https://github.com/tcballard/omarchy-badges"><img src="https://raw.githubusercontent.com/tcballard/omarchy-badges/85f859029e236e784e7b05ada6dbe73506d07a91/badges/v1/built-for-omarchy.svg" alt="Built for Omarchy"></a>
  <img src="https://img.shields.io/badge/Linux-Omarchy-333" alt="Linux on Omarchy">
  <img src="https://img.shields.io/badge/Windows-x64-0078d4" alt="Windows x64">
  <img src="https://img.shields.io/badge/WoW-Forever-805621" alt="WoW Forever">
</p>

<p align="center"><a href="#install">Install</a> · <a href="#usage">Usage</a> · <a href="#how-it-works">How it works</a> · <a href="#privacy">Privacy</a> · <a href="#faq">FAQ</a></p>

<p align="center"><img src="docs/media/dictate.gif" alt="In-game captures of the Listening indicator, the private whisper edit box and the sent chat line"></p>

## Usage

Start with WoW focused and chat closed. Hold **Right Alt** on Omarchy or your **Superwhisper push-to-talk shortcut** on Windows. Speak one message, then release. Type `/sww` in game for help.

### Dictate to chat

Say “whisper”, the character’s name, then your message. WoW shows the recipient in the purple **Tell** header.

![The whisper recipient is obscured. The dictated message reads “the microphone check is complete.”](docs/media/whisper.png)

### Choose a channel by voice

Say “party, the microphone check is complete” and only the words after “party” become the message. You can also start with **raid**, **raid warning**, **guild**, **officer**, **say**, **yell**, **whisper** or **reply**. Omit the prefix to use the current chat channel.

![The blue Party header and dictated message in the native chat edit box](docs/media/party.png)

### See when WoW is listening

A pulsing mic and **Listening…** appear inside the chat box while you hold the key.

![The microphone pulses beside Listening… in the WoW chat box](docs/media/listening.gif)

### Teach Superwhisper character names

Type `/sww sync`. The addon counts names from guild, friends, group members and recent whispers, then reloads the UI to save them. On Omarchy the companion makes those names available to Superwhisper while WoW is focused. On Windows, import the supplied word list during setup.

### Release to send

Dictation always auto-sends. Once Superwhisper finishes, the companion pastes the text and presses Enter. Start with a whisper to yourself to try it privately.

![The delivered self-whisper in the chat history with the name obscured](docs/media/whisper-sent.png)

## Install

### Omarchy

1. Install Superwhisper, select it as the Omarchy dictation provider and set its hold shortcut to **Right Alt**.
2. From this checkout, run `./install.sh`. It installs the addon in the default Forever Wine prefix and enables the companion for your graphical session. For another install, set `WOW_FOREVER_DIR` to the folder containing `Wow.exe` first.
3. Enable **Superwhisper** in WoW’s AddOns list and reload the UI. Under **Key Bindings → Superwhisper**, keep **Ctrl+Shift+F12** free for **Dictate chat**.

### Windows x64

Windows CI exercises the companion and installer with a test window. In-game verification on Windows is pending.

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


## How it works

1. The desktop companion detects the focused WoW client and opens the addon's chat binding on a push-to-talk press.
2. On release, the Linux companion submits the recording to Superwhisper. The Windows companion reads the completed WoW-mode recording from Superwhisper on disk.
3. The addon translates the spoken prefix into WoW’s native chat channel. The companion sends Enter to the focused client.

## Privacy

The addon and companion work locally. For offline transcription, choose an offline mode in Superwhisper. A sent chat message goes to WoW and its intended recipients.

<details>
<summary>Removal, dependencies and privacy details</summary>

**Omarchy.** Requires the stock Hyprland Lua hold bindings, Python 3, Superwhisper, `parecord`, `wl-copy` and a systemd user session. To remove the companion, run `systemctl --user disable --now superwhisper-wow.service`, remove its unit from the user systemd directory, then run `systemctl --user daemon-reload`. Remove the installed `superwhisper-wow` user data directory and the game's `Interface/AddOns/SuperwhisperWoW` folder. A backup of the original Superwhisper configuration lives in the `superwhisper-wow/original-config` user state directory.

**Windows.** Requires Windows x64, the inbox .NET Framework, PowerShell and Superwhisper. Run the following in PowerShell to stop the companion and remove the login entry, companion and addon. Remove the mode and imported words separately in Superwhisper if desired.

```powershell
& "$env:LOCALAPPDATA\SuperwhisperWoW\uninstall.ps1"
```

**Local data.** The addon stores the deduplicated character names collected by `/sww sync` in its account SavedVariables. The Linux journal records focus, recording and delivery events and the channel. It omits transcript text and recipients. To inspect it, run `journalctl --user -u superwhisper-wow.service`. Superwhisper manages its own recording history and processing mode. On Windows the companion reads Superwhisper’s local recording metadata and restores the clipboard after delivery.

</details>

## FAQ

**Is this allowed by Blizzard?** This project has no Blizzard approval to cite. Its scope is chat dictation: one deliberate press, one message, one focused client. It does not automate gameplay or broadcast input to other clients. Blizzard sets the rules for third-party software. [Blizzard End User License Agreement](https://www.blizzard.com/en-us/legal/08b946df-660a-40e4-a072-1fbde65173b1/blizzard-end-user-license-agreement)

**Does it work on Retail or Classic?** Forever is the tested client on Omarchy. Other client versions are unverified.

**Why don’t names sync on Windows?** The Windows companion reads Superwhisper’s files. It leaves vocabulary changes to Superwhisper’s own importer. The supplied CSV contains static WoW terms.

**Can I dictate in another language?** Choose a language supported by your Superwhisper mode for the message. Speak the channel prefix in English.

## Development

Run `make test` with Python 3 and Lua 5.1 installed. Stop the installed service before running `make run` from a Hyprland desktop session to use the companion from the checkout.

For a Windows build, run `windows/build.ps1` from PowerShell on Windows x64. Run `windows/tests/unit.ps1` for unit tests and `windows/tests/desktop.ps1` under a fresh Windows account for the desktop fixture. The fixture refuses to use an existing Superwhisper data directory. The Windows workflow also installs and uninstalls the package.
