using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LevelExtender.Tests
{
    [TestFixture]
    public class SkillsTests
    {
        private MockModEntry _mod;

        [SetUp]
        public void Setup()
        {
            _mod = new MockModEntry();
        }

        [TearDown]
        public void Teardown()
        {
            _mod?.Dispose();
            _mod = null!;
        }

        // Allow nullable args here so we can omit them without CS8625 warnings.
        private Skill NewSkill(
            string name = "Test",
            int currentXp = 0,
            double? xpModifier = null,
            List<int>? xpTable = null,
            int[]? categories = null
        )
        {
            return new Skill(_mod, name, currentXp, xpModifier, xpTable, categories);
        }

        [Test]
        public void Ctor_SetsNameKeyExperienceAndLevel_FromCurrentXp()
        {
            var expectedKey = _mod.Skills.Count; // 0 initially

            var s = NewSkill(name: "Alchemy", currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));

            Assert.That(s.Name, Is.EqualTo("Alchemy"));
            Assert.That(s.Key, Is.EqualTo(expectedKey));
            Assert.That(s.Experience, Is.EqualTo(0));
            Assert.That(s.Level, Is.EqualTo(0));
        }

        [Test]
        public void AddExperience_AccumulatesAndLevelsAccordingToTable()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));

            s.AddExperience(500);

            Assert.That(s.Experience, Is.EqualTo(500));
            Assert.That(s.Level, Is.EqualTo(2)); // 100 -> L1, 380 -> L2
        }

        [Test]
        public void Level_Setter_SetsExperienceToMinimumForThatLevel()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));

            s.Level = 5;

            Assert.That(s.Level, Is.EqualTo(5));
            Assert.That(s.Experience, Is.EqualTo(2150));
        }

        [Test]
        public void Experience_Setter_RecalculatesLevel()
        {
            var s = NewSkill(currentXp: 10000, xpTable: new List<int>(_mod.DefaultRequiredXp));
            Assert.That(s.Level, Is.EqualTo(9));

            s.Experience = 500;

            Assert.That(s.Level, Is.EqualTo(2));
        }

        [Test]
        public void GetRequiredExperienceForLevel_BeyondInitialTable_GeneratesMoreLevels()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));

            // ctor already generates to 101 entries; ask for 150 to force growth
            int beforeCount = s.ExperienceTable.Count; // 101
            int xpL150 = s.GetRequiredExperienceForLevel(150); // forces extension to 151
            int afterCount = s.ExperienceTable.Count;

            Assert.That(beforeCount, Is.EqualTo(101));
            Assert.That(afterCount, Is.GreaterThan(beforeCount));
            Assert.That(xpL150, Is.GreaterThan(_mod.DefaultRequiredXp.Last()));

            int xpL149 = s.GetRequiredExperienceForLevel(149);
            Assert.That(xpL150, Is.GreaterThan(xpL149));
        }

        [Test]
        public void ExperienceModifier_AffectsPost10Growth()
        {
            var s1 = NewSkill(name: "Base", currentXp: 0, xpModifier: 1.0);
            var s2 = NewSkill(name: "Boosted", currentXp: 0, xpModifier: 2.0);

            // Index 10 => Level 11 threshold
            int l11_base = s1.GetRequiredExperienceForLevel(10);
            int l11_boost = s2.GetRequiredExperienceForLevel(10);

            Assert.That(l11_boost, Is.GreaterThan(l11_base));
        }

        [Test]
        public void BinarySearch_Edges_Level0BelowFirstThreshold_LevelIncrementsAtExactThresholds()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));

            s.Experience = 99;
            Assert.That(s.Level, Is.EqualTo(0));

            s.Experience = 100;
            Assert.That(s.Level, Is.EqualTo(1));

            s.Experience = 380;
            Assert.That(s.Level, Is.EqualTo(2));
        }

        [Test]
        public void IsVanillaSkill_DependsOnKey()
        {
            // Create 5 skills and add them to the mod so Keys increment 0..4
            var created = new List<Skill>();
            for (int i = 0; i < 5; i++)
            {
                var s = NewSkill(name: $"Vanilla{i}", xpTable: new List<int>(_mod.DefaultRequiredXp));
                created.Add(s);
                _mod._skills.Add(s); // Make count visible to the next Skill's Key computation
            }

            Assert.That(created[4].Key, Is.EqualTo(4));
            Assert.That(created[4].IsVanillaSkill, Is.True);

            // Next one gets Key == 5 -> non-vanilla
            var custom = NewSkill(name: "Custom", xpTable: new List<int>(_mod.DefaultRequiredXp));
            Assert.That(custom.Key, Is.EqualTo(5));
            Assert.That(custom.IsVanillaSkill, Is.False);
        }

        [Test]
        public void Categories_DefaultsToEmpty_ArrayAppliedWhenProvided()
        {
            var empty = NewSkill(xpTable: new List<int>(_mod.DefaultRequiredXp)); // omit categories arg
            Assert.That(empty.Categories, Is.Not.Null);
            Assert.That(empty.Categories.Count, Is.EqualTo(0));

            var withCats = NewSkill(categories: new[] { -4, -15 }, xpTable: new List<int>(_mod.DefaultRequiredXp));
            CollectionAssert.AreEqual(new[] { -4, -15 }, withCats.Categories);
        }

        [Test]
        public void AddExperience_IgnoresZeroOrNegative()
        {
            var s = NewSkill(currentXp: 200, xpTable: new List<int>(_mod.DefaultRequiredXp));

            s.AddExperience(0);
            Assert.That(s.Experience, Is.EqualTo(200));

            s.AddExperience(-5);
            Assert.That(s.Experience, Is.EqualTo(200));
        }

        [Test]
        public void Experience_SetHighTriggersTableGrowth_AndLevelsAdvance()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));
            int initialCount = s.ExperienceTable.Count; // 101

            // Force growth via getter, then set a high XP to advance level well beyond initialCount
            int xpL150 = s.GetRequiredExperienceForLevel(150); // extend to 151
            s.Experience = xpL150 + 1;

            Assert.That(s.ExperienceTable.Count, Is.GreaterThan(initialCount)); // >= 151
            Assert.That(s.Level, Is.GreaterThan(150));
        }

        [Test]
        public void Events_Fire_OnExperienceChange()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));
            int calls = 0;

            _mod.Events.OnXPChanged += (_, __) => calls++;

            s.Experience = 10;   // setter
            s.AddExperience(10); // via setter

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void GetRequiredExperienceForLevel_NegativeIndex_ReturnsZero()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));
            Assert.That(s.GetRequiredExperienceForLevel(-1), Is.EqualTo(0));
        }

        [Test]
        public void ExperienceTable_IsMonotonicallyIncreasing()
        {
            var s = NewSkill(currentXp: 0, xpTable: new List<int>(_mod.DefaultRequiredXp));

            for (int i = 10; i < 25; i++)
                _ = s.GetRequiredExperienceForLevel(i);

            for (int i = 1; i < s.ExperienceTable.Count; i++)
            {
                Assert.That(s.ExperienceTable[i], Is.GreaterThan(s.ExperienceTable[i - 1]),
                    $"ExperienceTable must be strictly increasing at index {i}");
            }
        }
    }
}
