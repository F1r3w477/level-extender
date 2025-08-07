using System.Collections.Generic;

namespace LevelExtender.Tests
{
    public class MockModEntry : ModEntry
    {
        public MockModEntry()
        {
            this._skills = new List<Skill>();
            this._config = new ModConfig();
            this.DefaultRequiredXp.Clear();
            this.DefaultRequiredXp.AddRange(new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 });
        }
    }
}
