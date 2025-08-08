using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelExtender
{
    /// <summary>Defines the public contract for a skill that other mods can interact with.</summary>
    public interface ISkillApi
    {
        /// <summary>Gets the unique name of the skill.</summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the current level of the skill.
        /// Setting this value will automatically update the experience to match.
        /// </summary>
        int Level { get; set; }

        /// <summary>
        /// Gets or sets the current experience of the skill.
        /// Setting this value will automatically update the level to match.
        /// </summary>
        int Experience { get; set; }

        /// <summary>
        /// Gets or sets the multiplier applied to XP requirements for levels beyond 10.
        /// For example, <c>1.05</c> means +5% XP required per level.
        /// </summary>
        double ExperienceModifier { get; set; }

        /// <summary>
        /// Adds a specified amount of experience to the skill.
        /// </summary>
        /// <param name="amount">The non-negative amount of experience to add.</param>
        void AddExperience(int amount);
    }

    /// <summary>The public API for Level Extender, allowing other mods to interact with its skill system.</summary>
    public interface ILevelExtenderApi
    {
        /// <summary>Raised whenever experience is changed for any skill.</summary>
        event EventHandler<EXPEventArgs> OnXPChanged;

        /// <summary>
        /// Registers a new custom skill with the Level Extender system.
        /// This should be called once, preferably in the GameLoop.SaveLoaded event.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if a skill with the same name is already registered.</exception>
        void InitializeSkill(string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] itemCategories = null);

        /// <summary>
        /// Gets an API wrapper for a specific skill by name.
        /// </summary>
        /// <param name="name">The name of the skill.</param>
        /// <returns>An <see cref="ISkillApi"/> instance for the skill, or <c>null</c> if not found.</returns>
        ISkillApi GetSkill(string name);

        /// <summary>
        /// Attempts to get an API wrapper for a specific skill.
        /// </summary>
        /// <param name="name">The name of the skill.</param>
        /// <param name="skill">The found skill, or <c>null</c> if not found.</param>
        /// <returns><c>true</c> if the skill exists; otherwise <c>false</c>.</returns>
        bool TryGetSkill(string name, out ISkillApi skill);

        /// <summary>Gets an API wrapper for all registered skills.</summary>
        IEnumerable<ISkillApi> GetAllSkills();

        /// <summary>
        /// Gets the XP required for a given level in a specific skill.
        /// </summary>
        /// <param name="skillName">The name of the skill.</param>
        /// <param name="level">The level to query.</param>
        /// <returns>The XP required for that level, or <c>0</c> if the skill is not found.</returns>
        int GetRequiredExperience(string skillName, int level);
    }

    /// <summary>
    /// Internal implementation of the Level Extender API that is passed to SMAPI.
    /// </summary>
    public sealed class LEModApi : ILevelExtenderApi
    {
        private readonly ModEntry _modEntry;

        internal LEModApi(ModEntry modEntry)
        {
            _modEntry = modEntry;
        }

        public event EventHandler<EXPEventArgs> OnXPChanged
        {
            add => _modEntry.Events.OnXPChanged += value;
            remove => _modEntry.Events.OnXPChanged -= value;
        }

        public void InitializeSkill(string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] itemCategories = null)
        {
            if (GetSkill(name) != null)
                throw new InvalidOperationException($"Skill '{name}' is already registered.");

            _modEntry.InitializeSkill(name, currentXp, xpModifier, xpTable, itemCategories);
        }

        public ISkillApi GetSkill(string name)
        {
            return _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool TryGetSkill(string name, out ISkillApi skill)
        {
            skill = GetSkill(name);
            return skill != null;
        }

        public IEnumerable<ISkillApi> GetAllSkills()
        {
            return _modEntry.Skills;
        }

        public int GetRequiredExperience(string skillName, int level)
        {
            var skill = GetSkill(skillName);
            return skill is Skill s ? s.GetRequiredExperienceForLevel(level) : 0;
        }
    }
}
