# Level Extender for Stardew Valley

This mod allows skill levels in Stardew Valley to go far beyond 10, with configurable XP bars, item duplication bonuses, and other features.

## Authors & Maintainers

* **Original Author:** [unidarkshin](https://github.com/unidarkshin)
* **Lead Maintainer:** [F1r3w477](https://github.com/F1r3w477)

This mod was originally created by unidarkshin. It is now primarily maintained by F1r3w477, with ongoing contributions from the original author.

## Features

* **Extend All 5 Skills:** Go beyond level 10 up to level 100 (configurable).
* **On-Screen XP Bars:** Optional UI element that appears when you gain experience to track your progress.
* **Item Duplication:** Gain a chance to acquire extra items based on your skill level (e.g., duplicate fish, ore, or foraged goods).
* **Enhanced Fishing:** The fishing bobber bar becomes larger at higher skill levels, making fishing easier.
* **Overworld Monster Spawning:** Optionally spawn tougher monsters on your farm that scale with your combat level.
* **Console Commands:** A full suite of commands to check status or configure the mod on the fly.

## Console Commands

| Command         | Description                                                                                               | Example                          |
| --------------- | --------------------------------------------------------------------------------------------------------- | -------------------------------- |
| `xp`            | Displays the XP table for your current skill levels.                                                      | `xp`                             |
| `lev`           | Sets a skill to a specific level.                                                                         | `lev Fishing 25`                 |
| `set_xp`        | Sets the raw experience points for a skill.                                                               | `set_xp Mining 20000`            |
| `draw_bars`     | Toggles the on-screen XP bars.                                                                            | `draw_bars false`                |
| `wm_toggle`     | Toggles the overworld monster spawning feature.                                                           | `wm_toggle`                      |
| `xp_m`          | Changes the XP modifier for a skill. Must restart game.                                                   | `xp_m Farming 1.5`               |
| `spawn_modifier`| Forcefully changes the monster spawn rate. Use `-1` to disable.                                           | `spawn_modifier 0.5`             |
| `draw_ein`      | Toggles the "extra item notification" pop-ups.                                                            | `draw_ein false`                 |
| `min_ein_price` | Sets the minimum gold value for an item to show a notification.                                           | `min_ein_price 100`              |

## Installation

1.  Install the latest version of [SMAPI](https://smapi.io).
2.  Download the latest version of this mod from the [Releases page](https://github.com/F1r3w477/Level-Extender/releases).
3.  Unzip the downloaded file into your `Stardew Valley/Mods` folder.
4.  Run the game using SMAPI!
