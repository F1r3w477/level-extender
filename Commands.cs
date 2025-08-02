using StardewModdingAPI;
using System;
using System.Linq;
using System.Text;

namespace LevelExtender
{
    /// <summary>Handles the console commands for the mod.</summary>
    internal class Commands
    {
        private readonly ModEntry _modEntry;
        private IMonitor Monitor => _modEntry.Monitor;
        // A helper property to safely access the config
        private ModConfig Config => (ModConfig)this._modEntry.GetType().GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(this._modEntry);

        public Commands(ModEntry modEntry)
        {
            this._modEntry = modEntry;
        }

        public void ShowExperienceSummary(string command, string[] args)
        {
            this.Monitor.Log("Skill      | Level | Current XP   | XP for Next Lvl", LogLevel.Info);
            this.Monitor.Log("----------------------------------------------------", LogLevel.Info);

            foreach (var skill in this._modEntry.Skills)
            {
                // Now this logic is much clearer. The XP for the next level is at index skill.Level.
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

        // Replaces old 'wm_toggle' command
        public void ToggleWorldMonsters(string command, string[] args)
        {
            var config = this.Config;
            config.EnableWorldMonsters = !config.EnableWorldMonsters;
            this.Monitor.Log($"Wilderness monster spawning for this save is now {(config.EnableWorldMonsters ? "ON" : "OFF")}.", LogLevel.Info);
        }

        // Replaces old 'draw_bars' command
        public void ToggleDrawBars(string command, string[] args)
        {
            var config = this.Config;
            config.DrawXpBars = !config.DrawXpBars;
            this.Monitor.Log($"XP bars will now be {(config.DrawXpBars ? "drawn" : "hidden")}.", LogLevel.Info);
        }

        // Replaces old 'xp_m' command
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
                this.Monitor.Log($"XP Modifier for {skill.Name} set to {modifier}.", LogLevel.Info);
            }
        }

        // Replaces old 'xp_table' command
        public void ShowSkillXpTable(string command, string[] args)
        {
            if (args.Length < 1)
            {
                this.Monitor.Log("Command failed. Usage: le_xp_table <skillname>.", LogLevel.Error);
                return;
            }

            var skill = GetSkill(args[0]);
            if (skill != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"XP Table for {skill.Name} (Cumulative XP required):");
                for (int i = 0; i < skill.ExperienceTable.Count; i++)
                {
                    sb.Append($"Lvl {i + 1}: {skill.ExperienceTable[i]}   ");
                    if ((i + 1) % 5 == 0) sb.AppendLine();
                }
                this.Monitor.Log(sb.ToString(), LogLevel.Info);
            }
        }

        // Replaces old 'draw_ein' command
        public void ToggleExtraItemNotifications(string command, string[] args)
        {
            var config = this.Config;
            config.DrawExtraItemNotifications = !config.DrawExtraItemNotifications;
            this.Monitor.Log($"Extra item notifications will now be {(config.DrawExtraItemNotifications ? "shown" : "hidden")}.", LogLevel.Info);
        }

        // Replaces old 'min_ein_price' command
        public void SetMinNotificationPrice(string command, string[] args)
        {
             if (args.Length < 1 || !int.TryParse(args[0], out int price) || price < 0)
            {
                this.Monitor.Log("Command failed. Usage: le_min_notification_price <amount>.", LogLevel.Error);
                return;
            }

            var config = this.Config;
            config.MinItemPriceForNotifications = price;
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