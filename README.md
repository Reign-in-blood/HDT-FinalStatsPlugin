# HDT-FinalStatsPlugin

**HDT-FinalStatsPlugin** is a plugin for **Hearthstone Deck Tracker**, designed for **Battlegrounds**.

It tracks useful statistics during a match and displays them in a compact overlay. At the end of the game, the final summary remains visible until the next match starts.

## Features

The plugin currently tracks:

- Gold spent
- Tavern rolls
- Free rolls gained
- Cards, minions and spells bought
- Cards, minions and spells played
- Battlecries and Rally effects triggered
- Highest minion Attack and Health
- Highest combined minion stats
- Tavern and spell power buffs
- Hero damage dealt and received
- Highest hero damage dealt or received in one combat
- Combat wins, losses and draws
- Live match duration, preserved in the final summary
- Last intact minion board displayed after the match, with the hero, final
  placement, MMR change when available, turn count, highest creature and
  total duration
- Final hero card portrait displayed above the saved board
- Final Hero Power displayed with HDT's native Hero Power control
- Automatic PNG capture of the final board in
  `Pictures\Hearthstone final board`

The current test build saves final-board screenshots for every placement.
After validation, the prepared placement limit can be changed from `8` to `3`
to keep only Top 1, Top 2 and Top 3 results.

The compact graphite overlay uses crisp, high-contrast typography and can be
shown or hidden during and after the game with a dedicated button. The final
interface is automatically hidden after leaving Battlegrounds for Hearthstone's
main menu.

## Installation

1. Download the latest release.
2. Copy `HDT-FinalStatsPlugin.dll` into your Hearthstone Deck Tracker `Plugins` folder.
3. Open HDT.
4. Go to `Options > Tracker > Plugins`.
5. Enable **Battlegrounds Final Stats**.

## Development

This project targets:

- .NET Framework 4.7.2
- x64
- WPF
- Hearthstone Deck Tracker

Use `Build.bat` to compile the plugin against your local HDT installation.

## Status

HDT-FinalStatsPlugin is still in active development.

Some statistics may require adjustments after Hearthstone or HDT updates. Bug reports, test results and suggestions are welcome.

## Privacy

All statistics are processed locally. The plugin does not send gameplay data to an external server.
