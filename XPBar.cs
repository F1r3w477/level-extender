using System;
using System.Timers;

namespace LevelExtender
{
    public class XPBar
    {
        public Skill skill;
        public Timer timer;
        public DateTime time;
        public double ych = 0;
        public bool startmove = false;
        public int movedir = 0;

        public XPBar(Skill skill)
        {
            this.skill = skill;
            this.timer = new Timer(5000);
            timer.Elapsed += delegate { skill.LE.EndXPBar(skill.key); };
            timer.AutoReset = false;
            timer.Enabled = true;
            time = DateTime.Now;
        }

        public int ychi
        {
            get => (int)Math.Round(ych);
            set { } // This setter was incorrect, leaving it empty is safer than the recursive call it had.
        }
    }
}