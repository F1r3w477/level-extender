using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LevelExtender.Tests
{
    [TestFixture]
    public class ModConfigTests
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

        [Test]
        public void Defaults_AreAsExpected()
        {
            var c = new ModConfig();

            Assert.That(c.DrawXpBars, Is.True);
            Assert.That(c.DrawExtraItemNotifications, Is.True);
            Assert.That(c.MinItemPriceForNotifications, Is.EqualTo(50));

            Assert.That(c.EnableWorldMonsters, Is.False);

            Assert.That(c.LevelingCurveBaseExperience, Is.EqualTo(7800d));
            Assert.That(c.LevelingCurveGrowthRate, Is.EqualTo(1.042d));

            Assert.That(c.TableColumns, Is.EqualTo(4));

            Assert.That(c.BaseSpawnChance, Is.EqualTo(0.01d));
            Assert.That(c.SpawnChancePerLevel, Is.EqualTo(0.0001d));
        }

        [Test]
        public void Setters_UpdateValues()
        {
            var c = new ModConfig
            {
                DrawXpBars = false,
                DrawExtraItemNotifications = false,
                MinItemPriceForNotifications = 123,
                EnableWorldMonsters = true,
                LevelingCurveBaseExperience = 9000,
                LevelingCurveGrowthRate = 1.08,
                TableColumns = 6,
                BaseSpawnChance = 0.02,
                SpawnChancePerLevel = 0.00025
            };

            Assert.Multiple(() =>
            {
                Assert.That(c.DrawXpBars, Is.False);
                Assert.That(c.DrawExtraItemNotifications, Is.False);
                Assert.That(c.MinItemPriceForNotifications, Is.EqualTo(123));
                Assert.That(c.EnableWorldMonsters, Is.True);
                Assert.That(c.LevelingCurveBaseExperience, Is.EqualTo(9000d));
                Assert.That(c.LevelingCurveGrowthRate, Is.EqualTo(1.08d));
                Assert.That(c.TableColumns, Is.EqualTo(6));
                Assert.That(c.BaseSpawnChance, Is.EqualTo(0.02d));
                Assert.That(c.SpawnChancePerLevel, Is.EqualTo(0.00025d));
            });
        }

        [Test]
        public void Changing_BaseExperience_AltersPost10Thresholds()
        {
            // Baseline config
            _mod._config = new ModConfig
            {
                LevelingCurveBaseExperience = 7800,
                LevelingCurveGrowthRate = 1.042
            };
            var baselineSkill = new Skill(_mod, "Baseline", 0, xpModifier: 1.0, xpTable: null, categories: null);
            int baselineL11 = baselineSkill.GetRequiredExperienceForLevel(10); // index 10 => level 11

            // Higher base experience -> higher L11 threshold
            var modHigherBase = new MockModEntry();
            modHigherBase._config = new ModConfig
            {
                LevelingCurveBaseExperience = 9000, // bump this
                LevelingCurveGrowthRate = 1.042     // same growth
            };
            var higherBaseSkill = new Skill(modHigherBase, "HigherBase", 0, xpModifier: 1.0, xpTable: null, categories: null);
            int higherBaseL11 = higherBaseSkill.GetRequiredExperienceForLevel(10);

            Assert.That(higherBaseL11, Is.GreaterThan(baselineL11));
        }

        [Test]
        public void Changing_GrowthRate_AltersHigherLevelThresholds()
        {
            // Lower growth rate baseline
            _mod._config = new ModConfig
            {
                LevelingCurveBaseExperience = 7800,
                LevelingCurveGrowthRate = 1.03
            };
            var slowCurveSkill = new Skill(_mod, "SlowCurve", 0, xpModifier: 1.0, xpTable: null, categories: null);
            int slowL25 = slowCurveSkill.GetRequiredExperienceForLevel(24); // Level 25 threshold

            // Higher growth rate -> much higher L25 threshold
            var modFasterGrowth = new MockModEntry();
            modFasterGrowth._config = new ModConfig
            {
                LevelingCurveBaseExperience = 7800,
                LevelingCurveGrowthRate = 1.08 // steeper curve
            };
            var fastCurveSkill = new Skill(modFasterGrowth, "FastCurve", 0, xpModifier: 1.0, xpTable: null, categories: null);
            int fastL25 = fastCurveSkill.GetRequiredExperienceForLevel(24);

            Assert.That(fastL25, Is.GreaterThan(slowL25));
        }
    }
}
