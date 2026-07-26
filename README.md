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
- Last intact minion board displayed after the match

The compact graphite overlay uses crisp, high-contrast typography and can be
shown or hidden during the game with a dedicated button.

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
