using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace LevelExtender
{
    /// <summary>A simple data class for robust JSON serialization of skill data.</summary>
    public record SkillData(string Name, int Experience, double ExperienceModifier);

    public class ModEntry : Mod
    {
        #region Fields

        public static ModEntry Instance { get; private set; }

        private Harmony _harmony;
        private ModConfig _config;
        private LEModApi _api;
        private LEEvents _events;

        private readonly Random _random = new();
        private readonly List<Skill> _skills = new();
        private readonly List<XPBar> _xpBars = new();
        private DateTime _lastRenderTime;

        // Game state variables
        private bool _isFishingBobberLogicActive;
        private bool _isInitialToolUse;
        private float _originalStamina;
        private bool _disableMonsterSpawningThisSession;
        private int _totalMonstersSpawned;

        #endregion
        
        #region Properties

        /// <summary>Provides controlled, read-only access to the list of active skills.</summary>
        public IReadOnlyList<Skill> Skills => _skills;

        /// <summary>The default experience points required for the first 10 levels.</summary>
        public readonly List<int> DefaultRequiredXP = new() { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

        #endregion

        #region Mod Lifecycle

        /// <summary>The main entry point for the mod, called once by SMAPI.</summary>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            _events = new LEEvents();
            _config = this.Helper.ReadConfig<ModConfig>();

            // Event Subscriptions
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdate;
            helper.Events.Display.Rendered += this.OnRendered;
            _events.OnXPChanged += this.OnExperienceChanged;

            // Harmony Patching
            _harmony = new Harmony(this.ModManifest.UniqueID);
            _harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(AddItemToInventoryPrefix))
            );

            // Console Commands
            var commands = new Commands(this);
            helper.ConsoleCommands.Add("le_xp", "Shows a summary of your current levels and experience.", commands.ShowExperienceSummary);
            helper.ConsoleCommands.Add("le_setlevel", "Sets a skill level. Usage: le_setlevel <skill> <level>", commands.SetLevel);
            helper.ConsoleCommands.Add("le_setxp", "Sets a skill's XP. Usage: le_setxp <skill> <amount>", commands.SetExperience);
            helper.ConsoleCommands.Add("le_toggle_monsters", "Toggles wilderness monster spawning for this save.", commands.ToggleWorldMonsters);
            helper.ConsoleCommands.Add("le_toggle_xpbars", "Toggles visibility of the XP bars.", commands.ToggleDrawBars);
            helper.ConsoleCommands.Add("le_xp_modifier", "Sets the XP modifier for a skill. Usage: le_xp_modifier <skill> <modifier>", commands.SetExperienceModifier);
            helper.ConsoleCommands.Add("le_xp_table", "Displays the full XP table for a specific skill. Usage: le_xp_table <skill>", commands.ShowSkillXpTable);
            helper.ConsoleCommands.Add("le_toggle_notifications", "Toggles the 'extra item' notifications.", commands.ToggleExtraItemNotifications);
            helper.ConsoleCommands.Add("le_min_notification_price", "Sets the minimum price for an item to trigger an 'extra item' notification.", commands.SetMinNotificationPrice);
        }

        /// <summary>Expose the mod's API to other mods.</summary>
        public override object GetApi() => _api ??= new LEModApi(this);

        /// <summary>Provides access to the mod's custom events. For use by the API.</summary>
        public LEEvents Events => _events;

        #endregion

        #region Event Handlers

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _skills.Clear();

            // --- Data Migration Logic Starts Here ---

            string skillsFilePath = $"data/{Constants.SaveFolderName}.json";
            List<SkillData> skillDataList = null;

            // Read the file generically to check its structure without crashing.
            JToken parsedJson = this.Helper.Data.ReadJsonFile<JToken>(skillsFilePath);

            if (parsedJson is JObject oldFormatObject && oldFormatObject.ContainsKey("skills"))
            {
                // OLD FORMAT DETECTED: It's an object with a "skills" property. Let's migrate it.
                this.Monitor.Log("Old save data format detected. Migrating to new format...", LogLevel.Info);
                skillDataList = new List<SkillData>();

                // Get the list of skill strings (e.g., "Farming,15000,1.0")
                JArray oldSkillsArray = oldFormatObject["skills"] as JArray;
                if (oldSkillsArray != null)
                {
                    foreach (var skillString in oldSkillsArray)
                    {
                        try
                        {
                            string[] parts = skillString.ToString().Split(',');
                            if (parts.Length == 3)
                            {
                                string name = parts[0];
                                int experience = int.Parse(parts[1]);
                                double modifier = double.Parse(parts[2]);
                                skillDataList.Add(new SkillData(name, experience, modifier));
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"Could not parse an old skill entry ('{skillString}'). It will be skipped. Error: {ex.Message}", LogLevel.Error);
                        }
                    }
                }
            }
            else if (parsedJson is JArray newFormatArray)
            {
                // NEW FORMAT DETECTED: It's already an array, so deserialize it normally.
                skillDataList = newFormatArray.ToObject<List<SkillData>>();
            }

            // If the file didn't exist or was empty, initialize an empty list.
            skillDataList ??= new List<SkillData>();

            // --- Data Migration Logic Ends Here ---


            // The rest of the method proceeds normally using the 'skillDataList' we now have.
            var loadedSkillNames = new HashSet<string>(skillDataList.Select(s => s.Name));

            // Add skills from the save file
            foreach (var skillData in skillDataList)
            {
                var skill = new Skill(this, skillData.Name, skillData.Experience, skillData.ExperienceModifier, new List<int>(this.DefaultRequiredXP), GetCategoriesForSkill(skillData.Name));
                _skills.Add(skill);
            }

            // Add any missing vanilla skills
            string[] vanillaSkillNames = { "Farming", "Fishing", "Foraging", "Mining", "Combat" };
            for (int i = 0; i < vanillaSkillNames.Length; i++)
            {
                if (!loadedSkillNames.Contains(vanillaSkillNames[i]))
                {
                    var skill = new Skill(this, vanillaSkillNames[i], Game1.player.experiencePoints[i], 1.0, new List<int>(this.DefaultRequiredXP), GetCategoriesForSkill(vanillaSkillNames[i]));
                    _skills.Add(skill);
                }
            }

            this.Monitor.Log($"Level Extender: Loaded {_skills.Count} skills.", LogLevel.Info);

            // Don't forget to load your other per-save data
            var saveData = this.Helper.Data.ReadSaveData<SaveDataModel>("LevelExtender-SaveData") ?? new SaveDataModel();
            _config.EnableWorldMonsters = saveData.EnableWorldMonsters;
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            // Save the skill data to its own JSON file
            var skillsToSave = _skills
                .Select(s => new SkillData(s.Name, s.Experience, s.ExperienceModifier))
                .ToList();
            this.Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", skillsToSave);

            // Create an instance of model to hold other per-save settings.
            var saveData = new SaveDataModel
            {
                EnableWorldMonsters = _config.EnableWorldMonsters
            };

            // Save the entire model object.
            this.Helper.Data.WriteSaveData("LevelExtender-SaveData", saveData);

            // Clean up monsters before the save completes.
            if (!_disableMonsterSpawningThisSession)
            {
                RemoveAllMonsters();
            }
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            // Reset all state when returning to the main menu
            _skills.Clear();
            _xpBars.Clear();
            _config = new ModConfig();
            _isFishingBobberLogicActive = false;
            _disableMonsterSpawningThisSession = false;
            _totalMonstersSpawned = 0;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            _disableMonsterSpawningThisSession = false;
            ApplyDailyFarmGrowth();
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            HandleToolStamina();
            UpdateFishingBobber();
        }

        private void OnOneSecondUpdate(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            _xpBars.RemoveAll(bar => (DateTime.Now - bar.CreationTime).TotalSeconds > 5);

            SyncVanillaExperience();

            if (_config.EnableWorldMonsters && !_disableMonsterSpawningThisSession)
            {
                TrySpawnWildernessMonster();
            }
        }

        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady || !_config.DrawXpBars || !_xpBars.Any()) return;

            for (int i = 0; i < _xpBars.Count; i++)
            {
                DrawExperienceBar(e.SpriteBatch, _xpBars[i], i);
            }
            _lastRenderTime = DateTime.Now;
        }

        private void OnExperienceChanged(object sender, EXPEventArgs e)
        {
            var skill = _skills.FirstOrDefault(s => s.Key == e.Key);
            if (skill is null) return;

            _xpBars.RemoveAll(bar => bar.Skill.Key == e.Key);

            _xpBars.Add(new XPBar(skill));
            _xpBars.Sort((a, b) => a.CreationTime.CompareTo(b.CreationTime));
        }

        #endregion

        #region Harmony Patches

        public static void AddItemToInventoryPrefix(ref Item item)
        {
            if (item is null || item.HasBeenInInventory) return;

            try
            {
                foreach (var skill in Instance._skills)
                {
                    if (skill.Categories.Contains(item.Category) && ShouldDuplicateItem(skill))
                    {
                        int originalStack = item.Stack;
                        item.Stack++;
                        while (ShouldDuplicateItem(skill))
                        {
                            item.Stack++;
                        }

                        int newItems = item.Stack - originalStack;
                        if (Instance._config.DrawExtraItemNotifications && item.salePrice() >= Instance._config.MinItemPriceForNotifications)
                        {
                             string message = $"Your {skill.Name} skill granted you {newItems} extra {item.DisplayName}!";
                             Game1.addHUDMessage(new HUDMessage(message, HUDMessage.achievement_type));
                        }
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                Instance.Monitor.Log($"Failed in {nameof(AddItemToInventoryPrefix)}:\n{ex}", LogLevel.Error);
            }
        }

        #endregion

        #region Private Logic Methods

        private void SyncVanillaExperience()
        {
            foreach (var skill in _skills)
            {
                if (skill.Key < 5 && skill.Experience != Game1.player.experiencePoints[skill.Key])
                {
                    skill.Experience = Game1.player.experiencePoints[skill.Key];
                }
            }
        }

        /// <summary>Applies random growth boosts to crops on the farm at the start of a day.</summary>
        private void ApplyDailyFarmGrowth()
        {
            var farmingSkill = _skills.FirstOrDefault(s => s.Name == "Farming");
            if (farmingSkill is null) return;

            double instantGrowthChance = farmingSkill.Level * 0.0002;
            double phaseSkipChance = farmingSkill.Level * 0.001;

            var farm = Game1.getFarm();
            foreach (var dirt in farm.terrainFeatures.Values.OfType<StardewValley.TerrainFeatures.HoeDirt>())
            {
                if (dirt.crop != null)
                {
                    if (_random.NextDouble() < instantGrowthChance)
                        dirt.crop.growCompletely();
                    else if (_random.NextDouble() < phaseSkipChance)
                        dirt.crop.currentPhase.Value = Math.Min(dirt.crop.currentPhase.Value + 1, dirt.crop.phaseDays.Count - 1);
                }
            }
        }

        private void HandleToolStamina()
        {
            if (Game1.player.UsingTool && !_isInitialToolUse)
            {
                _originalStamina = Game1.player.Stamina;
                _isInitialToolUse = true;
            }
            else if (!Game1.player.UsingTool && _isInitialToolUse)
            {
                if (Game1.player.Stamina > _originalStamina)
                {
                    Game1.player.Stamina = Math.Max(_originalStamina - 0.5f, 0.0f);
                }
                _isInitialToolUse = false;
            }
        }

        private void UpdateFishingBobber()
        {
            if (Game1.activeClickableMenu is not BobberBar bar)
            {
                _isFishingBobberLogicActive = false;
                return;
            }

            if (!_isFishingBobberLogicActive)
            {
                // This logic runs once when the fishing minigame starts
                var fishingLevel = Game1.player.FishingLevel;
                int bobberBonus = 0;
                if (fishingLevel > 99) bobberBonus = 8;
                else if (fishingLevel > 74) bobberBonus = 6;
                else if (fishingLevel > 49) bobberBonus = 4;
                else if (fishingLevel > 24) bobberBonus = 2;

                int bobberBarSize = 80 + bobberBonus + (fishingLevel * 9); // simplified example

                this.Helper.Reflection.GetField<int>(bar, "bobberBarHeight").SetValue(bobberBarSize);
                this.Helper.Reflection.GetField<float>(bar, "bobberBarPos").SetValue(568 - bobberBarSize);
                _isFishingBobberLogicActive = true;
            }
            else
            {
                // This logic runs on subsequent ticks while the minigame is active
                bool bobberInBar = this.Helper.Reflection.GetField<bool>(bar, "bobberInBar").GetValue();
                if (!bobberInBar)
                {
                    float dist = this.Helper.Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
                    this.Helper.Reflection.GetField<float>(bar, "distanceFromCatching").SetValue(dist + ((Game1.player.FishingLevel - 10) / 22000.0f));
                }
            }
        }

        private void TrySpawnWildernessMonster()
        {
            if (Game1.player.currentLocation.IsOutdoors && Game1.activeClickableMenu == null && _random.NextDouble() <= GetMonsterSpawnRate())
            {
                 // Monster spawning logic from your original code would go here
                 // ...
            }
        }

        private void DrawExperienceBar(SpriteBatch b, XPBar bar, int index)
        {
            double elapsedSeconds = (_lastRenderTime == default) ? 0 : (DateTime.Now - _lastRenderTime).TotalSeconds;
            bar.HighlightTimer = Math.Max(0, bar.HighlightTimer - (float)elapsedSeconds);

            double fadeTime = (DateTime.Now - bar.CreationTime).TotalMilliseconds;
            float transparency = 1f;
            if (fadeTime < 500) transparency = (float)(fadeTime / 500.0);
            else if (fadeTime > 4500) transparency = 1 - ((float)(fadeTime - 4500) / 500.0f);

            if (transparency <= 0) return;

            int startX = 8, startY = 8 + (index * 72), width = 280, height = 72, barWidth = 240;

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), startX, startY, width, height, Color.White * transparency, 4f, true);

            Skill skill = bar.Skill;
            Rectangle iconRect = GetIconRectForSkill(skill.Key);
            b.Draw(Game1.mouseCursors, new Vector2(startX + 16, startY + 16), iconRect, Color.White * transparency, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

            Color levelTextColor = bar.HighlightTimer > 0 ? Color.LimeGreen : Game1.textColor;
            string levelText = $"Lvl {skill.Level}";
            Utility.drawTextWithShadow(b, skill.Name, Game1.smallFont, new Vector2(startX + 68, startY + 22), Game1.textColor * transparency);
            Utility.drawTextWithShadow(b, levelText, Game1.smallFont, new Vector2(startX + width - Game1.smallFont.MeasureString(levelText).X - 20, startY + 22), levelTextColor * transparency);

            int currentXpInLevel = skill.Experience - skill.GetRequiredExperienceForLevel(skill.Level - 1);
            int requiredXpForLevel = skill.GetRequiredExperienceForLevel(skill.Level) - skill.GetRequiredExperienceForLevel(skill.Level - 1);
            if (requiredXpForLevel <= 0) requiredXpForLevel = 1;

            float xpPercent = Math.Clamp((float)currentXpInLevel / requiredXpForLevel, 0f, 1f);
            int fillWidth = (int)(barWidth * xpPercent);

            b.Draw(Game1.staminaRect, new Rectangle(startX + 20, startY + 52, barWidth, 12), Color.Black * 0.35f);
            if (fillWidth > 0)
            {
                 b.Draw(Game1.staminaRect, new Rectangle(startX + 20, startY + 52, fillWidth, 12), new Color(15, 122, 255));
            }
        }

        #endregion

        #region Public Helper Methods

        /// <summary>Removes all monsters spawned by this mod from all game locations.</summary>
        public void RemoveAllMonsters()
        {
            _disableMonsterSpawningThisSession = true;
            int removedCount = 0;
            
            foreach (GameLocation location in Game1.locations)
            {
                removedCount += location.characters.RemoveWhere(c => c.IsMonster && ((Monster)c).wildernessFarmMonster);
            }
            this.Monitor.Log($"Removed {removedCount} / {_totalMonstersSpawned} wilderness monsters.", LogLevel.Info);
            _totalMonstersSpawned = 0;
        }

        #endregion

        #region Static Helpers

        private static bool ShouldDuplicateItem(Skill skill)
        {
            double baseRate = 0.002;
            if (skill.Key == 0 || skill.Key == 2) baseRate /= 2.0; // Farming or Foraging
            return Instance._random.NextDouble() < (skill.Level * baseRate);
        }

        private static double GetMonsterSpawnRate()
        {
             // Monster spawn rate logic from your original code's S_R() method
            return 0.01 + (Game1.player.CombatLevel * 0.0001);
        }

        private static Rectangle GetIconRectForSkill(int key) => key switch
        {
            0 => new Rectangle(10, 428, 10, 10), // Farming
            1 => new Rectangle(20, 428, 10, 10), // Fishing
            2 => new Rectangle(60, 428, 10, 10), // Foraging
            3 => new Rectangle(30, 428, 10, 10), // Mining
            4 => new Rectangle(120, 428, 10, 10),// Combat
            _ => new Rectangle(50, 428, 10, 10), // Luck (default)
        };

        private static int[] GetCategoriesForSkill(string name) => name switch
        {
            "Farming" => new[] { -75, -79, -80 },
            "Fishing" => new[] { -4 },
            "Foraging" => new[] { -81, -23, -16 },
            "Mining" => new[] { -15, -12, -2 },
            "Combat" => new[] { -96, -98 },
            _ => Array.Empty<int>()
        };

        #endregion

        #region Public API Methods

        /// <summary>Initializes and registers a new skill. This is intended to be called by other mods through the API.</summary>
        public void InitializeSkill(string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] itemCategories = null)
        {
            // Check if a skill with this name already exists to prevent duplicates.
            if (_skills.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                this.Monitor.Log($"A skill with the name '{name}' has already been initialized. Skipping.", LogLevel.Warn);
                return;
            }

            // Create the new skill and add it to the list.
            var newSkill = new Skill(this, name, currentXp, xpModifier, xpTable, itemCategories);
            _skills.Add(newSkill);

            this.Monitor.Log($"Successfully initialized custom skill: {name}", LogLevel.Info);
        }

        #endregion
    }
}

