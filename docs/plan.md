# Superwhisper for WoW: plan

Agreed with Neil in a grill session, 2026-11-17.

## Product

- Dictate World of Warcraft chat by voice. Chat only: no voice commands, keybind actions or accessibility automation.
- **WoW Forever first**, then Retail and the Classic flavours (mostly a `.toc` interface bump).
- **Omarchy Linux (Hyprland) first**, Windows later.
- Self-contained **public** repo `superultrainc/superwhisper-wow` (Ryan Hughes from the Omarchy core team may contribute). The installed **Superwhisper Linux** is the core: recording, transcription, vocabulary, pill.
- **Zero changes to superwhisper-linux.** If a hook there turns out to be required, stop and ask Neil before opening any PR in that repo.

## ToS line

One press of the shortcut → one chat message, to one client. No macros, no sequences of game actions, no multiboxing. Chat only.
(Blizzard has no explicit ruling on voice tools; "one key, one action, one client" is the working rule. Input broadcasting is banned.)

## Flow

1. The normal Superwhisper push-to-talk (Right Alt on Hyprland) behaves differently while WoW is focused.
2. **Press:** the companion sends one key bound to the addon's entry under *Key Bindings → Superwhisper*. The addon opens WoW's chat edit box in a **Listening…** state (the in-game dictation element).
3. **Release:** Superwhisper transcribes → the companion pastes into the box → Enter. **Always auto-send.** No cancel/review step.
4. **Spoken routing** (first words only, stripped from the message), using WoW's own slash commands so the header and colour change natively:

   | Say | Sends |
   |---|---|
   | party … | `/p` |
   | raid … | `/raid` |
   | raid warning … | `/rw` |
   | guild … | `/g` |
   | officer … | `/o` |
   | say … | `/s` |
   | yell … | `/y` |
   | whisper Name … | `/w Name` |
   | reply … | `/r` |

   No spoken channel → WoW's last-used channel.

## Addon (Lua, `SuperwhisperWoW`)

- Listening state on the chat edit box.
- Key Bindings entry the companion triggers.
- Small core word list: classes, races, zones, dungeons/raids, common terms (LFG, DPS, tank, heals, OOM, CC, …). Core nouns only; keep it small.
- `/sww sync`: collects guild roster, friends, party/raid, this session's whisperers, Battle.net character names if the Forever API exposes them → SavedVariables → `/reload` to flush.
- The addon is the single source of truth for WoW vocabulary.

## Companion (`superwhisper-wow` on Linux)

- Follows the focused window over Hyprland's socket; detects WoW (`Wow.exe` under Wine/Proton, XWayland).
- While WoW is focused: takes over push-to-talk and drives `superwhisper record`/CLI itself; pastes and sends.
- Loads the addon's word list + synced names into Superwhisper vocabulary (`superwhisper vocab …`) when WoW gains focus, removes them when it loses focus. Vocabulary snapshots per recording and doesn't reload models, so the swap is cheap.
- Watches the SavedVariables file for new synced names.
- Installer copies the addon into WoW Forever's `Interface/AddOns`. CurseForge/Wago later.

## Process

1. **Mocks first** (`mocks/superwhisper-wow-mocks.html`). **Approved 2026-11-18:** Listening style A (native box, pulsing mic + "Listening…"), Superwhisper pill hidden while WoW is focused, WoW's own channel headers, `/sww` output in system yellow. Any new visual element gets a mock and Neil's approval first.
2. **Build:** Codex `gpt-6.2-astra` (`codex exec -m gpt-6.2-astra`) on **goose**, in a rex session under `systemd-inhibit`, with a log. Long runs on goose are OK while it's docked; it can still sleep, so milestones stay resumable.
3. In-game checks on goose are announced first and never steal focus.
4. One branch + PR per milestone, default reviewer `austensutherland`. Runs autonomously; Neil is asked only for visuals, merges and anything public.

## Milestones

1. ~~Mock page → approval.~~ Done.
2. Companion skeleton: focus detection, push-to-talk takeover, `superwhisper record` → paste → Enter into WoW chat on goose (prove zero-change integration).
3. Addon: Key Binding, Listening state, channel routing.
4. Vocabulary: core word list, focus-scoped load/unload, `/sww sync` + SavedVariables watcher.
5. Installer + README.
6. Later: Windows, Retail/Classic `.toc`s, CurseForge/Wago.

## Facts from goose (2026-11-17)

- WoW Forever: `~/Games/battlenet/drive_c/Program Files (x86)/World of Warcraft/_forever_` (plain Wine prefix, no Lutris/Steam).
- On goose: `superwhisper`, `codex` 0.169.0, `hyprctl`, `wtype`, `wl-copy`, `flk`. No `ydotool`.
- Superwhisper CLI: `record|start|stop`, `cancel`, `status`, `transcribe FILE [--insert]`. `status` JSON exposes `last.text`, `last.delivery`, `last.route` (fcitx5) and the shortcuts (hold `RightAlt`).
- Start Codex there with `flk new goose <folder> "<task>" codex` or `rex -S goose run --focus=false -- …`.

## Open facts to verify on goose

- WoW Forever interface version (`.toc`).
- Whether `start`/`stop` can skip pasting so the companion owns delivery (else `transcribe FILE` without `--insert`); decides if zero-change works.
- Paste into WoW's edit box over XWayland (Ctrl+V) and whether a pasted `/p ` switches the header.
- Whether Right Alt reaching WoW as Alt causes side effects.
