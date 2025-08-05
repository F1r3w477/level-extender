using NUnit.Framework;
using LevelExtender;
using System.Collections.Generic;
using System.Linq;

namespace LevelExtender.Tests
{
    // A simplified "mock" version of ModEntry.
    public class MockModEntry : ModEntry
    {
        public MockModEntry()
        {
            this._config = new ModConfig();
        }
    }

    [TestFixture]
    public class ApiTests
    {
        private ILevelExtenderApi? _api;
        private MockModEntry? _mockModEntry;

        [SetUp]
        public void Setup()
        {
            _mockModEntry = new MockModEntry();
            _api = new LEModApi(_mockModEntry);
            _mockModEntry.DefaultRequiredXp.AddRange(new[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 });
        }

        [TearDown]
        public void Teardown()
        {
            _mockModEntry?.Dispose();
            _mockModEntry = null;
            _api = null;
        }

        [Test]
        public void InitializeSkill_WhenCalled_AddsSkillToList()
        {
            string skillName = "Magic";
            int startXp = 500;
            Assert.That(_mockModEntry!._skills.Any(), Is.False);

            _api!.InitializeSkill(skillName, startXp);

            Assert.That(_mockModEntry._skills.Count, Is.EqualTo(1));
            Assert.That(_mockModEntry._skills[0].Name, Is.EqualTo(skillName));
        }

        [Test]
        public void GetSkill_WithExistingSkill_ReturnsCorrectSkill()
        {
            _api!.InitializeSkill("Alchemy", 123);
            _api!.InitializeSkill("Tinkering", 456);

            var result = _api.GetSkill("Tinkering");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Tinkering"));
        }

        [Test]
        public void GetSkill_WithNonExistentSkill_ReturnsNull()
        {
            var result = _api!.GetSkill("NonExistentSkill");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void AddExperience_ToExistingSkill_UpdatesExperienceAndLevel()
        {
            _api!.InitializeSkill("Foraging", 0);
            var skill = _api.GetSkill("Foraging");

            skill!.AddExperience(500);

            Assert.That(skill.Experience, Is.EqualTo(500));
            Assert.That(skill.Level, Is.EqualTo(2));
        }

        [Test]
        public void SetLevel_ToExistingSkill_UpdatesExperienceToMinimum()
        {
            _api!.InitializeSkill("Mining", 0);
            var skill = _api.GetSkill("Mining");

            skill!.Level = 5;

            Assert.That(skill.Level, Is.EqualTo(5));
            Assert.That(skill.Experience, Is.EqualTo(2150));
        }

        [Test]
        public void SetExperience_ToExistingSkill_UpdatesLevelCorrectly()
        {
            _api!.InitializeSkill("Combat", 10000);
            var skill = _api.GetSkill("Combat");
            Assert.That(skill!.Level, Is.EqualTo(9));

            skill.Experience = 500;

            Assert.That(skill.Level, Is.EqualTo(2));
        }
    }
}