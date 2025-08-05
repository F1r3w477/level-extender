using System;

namespace LevelExtender
{
    /// <summary>
    /// Represents an on-screen experience bar UI element.
    /// </summary>
    public class XPBar
    {
        /// <summary>
        /// The skill this bar represents.
        /// </summary>
        public Skill Skill { get; }

        /// <summary>
        /// The time the bar was created, used for fade-in/out animations.
        /// </summary>
        public DateTime CreationTime { get; }

        /// <summary>
        /// A timer used to make the level text flash when a level-up occurs.
        /// </summary>
        public float HighlightTimer { get; set; } = 0f;

        public XPBar(Skill skill)
        {
            this.Skill = skill;
            this.CreationTime = DateTime.Now;

            // When a new bar is created after a level up, set the timer to make it flash.
            this.HighlightTimer = 1.5f; // Flash for 1.5 seconds
        }
    }
}