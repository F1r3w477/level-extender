using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using StardewModdingAPI;

namespace LevelExtender.Tests
{
    internal sealed class FakeMonitor : IMonitor
    {
        public readonly List<(LogLevel Level, string Message)> Entries = new();

        public bool IsVerbose => true;

        public void Log(string message, LogLevel level = LogLevel.Trace)
            => Entries.Add((level, message));

        public void LogOnce(string message, LogLevel level = LogLevel.Trace)
            => Entries.Add((level, message));

        // Older SMAPI overload
        public void VerboseLog(string message)
            => Entries.Add((LogLevel.Trace, message));

        public void VerboseLog(ref StardewModdingAPI.Framework.Logging.VerboseLogStringHandler handler)
            => Entries.Add((LogLevel.Trace, "<verbose>"));
    }

    [TestFixture]
    public class CommandsTests
    {
        private MockModEntry _mod;
        private ModConfig _config;
        private FakeMonitor _monitor;
        private Commands _cmd;

        [SetUp]
        public void Setup()
        {
            _mod = new MockModEntry();
            _config = _mod.Config; // points at _mod._config
            _monitor = new FakeMonitor();
            _cmd = new Commands(_mod, _config, _monitor);

            // Seed a couple of skills so commands have something to act on
            var s1 = new Skill(_mod, "Farming", 0, xpModifier: 1.0, xpTable: new List<int>(_mod.DefaultRequiredXp), categories: null);
            var s2 = new Skill(_mod, "Mining", 0, xpModifier: 1.0, xpTable: new List<int>(_mod.DefaultRequiredXp), categories: null);
            _mod._skills.Add(s1);
            _mod._skills.Add(s2);
        }

        [TearDown]
        public void Teardown()
        {
            _mod?.Dispose();
            _mod = null!;
        }

        #region Helpers

        private (LogLevel Level, string Message) LastLog()
            => _monitor.Entries.Last();

        private IEnumerable<string> InfoLines()
            => _monitor.Entries.Where(e => e.Level == LogLevel.Info).Select(e => e.Message);

        #endregion

        [Test]
        public void ShowExperienceSummary_LogsHeaderAndRows()
        {
            _cmd.ShowExperienceSummary("le_xp", Array.Empty<string>());

            var logs = InfoLines().ToList();
            Assert.That(logs[0], Does.Contain("Skill").And.Contain("Level").And.Contain("Current XP"));
            Assert.That(logs[1], Does.StartWith("----")); // separator line
            Assert.That(logs.Any(l => l.StartsWith("Farming")), Is.True);
            Assert.That(logs.Any(l => l.StartsWith("Mining")), Is.True);
        }

        [Test]
        public void SetLevel_InvalidArgs_LogsErrorAndDoesNotThrow()
        {
            Assert.DoesNotThrow(() => _cmd.SetLevel("le_setlevel", new[] { "Farming", "notANumber" }));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));

            Assert.DoesNotThrow(() => _cmd.SetLevel("le_setlevel", new[] { "Farming", "-1" }));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void SetLevel_Valid_SetsLevelAndResetsExperienceToMinimum()
        {
            var farming = _mod.Skills.First(s => s.Name == "Farming");
            Assert.That(farming.Level, Is.EqualTo(0));
            Assert.That(farming.Experience, Is.EqualTo(0));

            _cmd.SetLevel("le_setlevel", new[] { "Farming", "3" });

            Assert.That(farming.Level, Is.EqualTo(3));
            Assert.That(farming.Experience, Is.EqualTo(farming.GetRequiredExperienceForLevel(2))); // min XP for level 3
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
            Assert.That(LastLog().Message, Does.Contain("level set to 3"));
        }

        [Test]
        public void SetExperience_InvalidArgs_LogsError()
        {
            Assert.DoesNotThrow(() => _cmd.SetExperience("le_setxp", new[] { "Farming", "NaN" }));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));

            Assert.DoesNotThrow(() => _cmd.SetExperience("le_setxp", new[] { "Farming", "-5" }));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void SetExperience_Valid_SetsExperienceAndUpdatesLevel()
        {
            var mining = _mod.Skills.First(s => s.Name == "Mining");
            _cmd.SetExperience("le_setxp", new[] { "Mining", "500" });

            Assert.That(mining.Experience, Is.EqualTo(500));
            Assert.That(mining.Level, Is.EqualTo(2)); // 100 -> L1, 380 -> L2 with default table
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
            Assert.That(LastLog().Message, Does.Contain("XP set to 500"));
        }

        [Test]
        public void ToggleWorldMonsters_TogglesConfigAndLogs()
        {
            var initial = _config.EnableWorldMonsters;
            _cmd.ToggleWorldMonsters("le_toggle_monsters", Array.Empty<string>());
            Assert.That(_config.EnableWorldMonsters, Is.EqualTo(!initial));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void ToggleDrawBars_TogglesConfigAndLogs()
        {
            var initial = _config.DrawXpBars;
            _cmd.ToggleDrawBars("le_toggle_xpbars", Array.Empty<string>());
            Assert.That(_config.DrawXpBars, Is.EqualTo(!initial));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void SetExperienceModifier_InvalidArgs_LogsError()
        {
            Assert.DoesNotThrow(() => _cmd.SetExperienceModifier("le_xp_modifier", new[] { "Farming", "nope" }));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));

            Assert.DoesNotThrow(() => _cmd.SetExperienceModifier("le_xp_modifier", new[] { "Farming", "0" })); // must be > 0
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void SetExperienceModifier_Valid_UpdatesSkillAndLogs()
        {
            var farming = _mod.Skills.First(s => s.Name == "Farming");
            _cmd.SetExperienceModifier("le_xp_modifier", new[] { "Farming", "2.5" });

            Assert.That(farming.ExperienceModifier, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
            Assert.That(LastLog().Message, Does.Contain("XP Modifier for Farming set to 2.5"));
        }

        [Test]
        public void ShowSkillXpTable_InvalidArgs_LogsError()
        {
            _cmd.ShowSkillXpTable("le_xp_table", Array.Empty<string>());
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void ShowSkillXpTable_Valid_PrintsGrid()
        {
            _cmd.ShowSkillXpTable("le_xp_table", new[] { "Farming" });

            // Find the big blob logged at the end
            var blob = _monitor.Entries
                .Where(e => e.Level == LogLevel.Info)
                .Select(e => e.Message)
                .Last(); // the table dump is the last info entry

            Assert.That(blob, Does.Contain("XP Table for Farming"));
            Assert.That(blob, Does.Contain("Lvl 1: 100"));  // first threshold
            Assert.That(blob, Does.Contain("Lvl 2: 380"));  // second threshold

            // Cell padding sanity: "Lvl 1: 100" should be padded to CellWidth (18)
            var firstRowLine = blob.Split('\n').Skip(1).FirstOrDefault(); // line after the header
            Assert.That(firstRowLine, Is.Not.Null);
        }

        [Test]
        public void ToggleExtraItemNotifications_TogglesConfigAndLogs()
        {
            var initial = _config.DrawExtraItemNotifications;
            _cmd.ToggleExtraItemNotifications("le_toggle_notifications", Array.Empty<string>());
            Assert.That(_config.DrawExtraItemNotifications, Is.EqualTo(!initial));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void SetMinNotificationPrice_InvalidArgs_LogsError()
        {
            _cmd.SetMinNotificationPrice("le_min_notification_price", new[] { "nope" });
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));

            _cmd.SetMinNotificationPrice("le_min_notification_price", new[] { "-1" });
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void SetMinNotificationPrice_Valid_SetsConfigAndLogs()
        {
            _cmd.SetMinNotificationPrice("le_min_notification_price", new[] { "1234" });
            Assert.That(_config.MinItemPriceForNotifications, Is.EqualTo(1234));
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Info));
            Assert.That(LastLog().Message, Does.Contain("set to 1234g"));
        }

        [Test]
        public void GetSkill_UnknownSkill_LogsError()
        {
            // Exercise the private helper via a command that calls it
            _cmd.SetLevel("le_setlevel", new[] { "Nope", "1" });
            Assert.That(LastLog().Level, Is.EqualTo(LogLevel.Error));
            Assert.That(LastLog().Message, Does.Contain("Could not find a skill named 'Nope'"));
        }
    }
}
