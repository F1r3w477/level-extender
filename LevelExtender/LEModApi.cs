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

        /// <summary>Gets or sets the current level of the skill. Setting this value will automatically update the experience to match.</summary>
        int Level { get; set; }

        /// <summary>Gets or sets the current experience of the skill. Setting this value will automatically update the level to match.</summary>
        int Experience { get; set; }

        /// <summary>Gets or sets the experience multiplier used for levels beyond 10.</summary>
        double ExperienceModifier { get; set; }

        /// <summary>Adds a specified amount of experience to the skill.</summary>
        /// <param name="amount">The non-negative amount of experience to add.</param>
        void AddExperience(int amount);
    }

    /// <summary>The public API for Level Extender, allowing other mods to interact with its skill system.</summary>
    public interface ILevelExtenderApi
    {
        /// <summary>An event that is raised whenever experience is changed for any skill.</summary>
        event EventHandler<EXPEventArgs> OnXPChanged;

        /// <summary>
        /// Registers a new custom skill with the Level Extender system.
        /// This should be called once, preferably in the GameLoop.SaveLoaded event.
        /// </summary>
        void InitializeSkill(string name, int currentXp, double? xpModifier = null, List<int> xpTable = null, int[] itemCategories = null);

        /// <summary>Gets an API wrapper for a specific skill by name.</summary>
        /// <param name="name">The name of the skill.</param>
        /// <returns>An <see cref="ISkillApi"/> instance for the skill, or <c>null</c> if the skill is not found.</returns>
        ISkillApi GetSkill(string name);

        /// <summary>Gets an API wrapper for all registered skills.</summary>
        /// <returns>An enumerable collection of all registered skills.</returns>
        IEnumerable<ISkillApi> GetAllSkills();
    }

    // This is the internal implementation class that you pass to SMAPI.
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
            _modEntry.InitializeSkill(name, currentXp, xpModifier, xpTable, itemCategories);
        }

        public ISkillApi GetSkill(string name)
        {
            // The lookup logic now only exists in one place.
            return _modEntry.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<ISkillApi> GetAllSkills()
        {
            return _modEntry.Skills;
        }
    }
}