using System;

namespace LevelExtender
{
    public class XPBar
    {
        public Skill skill;
        public DateTime CreationTime;   // The time the bar was first created, for the animation.
        public DateTime LastUpdateTime; // The last time XP was gained, for the 5-second lifespan.
        public double ych = 0;
        public float highlightTimer = 0f;

        public XPBar(Skill skill)
        {
            this.skill = skill;
            this.CreationTime = DateTime.Now;
            this.LastUpdateTime = DateTime.Now;
        }

        public int ychi => (int)Math.Round(this.ych);
    }
}