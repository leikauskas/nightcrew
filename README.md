<p align="center">
  <img src="docs/assets/banner.png" alt="Nightcrew. Awake while your agents work. Asleep when they're done." width="100%">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%C2%B7%2011-F0D7FF?style=flat-square&labelColor=26282B" alt="Windows 10 and 11">
  <img src="https://img.shields.io/badge/license-MIT-F0D7FF?style=flat-square&labelColor=26282B" alt="MIT licence">
</p>

Nightcrew keeps a Windows laptop awake with the lid closed while Claude Code or
Codex works. When the work stops, it puts your power settings back and the
laptop sleeps as usual. With no agent at work, the lid works as usual.

It is one 1.7 MB exe in the tray. It uses less than 1% of one CPU core. There
are no hooks or plugins to set up.

https://github.com/user-attachments/assets/200be945-4714-42fb-981e-1815e68ddf0e

## Install

1. Download `nightcrew.exe` from [Releases](../../releases/latest).
2. Put it in a permanent folder, for example `%LOCALAPPDATA%\Programs\Nightcrew`.
3. Run it. A lantern appears in the tray and the panel opens once. From then
   on, Nightcrew starts with Windows.

The exe is not code-signed. On the first run, Windows shows "Windows protected
your PC". Click **More info**, then **Run anyway**. If Smart App Control is on,
Windows can block the exe.

To check the download, compare its hash with `nightcrew.exe.sha256` on the
release, or verify the GitHub build attestation:

```powershell
Get-FileHash .\nightcrew.exe
gh attestation verify .\nightcrew.exe -R leikauskas/nightcrew
```

## How it works

<img src="docs/assets/night.png" alt="One night in four steps: an agent starts, you close the lid, the work ends, it sleeps" width="100%">

1. **An agent starts.** Every 10 seconds, Nightcrew looks for a Claude Code or
   Codex session file that grows, a build or script that an agent started, or
   high GPU use by an agent or its tools. It also reads the Claude Code status
   files in `~\.claude\sessions`, so a long, quiet command counts too. The
   lantern lights up.
2. **Nightcrew saves your power settings,** then sets the lid action to Do
   nothing and the sleep timer to Never. On battery, the timer is your
   lid-closed limit plus one hour.
3. **You close the lid.** The laptop continues to work and locks, unless your
   lid action is Do nothing or **Lock when lid closes** is off.
4. **The work stops.** After 5 minutes with no sign of work, Nightcrew puts your
   settings back.
5. **The laptop sleeps.** After 5 minutes with no input, the laptop does your
   lid action. If it is still awake 5 minutes later, Nightcrew hibernates it.

With the lid closed, Nightcrew hibernates the laptop at 20% battery or after 8
hours with no input, also during work. You can change both limits.

A game or a video export does not count as work. In `~\.claude`, Nightcrew
opens only the status files. It reads transcripts for size only and never
touches the login `.key` files. Nightcrew has no network code.

## Your settings

Nightcrew saves your values in `%LOCALAPPDATA%\Nightcrew\restore.json` and in
the registry under `HKCU\Software\Nightcrew\Restore`. It puts them back when
the work stops, when you pause or quit, when you sign out, and on the next start
after a crash. If Nightcrew is killed, a watchdog task puts them back within 5
minutes.

If you change the lid action or sleep timer during work, Nightcrew saves your
new value and keeps its own until the work stops. If both saved copies are
lost, Nightcrew sets the Windows defaults for your plan. It can do this only
when the lid-closed limit is on, because it finds its own values by a 7-second
mark on the battery timer.

**Efficiency mode** is off by default. When it is on, apps the agents do not
need run at low priority (Windows EcoQoS) after 5 minutes with no input. Agents,
their connected processes, terminals, editors, games and browsers with a Claude
or Codex connector keep full speed. All apps go back to normal when you return
or the work stops. Nightcrew never suspends or kills an app.

If your organization locks the power settings, Nightcrew does nothing and tells
you.

## The panel

<img src="docs/assets/panel-working.png" alt="The Nightcrew panel while Claude Code and Codex are working" width="352" align="right">

Click the lantern, or start `nightcrew.exe` again. The panel shows what keeps
the laptop awake, the battery, and what happens when the work stops.

- **Hold awake** keeps the laptop awake until you turn it off. Use it for work
  that Nightcrew cannot detect.
- **Pause** gives the lid back to Windows until you resume.
- **Settings** (also on right-click) has the limits and options above.
- **Stay awake for approvals**: with the lid closed, a Claude Code session that
  waits for your answer counts as work, so you can answer from your phone.
- **Keep at full speed** in Settings: add an app here if it does not work
  correctly after you were away.

<br clear="right">

<details>
<summary><b>Other agents and advanced settings</b></summary>

Nightcrew detects Claude Code and Codex. For another agent, use **Hold awake**,
or add the agent to the settings file:

1. Quit Nightcrew.
2. Edit `%LOCALAPPDATA%\Nightcrew\settings.json`:
   - Add the agent's process name to `AgentProcessNames`. Programs that it
     starts then count as agent tools.
   - If the agent does its work in its own process, also add it to
     `SelfCpuAgentNames`.
   - Add the folder where it writes `*.jsonl` session files to
     `TranscriptRoots`.
3. Start Nightcrew.

A key that you leave out keeps its default. The log, `nightcrew.log`, is in the
same folder.

To see what Nightcrew would do without changes, quit Nightcrew, run
`.\nightcrew.exe --dry-run` in its folder, and read the log.

| Key | Default | Meaning |
|---|---|---|
| `GraceMinutes` | 5 | Minutes that work stays live after the last sign of work |
| `BatteryFloorPercent` | 20 | Hibernate at this charge, lid closed, on battery. 0 = off |
| `LidClosedCapHours` | 8 | Hibernate after this many hours, lid closed, no input. During work, the battery sleep timer is this plus 1 hour. 0 = off (timer Never) |
| `AwayMinutes` | 5 | Minutes with no input before: the lid action runs, lid-closed time counts, the PC locks if work starts with the lid closed, apps go into Efficiency mode |
| `LockOnLidClose` | true | Lock the PC when the lid closes |
| `StartAtLogin` | true | Start Nightcrew when you sign in |
| `DryRun` | false | Log what Nightcrew would do, but do nothing |
| `TranscriptRoots` | Claude Code and Codex folders | Folders with session files to watch |
| `IgnorePathContains` | `claude-mem-observer` | Ignore session files whose path contains this |
| `AgentProcessNames` | `claude`, `codex` | Agent processes. Programs they start count as agent tools |
| `SelfCpuAgentNames` | `codex` | Agents whose own CPU use counts |
| `SelfCpuPercent` | 2 | CPU limit for those agents, % of one core |
| `ToolCpuPercent` | 10 | CPU limit for agent tools, % of one core |
| `GpuPercent` | 30 | GPU engine limit, %, for agents and their tools |
| `SampleSeconds` | 10 | Seconds between checks |
| `EfficiencyWhileAway` | false | Efficiency mode for apps the agents do not need while you are away |
| `LeaveAlone` | none | Process names never put into Efficiency mode (Settings > Keep at full speed) |
| `LeaveAloneLabels` | none | Names that Settings shows for `LeaveAlone` entries |
| `ConnectorHostNames` | `chrome-native-host`, `extension-host` | Browser connector helpers. Their browser is left alone |
| `StayAwakeForApprovals` | true | Lid closed: a Claude Code session that waits for you counts as work |
| `ClaudeSessionsDir` | `%USERPROFILE%\.claude\sessions` | Folder with the Claude Code status files |

</details>

## Update

1. Right-click the lantern, then click **Quit Nightcrew**. This puts your
   settings back.
2. Replace the exe with the new one.
3. Start it.

From a script, run `Start-Process .\nightcrew.exe -ArgumentList '--exit' -Wait`
in its folder. PowerShell does not wait for `.\nightcrew.exe --exit`.

## Uninstall

1. Quit Nightcrew.
2. In its folder, run `.\nightcrew.exe --uninstall`. This puts your settings
   back, takes all apps out of Efficiency mode, and removes the startup entry
   and the watchdog task. When your settings are back, it also removes the
   registry key `HKCU\Software\Nightcrew`.
3. Delete the exe. To remove the settings and the log, also delete
   `%LOCALAPPDATA%\Nightcrew`.

Do not delete only the exe. The watchdog task stays behind.

## Credits

- The cream ink, serif headline and lavender come from Wispr Flow's design
  language.
- Type is [EB Garamond](https://github.com/octaviopardo/EBGaramond12) and
  [Figtree](https://github.com/erikdkennedy/figtree), both under the SIL Open
  Font License. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License

[MIT](LICENSE)
