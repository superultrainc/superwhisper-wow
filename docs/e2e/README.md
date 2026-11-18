# WoW Forever route test on goose

On 2026-11-18, Superwhisper recorded a PipeWire virtual source fed by eSpeak WAVs. Each case used a virtual Right Alt press and release with the game focused. The test checked the actual capture stream's source. A test-only guard intercepted the companion's final Enter. It allowed Enter only after the game showed the character's own whisper header. All previews were cleared with Escape.

| Spoken route | Native edit-box result | Evidence |
| --- | --- | --- |
| Whisper | Purple Tell header for the current character. Sent. | [Edit box](whisper.png) · [Sent message](whisper-sent.png) |
| Plain speech | Retained the same private conversation. Sent. | [Edit box](plain.png) · [Sent message](plain-sent.png) |
| Party | Blue Party header and body | [Edit box](party.png) |
| Raid | Orange Raid header and body | [Edit box](raid.png) |
| Raid warning | Red-orange Raid Warning header and body | [Edit box](raid-warning.png) |
| Guild | Green Guild header and body | [Edit box](guild.png) |
| Officer | Light-green Officer header and body | [Edit box](officer.png) |
| Say | White Say header and body | [Edit box](say.png) |
| Yell | Red Yell header and body | [Edit box](yell.png) |
| Reply | Purple Tell header for the last whisper correspondent | [Edit box](reply.png) |

The channel cases used “the microphone check is complete.” The plain-speech case used “the plain message is ready.” The recognizer returned “the plane message is ready,” which the companion delivered verbatim. A temporary Superwhisper replacement mapped “test character” to the character's name for the whisper case. The screenshots are crops of the live edit box or the outgoing message.

The installed user service was the build from PR #7. A fresh systemd user-manager test transaction for graphical-session.target queued superwhisper-wow.service. Starting a separate target through the installed graphical-session enablement link launched a cold companion process, which detected the focused WoW client and took over PTT. The desktop session stayed running. Physical keyboard and microphone hardware and server delivery to public or group channels were outside this test.

The stock binding initially failed to open the edit box after reconnecting. The fix installs a session-local binding on world entry when the chord is free and defers protected binding changes during combat. After a second disconnect, reconnect and character login, a new recorded party utterance opened the correct edit box without a UI reload. [Reconnect check](reconnect.png)
