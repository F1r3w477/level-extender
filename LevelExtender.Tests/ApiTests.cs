using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LevelExtender;

namespace LevelExtender.Tests
{
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
            _mockModEntry.DefaultRequiredXp.Clear();
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

        // --- Additional API coverage ---

        [Test]
        public void InitializeSkill_WithDuplicateName_DoesNotAddTwice()
        {
            _api!.InitializeSkill("Alchemy", 0);
            _api.InitializeSkill("Alchemy", 100); // duplicate (same name)
            Assert.That(_mockModEntry!._skills.Count, Is.EqualTo(1));
            Assert.That(_mockModEntry._skills[0].Experience, Is.EqualTo(0)); // original unchanged
        }

        [Test]
        public void GetSkill_NameLookup_IsCaseInsensitive()
        {
            _api!.InitializeSkill("Tinkering", 0);
            var lower = _api.GetSkill("tinkering");
            var mixed = _api.GetSkill("TiNkErInG");
            Assert.That(lower, Is.Not.Null);
            Assert.That(mixed, Is.Not.Null);
            Assert.That(lower, Is.SameAs(mixed));
        }

        [Test]
        public void GetAllSkills_ReturnsAllRegisteredSkills()
        {
            _api!.InitializeSkill("A", 0);
            _api!.InitializeSkill("B", 0);
            var all = _api.GetAllSkills().ToList();
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, all.Select(s => s.Name));
        }

        [Test]
        public void GetSkill_ReturnsLiveReference_ChangesAreVisible()
        {
            _api!.InitializeSkill("Foraging", 0);
            var s1 = _api.GetSkill("Foraging");
            s1!.AddExperience(100);
            var s2 = _api.GetSkill("Foraging");
            Assert.That(s2!.Experience, Is.EqualTo(100));
        }

        [Test]
        public void InitializeSkill_NullXpTableAndCategories_UsesDefaults_NoThrow()
        {
            _api!.InitializeSkill("Custom", 0, xpModifier: null, xpTable: null, itemCategories: null);
            var skill = _api.GetSkill("Custom");
            Assert.That(skill, Is.Not.Null);

            // Sanity: XP/Level behave
            skill!.AddExperience(100);
            Assert.That(skill.Level, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void OnXPChanged_Event_Fires_OnAddExperience()
        {
            _api!.InitializeSkill("Mining", 0);
            var skill = _api.GetSkill("Mining")!;
            int calls = 0;

            _api.OnXPChanged += (_, __) => calls++;
            skill.AddExperience(50);

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void OnXPChanged_Event_Unsubscribe_StopsFiring()
        {
            _api!.InitializeSkill("Combat", 0);
            var skill = _api.GetSkill("Combat")!;

            int calls = 0;
            EventHandler<EXPEventArgs> handler = (_, __) => calls++;
            _api.OnXPChanged += handler;

            skill.AddExperience(10);       // should fire
            _api.OnXPChanged -= handler;
            skill.AddExperience(10);       // should NOT fire

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void SetExperience_AtThreshold_SetsExpectedLevel()
        {
            _api!.InitializeSkill("Fishing", 0);
            var s = _api.GetSkill("Fishing")!;
            s.Experience = 380;  // exact second threshold in DefaultRequiredXp
            Assert.That(s.Level, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void InitializeSkill_WithCustomXpTable_IsHonored()
        {
            _api!.InitializeSkill(
                name: "CustomCurve",
                currentXp: 0,
                xpModifier: null,
                xpTable: new List<int> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
                itemCategories: new[] { -4 }
            );

            var s = _api.GetSkill("CustomCurve")!;
            s.AddExperience(25);
            Assert.That(s.Experience, Is.EqualTo(25));
            Assert.That(s.Level, Is.GreaterThanOrEqualTo(2)); // reached 20
        }
    }
}
