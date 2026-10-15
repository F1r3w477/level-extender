# Level Extender for Stardew Valley

This mod allows skill levels in Stardew Valley to go far beyond 10, with configurable XP bars, item duplication bonuses, and other features.

## Authors & Maintainers

* **Original Author:** [unidarkshin](https://github.com/unidarkshin)
* **Lead Maintainer:** [F1r3w477](https://github.com/F1r3w477)

This mod was originally created by unidarkshin. It is now primarily maintained by F1r3w477, with ongoing contributions from the original author.

## Features

* **Extend All 5 Skills:** Go beyond level 10 with a configurable XP curve.
* **On-Screen XP Bars:** Optional UI element that appears when you gain experience to track your progress.
* **Item Duplication:** Gain a chance to acquire extra items based on your skill level (e.g., duplicate fish, ore, or foraged goods).
* **Enhanced Fishing:** The fishing bobber bar becomes larger at higher skill levels, making fishing easier.
* **Overworld Monster Spawning:** Optionally spawn tougher monsters on your farm that scale with your combat level.
* **Console Commands:** A full suite of commands to check status or configure the mod on the fly.

## Console Commands

| Command         | Description                                                                                               | Example                          |
| --------------- | --------------------------------------------------------------------------------------------------------- | -------------------------------- |
| `le_xp`                    | Shows a summary of current skill levels and XP.                                      | `le_xp`                              |
| `le_setlevel`              | Sets a skill to a specific level.                                                    | `le_setlevel Fishing 25`             |
| `le_setxp`                 | Sets the raw experience points for a skill.                                          | `le_setxp Mining 20000`              |
| `le_toggle_xpbars`         | Toggles the on-screen XP bars.                                                       | `le_toggle_xpbars`                   |
| `le_toggle_monsters`       | Toggles per-save wilderness monster spawning.                                        | `le_toggle_monsters`                 |
| `le_xp_modifier`           | Changes the XP modifier for a skill.                                                 | `le_xp_modifier Farming 1.5`         |
| `le_xp_table`              | Displays the full XP table for a skill.                                              | `le_xp_table Farming`                |
| `le_toggle_notifications`  | Toggles extra-item notifications.                                                   | `le_toggle_notifications`            |
| `le_min_notification_price`| Sets the minimum item price for an extra-item notification.                          | `le_min_notification_price 100`      |
| `le_skills`                | Opens the Level Extender skills menu.                                                | `le_skills`                          |

## Installation

1.  Install the latest version of [SMAPI](https://smapi.io).
2.  Download the latest version of this mod from the [Releases page](https://github.com/Varrxion/level-extender/releases).
3.  Unzip the downloaded file into your `Stardew Valley/Mods` folder.
4.  Run the game using SMAPI!
