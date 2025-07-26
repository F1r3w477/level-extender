using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelExtender
{
    public class Skill
    {
        public string name;
        public int key;
        public bool lvlbyxp = false;
        private int Level;
        public int level
        {
            get { return Level; }
            set
            {
                Level = value;
                if (key < 5)
                {
                    if (key == 0) Game1.player.farmingLevel.Value = level;
                    else if (key == 1) Game1.player.fishingLevel.Value = level;
                    else if (key == 2) Game1.player.foragingLevel.Value = level;
                    else if (key == 3) Game1.player.miningLevel.Value = level;
                    else if (key == 4) Game1.player.combatLevel.Value = level;
                }
                if (!lvlbyxp)
                {
                    int reqxp = getReqXP(level - 1);
                    if (key < 5)
                        Game1.player.experiencePoints[key] = reqxp;
                    XP = reqxp;
                }
                lvlbyxp = false;
            }
        }
        public int xpc;
        public EXPEventArgs args;
        public ModEntry LE;
        public List<int> xp_table;
        public double xp_mod;
        private int XP;
        public int xp
        {
            get { return XP; }
            set
            {
                if (xp != value)
                {
                    xpc = value - xp;
                    XP = value;
                    checkForLevelUp();
                    LE.LEE.RaiseEvent(args);
                }
            }
        }

        public int[] cats;
        int bmaxxp = 0;

        public Skill(ModEntry LE, string name, int xp, double? xp_mod = null, List<int> xp_table = null, int[] cats = null)
        {
            if (xp_table != null && xp_table.Count > 0)
            {
                bmaxxp = xp_table.Max();
            }

            this.LE = LE;
            this.name = name;
            this.key = LE.skills.Count;
            args = new EXPEventArgs();
            args.key = key;
            this.xp_table = xp_table ?? new List<int>();
            if (xp_mod == null)
                this.xp_mod = 1.0;
            else
                this.xp_mod = xp_mod.Value;

            this.cats = cats ?? new int[0];

            if (key == 0) Level = Game1.player.farmingLevel.Value;
            else if (key == 1) Level = Game1.player.fishingLevel.Value;
            else if (key == 2) Level = Game1.player.foragingLevel.Value;
            else if (key == 3) Level = Game1.player.miningLevel.Value;
            else if (key == 4) Level = Game1.player.combatLevel.Value;
            else Level = 0;

            generateTable(101);

            if (key < 5)
            {
                Game1.player.experiencePoints[key] = xp;
                XP = xp;
            }
            else
                XP = xp;

            checkForLevelUp();
        }
        public void checkForLevelUp()
        {
            int l = getLevByXP();
            if (l != level)
            {
                lvlbyxp = true;
                level = l;
            }
        }

        public int getReqXP(int lev)
        {
            if (lev < 0) return 0; // Guard against negative index
            if (xp_table.Count > lev)
                return xp_table[lev];
            else
                generateTable(lev);
            return xp_table[lev];
        }

        public void generateTable(int lev)
        {
            for (int i = xp_table.Count; i <= lev; i++)
            {
                int exp = getXPByLev(i);
                xp_table.Add(exp);
            }
        }

        public int getXPByLev(int i)
        {
            if (i <= 0) return 0; // Base case
            if (xp_table.Count > i)
                return xp_table[i];

            if (i < 45)
                return getXPByLev(i - 1) + 300 + (int)Math.Round(1000 * i * xp_mod);
            else
                return getXPByLev(i - 1) + 300 + (int)Math.Round(((i * i * i * 0.5)) * xp_mod);
        }

        public int getLevByXP()
        {
            int l = 0;
            if (xp_table.Count > 0 && xp > xp_table.Max())
                l = xp_table.Count - 1;

            for (int i = 0; i < xp_table.Count; i++)
            {
                if (xp <= xp_table[i])
                {
                    l = i;
                    break;
                }
            }
            return l;
        }
    }
}