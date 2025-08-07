using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelExtender
{
    /// <summary>
    /// Represents a player skill, handling its level, experience, and interactions with the game.
    /// This class is self-contained and does not directly interact with the live game state.
    /// </summary>
    public class Skill : ISkillApi
    {
        private const int VanillaSkillCount = 5;
        private const int DefaultExperienceTableSize = 101;

        // Backing fields for properties are private and use _camelCase.
        private readonly ModEntry _mod;
        private readonly List<int> _experienceTable;
        private int _level;
        private int _experience;

        /// <summary>A flag to indicate if a level change is coming from an XP update.</summary>
        /// <remarks>This prevents the experience from being reset when a level-up occurs naturally.</remarks>
        private bool _isLevelingViaExperience = false;

        #region Properties

        /// <summary>Gets a value indicating whether this is a vanilla skill (Farming, Fishing, etc.).</summary>
        public bool IsVanillaSkill => this.Key < VanillaSkillCount;

        /// <summary>Gets the display name of the skill.</summary>
        public string Name { get; private set; }

        /// <summary>Gets the unique key (index) for this skill.</summary>
        public int Key { get; private set; }

        /// <summary>Gets or sets the modifier for calculating required experience for levels greater than 10.</summary>
        public double ExperienceModifier { get; set; }

        /// <summary>Gets a read-only view of the experience required for each level.</summary>
        public IReadOnlyList<int> ExperienceTable => _experienceTable;

        /// <summary>Gets the item categories associated with this skill, if any.</summary>
        public IReadOnlyList<int> Categories { get; private set; }

        /// <summary>Gets or sets the current level of the skill. Setting this value will automatically update the experience to match.</summary>
        public int Level
        {
            get => _level;
            set
            {
                if (_level == value) return;
                _level = value;

                // If the level was set manually, update the XP to match the minimum for that level.
                // This is skipped if the level change was triggered by an XP gain.
                if (!_isLevelingViaExperience)
                {
                    int requiredXp = (_level > 0) ? this.GetRequiredExperienceForLevel(_level - 1) : 0;
                    this.Experience = requiredXp;
                }

                _isLevelingViaExperience = false;
            }
        }

        /// <summary>Gets or sets the current experience of the skill. Setting this value will automatically update the level to match.</summary>
        public int Experience
        {
            get => _experience;
            set
            {
                if (_experience == value) return;
                _experience = value;

                this.CheckForLevelUp();
                _mod.Events.RaiseEvent(new EXPEventArgs { Key = this.Key });
            }
        }

        #endregion

        /// <summary>Initializes a new instance of the <see cref="Skill"/> class.</summary>
        public Skill(ModEntry mod, string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] categories = null)
        {
            _mod = mod ?? throw new ArgumentNullException(nameof(mod));
            this.Name = name;
            this.Key = mod.Skills.Count();
            _experienceTable = xpTable ?? new List<int>();
            this.ExperienceModifier = xpModifier ?? 1.0;
            this.Categories = categories ?? Array.Empty<int>();

            // Set the initial state from the provided experience.
            _experience = currentXp;
            this.GenerateExperienceTable(DefaultExperienceTableSize);
            _level = this.GetLevelByExperience();
        }

        #region Public Methods

        /// <summary>Adds a specified amount of experience to the skill.</summary>
        /// <param name="amount">The non-negative amount of experience to add.</param>
        public void AddExperience(int amount)
        {
            if (amount > 0)
            {
                this.Experience += amount;
            }
        }

        /// <summary>Gets the total experience points required to reach a given level index.</summary>
        /// <param name="levelIndex">The 0-based index of the target level (e.g., level 1 is index 0).</param>
        /// <returns>The total experience points needed.</returns>
        public int GetRequiredExperienceForLevel(int levelIndex)
        {
            if (levelIndex < 0) return 0;

            // Dynamically generate more levels if the requested level is beyond our current table size.
            if (_experienceTable.Count <= levelIndex)
            {
                GenerateExperienceTable(levelIndex + 1);
            }

            return _experienceTable[levelIndex];
        }

        #endregion

        #region Private Methods

        /// <summary>Checks if the current experience total corresponds to a different level and applies the change.</summary>
        private void CheckForLevelUp()
        {
            int levelFromXp = this.GetLevelByExperience();
            if (levelFromXp != _level)
            {
                _isLevelingViaExperience = true;
                this.Level = levelFromXp;
            }
        }

        /// <summary>Calculates the level based on the current total experience points using a highly efficient binary search.</summary>
        private int GetLevelByExperience()
        {
            // Failsafe: ensure the table is large enough to contain our current XP.
            while (_experienceTable.Any() && _experience >= _experienceTable.Last())
            {
                GenerateExperienceTable(_experienceTable.Count + 10);
            }
            return FindLevelWithBinarySearch(_experienceTable, _experience);
        }

        /// <summary>Efficiently finds the level by binary search in the experience table.</summary>
        /// <param name="experienceTable">The sorted list of cumulative experience points required to reach each level.</param>
        /// <param name="experience">The player's current total experience points.</param>
        /// <returns>The calculated skill level corresponding to the given experience.</returns>
        private static int FindLevelWithBinarySearch(IReadOnlyList<int> experienceTable, int experience)
        {
            int left = 0;
            int right = experienceTable.Count - 1;
            int result = 0; // Default to level 0 if no thresholds are met

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (experience >= experienceTable[mid])
                {
                    // This is a potential level, so store it and check the upper half for a better one.
                    result = mid + 1;
                    left = mid + 1;
                }
                else
                {
                    // The target is in the lower half.
                    right = mid - 1;
                }
            }
            return result;
        }

        /// <summary>Populates the experience table up to a specified level.</summary>
        /// <param name="targetLevel">The target number of levels to generate in the experience table.</param>
        private void GenerateExperienceTable(int targetLevel)
        {
            const int customCurveStartLevel = 11;

            // Use vanilla XP values for the first 10 levels if the table is empty.
            if (_experienceTable.Count == 0 && _mod.DefaultRequiredXp.Any())
            {
                _experienceTable.AddRange(_mod.DefaultRequiredXp);
            }

            if (_experienceTable.Count >= targetLevel)
            {
                return;
            }

            // Pre-calculate values for the optimized loop.
            double baseXp = _mod.Config.LevelingCurveBaseExperience;
            double growthRate = _mod.Config.LevelingCurveGrowthRate;
            int startLevel = _experienceTable.Count + 1;
            double power = Math.Pow(growthRate, startLevel - customCurveStartLevel);

            for (int i = _experienceTable.Count; i < targetLevel; i++)
            {
                int previousXp = _experienceTable[i - 1];
                int additionalXp = (int)Math.Round(baseXp * power * this.ExperienceModifier);
                int requiredXp = previousXp + additionalXp;
                _experienceTable.Add(requiredXp);

                // Update the power for the next loop with a fast multiplication.
                power *= growthRate;
            }
        }

        #endregion
    }
}