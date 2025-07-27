using StardewModdingAPI;
using System.Linq;
using StardewValley;
using System.Text;

namespace LevelExtender
{
    internal class Commands
    {
        private readonly ModEntry modEntry;
        private IMonitor Monitor => this.modEntry.Monitor;

        public Commands(ModEntry modEntry)
        {
            this.modEntry = modEntry;
        }

        public void XPT(string command, string[] args)
        {
            this.Monitor.Log("Skill      | Level | Current XP   | XP for Next Lvl", LogLevel.Info);
            this.Monitor.Log("----------------------------------------------------", LogLevel.Info);
            foreach (var skill in this.modEntry.skills)
            {
                int xpForNextLevel = skill.getReqXP(skill.level);
                this.Monitor.Log($"{skill.name,-10} | {skill.level,-5} | {skill.xp,-12} | {xpForNextLevel}", LogLevel.Info);
            }
        }

        public void SetLev(string command, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int level) || level < 0)
            {
                this.Monitor.Log("Command failed. Use 'lev <skillname> <level>'. Level must be a positive number.", LogLevel.Error);
                return;
            }
            
            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.Equals(args[0], System.StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                this.Monitor.Log($"Could not find a skill named '{args[0]}'.", LogLevel.Error);
                return;
            }

            skill.level = level;
            this.Monitor.Log($"{skill.name} level set to {level}. XP adjusted accordingly.", LogLevel.Info);

            // Invalidate fish data if fishing level changed, as it affects fish difficulty
            if (skill.key == 1) 
                this.modEntry.Helper.GameContent.InvalidateCache("Data/Fish");
        }

        public void WmT(string command, string[] args)
        {
            this.modEntry.wm = !this.modEntry.wm;
            this.Monitor.Log($"Overworld Monster Spawning -> {(this.modEntry.wm ? "ON" : "OFF")}.", LogLevel.Info);
        }

        public void XpM(string command, string[] args)
        {
            if (args.Length < 2 || !double.TryParse(args[1], out double modifier) || modifier <= 0.0)
            {
                this.Monitor.Log("Command failed. Use 'xp_m <skillname> <modifier>'. Modifier must be a positive number.", LogLevel.Error);
                return;
            }

            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.Equals(args[0], System.StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                this.Monitor.Log($"Could not find a skill named '{args[0]}'.", LogLevel.Error);
                return;
            }
            
            skill.xp_mod = modifier;
            this.Monitor.Log($"The XP modifier for {skill.name} was set to: {modifier}", LogLevel.Info);
        }

        public void SM(string command, string[] args)
        {
            if (args.Length < 1 || !double.TryParse(args[0], out double modifier))
            {
                this.Monitor.Log("Command failed. Use 'spawn_modifier <decimal_value>'.", LogLevel.Error);
                return;
            }
            this.modEntry.s_mod = modifier;
            this.Monitor.Log($"Monster spawn rate modifier set to {modifier} ({modifier:P}).", LogLevel.Info);
        }

        public void TellXP(string command, string[] args)
        {
            if (args.Length < 1)
            {
                this.Monitor.Log("Please provide a skill name.", LogLevel.Error);
                return;
            }

            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.Equals(args[0], System.StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                this.Monitor.Log($"Could not find a skill named '{args[0]}'.", LogLevel.Error);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"XP Table for {skill.name} (Cumulative XP required):");
            for (int i = 0; i < skill.xp_table.Count; i++)
            {
                sb.Append($"Lvl {i + 1}: {skill.xp_table[i]}   ");
                if ((i + 1) % 5 == 0)
                    sb.AppendLine();
            }
            this.Monitor.Log(sb.ToString(), LogLevel.Info);
        }

        public void SetXP(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("This command can only be used after a save is loaded.", LogLevel.Error);
                return;
            }
            
            if (args.Length < 2 || !int.TryParse(args[1], out int xp))
            {
                this.Monitor.Log("Command failed. Use 'set_xp <skillname> <amount>'. XP must be a whole number.", LogLevel.Error);
                return;
            }

            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.Equals(args[0], System.StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                this.Monitor.Log($"Could not find a skill named '{args[0]}'.", LogLevel.Error);
                return;
            }

            // The Skill class setter handles all logic, including syncing with the vanilla game
            skill.xp = xp;
            this.Monitor.Log($"{skill.name} XP set to {xp}. Level has been updated.", LogLevel.Info);
        }

        public void DrawBars(string command, string[] args)
        {
            if (args.Length < 1 || !bool.TryParse(args[0], out bool val))
            {
                 this.Monitor.Log("Command failed. Use 'draw_bars <true|false>'.", LogLevel.Error);
                return;
            }

            this.modEntry.config.drawBars = val;
            this.Monitor.Log($"XP bars will now be {(val ? "drawn" : "hidden")}.", LogLevel.Info);
        }

        public void DrawEIN(string command, string[] args)
        {
            if (args.Length < 1 || !bool.TryParse(args[0], out bool val))
            {
                this.Monitor.Log("Command failed. Use 'draw_ein <true|false>'.", LogLevel.Error);
                return;
            }

            this.modEntry.config.drawExtraItemNotifications = val;
            this.Monitor.Log($"Extra item notifications will now be {(val ? "shown" : "hidden")}.", LogLevel.Info);
        }

        public void MinEINP(string command, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int val))
            {
                this.Monitor.Log("Command failed. Use 'min_ein_price <amount>'.", LogLevel.Error);
                return;
            }

            this.modEntry.config.minItemPriceForNotifications = val;
            this.Monitor.Log($"Minimum price for extra item notifications set to {val}g.", LogLevel.Info);
        }
    }
}