using System;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.Xna.Framework;
using LevelExtender;

namespace LevelExtender.Tests
{
    [TestFixture]
    public class SkillsMenuTests
    {
        private MockModEntry _mod;

        [SetUp]
        public void Setup()
        {
            _mod = new MockModEntry();
            _mod.DefaultRequiredXp.Clear();
            _mod.DefaultRequiredXp.AddRange(new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 });
        }

        [TearDown]
        public void Teardown()
        {
            _mod?.Dispose();
            _mod = null!;
        }

        private Skill NewSkill(string name, int xp)
            => new Skill(_mod, name, xp, xpModifier: 1.0, xpTable: new List<int>(_mod.DefaultRequiredXp), categories: Array.Empty<int>());
#if DEBUG
        [Test]
        public void ComputePageRange_BasicAndEdges()
        {
            // count 23, rows/page 10
            var p0 = SkillsMenu.ComputePageRange(0, 10, 23);
            var p1 = SkillsMenu.ComputePageRange(1, 10, 23);
            var p2 = SkillsMenu.ComputePageRange(2, 10, 23); // partial last page

            Assert.That(p0.start, Is.EqualTo(0));   Assert.That(p0.end, Is.EqualTo(10));
            Assert.That(p1.start, Is.EqualTo(10));  Assert.That(p1.end, Is.EqualTo(20));
            Assert.That(p2.start, Is.EqualTo(20));  Assert.That(p2.end, Is.EqualTo(23));

            // empty
            var empty = SkillsMenu.ComputePageRange(0, 10, 0);
            Assert.That(empty.start, Is.EqualTo(0));
            Assert.That(empty.end, Is.EqualTo(0));
        }

        [Test]
        public void ComputeBarWidth_LeavesSpaceForLevelText()
        {
            int listLeft = 100;
            int listRight = 800;
            int levelTextWidth = 72; // pretend measure for "Lvl 12"

            int barWidth = SkillsMenu.ComputeBarWidth(listLeft, listRight, levelTextWidth);
            Assert.That(barWidth, Is.GreaterThan(0));

            // Manually verify bar end does not cross into level label area.
            int nameX = listLeft + SkillsMenu.Debug_NameOffsetX;
            int lvlX  = listRight - SkillsMenu.Debug_RightPadding - levelTextWidth;
            int barMaxRight = lvlX - SkillsMenu.Debug_BarRightGap;

            Assert.That(nameX + barWidth, Is.LessThanOrEqualTo(barMaxRight));
        }
#endif

        [Test]
        public void ComputeLevelProgress_MatchesExpectedMath()
        {
            // 500 total XP -> Level 2.
            var s = NewSkill("Test", 500);
            Assert.That(s.Level, Is.EqualTo(2));

            var (prev, next, need, have, pct) = SkillsMenu.ComputeLevelProgress(s);

            // Next-level model: progress goes from current level threshold (380) to next (770)
            Assert.That(prev, Is.EqualTo(380));          // L2 threshold
            Assert.That(next, Is.EqualTo(770));          // L3 threshold
            Assert.That(need, Is.EqualTo(770 - 380));    // 390
            Assert.That(have, Is.EqualTo(500 - 380));    // 120
            Assert.That(Math.Round(pct, 4), Is.EqualTo(Math.Round(120f / 390f, 4)));
        }

        [Test]
        public void ComputeLevelProgress_RealisticCases()
        {
            // Case A: 180 XP total -> Level 1, progress toward Level 2 (100..380)
            var a = NewSkill("Fishing", 180);
            var ap = SkillsMenu.ComputeLevelProgress(a);
            Assert.That(a.Level, Is.EqualTo(1));
            Assert.That(ap.prev, Is.EqualTo(100));       // L1 threshold
            Assert.That(ap.next, Is.EqualTo(380));       // L2 threshold
            Assert.That(ap.have, Is.EqualTo(180 - 100)); // 80
            Assert.That(ap.need, Is.EqualTo(380 - 100)); // 280
            Assert.That(Math.Round(ap.pct * 100, 1), Is.EqualTo(28.6).Within(0.1));

            // Case B: exactly at a threshold (380) -> Level 2, 0 progress toward Level 3 (380..770)
            var b = NewSkill("Foraging", 380);
            var bp = SkillsMenu.ComputeLevelProgress(b);
            Assert.That(b.Level, Is.EqualTo(2));
            Assert.That(bp.prev, Is.EqualTo(380));       // L2 threshold
            Assert.That(bp.next, Is.EqualTo(770));       // L3 threshold
            Assert.That(bp.have, Is.EqualTo(0));         // 380 - 380
            Assert.That(bp.need, Is.EqualTo(770 - 380)); // 390
            Assert.That(bp.pct, Is.EqualTo(0f));
        }
#if DEBUG
        [Test]
        public void GetIconRectForSkill_KnownVanillaKeys()
        {
            Assert.That(SkillsMenu.Debug_GetIconRectForSkill(0), Is.EqualTo(new Rectangle(10, 428, 10, 10)));   // Farming
            Assert.That(SkillsMenu.Debug_GetIconRectForSkill(1), Is.EqualTo(new Rectangle(20, 428, 10, 10)));   // Fishing
            Assert.That(SkillsMenu.Debug_GetIconRectForSkill(2), Is.EqualTo(new Rectangle(60, 428, 10, 10)));   // Foraging
            Assert.That(SkillsMenu.Debug_GetIconRectForSkill(3), Is.EqualTo(new Rectangle(30, 428, 10, 10)));   // Mining
            Assert.That(SkillsMenu.Debug_GetIconRectForSkill(4), Is.EqualTo(new Rectangle(120, 428, 10, 10)));  // Combat

            // Unknown -> default
            Assert.That(SkillsMenu.Debug_GetIconRectForSkill(999), Is.EqualTo(new Rectangle(50, 428, 10, 10)));
        }
#endif
    }
}
