using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelExtender
{
    /// <summary>
    /// Represents a player skill, handling its level, experience, and interactions with the game.
    /// </summary>
    public class Skill
    {
        // A constant makes the code clearer than using the "magic number" 5.
        private const int VanillaSkillCount = 5;

        // Backing fields for properties are private and use _camelCase.
        private readonly ModEntry _mod;
        private readonly List<int> _experienceTable;
        private int _level;
        private int _experience;

        /// <summary>A flag to indicate if a level change is coming from an XP update.</summary>
        /// <remarks>This prevents the experience from being reset when a level-up occurs naturally.</remarks>
        private bool _isLevelingViaExperience = false;

        #region Properties
        // Properties use PascalCase and provide controlled access (encapsulation).
        // 'private set' prevents other classes from changing these values directly.

        /// <summary>Gets a value indicating whether this is a vanilla skill (Farming, Fishing, etc.).</summary>
        public bool IsVanillaSkill => this.Key < VanillaSkillCount;

        /// <summary>Gets the display name of the skill.</summary>
        public string Name { get; private set; }

        /// <summary>Gets the unique key (index) for this skill.</summary>
        public int Key { get; private set; }

        /// <summary>Gets the modifier for calculating required experience past level 10.</summary>
        public double ExperienceModifier { get; set; }

        /// <summary>Gets a read-only view of the experience required for each level.</summary>
        public IReadOnlyList<int> ExperienceTable => _experienceTable;

        /// <summary>Gets the item categories associated with this skill, if any.</summary>
        public IReadOnlyList<int> Categories { get; private set; }

        /// <summary>
        /// Gets or sets the current level of the skill.
        /// Setting the level also updates the vanilla game state and can reset experience points.
        /// </summary>
        public int Level
        {
            get => _level;
            set
            {
                if (_level == value) return;

                _level = value;

                // Update the corresponding vanilla skill level in the game.
                // A switch statement is cleaner than a long if/else if chain.
                if (this.IsVanillaSkill)
                {
                    switch (this.Key)
                    {
                        case 0: Game1.player.farmingLevel.Value = _level; break;
                        case 1: Game1.player.fishingLevel.Value = _level; break;
                        case 2: Game1.player.foragingLevel.Value = _level; break;
                        case 3: Game1.player.miningLevel.Value = _level; break;
                        case 4: Game1.player.combatLevel.Value = _level; break;
                    }
                }

                // If the level was set manually (e.g., via a command), update the XP to match.
                // If it was triggered by an XP gain, this block is skipped to preserve the current XP.
                if (!_isLevelingViaExperience)
                {
                    // Get the XP required for the START of the current level.
                    // A level of 0 has 0 XP.
                    int requiredXp = (_level > 0) ? this.GetRequiredExperienceForLevel(_level - 1) : 0;

                    // Set the Experience property, which handles all other updates.
                    this.Experience = requiredXp;
                }

                // Reset the flag after the operation is complete.
                _isLevelingViaExperience = false;
            }
        }

        /// <summary>
        /// Gets or sets the total experience points for the skill.
        /// Setting the experience may trigger a level-up.
        /// </summary>
        public int Experience
        {
            get => _experience;
            set
            {
                if (_experience == value) return;

                _experience = value;
                if (this.IsVanillaSkill)
                {
                    Game1.player.experiencePoints[this.Key] = _experience;
                }

                this.CheckForLevelUp();

                // Note: The EXPEventArgs class might need the amount of XP gained.
                // If so, you would calculate it here: int xpGained = value - _experience;
                _mod.Events.RaiseEvent(new EXPEventArgs { Key = this.Key });
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Skill"/> class.
        /// </summary>
        public Skill(ModEntry mod, string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] categories = null)
        {
            _mod = mod ?? throw new ArgumentNullException(nameof(mod));
            this.Name = name;
            this.Key = mod.Skills.Count;

            _experienceTable = xpTable ?? new List<int>();
            this.ExperienceModifier = xpModifier ?? 1.0;
            this.Categories = categories ?? Array.Empty<int>();

            // --- LOGIC CHANGE HIGHLIGHTED BELOW ---

            // 1. Set the experience from the provided value. This is our source of truth.
            _experience = currentXp;

            // 2. Generate the experience table so we can calculate the level.
            this.GenerateExperienceTable(101);

            // 3. Calculate the correct initial level directly from the experience.
            _level = this.GetLevelByExperience();

            // 4. For vanilla skills, sync BOTH the correct level and experience back to the game state.
            if (this.IsVanillaSkill)
            {
                Game1.player.experiencePoints[this.Key] = _experience;
                switch (this.Key)
                {
                    case 0: Game1.player.farmingLevel.Value = _level; break;
                    case 1: Game1.player.fishingLevel.Value = _level; break;
                    case 2: Game1.player.foragingLevel.Value = _level; break;
                    case 3: Game1.player.miningLevel.Value = _level; break;
                    case 4: Game1.player.combatLevel.Value = _level; break;
                }
            }
        }

        /// <summary>
        /// Checks if the current experience total corresponds to a different level and applies the change.
        /// This handles both leveling up and leveling down.
        /// </summary>
        private void CheckForLevelUp()
        {
            int levelFromXp = this.GetLevelByExperience();

            // The handles both gaining **and** losing levels. Hence !=
            if (levelFromXp != _level)
            {
                _isLevelingViaExperience = true;
                this.Level = levelFromXp;
            }
        }

        /// <summary>
        /// Calculates the level based on the current total experience points.
        /// </summary>
        /// <returns>The calculated skill level.</returns>
        private int GetLevelByExperience()
        {
            while (_experienceTable.Any() && _experience >= _experienceTable.Last())
            {
                // If XP is higher than the max in the table, generate more levels.
                GenerateExperienceTable(_experienceTable.Count + 10);
            }
            return _experienceTable.Count(reqXp => _experience >= reqXp);
        }

        /// <summary>
        /// Gets the total experience points required to reach a given level.
        /// </summary>
        /// <param name="levelIndex">The target level (e.g., level 1, 10, 50).</param>
        /// <returns>The total experience points needed.</returns>
        public int GetRequiredExperienceForLevel(int levelIndex)
        {
            if (levelIndex < 0) return 0;

            if (_experienceTable.Count <= levelIndex)
            {
                GenerateExperienceTable(levelIndex + 1);
            }

            return _experienceTable[levelIndex];
        }

        /// <summary>
        /// Populates the experience table up to a specified level.
        /// </summary>
        /// <param name="targetLevel">The target number of levels to generate in the experience table.</param>
        private void GenerateExperienceTable(int targetLevel)
        {
            // A named constant is clearer than the number 11.
            const int customCurveStartLevel = 11;

            // Use vanilla XP values for the first 10 levels if the table is empty.
            if (_experienceTable.Count == 0 && _mod.DefaultRequiredXp.Any())
            {
                _experienceTable.AddRange(_mod.DefaultRequiredXp);
            }

            // If we already have enough levels, we don't need to do anything.
            if (_experienceTable.Count >= targetLevel)
            {
                return;
            }

            // Get constant values outside the loop.
            double baseXp = _mod.Config.LevelingCurveBaseExperience;
            double growthRate = _mod.Config.LevelingCurveGrowthRate;
            int startLevel = _experienceTable.Count + 1;

            // Pre-calculate the initial power value ONCE before the loop.
            double power = Math.Pow(growthRate, startLevel - customCurveStartLevel);

            // Iteratively generate XP requirements for levels beyond the current table size.
            for (int i = _experienceTable.Count; i < targetLevel; i++)
            {
                int previousXp = _experienceTable[i - 1];
                int additionalXp = (int)Math.Round(baseXp * power * this.ExperienceModifier);
                int requiredXp = previousXp + additionalXp;
                _experienceTable.Add(requiredXp);

                // Update the power for the next loop iteration with a simple multiplication.
                power *= growthRate;
            }
        }
    }
}