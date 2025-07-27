using System;

namespace LevelExtender
{
    public class XPBar
    {
        public Skill skill;
        public DateTime CreationTime; // The time the bar was first created, for the animation.
        public float highlightTimer = 0f;

        public XPBar(Skill skill)
        {
            this.skill = skill;
            this.CreationTime = DateTime.Now;
        }
    }
}