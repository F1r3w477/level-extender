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
        public static Mod instance;
        private static System.Timers.Timer aTimer;
        bool firstFade = false;
        public static ModData config = new ModData();
        public static Random rand = new Random(Guid.NewGuid().GetHashCode());
        bool wm = false;

        float oStamina = 0.0f;
        public bool initialtooluse = false;

        bool no_mons = false;

        private LEModApi API;

        public LEEvents LEE;

        public ModEntry LE;

        private int total_m;
        private double s_mod;

        public MPModApi mpmod;
        private bool mpload;
        private double mpMult;

        private Timer aTimer2 = new Timer();

        List<XPBar> xpBars = new List<XPBar>();

        public static List<string> snames = new List<string>();

        Harmony harmony;

        public static List<Monster> monsters = new List<Monster>();

        public List<Skill> skills = new List<Skill>();
        public static List<int[]> categories = new List<int[]>();
        
        public static List<int> skillLevs = new List<int>();

        public ModEntry()
        {
            instance = this;
            LE = this;
            LEE = new LEEvents();
            total_m = 0;
            s_mod = -1.0;
            mpload = false;
            mpMult = 1.0;
        }

        public override object GetApi()
        {
            return API = new LEModApi(this);
        }

        public override void Entry(IModHelper helper)
        {
            Initialize(instance.Monitor);

            this.harmony = new Harmony(this.ModManifest.UniqueID);

            // Define parameters for the 'addItemToInventoryBool' method that takes an Item
            Type[] addItemParams = {
                typeof(Item),
                typeof(bool)
            };

            // Apply the patch for 'addItemToInventoryBool'
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Farmer), nameof(Farmer.addItemToInventoryBool), addItemParams),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(AITI2))
            );

            helper.Events.GameLoop.OneSecondUpdateTicked += this.GameEvents_OneSecondTick;
            helper.Events.GameLoop.UpdateTicked += this.GameEvents_QuarterSecondTick;
            helper.Events.GameLoop.GameLaunched += this.GameEvents_FirstUpdateTick;
            helper.Events.GameLoop.SaveLoaded += this.SaveEvents_AfterLoad;
            helper.Events.GameLoop.Saving += this.SaveEvents_BeforeSave;
            helper.Events.GameLoop.ReturnedToTitle += this.SaveEvents_AfterReturnToTitle;
            helper.Events.Display.MenuChanged += Display_MenuChanged;
            helper.Events.Input.ButtonPressed += this.ControlEvent_KeyPressed;
            helper.Events.GameLoop.DayStarted += this.TimeEvent_AfterDayStarted;
            helper.Events.Input.ButtonReleased += this.ControlEvent_KeyReleased;
            helper.Events.Display.Rendered += this.Display_Rendered;
            helper.Events.Player.Warped += this.Player_Warped;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;

            helper.ConsoleCommands.Add("xp", "Displays the xp table for your current skill levels.", this.XPT);
            helper.ConsoleCommands.Add("lev", "Sets the player's level: lev <skill name> <number>", this.SetLev);
            helper.ConsoleCommands.Add("wm_toggle", "Toggles monster spawning: wm_toggle", this.WmT);
            helper.ConsoleCommands.Add("xp_m", "Changes the xp modifier for a given skill: xp_m <skill name> <decimal 0.0 -> ANY>: 1.0 is default. Must restart game to take effect", this.XpM);
            helper.ConsoleCommands.Add("spawn_modifier", "Forcefully changes monster spawn rate to specified decimal value: spawn_modifier <decimal(percent)> : -1.0 to not have any effect.", this.SM);
            helper.ConsoleCommands.Add("xp_table", "Displays the XP table for a given skill: xp_table <skill name>", this.TellXP);
            helper.ConsoleCommands.Add("set_xp", "Sets your current XP for a given skill: set_xp <skill name> <XP: int 0 -> ANY>", this.SetXP);
            helper.ConsoleCommands.Add("draw_bars", "Sets whether the XP bars should be drawn or not: draw_bars <bool>, Default; true.", this.DrawBars);
            helper.ConsoleCommands.Add("draw_ein", "Sets whether the extra item notifications should be drawn or not: draw_ein <bool>, Default; true.", this.DrawEIN);
            helper.ConsoleCommands.Add("min_ein_price", "Sets the minimum price threshold for extra item notifications: min_ein_price <int>, Default; 50", this.MinEINP);

            LEE.OnXPChanged += this.OnXPChanged;
        }
        
        private void OnNpcListChanged(object sender, NpcListChangedEventArgs e)
        {
            if (!e.IsCurrentLocation || !e.Location.IsFarm)
                return;

            foreach (NPC npc in e.Removed)
            {
                if (npc is Monster monster && monsters.Contains(monster) && monster.Health <= 0)
                {
                    Game1.player.gainExperience(Farmer.combatSkill, monster.ExperienceGained);
                    monsters.Remove(monster); // Clean up monster list
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
                            int x = Math.Max(val - rand.Next(0, (int)(Game1.player.fishingLevel.Value / 4)), val / 2);
                            fields[1] = x.ToString();
                            data[pair.Key] = string.Join("/", fields);
                        }
                    }
                });
            }
        }

        private void MinEINP(string arg1, string[] arg2)
        {
            if (!int.TryParse(arg2[0], out int val))
                return;

            config.minItemPriceForNotifications = val;
            Monitor.Log($"You successfully set the minimum price threshold for extra item notifications to {val}.");
        }

        private void DrawEIN(string arg1, string[] arg2)
        {
            if (!bool.TryParse(arg2[0], out bool val))
                return;

            config.drawExtraItemNotifications = val;
            Monitor.Log($"You successfully set draw extra item notifications to {val}.");
        }

        private void DrawBars(string arg1, string[] arg2)
        {
            if (!bool.TryParse(arg2[0], out bool val))
                return;

            config.drawBars = val;
            Monitor.Log($"You successfully set draw XP bars to {val}.");
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

                foreach (int[] cats in categories)
                {
                    if (cats.Contains(cat) && ShouldDup(i))
                    {
                        item.Stack += 1;
                        while (ShouldDup(i))
                        {
                            item.Stack += 1;
                        }
                        if (config.drawExtraItemNotifications)
                            str = $"Your {snames[i]} level allowed you to obtain {item.Stack - tstack} extra {item.DisplayName}!";
                        break;
                    }
                    i++;
                }

                if (str.Length > 0 && item.salePrice() >= config.minItemPriceForNotifications)
                {
                    Game1.addHUDMessage(new HUDMessage(str, 2));
                }
            }
            catch (Exception ex)
            {
                instance.Monitor.Log($"Failed in {nameof(AITI2)}:\n{ex}", LogLevel.Error);
            }
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            // This method was empty in your original code, but it needs to exist.
        }

        DateTime otime;
        private void Display_Rendered(object sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (otime == default(DateTime))
                otime = DateTime.Now;

            if (xpBars.Count > 0)
            {
                for (int i = 0; i < xpBars.Count; i++)
                {
                    try
                    {
                        float bscale = 1.0f;
                        if (xpBars[i] == null)
                            continue;

                        Skill skill = xpBars[i].skill;
                        string name = String.Join(" ", skill.name.ToCharArray());

                        int startX = 8;
                        int startY = 8;
                        int sep = (int)(30 * bscale);
                        int barSep = (int)(60 * bscale);

                        int xp = skill.xp;
                        int xpc = skill.xpc;
                        int lev = skill.level;
                        int startXP = skill.getReqXP(lev - 1);
                        double deltaTime = DateTime.Now.Subtract(xpBars[i].time).TotalMilliseconds;
                        float transp;

                        if (deltaTime >= 0 && deltaTime <= 1000)
                        {
                            transp = ((float)deltaTime) / 1200.0f;
                        }
                        else if (deltaTime > 1000 && deltaTime <= 4000)
                        {
                            transp = 0.833f;
                        }
                        else
                        {
                            transp = ((float)(5000 - deltaTime)) / 1200.0f;
                        }

                        int curXP = xp;
                        int maxXP = skill.getReqXP(lev);

                        if (startXP > 0)
                        {
                            maxXP = maxXP - startXP;
                            curXP = curXP - startXP;
                            startXP = 0;
                        }

                        int iWidth = (int)(198 * bscale);
                        double mod = (maxXP > 0) ? iWidth / (maxXP * 1.0) : 0;
                        int bar2w = (int)Math.Round(xpc * mod) + 1;
                        int bar1w = (int)Math.Round(curXP * mod) - bar2w;

                        if (i == 0 && xpBars[i].ych < 0)
                        {
                            double ms = (DateTime.Now - otime).TotalMilliseconds;
                            double addv = (xpBars[i].ych + (ms / 15.625 * bscale));
                            xpBars[i].ych = (addv >= 0 ? 0 : addv);
                        }
                        else if (i == 0 && deltaTime >= 4000)
                        {
                            double addv = (deltaTime - 4000) / 15.625 * bscale;
                            xpBars[i].ych = (addv >= 64 ? 64 : addv);
                        }

                        if (xpBars.Count > 0 && i > 0)
                        {
                            xpBars[i].ych = xpBars[0].ych;
                        }


                        if (config.drawBars)
                        {
                            Vector2 r1d = new Vector2((float)Math.Round(214 * bscale), (float)Math.Round(64 * bscale));
                            Vector2 r2d = new Vector2((float)Math.Round(210 * bscale), (float)Math.Round(60 * bscale));
                            Vector2 r3d = new Vector2((float)Math.Round(200 * bscale), (float)Math.Round(20 * bscale));
                            Vector2 r4d = new Vector2(bar1w, (float)Math.Round(18 * bscale));
                            Vector2 r5d = new Vector2(bar2w, (float)Math.Round(18 * bscale));

                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(startX - 7, startY + (barSep * i) - 7 - xpBars[i].ychi, (int)r1d.X, (int)r1d.Y), Color.DarkRed * transp);
                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(startX - 5, startY + (barSep * i) - 5 - xpBars[i].ychi, (int)r2d.X, (int)r2d.Y), new Color(210, 173, 85) * transp);

                            Game1.spriteBatch.DrawString(Game1.dialogueFont, $"{name}", new Vector2((int)Math.Round(((startX - 7 + r1d.X) / 2.0) - (Game1.dialogueFont.MeasureString(name).X * (Game1.pixelZoom / 6.0f / 2.0f) * bscale)), (startY - 3 + (barSep * i) - xpBars[i].ychi) * bscale), new Color(30, 3, 0) * (transp * 1.1f), 0.0f, Vector2.Zero, (float)(Game1.pixelZoom / 6f * bscale), SpriteEffects.None, 0.5f);
                            Game1.spriteBatch.DrawString(Game1.dialogueFont, $"{name}", new Vector2((int)Math.Round(((startX - 7 + r1d.X) / 2.0) - (Game1.dialogueFont.MeasureString(name).X * (Game1.pixelZoom / 6.0f / 2.0f) * bscale)) + 1, (startY - 3 + (barSep * i) - xpBars[i].ychi + 1) * bscale), new Color(90, 35, 0) * (transp), 0.0f, Vector2.Zero, (float)(Game1.pixelZoom / 6.0f * bscale), SpriteEffects.None, 0.5f);

                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(startX, startY + (barSep * i) + sep - xpBars[i].ychi, (int)r3d.X, (int)r3d.Y), Color.Black * transp);
                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(startX + 1, startY + (barSep * i) + sep + 1 - xpBars[i].ychi, bar1w, (int)r4d.Y), Color.SeaGreen * transp);
                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(startX + 1 + bar1w, startY + (barSep * i) + sep + 1 - xpBars[i].ychi, bar2w, (int)r5d.Y), Color.Turquoise * transp);

                            Vector2 mPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
                            Vector2 bCenter = new Vector2(startX + (200 / 2), startY + (barSep * i) + sep + (20 / 2) - xpBars[i].ychi);
                            float dist = Vector2.Distance(mPos, bCenter);

                            if (dist <= 250f)
                            {
                                float f = Math.Min(25f / dist, 1.0f);
                                string xpt = $"{curXP} / {maxXP}";
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, xpt, new Vector2((int)Math.Round(((startX + 200) / 2.0) - (Game1.dialogueFont.MeasureString(xpt).X * (Game1.pixelZoom / 10.0f / 2.0f))), startY + (barSep * i) + sep + 1 - xpBars[i].ychi), Color.White * f * (transp + 0.05f), 0.0f, Vector2.Zero, (Game1.pixelZoom / 10f), SpriteEffects.None, 0.5f);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Non-Serious draw violation: {ex.Message}");
                        continue;
                    }
                }
            }
            otime = DateTime.Now;
        }

        private void SetXP(string command, string[] arg)
        {
            if (!Context.IsWorldReady || arg.Length < 2 || !int.TryParse(arg[1], out int xp))
            {
                Monitor.Log("No skill name entered or the xp was not a whole number.");
                return;
            }

            Skill skill = skills.SingleOrDefault(sk => sk.name.ToLower() == arg[0].ToLower());

            if (skill == null)
            {
                Monitor.Log($"Invalid skill name: {arg[0]}");
                return;
            }

            if (skill.key < 5)
                Game1.player.experiencePoints[skill.key] = xp;
            else
                skill.xp = xp;
        }

        private new static IMonitor Monitor;
        public static void Initialize(IMonitor monitor) { Monitor = monitor; }
        private void ControlEvent_KeyReleased(object sender, ButtonReleasedEventArgs e) { if (!Context.IsWorldReady) return; }
        private void GameEvents_FirstUpdateTick(object sender, EventArgs e) { }

        private void SetTimer(int time, int index)
        {
            if (index == 0)
            {
                aTimer = new System.Timers.Timer(1100);
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = false;
                aTimer.Enabled = true;
            }
            else if (index == 1)
            {
                aTimer2 = new System.Timers.Timer(time);
                aTimer2.Elapsed += OnTimedEvent2;
                aTimer2.AutoReset = false;
                aTimer2.Enabled = true;
            }
            else if (index == 2)
            {
                shouldDraw = new System.Timers.Timer(time);
                shouldDraw.Elapsed += sDrawEnd;
                shouldDraw.AutoReset = false;
                shouldDraw.Enabled = true;
            }
        }

        private void sDrawEnd(object sender, ElapsedEventArgs e) { shouldDraw.Enabled = false; }

        public void EndXPBar(int key)
        {
            var bar = xpBars.SingleOrDefault(x => x.skill.key == key);
            if (bar != null)
            {
                xpBars.Remove(bar);
                if (xpBars.Count > 0)
                {
                    xpBars[0].ych = 0;
                }
            }
        }

        private void OnTimedEvent2(object sender, ElapsedEventArgs e) { if (mpmod != null) mpMult = mpmod.Exp_Rate(); aTimer2.Enabled = false; }
        private void XPT(string arg1, string[] arg2)
        {
            Monitor.Log("Skill:  | Level:  |  Current Experience:  | Experience Needed:", LogLevel.Info);
            for (int i = 0; i < skills.Count; i++)
            {
                int xpn = skills[i].getReqXP(skills[i].level);
                Monitor.Log($"{skills[i].name} | {skills[i].level} | {skills[i].xp} | {xpn}", LogLevel.Info);
            }
        }

        private void SM(string command, string[] args)
        {
            if (args.Length < 1 || args[0] == null || !double.TryParse(args[0], out double n))
            {
                Monitor.Log("No decimal value found.");
                return;
            }
            s_mod = n;
            Monitor.Log($"Modifier set to {n * 100}%.");
        }

        private void OnXPChanged(object sender, EXPEventArgs e)
        {
            XPBar bar = xpBars.SingleOrDefault(b => b.skill.key == e.key);
            Skill skill = skills.SingleOrDefault(sk => sk.key == e.key);

            if (skill == null || skill.xpc < 0 || skill.xpc > 100001 || (shouldDraw != null && shouldDraw.Enabled))
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
                xpBars.Add(new XPBar(skill));
            }
        }

        public void sortByTime() { xpBars = xpBars.OrderBy(o => o.time).ToList(); }
        public void setYchVals(double val) { foreach (var bar in xpBars) { bar.ych = val; } }
        private void TellXP(string command, string[] args)
        {
            if (args.Length < 1)
                return;

            Skill skill = skills.SingleOrDefault(sk => sk.name.ToLower() == args[0].ToLower());
            if (skill == null)
            {
                Monitor.Log("Could not find a match for given skill name.");
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
            Monitor.Log(str);
        }

        private void SetLev(string command, string[] args)
        {
            if (args.Length < 2 || args[0] == null || args[1] == null || !int.TryParse(args[1], out int n))
            {
                Monitor.Log($"Function Failed!");
                return;
            }
            if (n < 0 || n > 100)
            {
                Monitor.Log($"Function Failed!");
                return;
            }
            Skill skill = skills.SingleOrDefault(sk => sk.name.ToLower() == args[0].ToLower());
            if (skill == null)
                return;

            skill.level = n;
            if (skill.key == 1)
                this.Helper.GameContent.InvalidateCache("Data/Fish");
        }

        private void WmT(string command, string[] args)
        {
            wm = !wm;
            Monitor.Log($"Overworld Monster Spawning -> {(wm ? "ON" : "OFF")}.");
        }

        private void XpM(string command, string[] args)
        {
            if (args.Length > 1 && double.TryParse(args[1], out double x) && x > 0.0)
            {
                Skill skill = skills.SingleOrDefault(sk => sk.name.ToLower() == args[0].ToLower());
                if (skill == null)
                    return;
                skill.xp_mod = x;
                Monitor.Log($"The XP modifier for {skill.name} was set to: {x}");
            }
            else
            {
                Monitor.Log($"Valid decimal not used; refer to help command.");
            }
        }

        private void Display_MenuChanged(object sender, MenuChangedEventArgs e) { if (!Context.IsWorldReady || e.OldMenu == null) return; }
        public void Closing() { }
        public List<int> defReqXPs = new List<int> { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };
        private void ControlEvent_KeyPressed(object sender, ButtonPressedEventArgs e) { if (!Context.IsWorldReady) return; }

        private void GameEvents_OneSecondTick(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.IsMultipleOf(3600))
            {
                monsters.RemoveAll(mon => mon == null || mon.Health <= 0 || mon.currentLocation == null);
            }

            if (e.IsMultipleOf(1800))
            {
                for (int i = 0; i < skillLevs.Count; i++)
                {
                    skillLevs[i] = skills[i].level;
                }
            }

            if (skills.Count > 4)
            {
                for (int i = 0; i < 5; i++)
                {
                    Skill skill = skills.SingleOrDefault(sk => sk.key == i);
                    if (skill == null)
                    {
                        Monitor.Log($"LE ERROR - Skill {snames[i]} not registered properly for exp gain, please restart and/or report if no change.");
                    }
                    if (skill.xp != Game1.player.experiencePoints[i])
                    {
                        skill.xp = Game1.player.experiencePoints[i];
                    }
                }
            }

            if (!no_mons && wm && Game1.player.currentLocation.IsOutdoors && Game1.activeClickableMenu == null && rand.NextDouble() <= S_R())
            {
                Vector2 loc = Game1.player.currentLocation.getRandomTile();
                while (!(Game1.player.currentLocation.isTilePlaceable(loc)))
                {
                    loc = Game1.player.currentLocation.getRandomTile();
                }

                int tier = rand.Next(0, 9);
                Monster m = GetMonster(tier, loc * (float)Game1.tileSize);
                if (tier == 8)
                {
                    tier = 5;
                    m.resilience.Value += 20;
                    m.Slipperiness += rand.Next(10) + 5;
                    m.startGlowing(new Color(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)), true, 1.0f);
                    m.Health *= 1 + (rand.Next(Game1.player.CombatLevel / 2, Game1.player.CombatLevel));
                    var data = Game1.content.Load<Dictionary<int, string>>("Data\\ObjectInformation");
                    m.objectsToDrop.Add(rand.Next(data.Count).ToString());
                    m.displayName += ": LE BOSS";
                    m.Scale = m.Scale * (float)(1 + (rand.NextDouble() * Game1.player.CombatLevel / 25.0));
                }
                else
                {
                    tier = 1;
                }

                m.DamageToFarmer = (int)(m.DamageToFarmer / 1.5) + (int)(Game1.player.combatLevel.Value / 3);
                m.Health *= 1 + (Game1.player.CombatLevel / 4);
                m.focusedOnFarmers = true;
                m.wildernessFarmMonster = true;
                m.Speed += rand.Next((int)Math.Round((Game1.player.combatLevel.Value / 10.0)));
                m.resilience.Value = m.resilience.Value + (Game1.player.combatLevel.Value / 10);
                m.ExperienceGained += (int)(m.Health / 100.0) + ((10 + (Game1.player.combatLevel.Value * 2)) * tier);

                Game1.currentLocation.characters.Add((NPC)m);
                total_m++;

                if (tier == 5)
                    Game1.chatBox.addMessage($"A boss has spawned in your current location!", Color.DarkRed);
                monsters.Add(m);
            }
        }

        public double S_R()
        {
            if (Game1.player.combatLevel.Value == 0) return 0.0;
            if (s_mod != -1.0) return s_mod;
            if (API != null && API.overSR != -1.0) return API.overSR;
            if (Game1.isDarkOut(Game1.currentLocation) || Game1.isRaining) return (0.01 + (Game1.player.combatLevel.Value * 0.0001)) * 1.5;
            return (0.01 + (Game1.player.combatLevel.Value * 0.0001));
        }

        private void GameEvents_QuarterSecondTick(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.player.UsingTool && initialtooluse == false)
            {
                oStamina = Game1.player.Stamina;
                initialtooluse = true;
            }
            else if (!Game1.player.UsingTool && initialtooluse == true)
            {
                if (Game1.player.Stamina > oStamina)
                {
                    Game1.player.Stamina = Math.Max(oStamina - 0.5f, 0.0f);
                }
                oStamina = 0.0f;
                initialtooluse = false;
            }

            if (e.IsMultipleOf(8))
            {
                if (Game1.activeClickableMenu is BobberBar && !firstFade)
                {
                    int bobberBonus = 0;
                    Tool tool = Game1.player.CurrentTool;
                    bool beginnersRod = tool != null && tool is FishingRod && tool.UpgradeLevel == 1;

                    foreach (var attachment in tool.attachments.Where(n => n != null))
                    {
                        if (attachment.name == "Cork Bobber") bobberBonus = 24;
                    }

                    if (Game1.player.FishingLevel > 99) bobberBonus += 8;
                    else if (Game1.player.FishingLevel > 74) bobberBonus += 6;
                    else if (Game1.player.FishingLevel > 49) bobberBonus += 4;
                    else if (Game1.player.FishingLevel > 24) bobberBonus += 2;

                    int bobberBarSize;
                    if (!(this.Helper.ModRegistry.IsLoaded("DevinLematty.ExtremeFishingOverhaul")))
                    {
                        if (beginnersRod) bobberBarSize = 80 + (5 * 9);
                        else if (Game1.player.FishingLevel < 11) bobberBarSize = 80 + bobberBonus + (int)(Game1.player.FishingLevel * 9);
                        else bobberBarSize = 165 + bobberBonus + (int)(Game1.player.FishingLevel * (0.5 + (rand.NextDouble() / 2.0)));
                    }
                    else
                    {
                        if (beginnersRod) bobberBarSize = 80 + (5 * 7);
                        else if (Game1.player.FishingLevel < 11) bobberBarSize = 80 + bobberBonus + (int)(Game1.player.FishingLevel * 7);
                        else if (Game1.player.FishingLevel > 10 && Game1.player.FishingLevel < 20) bobberBarSize = 150 + bobberBonus + (int)(Game1.player.FishingLevel);
                        else bobberBarSize = 170 + bobberBonus + (int)(Game1.player.FishingLevel * 0.8 * (0.5 + (rand.NextDouble() / 2.0)));
                    }

                    firstFade = true;
                    this.Helper.Reflection.GetField<int>(Game1.activeClickableMenu, "bobberBarHeight").SetValue(bobberBarSize);
                    this.Helper.Reflection.GetField<float>(Game1.activeClickableMenu, "bobberBarPos").SetValue((float)(568 - bobberBarSize));
                }
                else if (!(Game1.activeClickableMenu is BobberBar) && firstFade)
                {
                    firstFade = false;
                }
                else if (Game1.activeClickableMenu is BobberBar && firstFade)
                {
                    bool bobberInBar = this.Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "bobberInBar").GetValue();
                    if (!bobberInBar)
                    {
                        float dist = this.Helper.Reflection.GetField<float>(Game1.activeClickableMenu, "distanceFromCatching").GetValue();
                        this.Helper.Reflection.GetField<float>(Game1.activeClickableMenu, "distanceFromCatching").SetValue(dist + ((float)(Game1.player.FishingLevel - 10) / 22000.0f));
                    }
                }
            }
        }

        public static bool ShouldDup(int index)
        {
            double drate = 0.002;
            if (index == 0 || index == 2)
                drate = 0.002 / 2.0;

            if (skillLevs.Count > index && rand.NextDouble() <= (skillLevs[index] * drate))
            {
                return true;
            }
            return false;
        }

        private void TimeEvent_AfterDayStarted(object sender, DayStartedEventArgs e)
        {
            if (Context.IsSplitScreen)
            {
                Monitor.Log("LE: Splitscreen Multiplayer is not currently supported. Mod will not load.");
                return;
            }

            Farm farm = Game1.getFarm();
            double gchance = Game1.player.farmingLevel.Value * 0.0002;
            double pchance = Game1.player.farmingLevel.Value * 0.001;
            foreach (Vector2 key in farm.terrainFeatures.Keys)
            {
                if (farm.terrainFeatures[key] is HoeDirt tf && tf.crop != null && rand.NextDouble() < gchance)
                {
                    tf.crop.growCompletely();
                }
                else if (farm.terrainFeatures[key] is HoeDirt tf2 && tf2.crop != null && rand.NextDouble() < pchance)
                {
                    tf2.crop.currentPhase.Value = Math.Min(tf2.crop.currentPhase.Value + 1, tf2.crop.phaseDays.Count - 1);
                }
            }

            if (!mpload && this.Helper.ModRegistry.IsLoaded("f1r3w477.Level_Extender"))
            {
                mpmod = this.Helper.ModRegistry.GetApi<MPModApi>("f1r3w477.Level_Extender");
                mpload = true;
                SetTimer(1000, 1);
            }
            else if (mpload)
            {
                SetTimer(1000, 1);
            }
            no_mons = false;
        }

        public void Rem_mons()
        {
            no_mons = true;
            int x = 0;
            int y;
            foreach (GameLocation location in Game1.locations)
            {
                y = location.characters.Count;
                location.characters.RemoveWhere(c => c.IsMonster);
                x += (y - location.characters.Count);
            }
            Monitor.Log($"Removed | {x} | / | {total_m} | monsters.");
            total_m = 0;
        }

        private Monster GetMonster(int x, Vector2 loc)
        {
            Monster m;
            switch (x)
            {
                case 0: m = new DustSpirit(loc); break;
                case 1: m = new Grub(loc); break;
                case 2: m = new Skeleton(loc); break;
                case 3: m = new RockCrab(loc); break;
                case 4: m = new Ghost(loc); break;
                case 5: m = new GreenSlime(loc); break;
                case 6: m = new RockGolem(loc); break;
                case 7: m = new ShadowBrute(loc); break;
                case 8:
                    int y = rand.Next(1, 6);
                    if (y == 1) m = new RockCrab(loc, "Iridium Crab");
                    else if (y == 2) m = new Ghost(loc, "Carbon Ghost");
                    else if (y == 3) m = new RockCrab(loc, "Lava Crab");
                    else if (y == 4) m = new GreenSlime(loc, Math.Max(Game1.player.combatLevel.Value * 5, 50));
                    else if (y == 5) m = new BigSlime(loc, Math.Max(Game1.player.combatLevel.Value * 5, 50));
                    else m = new Mummy(loc);
                    break;
                default: m = new GreenSlime(loc); break; // Default to a basic monster
            }
            return m;
        }

        Timer shouldDraw;

        private void SaveEvents_AfterLoad(object sender, SaveLoadedEventArgs e)
        {
            SetTimer(2000, 2);
            try
            {
                Monitor.Log("Starting skill load for LE");
                var config_t = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();

                string[] sdnames = { "Farming", "Fishing", "Foraging", "Mining", "Combat" };
                int[] cats0 = { -16, -74, -75, -79, -80, -81 };
                int[] cats1 = { -4 };
                int[] cats2 = cats0;
                int[] cats3 = { -2, -12, -15 };
                int[] cats4 = { -28, -29, -95, -96, -98 };
                List<int[]> cats = new List<int[]>() { cats0, cats1, cats2, cats3, cats4 };

                int count = 0;
                if (config_t.skills != null)
                {
                    foreach (string str in config_t.skills)
                    {
                        Monitor.Log($"skill load - {str}");
                        string[] vals = str.Split(',');
                        Skill sk = new Skill(LE, vals[0], int.Parse(vals[1]), double.Parse(vals[2]), new List<int>(defReqXPs), cats[count]);
                        skills.Add(sk);
                        snames.Add(sk.name);
                        categories.Add(sk.cats);
                        skillLevs.Add(sk.level);
                        count++;
                    }
                }


                for (int i = count; i < 5; i++)
                {
                    Monitor.Log($"adding skills - {i}, dxp: {Game1.player.experiencePoints[i]}");
                    Skill sk = new Skill(LE, sdnames[i], Game1.player.experiencePoints[i], 1.0, new List<int>(defReqXPs), cats[i]);
                    skills.Add(sk);
                    snames.Add(sk.name);
                    categories.Add(sk.cats);
                    skillLevs.Add(sk.level);
                }

                wm = config_t.WorldMonsters;
                config = config_t;
            }
            catch (Exception ex)
            {
                Monitor.Log($"LE failed loading skills, mod will not start: {ex.Message}", LogLevel.Trace);
            }
            this.Helper.GameContent.InvalidateCache("Data/Fish");
        }

        private void SaveEvents_BeforeSave(object sender, SavingEventArgs e)
        {
            config.skills = new List<string>();
            foreach (Skill skill in skills)
            {
                config.skills.Add($"{skill.name},{skill.xp},{skill.xp_mod}");
            }
            config.WorldMonsters = wm;
            this.Helper.Data.WriteJsonFile<ModData>($"data/{Constants.SaveFolderName}.json", config);

            if (!no_mons)
            {
                Rem_mons();
            }
        }

        private void SaveEvents_AfterReturnToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            wm = new bool();
            firstFade = false;
            config = new ModData();

            skills = new List<Skill>();
            snames = new List<string>();
            categories = new List<int[]>();
            skillLevs = new List<int>();
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

            Skill s = skills.SingleOrDefault(sk => sk.name.ToLower() == arg1);
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
            Skill sk = new Skill(LE, name, xp, xp_mod, xp_table, cats);
            if (sk == null) return -1;
            skills.Add(sk);
            return 0;
        }
    }
}