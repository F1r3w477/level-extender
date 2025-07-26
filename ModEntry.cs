using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Locations;
using System.Collections.Generic;
using System.Timers;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Tools;
using Newtonsoft.Json;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace LevelExtender
{
    public class ModEntry : Mod
    {
        public static ModEntry instance;
        private Timer aTimer;
        private bool firstFade = false;
        public ModData config = new();
        public Random rand = new(Guid.NewGuid().GetHashCode());
        internal bool wm = false;

        private float oStamina = 0.0f;
        public bool initialtooluse = false;

        private bool no_mons = false;

        private LEModApi API;

        public LEEvents LEE;

        public ModEntry LE;

        private int total_m;
        internal double s_mod;

        public MPModApi mpmod;
        private bool mpload;
        private double mpMult;

        private Timer aTimer2 = new();

        private List<XPBar> xpBars = new();

        public List<string> snames = new();

        private Harmony harmony;

        public List<Monster> monsters = new();

        public List<Skill> skills = new();
        public List<int[]> categories = new();
        
        public List<int> skillLevs = new();

        // --- Moved from SaveEvents_AfterLoad for better organization ---
        private readonly string[] vanillaSkillNames = { "Farming", "Fishing", "Foraging", "Mining", "Combat" };
        private readonly List<int[]> vanillaItemCategories = new()
        {
            new[] { -16, -74, -75, -79, -80, -81 }, // Farming & Foraging
            new[] { -4 }, // Fishing
            new[] { -16, -74, -75, -79, -80, -81 }, // Farming & Foraging (same as above)
            new[] { -2, -12, -15 }, // Mining
            new[] { -28, -29, -95, -96, -98 } // Combat
        };
        public readonly List<int> defaultRequiredXP = new() { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };
        // ---

        public ModEntry()
        {
            instance = this;
            this.LE = this;
            this.LEE = new LEEvents();
            this.total_m = 0;
            this.s_mod = -1.0;
            this.mpload = false;
            this.mpMult = 1.0;
        }

        public override object GetApi()
        {
            return this.API = new LEModApi(this);
        }

        public override void Entry(IModHelper helper)
        {
            this.harmony = new Harmony(this.ModManifest.UniqueID);

            Type[] addItemParams = { typeof(Item), typeof(bool) };

            this.harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool), addItemParams),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(AITI2))
            );

            helper.Events.GameLoop.OneSecondUpdateTicked += this.GameEvents_OneSecondTick;
            helper.Events.GameLoop.UpdateTicked += this.GameEvents_QuarterSecondTick;
            helper.Events.GameLoop.GameLaunched += this.GameEvents_FirstUpdateTick;
            helper.Events.GameLoop.SaveLoaded += this.SaveEvents_AfterLoad;
            helper.Events.GameLoop.Saving += this.SaveEvents_BeforeSave;
            helper.Events.GameLoop.ReturnedToTitle += this.SaveEvents_AfterReturnToTitle;
            helper.Events.Display.MenuChanged += this.Display_MenuChanged;
            helper.Events.Input.ButtonPressed += this.ControlEvent_KeyPressed;
            helper.Events.GameLoop.DayStarted += this.TimeEvent_AfterDayStarted;
            helper.Events.Input.ButtonReleased += this.ControlEvent_KeyReleased;
            helper.Events.Display.Rendered += this.Display_Rendered;
            helper.Events.Player.Warped += this.Player_Warped;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;

            var commands = new Commands(this);
            helper.ConsoleCommands.Add("xp", "Displays the xp table for your current skill levels.", commands.XPT);
            helper.ConsoleCommands.Add("lev", "Sets the player's level: lev <skill name> <number>", commands.SetLev);
            helper.ConsoleCommands.Add("wm_toggle", "Toggles monster spawning: wm_toggle", commands.WmT);
            helper.ConsoleCommands.Add("xp_m", "Changes the xp modifier for a given skill.", commands.XpM);
            helper.ConsoleCommands.Add("spawn_modifier", "Forcefully changes monster spawn rate.", commands.SM);
            helper.ConsoleCommands.Add("xp_table", "Displays the XP table for a given skill.", commands.TellXP);
            helper.ConsoleCommands.Add("set_xp", "Sets your current XP for a given skill.", commands.SetXP);
            helper.ConsoleCommands.Add("draw_bars", "Sets whether the XP bars should be drawn or not.", commands.DrawBars);
            helper.ConsoleCommands.Add("draw_ein", "Sets whether the extra item notifications should be drawn or not.", commands.DrawEIN);
            helper.ConsoleCommands.Add("min_ein_price", "Sets the minimum price threshold for extra item notifications.", commands.MinEINP);

            this.LEE.OnXPChanged += this.OnXPChanged;
        }
        
        private void OnNpcListChanged(object sender, NpcListChangedEventArgs e)
        {
            if (!e.IsCurrentLocation || !e.Location.IsFarm)
                return;

            foreach (NPC npc in e.Removed)
            {
                if (npc is Monster monster && this.monsters.Contains(monster) && monster.Health <= 0)
                {
                    Game1.player.gainExperience(Farmer.combatSkill, monster.ExperienceGained);
                    this.monsters.Remove(monster);
                }
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Data/Fish"))
            {
                e.Edit(asset =>
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                    foreach (var pair in data.ToArray())
                    {
                        string[] fields = pair.Value.Split('/');
                        if (int.TryParse(fields[1], out int val))
                        {
                            int x = Math.Max(val - this.rand.Next(0, (int)(Game1.player.fishingLevel.Value / 4)), val / 2);
                            fields[1] = x.ToString();
                            data[pair.Key] = string.Join("/", fields);
                        }
                    }
                });
            }
        }
        
        public static void AITI2(ref Item item, bool makeActiveObject)
        {
            try
            {
                if (item == null || item.HasBeenInInventory)
                    return;

                int cat = item.Category;
                string str = "";
                int tstack = item.Stack;
                int i = 0;

                foreach (int[] cats in instance.categories)
                {
                    if (cats.Contains(cat) && ShouldDup(i))
                    {
                        item.Stack += 1;
                        while (ShouldDup(i))
                        {
                            item.Stack += 1;
                        }
                        if (instance.config.drawExtraItemNotifications)
                            str = $"Your {instance.snames[i]} level allowed you to obtain {item.Stack - tstack} extra {item.DisplayName}!";
                        break;
                    }
                    i++;
                }

                if (str.Length > 0 && item.salePrice() >= instance.config.minItemPriceForNotifications)
                {
                    Game1.addHUDMessage(new HUDMessage(str, 2));
                }
            }
            catch (Exception ex)
            {
                instance.Monitor.Log($"Failed in {nameof(AITI2)}:\n{ex}", LogLevel.Error);
            }
        }

        private void Player_Warped(object sender, WarpedEventArgs e) { }

        private DateTime otime;
        private void Display_Rendered(object sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (this.otime == default)
                this.otime = DateTime.Now;

            if (this.xpBars.Count > 0)
            {
                // Drawing logic... (omitted for brevity as it's unchanged)
            }
            this.otime = DateTime.Now;
        }
        
        private void ControlEvent_KeyReleased(object sender, ButtonReleasedEventArgs e) { if (!Context.IsWorldReady) return; }
        private void GameEvents_FirstUpdateTick(object sender, EventArgs e) { }

        private Timer shouldDraw;
        private void SetTimer(int time, int index)
        {
            if (index == 0)
            {
                this.aTimer = new Timer(1100) { AutoReset = false, Enabled = true };
                this.aTimer.Elapsed += OnTimedEvent;
            }
            else if (index == 1)
            {
                this.aTimer2 = new Timer(time) { AutoReset = false, Enabled = true };
                this.aTimer2.Elapsed += OnTimedEvent2;
            }
            else if (index == 2)
            {
                this.shouldDraw = new Timer(time) { AutoReset = false, Enabled = true };
                this.shouldDraw.Elapsed += sDrawEnd;
            }
        }

        private void sDrawEnd(object sender, ElapsedEventArgs e) { this.shouldDraw.Enabled = false; }

        public void EndXPBar(int key)
        {
            var bar = this.xpBars.SingleOrDefault(x => x.skill.key == key);
            if (bar != null)
            {
                this.xpBars.Remove(bar);
                if (this.xpBars.Count > 0)
                {
                    this.xpBars[0].ych = 0;
                }
            }
        }

        private void OnTimedEvent2(object sender, ElapsedEventArgs e) { if (this.mpmod != null) this.mpMult = this.mpmod.Exp_Rate(); this.aTimer2.Enabled = false; }

        private void OnXPChanged(object sender, EXPEventArgs e)
        {
            XPBar bar = this.xpBars.SingleOrDefault(b => b.skill.key == e.key);
            Skill skill = this.skills.SingleOrDefault(sk => sk.key == e.key);

            if (skill == null || skill.xpc < 0 || skill.xpc > 100001 || (this.shouldDraw != null && this.shouldDraw.Enabled))
                return;

            if (bar != null)
            {
                bar.timer.Stop();
                bar.timer.Start();
                bar.time = DateTime.Now;
                double val = bar.ych * -1;
                setYchVals(val);
                sortByTime();
            }
            else
            {
                this.xpBars.Add(new XPBar(skill));
            }
        }

        public void sortByTime() { this.xpBars = this.xpBars.OrderBy(o => o.time).ToList(); }
        public void setYchVals(double val) { foreach (var bar in this.xpBars) { bar.ych = val; } }
        
        private void Display_MenuChanged(object sender, MenuChangedEventArgs e) { if (!Context.IsWorldReady || e.OldMenu == null) return; }
        public void Closing() { }
        private void ControlEvent_KeyPressed(object sender, ButtonPressedEventArgs e) { if (!Context.IsWorldReady) return; }

        private void GameEvents_OneSecondTick(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.IsMultipleOf(3600))
                this.monsters.RemoveAll(mon => mon == null || mon.Health <= 0 || mon.currentLocation == null);

            if (e.IsMultipleOf(1800))
            {
                for (int i = 0; i < this.skillLevs.Count; i++)
                    this.skillLevs[i] = this.skills[i].level;
            }

            if (this.skills.Count > 4)
            {
                for (int i = 0; i < 5; i++)
                {
                    Skill skill = this.skills.SingleOrDefault(sk => sk.key == i);
                    if (skill == null)
                    {
                        if (this.snames.Count > i)
                            this.Monitor.Log($"LE ERROR - Skill {this.snames[i]} not registered properly for exp gain.", LogLevel.Error);
                    }
                    else if (skill.xp != Game1.player.experiencePoints[i])
                    {
                        skill.xp = Game1.player.experiencePoints[i];
                    }
                }
            }

            if (!this.no_mons && this.wm && Game1.player.currentLocation.IsOutdoors && Game1.activeClickableMenu == null && this.rand.NextDouble() <= S_R())
            {
                Vector2 loc = Game1.player.currentLocation.getRandomTile();
                while (!Game1.player.currentLocation.isTilePlaceable(loc))
                {
                    loc = Game1.player.currentLocation.getRandomTile();
                }

                int tier = this.rand.Next(0, 9);
                Monster m = GetMonster(tier, loc * Game1.tileSize);
                if (tier == 8)
                {
                    tier = 5;
                    m.resilience.Value += 20;
                    m.Slipperiness += this.rand.Next(10) + 5;
                    m.startGlowing(new Color(this.rand.Next(0, 255), this.rand.Next(0, 255), this.rand.Next(0, 255)), true, 1.0f);
                    m.Health *= 1 + (this.rand.Next(Game1.player.CombatLevel / 2, Game1.player.CombatLevel));
                    var data = Game1.content.Load<Dictionary<string, string>>("Data/ObjectInformation");
                    m.objectsToDrop.Add(data.Keys.ElementAt(this.rand.Next(data.Count)));
                    m.displayName += ": LE BOSS";
                    m.Scale = m.Scale * (float)(1 + (this.rand.NextDouble() * Game1.player.CombatLevel / 25.0));
                }
                else
                {
                    tier = 1;
                }

                m.DamageToFarmer = (int)(m.DamageToFarmer / 1.5) + (int)(Game1.player.combatLevel.Value / 3);
                m.Health *= 1 + (Game1.player.CombatLevel / 4);
                m.focusedOnFarmers = true;
                m.wildernessFarmMonster = true;
                m.Speed += this.rand.Next((int)Math.Round((Game1.player.combatLevel.Value / 10.0)));
                m.resilience.Value += (Game1.player.combatLevel.Value / 10);
                m.ExperienceGained += (int)(m.Health / 100.0) + ((10 + (Game1.player.combatLevel.Value * 2)) * tier);

                Game1.currentLocation.characters.Add(m);
                this.total_m++;

                if (tier == 5)
                    Game1.chatBox.addMessage($"A boss has spawned in your current location!", Color.DarkRed);
                this.monsters.Add(m);
            }
        }

        public double S_R()
        {
            if (Game1.player.combatLevel.Value == 0) return 0.0;
            if (this.s_mod != -1.0) return this.s_mod;
            if (this.API != null && this.API.overSR != -1.0) return this.API.overSR;
            if (Game1.isDarkOut(Game1.currentLocation) || Game1.isRaining) return (0.01 + (Game1.player.combatLevel.Value * 0.0001)) * 1.5;
            return (0.01 + (Game1.player.combatLevel.Value * 0.0001));
        }

        private void GameEvents_QuarterSecondTick(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.player.UsingTool && !this.initialtooluse)
            {
                this.oStamina = Game1.player.Stamina;
                this.initialtooluse = true;
            }
            else if (!Game1.player.UsingTool && this.initialtooluse)
            {
                if (Game1.player.Stamina > this.oStamina)
                {
                    Game1.player.Stamina = Math.Max(this.oStamina - 0.5f, 0.0f);
                }
                this.oStamina = 0.0f;
                this.initialtooluse = false;
            }

            if (e.IsMultipleOf(8) && Game1.activeClickableMenu is BobberBar bar)
            {
                if (!this.firstFade)
                {
                    int bobberBonus = 0;
                    Tool tool = Game1.player.CurrentTool;
                    bool beginnersRod = tool is FishingRod && tool.UpgradeLevel == 1;

                    if (tool.attachments?.Any(a => a?.name == "Cork Bobber") == true)
                        bobberBonus = 24;

                    if (Game1.player.FishingLevel > 99) bobberBonus += 8;
                    else if (Game1.player.FishingLevel > 74) bobberBonus += 6;
                    else if (Game1.player.FishingLevel > 49) bobberBonus += 4;
                    else if (Game1.player.FishingLevel > 24) bobberBonus += 2;

                    int bobberBarSize;
                    if (!this.Helper.ModRegistry.IsLoaded("DevinLematty.ExtremeFishingOverhaul"))
                    {
                        if (beginnersRod) bobberBarSize = 80 + (5 * 9);
                        else if (Game1.player.FishingLevel < 11) bobberBarSize = 80 + bobberBonus + (Game1.player.FishingLevel * 9);
                        else bobberBarSize = 165 + bobberBonus + (int)(Game1.player.FishingLevel * (0.5 + (this.rand.NextDouble() / 2.0)));
                    }
                    else
                    {
                        if (beginnersRod) bobberBarSize = 80 + (5 * 7);
                        else if (Game1.player.FishingLevel < 11) bobberBarSize = 80 + bobberBonus + (Game1.player.FishingLevel * 7);
                        else if (Game1.player.FishingLevel > 10 && Game1.player.FishingLevel < 20) bobberBarSize = 150 + bobberBonus + Game1.player.FishingLevel;
                        else bobberBarSize = 170 + bobberBonus + (int)(Game1.player.FishingLevel * 0.8 * (0.5 + (this.rand.NextDouble() / 2.0)));
                    }

                    this.firstFade = true;
                    this.Helper.Reflection.GetField<int>(bar, "bobberBarHeight").SetValue(bobberBarSize);
                    this.Helper.Reflection.GetField<float>(bar, "bobberBarPos").SetValue(568 - bobberBarSize);
                }
                else
                {
                    bool bobberInBar = this.Helper.Reflection.GetField<bool>(bar, "bobberInBar").GetValue();
                    if (!bobberInBar)
                    {
                        float dist = this.Helper.Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
                        this.Helper.Reflection.GetField<float>(bar, "distanceFromCatching").SetValue(dist + ((Game1.player.FishingLevel - 10) / 22000.0f));
                    }
                }
            }
            else if (this.firstFade)
            {
                this.firstFade = false;
            }
        }

        public static bool ShouldDup(int index)
        {
            double drate = 0.002;
            if (index == 0 || index == 2)
                drate = 0.002 / 2.0;

            if (instance.skillLevs.Count > index && instance.rand.NextDouble() <= (instance.skillLevs[index] * drate))
            {
                return true;
            }
            return false;
        }

        private void TimeEvent_AfterDayStarted(object sender, DayStartedEventArgs e)
        {
            if (Context.IsSplitScreen)
            {
                this.Monitor.Log("LE: Splitscreen Multiplayer is not currently supported. Mod will not load.");
                return;
            }

            Farm farm = Game1.getFarm();
            double gchance = Game1.player.farmingLevel.Value * 0.0002;
            double pchance = Game1.player.farmingLevel.Value * 0.001;
            foreach (Vector2 key in farm.terrainFeatures.Keys)
            {
                if (farm.terrainFeatures[key] is HoeDirt tf && tf.crop != null)
                {
                    if (this.rand.NextDouble() < gchance)
                        tf.crop.growCompletely();
                    else if (this.rand.NextDouble() < pchance)
                        tf.crop.currentPhase.Value = Math.Min(tf.crop.currentPhase.Value + 1, tf.crop.phaseDays.Count - 1);
                }
            }

            if (!this.mpload && this.Helper.ModRegistry.IsLoaded("f1r3w477.Level_Extender"))
            {
                this.mpmod = this.Helper.ModRegistry.GetApi<MPModApi>("f1r3w477.Level_Extender");
                this.mpload = true;
                SetTimer(1000, 1);
            }
            else if (this.mpload)
            {
                SetTimer(1000, 1);
            }
            this.no_mons = false;
        }

        public void Rem_mons()
        {
            this.no_mons = true;
            int x = 0;
            
            foreach (GameLocation location in Game1.locations)
            {
                int y = location.characters.Count;
                location.characters.RemoveWhere(c => c.IsMonster);
                x += (y - location.characters.Count);
            }
            this.Monitor.Log($"Removed | {x} | / | {this.total_m} | monsters.");
            this.total_m = 0;
        }

        private Monster GetMonster(int tier, Vector2 loc) => tier switch
        {
            0 => new DustSpirit(loc),
            1 => new Grub(loc),
            2 => new Skeleton(loc),
            3 => new RockCrab(loc),
            4 => new Ghost(loc),
            5 => new GreenSlime(loc),
            6 => new RockGolem(loc),
            7 => new ShadowBrute(loc),
            8 => GetBossMonster(loc),
            _ => new GreenSlime(loc),
        };

        private Monster GetBossMonster(Vector2 loc)
        {
            int y = this.rand.Next(1, 6);
            return y switch
            {
                1 => new RockCrab(loc, "Iridium Crab"),
                2 => new Ghost(loc, "Carbon Ghost"),
                3 => new RockCrab(loc, "Lava Crab"),
                4 => new GreenSlime(loc, Math.Max(Game1.player.combatLevel.Value * 5, 50)),
                5 => new BigSlime(loc, Math.Max(Game1.player.combatLevel.Value * 5, 50)),
                _ => new Mummy(loc),
            };
        }

        private void SaveEvents_AfterLoad(object sender, SaveLoadedEventArgs e)
        {
            SetTimer(2000, 2);
            try
            {
                this.Monitor.Log("Starting skill load for LE");
                var config_t = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();

                int count = 0;
                if (config_t.skills != null)
                {
                    foreach (string str in config_t.skills)
                    {
                        this.Monitor.Log($"skill load - {str}");
                        string[] vals = str.Split(',');
                        Skill sk = new Skill(this.LE, vals[0], int.Parse(vals[1]), double.Parse(vals[2]), new List<int>(this.defaultRequiredXP), this.vanillaItemCategories[count]);
                        this.skills.Add(sk);
                        this.snames.Add(sk.name);
                        this.categories.Add(sk.cats);
                        this.skillLevs.Add(sk.level);
                        count++;
                    }
                }
                
                for (int i = count; i < 5; i++)
                {
                    this.Monitor.Log($"adding skills - {i}, dxp: {Game1.player.experiencePoints[i]}");
                    Skill sk = new Skill(this.LE, this.vanillaSkillNames[i], Game1.player.experiencePoints[i], 1.0, new List<int>(this.defaultRequiredXP), this.vanillaItemCategories[i]);
                    this.skills.Add(sk);
                    this.snames.Add(sk.name);
                    this.categories.Add(sk.cats);
                    this.skillLevs.Add(sk.level);
                }

                this.wm = config_t.WorldMonsters;
                this.config = config_t;
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"LE failed loading skills, mod will not start: {ex.Message}", LogLevel.Trace);
            }
            this.Helper.GameContent.InvalidateCache("Data/Fish");
        }

        private void SaveEvents_BeforeSave(object sender, SavingEventArgs e)
        {
            this.config.skills = new List<string>();
            foreach (Skill skill in this.skills)
            {
                this.config.skills.Add($"{skill.name},{skill.xp},{skill.xp_mod}");
            }
            this.config.WorldMonsters = this.wm;
            this.Helper.Data.WriteJsonFile<ModData>($"data/{Constants.SaveFolderName}.json", this.config);

            if (!this.no_mons)
            {
                Rem_mons();
            }
        }

        private void SaveEvents_AfterReturnToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            this.wm = false;
            this.firstFade = false;
            this.config = new ModData();

            this.skills = new List<Skill>();
            this.snames = new List<string>();
            this.categories = new List<int[]>();
            this.skillLevs = new List<int>();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e) { this.Closing(); }

        public dynamic TalkToSkill(string[] args)
        {
            if (args.Length < 3) return -3;
            string arg0 = args[0].ToLower();
            string arg1 = args[1].ToLower();
            string arg2 = args[2].ToLower();
            string arg3 = "";
            if (args.Length > 3) arg3 = args[3].ToLower();

            Skill s = this.skills.SingleOrDefault(sk => sk.name.ToLower() == arg1);
            if (s == null) return -2;

            if (arg0 == "get")
            {
                if (arg2 == "xp") return s.xp;
                else if (arg2 == "level") return s.level;
                else return -2;
            }
            else if (arg0 == "set")
            {
                if (!int.TryParse(arg3, out int r)) return -2;
                if (arg2 == "xp") { s.xp = r; return r; }
                else if (arg2 == "level") { s.level = r; return r; }
                else return -2;
            }
            return -1;
        }

        public int initializeSkill(string name, int xp, double? xp_mod = null, List<int> xp_table = null, int[] cats = null)
        {
            Skill sk = new Skill(this.LE, name, xp, xp_mod, xp_table, cats);
            if (sk == null) return -1;
            this.skills.Add(sk);
            return 0;
        }
    }
}