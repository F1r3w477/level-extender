using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LevelExtender.Tests
{
    [TestFixture]
    public class XPBarTests
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
        public void Ctor_SetsSkill_AndCreationTime_NearNow_AndHighlightTimerDefault()
        {
            // Arrange
            var skill = NewSkill(xpTable: new List<int>(_mod.DefaultRequiredXp));
            var before = DateTime.Now;

            // Act
            var bar = new XPBar(skill);
            var after = DateTime.Now;

            // Assert
            Assert.That(bar.Skill, Is.SameAs(skill), "XPBar should hold the same Skill reference that was passed in.");

            // CreationTime should be between 'before' and 'after'
            Assert.That(bar.CreationTime, Is.GreaterThanOrEqualTo(before));
            Assert.That(bar.CreationTime, Is.LessThanOrEqualTo(after));

            // Default highlight timer
            Assert.That(bar.HighlightTimer, Is.EqualTo(1.5f).Within(1e-6));
        }

        [Test]
        public void HighlightTimer_IsWritable()
        {
            var bar = new XPBar(NewSkill(xpTable: new List<int>(_mod.DefaultRequiredXp)));

            bar.HighlightTimer = 0.75f;
            Assert.That(bar.HighlightTimer, Is.EqualTo(0.75f).Within(1e-6));

            bar.HighlightTimer -= 0.25f;
            Assert.That(bar.HighlightTimer, Is.EqualTo(0.5f).Within(1e-6));
        }
    }
}
