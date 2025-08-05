using StardewModdingAPI;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace LevelExtender
{
    /// <summary>Handles the console commands for the mod.</summary>
    internal class Commands
    {
        private readonly ModEntry _modEntry;
        private readonly ModConfig _config;
        private IMonitor Monitor => _modEntry.Monitor;

        private const int CellWidth = 18;   // The width of each "Lvl X: YYYY" cell

        public Commands(ModEntry modEntry, ModConfig config)
        {
            this._modEntry = modEntry;
            this._config = config;
        }

        public void ShowExperienceSummary(string command, string[] args)
        {
            this.Monitor.Log("Skill      | Level | Current XP   | XP for Next Lvl", LogLevel.Info);
            this.Monitor.Log("----------------------------------------------------", LogLevel.Info);

            foreach (var skill in this._modEntry.Skills)
            {
                int xpForNextLevel = skill.GetRequiredExperienceForLevel(skill.Level);
                this.Monitor.Log($"{skill.Name,-10} | {skill.Level,-5} | {skill.Experience,-12} | {xpForNextLevel}", LogLevel.Info);
            }
        }

        public void SetLevel(string command, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int level) || level < 0)
            {
                this.Monitor.Log("Command failed. Usage: le_setlevel <skillname> <level>", LogLevel.Error);
                return;
            }

            var skill = GetSkill(args[0]);
            if (skill != null)
            {
                skill.Level = level;
                this.Monitor.Log($"{skill.Name} level set to {level}. XP adjusted accordingly.", LogLevel.Info);
            }
        }

        public void SetExperience(string command, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int xp) || xp < 0)
            {
                this.Monitor.Log("Command failed. Usage: le_setxp <skillname> <amount>", LogLevel.Error);
                return;
            }
            
            var skill = GetSkill(args[0]);
            if (skill != null)
            {
                skill.Experience = xp;
                this.Monitor.Log($"{skill.Name} XP set to {xp}. Level has been updated.", LogLevel.Info);
            }
        }

        public void ToggleWorldMonsters(string command, string[] args)
        {
            _config.EnableWorldMonsters = !_config.EnableWorldMonsters;
            this.Monitor.Log($"Wilderness monster spawning for this save is now {(_config.EnableWorldMonsters ? "ON" : "OFF")}.", LogLevel.Info);
        }

        public void ToggleDrawBars(string command, string[] args)
        {
            _config.DrawXpBars = !_config.DrawXpBars;
            this.Monitor.Log($"XP bars will now be {(_config.DrawXpBars ? "drawn" : "hidden")}.", LogLevel.Info);
        }

        public void SetExperienceModifier(string command, string[] args)
        {
            if (args.Length < 2 || !double.TryParse(args[1], out double modifier) || modifier <= 0)
            {
                this.Monitor.Log("Command failed. Usage: le_xp_modifier <skillname> <modifier>.", LogLevel.Error);
                return;
            }

            var skill = GetSkill(args[0]);
            if (skill != null)
            {
                skill.ExperienceModifier = modifier;
                this.Monitor.Log($"XP Modifier for {skill.Name} set to {modifier}.", LogLevel.Info);
            }
        }

        public void ShowSkillXpTable(string command, string[] args)
        {
            int columns = _config.TableColumns;
            if (args.Length < 1)
            {
                this.Monitor.Log("Command failed. Usage: le_xp_table <skillname>.", LogLevel.Error);
                return;
            }

            var skill = GetSkill(args[0]);
            if (skill != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"\nXP Table for {skill.Name} (Cumulative XP required):");
                var cells = new List<string>();
                for (int i = 0; i < skill.ExperienceTable.Count; i++)
                {
                    int level = i + 1;
                    int requiredXp = skill.ExperienceTable[i];

                    // Format the cell content and pad it to the fixed width
                    string cellContent = $"Lvl {level}: {requiredXp}";
                    cells.Add(cellContent.PadRight(CellWidth));
                }

                // Arrange the padded cells into rows
                for (int i = 0; i < cells.Count; i += columns)
                {
                    var rowCells = cells.Skip(i).Take(columns);
                    sb.AppendLine(string.Concat(rowCells));
                }

                this.Monitor.Log(sb.ToString(), LogLevel.Info);
            }
        }

        public void ToggleExtraItemNotifications(string command, string[] args)
        {
            _config.DrawExtraItemNotifications = !_config.DrawExtraItemNotifications;
            this.Monitor.Log($"Extra item notifications will now be {(_config.DrawExtraItemNotifications ? "shown" : "hidden")}.", LogLevel.Info);
        }

        public void SetMinNotificationPrice(string command, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int price) || price < 0)
            {
                this.Monitor.Log("Command failed. Usage: le_min_notification_price <amount>.", LogLevel.Error);
                return;
            }

            _config.MinItemPriceForNotifications = price;
            this.Monitor.Log($"Minimum price for extra item notifications set to {price}g.", LogLevel.Info);
        }

        // Helper method to reduce code duplication
        private Skill GetSkill(string name)
        {
            var skill = _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                this.Monitor.Log($"Could not find a skill named '{name}'.", LogLevel.Error);
            }
            return skill;
        }
    }
}