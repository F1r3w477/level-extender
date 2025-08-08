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
        private const int DefaultExperienceTableSize = 101; // pre-generate a decent runway
        private const int GrowthChunk = 64;                  // when extending table, add in chunks to avoid tight loops

        private readonly ModEntry _mod;
        private readonly List<int> _experienceTable;
        private int _level;
        private int _experience;

        /// <summary>
        /// True when a level change is coming from an XP update (prevents resetting XP to min on level-up).
        /// </summary>
        private bool _isLevelingViaExperience;

        #region Properties

        /// <summary>Gets a value indicating whether this is a vanilla skill (Farming, Fishing, etc.).</summary>
        public bool IsVanillaSkill => this.Key < VanillaSkillCount;

        /// <summary>Gets the display name of the skill.</summary>
        public string Name { get; private set; }

        /// <summary>Gets the unique key (index) for this skill.</summary>
        public int Key { get; private set; }

        /// <summary>
        /// Gets or sets the multiplier applied to required XP for this skill.
        /// 1.0 = no change, 1.1 = +10% more XP required, 0.9 = -10% XP required.
        /// </summary>
        public double ExperienceModifier { get; set; }

        /// <summary>Gets a read-only view of the experience required for each level (cumulative totals).</summary>
        public IReadOnlyList<int> ExperienceTable => _experienceTable;

        /// <summary>Gets the item categories associated with this skill, if any.</summary>
        public IReadOnlyList<int> Categories { get; private set; }

        /// <summary>
        /// Gets or sets the current level of the skill.
        /// Setting this value will automatically update the experience to the minimum for that level.
        /// </summary>
        public int Level
        {
            get => _level;
            set
            {
                int newLevel = Math.Max(0, value);
                if (_level == newLevel) return;

                _level = newLevel;

                // If the level was set manually, set XP to the minimum for that level (no extra event spam).
                if (!_isLevelingViaExperience)
                {
                    int requiredXp = (_level > 0) ? this.GetRequiredExperienceForLevel(_level - 1) : 0;
                    SetExperienceSilently(requiredXp);
                    // Raise a single change event after bringing XP in line with the new level.
                    _mod.Events.RaiseEvent(new EXPEventArgs { Key = this.Key });
                }

                _isLevelingViaExperience = false;
            }
        }

        /// <summary>
        /// Gets or sets the current experience of the skill (total, cumulative).
        /// Setting this value will update the level if needed and raise the XP-changed event.
        /// </summary>
        public int Experience
        {
            get => _experience;
            set
            {
                int newXp = Math.Max(0, value);
                if (_experience == newXp) return;

                _experience = newXp;

                CheckForLevelUp(); // may update Level (without resetting XP)
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
            _experienceTable = xpTable != null ? new List<int>(xpTable) : new List<int>();
            this.ExperienceModifier = xpModifier ?? 1.0;
            this.Categories = categories ?? Array.Empty<int>();

            // Normalize/seed the XP table and pre-generate a reasonable number of levels.
            NormalizeAndSeedExperienceTable();
            GenerateExperienceTable(DefaultExperienceTableSize);

            // Set the initial state from provided experience.
            _experience = Math.Max(0, currentXp);
            _level = GetLevelByExperience();
        }

        #region Public Methods

        /// <summary>Adds a specified amount of experience to the skill.</summary>
        /// <param name="amount">The non-negative amount of experience to add.</param>
        public void AddExperience(int amount)
        {
            if (amount > 0)
                this.Experience = checked(_experience + amount);
        }

        /// <summary>Gets the total experience points required to reach a given level index (0-based).</summary>
        public int GetRequiredExperienceForLevel(int levelIndex)
        {
            if (levelIndex < 0) return 0;

            if (_experienceTable.Count <= levelIndex)
                GenerateExperienceTable(levelIndex + 1);

            return _experienceTable[levelIndex];
        }

        #endregion

        #region Private Methods

        /// <summary>Sets <see cref="Experience"/> without firing the change event; used by the Level setter to avoid double events.</summary>
        private void SetExperienceSilently(int value)
        {
            _experience = Math.Max(0, value);
            // Keep level consistent too
            _level = GetLevelByExperience();
        }

        /// <summary>Checks if the current experience implies a different level and applies the change.</summary>
        private void CheckForLevelUp()
        {
            int levelFromXp = GetLevelByExperience();
            if (levelFromXp != _level)
            {
                _isLevelingViaExperience = true;
                _level = levelFromXp; // set level without resetting XP
            }
        }

        /// <summary>Calculates the level based on current experience via binary search; extends the table in chunks if needed.</summary>
        private int GetLevelByExperience()
        {
            if (_experienceTable.Count == 0)
                return 0;

            // Extend in chunks until table last is >= current XP.
            while (_experience >= _experienceTable[^1])
            {
                GenerateExperienceTable(_experienceTable.Count + GrowthChunk);
                // Safety: if growth parameters were pathological, ensure monotonic increase
                if (_experienceTable.Count > 1 && _experienceTable[^1] <= _experienceTable[^2])
                    _experienceTable[^1] = _experienceTable[^2] + 1;
            }

            return FindLevelWithBinarySearch(_experienceTable, _experience);
        }

        /// <summary>Binary search over a strictly increasing cumulative XP table.</summary>
        private static int FindLevelWithBinarySearch(IReadOnlyList<int> experienceTable, int experience)
        {
            int left = 0;
            int right = experienceTable.Count - 1;
            int result = 0; // level 0 if no thresholds met

            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);
                if (experience >= experienceTable[mid])
                {
                    result = mid + 1;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return result;
        }

        /// <summary>Ensures the table is seeded and cumulative/strictly increasing.</summary>
        private void NormalizeAndSeedExperienceTable()
        {
            // Seed with vanilla thresholds if empty
            if (_experienceTable.Count == 0 && _mod.DefaultRequiredXp.Any())
                _experienceTable.AddRange(_mod.DefaultRequiredXp);

            if (_experienceTable.Count == 0)
                return;

            // If the provided table looks like per-level increments (non-increasing cumulative),
            // convert to cumulative totals.
            bool nonIncreasing = false;
            for (int i = 1; i < _experienceTable.Count; i++)
            {
                if (_experienceTable[i] < _experienceTable[i - 1])
                {
                    nonIncreasing = true;
                    break;
                }
            }

            if (nonIncreasing)
            {
                for (int i = 1; i < _experienceTable.Count; i++)
                    _experienceTable[i] += _experienceTable[i - 1];
            }

            // Enforce strictly increasing (no duplicates / flats)
            for (int i = 1; i < _experienceTable.Count; i++)
            {
                if (_experienceTable[i] <= _experienceTable[i - 1])
                    _experienceTable[i] = _experienceTable[i - 1] + 1;
            }
        }

        /// <summary>Populates the experience table up to <paramref name="targetLevel"/> (count of entries).</summary>
        private void GenerateExperienceTable(int targetLevel)
        {
            const int customCurveStartLevel = 11;

            // Always make sure we have a seed first.
            if (_experienceTable.Count == 0 && _mod.DefaultRequiredXp.Any())
                _experienceTable.AddRange(_mod.DefaultRequiredXp);

            if (_experienceTable.Count >= targetLevel)
                return;

            // Curve parameters
            double baseXp = _mod.Config.LevelingCurveBaseExperience;
            double growthRate = 1.0 + (_mod.Config.LevelingCurveGrowthPercent / 100.0); // updated for percent form

            // Start exponent for the first level we’re generating (1-based levels)
            int startLevel = _experienceTable.Count + 1;
            double power = Math.Pow(growthRate, startLevel - customCurveStartLevel);

            for (int i = _experienceTable.Count; i < targetLevel; i++)
            {
                int previousXp = _experienceTable[i - 1];
                int additionalXp = (int)Math.Round(baseXp * power * this.ExperienceModifier);

                // Guarantee progress even if bad config yields zero
                if (additionalXp <= 0)
                    additionalXp = 1;

                int requiredXp = previousXp + additionalXp;
                _experienceTable.Add(requiredXp);

                power *= growthRate;
            }
        }

        #endregion
    }
}
