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
            get { return this.Level; }
            set
            {
                this.Level = value;
                if (this.key < 5)
                {
                    if (this.key == 0) Game1.player.farmingLevel.Value = this.level;
                    else if (this.key == 1) Game1.player.fishingLevel.Value = this.level;
                    else if (this.key == 2) Game1.player.foragingLevel.Value = this.level;
                    else if (this.key == 3) Game1.player.miningLevel.Value = this.level;
                    else if (this.key == 4) Game1.player.combatLevel.Value = this.level;
                }
                if (!this.lvlbyxp)
                {
                    int reqxp = this.getReqXP(this.level - 1);
                    if (this.key < 5)
                        Game1.player.experiencePoints[this.key] = reqxp;
                    this.XP = reqxp;
                }
                this.lvlbyxp = false;
            }
        }
        public int xpc;
        public EXPEventArgs args;
        private ModEntry mod;
        public List<int> xp_table;
        public double xp_mod;
        private int XP;
        public int xp
        {
            get { return this.XP; }
            set
            {
                if (this.XP == value)
                    return;
                
                this.xpc = value - this.XP;
                this.XP = value;
                
                this.checkForLevelUp();
                this.mod.LEE.RaiseEvent(this.args);
            }
        }

        public int[] cats;

        public Skill(ModEntry mod, string name, int xp, double? xp_mod = null, List<int> xp_table = null, int[] cats = null)
        {
            this.mod = mod;
            this.name = name;
            this.key = mod.skills.Count;
            this.args = new EXPEventArgs
            {
                key = this.key
            };
            this.xp_table = xp_table ?? new List<int>();
            this.xp_mod = xp_mod ?? 1.0;
            this.cats = cats ?? Array.Empty<int>();

            if (this.key == 0) this.Level = Game1.player.farmingLevel.Value;
            else if (this.key == 1) this.Level = Game1.player.fishingLevel.Value;
            else if (this.key == 2) this.Level = Game1.player.foragingLevel.Value;
            else if (this.key == 3) this.Level = Game1.player.miningLevel.Value;
            else if (this.key == 4) this.Level = Game1.player.combatLevel.Value;
            else this.Level = 0;

            this.generateTable(101);

            if (this.key < 5)
            {
                Game1.player.experiencePoints[this.key] = xp;
            }
            this.XP = xp;

            this.checkForLevelUp();
        }

        public void checkForLevelUp()
        {
            int l = this.getLevByXP();
            if (l != this.level)
            {
                this.lvlbyxp = true;
                this.level = l;
            }
        }

        public int getReqXP(int lev)
        {
            if (lev < 0) return 0;
            if (this.xp_table.Count > lev)
                return this.xp_table[lev];
            
            this.generateTable(lev);
            return this.xp_table[lev];
        }

        // REFACTORED: This method is now iterative instead of recursive for much better performance.
        public void generateTable(int lev)
        {
            // The XP table stores cumulative XP needed. Index 0 is for level 1, index 1 for level 2, etc.
            // If the table is empty and we have default values, use them first.
            if (this.xp_table.Count == 0 && mod.defaultRequiredXP.Any())
            {
                this.xp_table.AddRange(mod.defaultRequiredXP);
            }

            for (int i = this.xp_table.Count; i <= lev; i++)
            {
                // To get XP for level i+1 (at index i), we need the XP for level i (at index i-1).
                int previousXp = this.xp_table[i - 1];
                int requiredXp;
                int levelNum = i + 1; // The actual level number we're calculating for

                if (levelNum < 45)
                {
                    requiredXp = previousXp + 300 + (int)Math.Round(1000 * levelNum * this.xp_mod);
                }
                else
                {
                    requiredXp = previousXp + 300 + (int)Math.Round(((double)levelNum * levelNum * levelNum * 0.5) * this.xp_mod);
                }
                this.xp_table.Add(requiredXp);
            }
        }

        public int getLevByXP()
        {
            int requiredLevel = this.level;
            while (true)
            {
                int requiredXp = getReqXP(requiredLevel);
                if (this.xp >= requiredXp)
                {
                    requiredLevel++;
                }
                else
                {
                    break;
                }
            }
            
            if (this.xp < this.xp_table[0]) return 0;

            for (int i = 0; i < this.xp_table.Count; i++)
            {
                if (this.xp < this.xp_table[i])
                {
                    return i;
                }
            }
            
            // If xp is higher than any value in the current table, it's the highest level in the table
            return this.xp_table.Count;
        }
    }
}