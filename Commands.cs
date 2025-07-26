using StardewModdingAPI;
using System.Linq;

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
            this.Monitor.Log("Skill:  | Level:  |  Current Experience:  | Experience Needed:", LogLevel.Info);
            for (int i = 0; i < this.modEntry.skills.Count; i++)
            {
                int xpn = this.modEntry.skills[i].getReqXP(this.modEntry.skills[i].level);
                this.Monitor.Log($"{this.modEntry.skills[i].name} | {this.modEntry.skills[i].level} | {this.modEntry.skills[i].xp} | {xpn}", LogLevel.Info);
            }
        }

        public void SetLev(string command, string[] args)
        {
            if (args.Length < 2 || args[0] == null || args[1] == null || !int.TryParse(args[1], out int n))
            {
                this.Monitor.Log($"Function Failed!");
                return;
            }
            if (n < 0 || n > 100)
            {
                this.Monitor.Log($"Function Failed!");
                return;
            }
            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.ToLower() == args[0].ToLower());
            if (skill == null)
                return;

            skill.level = n;
            if (skill.key == 1)
                this.modEntry.Helper.GameContent.InvalidateCache("Data/Fish");
        }

        public void WmT(string command, string[] args)
        {
            this.modEntry.wm = !this.modEntry.wm;
            this.Monitor.Log($"Overworld Monster Spawning -> {(this.modEntry.wm ? "ON" : "OFF")}.");
        }

        public void XpM(string command, string[] args)
        {
            if (args.Length > 1 && double.TryParse(args[1], out double x) && x > 0.0)
            {
                Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.ToLower() == args[0].ToLower());
                if (skill == null)
                    return;
                skill.xp_mod = x;
                this.Monitor.Log($"The XP modifier for {skill.name} was set to: {x}");
            }
            else
            {
                this.Monitor.Log($"Valid decimal not used; refer to help command.");
            }
        }

        public void SM(string command, string[] args)
        {
            if (args.Length < 1 || args[0] == null || !double.TryParse(args[0], out double n))
            {
                this.Monitor.Log("No decimal value found.");
                return;
            }
            this.modEntry.s_mod = n;
            this.Monitor.Log($"Modifier set to {n * 100}%.");
        }

        public void TellXP(string command, string[] args)
        {
            if (args.Length < 1)
                return;

            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.ToLower() == args[0].ToLower());
            if (skill == null)
            {
                this.Monitor.Log("Could not find a match for given skill name.");
                return;
            }

            string str = $"{skill.name}: ";
            int count = 0;
            foreach (int xp in skill.xp_table)
            {
                str += $"{count} -> {xp}, ";
                count++;
                if (count % 5 == 0)
                    str += "\n";
            }
            this.Monitor.Log(str);
        }

        public void SetXP(string command, string[] arg)
        {
            if (!Context.IsWorldReady || arg.Length < 2 || !int.TryParse(arg[1], out int xp))
            {
                this.Monitor.Log("No skill name entered or the xp was not a whole number.");
                return;
            }

            Skill skill = this.modEntry.skills.SingleOrDefault(sk => sk.name.ToLower() == arg[0].ToLower());

            if (skill == null)
            {
                this.Monitor.Log($"Invalid skill name: {arg[0]}");
                return;
            }

            if (skill.key < 5)
                StardewValley.Game1.player.experiencePoints[skill.key] = xp;
            else
                skill.xp = xp;
        }

        public void DrawBars(string arg1, string[] arg2)
        {
            if (!bool.TryParse(arg2[0], out bool val))
                return;

            ModEntry.config.drawBars = val;
            this.Monitor.Log($"You successfully set draw XP bars to {val}.");
        }

        public void DrawEIN(string arg1, string[] arg2)
        {
            if (!bool.TryParse(arg2[0], out bool val))
                return;

            ModEntry.config.drawExtraItemNotifications = val;
            this.Monitor.Log($"You successfully set draw extra item notifications to {val}.");
        }

        public void MinEINP(string arg1, string[] arg2)
        {
            if (!int.TryParse(arg2[0], out int val))
                return;

            ModEntry.config.minItemPriceForNotifications = val;
            this.Monitor.Log($"You successfully set the minimum price threshold for extra item notifications to {val}.");
        }
    }
}