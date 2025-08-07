using NUnit.Framework;

namespace LevelExtender.Tests
{
    [TestFixture]
    public class SaveDataModelTests
    {
        [Test]
        public void Default_EnableWorldMonsters_IsFalse()
        {
            var model = new SaveDataModel();
            Assert.That(model.EnableWorldMonsters, Is.False);
        }

        [Test]
        public void Can_Set_EnableWorldMonsters_ToTrue()
        {
            var model = new SaveDataModel();
            model.EnableWorldMonsters = true;
            Assert.That(model.EnableWorldMonsters, Is.True);
        }

        [Test]
        public void Can_Set_EnableWorldMonsters_BackToFalse()
        {
            var model = new SaveDataModel { EnableWorldMonsters = true };
            model.EnableWorldMonsters = false;
            Assert.That(model.EnableWorldMonsters, Is.False);
        }
    }
}
