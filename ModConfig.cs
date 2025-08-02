// File: ModConfig.cs
using StardewModdingAPI;

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
    }
}