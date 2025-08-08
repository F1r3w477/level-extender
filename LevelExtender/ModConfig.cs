using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace LevelExtender
{
    /// <summary>
    /// Contains the user-configurable settings for the Level Extender mod.
    /// These settings apply to all save files.
    /// </summary>
    public class ModConfig
    {
        /// <summary>
        /// If true, displays the experience bars on the screen when XP is gained.
        /// </summary>
        public bool DrawXpBars { get; set; } = true;

        /// <summary>
        /// If true, shows a notification when a skill grants you extra items.
        /// </summary>
        public bool DrawExtraItemNotifications { get; set; } = true;

        /// <summary>
        /// The minimum sale price an item must have to trigger an "extra item" notification.
        /// </summary>
        public int MinItemPriceForNotifications { get; set; } = 50;

        /// <summary>
        /// If true, enables custom monster spawning in outdoor locations. This can also be toggled per-save.
        /// This acts as the default value for new save files.
        /// </summary>
        public bool EnableWorldMonsters { get; set; } = false;

        /// <summary>
        /// The base amount of additional XP required to go from level 10 to 11.
        /// </summary>
        public double LevelingCurveBaseExperience { get; set; } = 7800;

        /// <summary>
        /// The percentage increase in required XP for each level after 10.
        /// Example: 4.2 means each level requires 4.2% more XP than the last.
        /// </summary>
        public double LevelingCurveGrowthPercent { get; set; } = 4.2;

        /// <summary>The number of columns to use when displaying the `le_xp_table` command.</summary>
        public int TableColumns { get; set; } = 4;

        /// <summary>The base chance for a monster to spawn each second (e.g., 0.01 is a 1% chance).</summary>
        public double BaseSpawnChance { get; set; } = 0.01;

        /// <summary>The bonus chance for a monster to spawn for each combat level the player has.</summary>
        public double SpawnChancePerLevel { get; set; } = 0.0001;

        /// <summary>
        /// Key(s) to open the Extended Skills menu. Supports combos and multiple fallbacks.
        /// </summary>
        public KeybindList OpenSkillsMenu { get; set; }

        /// <summary>Initialize defaults that don’t require SMAPI.Toolkit.</summary>
        public ModConfig()
        {
            // Default: LeftShift + K (combo)
            OpenSkillsMenu = new KeybindList(new Keybind(SButton.LeftShift, SButton.K));
        }
    }
}
