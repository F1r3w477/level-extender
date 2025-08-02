using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelExtender
{
    /// <summary>
    /// The public API for Level Extender, allowing other mods to interact with its skill system.
    /// </summary>
    public sealed class LEModApi
    {
        private readonly ModEntry _modEntry;

        /// <summary>Internal constructor.</summary>
        /// <param name="modEntry">The instance of the main mod class.</param>
        internal LEModApi(ModEntry modEntry)
        {
            _modEntry = modEntry;
        }

        #region Events

        /// <summary>An event that is raised whenever experience is changed for any skill.</summary>
        public event EventHandler<EXPEventArgs> OnXPChanged
        {
            add => _modEntry.Events.OnXPChanged += value;
            remove => _modEntry.Events.OnXPChanged -= value;
        }

        #endregion

        #region Skill Management

        /// <summary>
        /// Registers a new custom skill with the Level Extender system.
        /// This should be called once, preferably in the GameLoop.SaveLoaded event.
        /// </summary>
        /// <param name="name">The unique name of the skill (e.g., "Cooking").</param>
        /// <param name="currentXp">The player's current XP for this skill.</param>
        /// <param name="xpModifier">An optional modifier for XP calculations past level 10.</param>
        /// <param name="xpTable">An optional custom XP table. If null, defaults will be used.</param>
        /// <param name="itemCategories">An optional array of Stardew Valley item category IDs that this skill affects.</param>
        public void InitializeSkill(string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] itemCategories = null)
        {
            _modEntry.InitializeSkill(name, currentXp, xpModifier, xpTable, itemCategories);
        }

        /// <summary>Gets the current level for a specific skill.</summary>
        /// <param name="name">The name of the skill.</param>
        /// <returns>The skill's current level, or -1 if the skill is not found.</returns>
        public int GetSkillLevel(string name)
        {
            var skill = _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return skill?.Level ?? -1;
        }

        /// <summary>Gets the current total experience for a specific skill.</summary>
        /// <param name="name">The name of the skill.</param>
        /// <returns>The skill's current experience, or -1 if the skill is not found.</returns>
        public int GetSkillExperience(string name)
        {
            var skill = _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return skill?.Experience ?? -1;
        }

        /// <summary>Adds a specified amount of experience to a skill.</summary>
        /// <param name="name">The name of the skill.</param>
        /// <param name="amount">The amount of experience to add.</param>
        public void AddExperience(string name, int amount)
        {
            var skill = _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (skill != null && amount > 0)
            {
                skill.Experience += amount;
            }
        }

        /// <summary>Sets the level for a skill directly, adjusting XP to the minimum for that level.</summary>
        /// <param name="name">The name of the skill.</param>
        /// <param name="level">The level to set.</param>
        public void SetSkillLevel(string name, int level)
        {
            var skill = _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (skill != null && level >= 0)
            {
                skill.Level = level;
            }
        }

        #endregion
    }
}