using Core.Gameplay.Progression;
using Core.Ships;
using NUnit.Framework;

namespace Services.Tests
{
    public class ProgressionRepositoryTests
    {
        private ProgressionRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _repository = new ProgressionRepository();
            if (_repository.SlotHasSave(1))
                _repository.Delete(1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_repository.SlotHasSave(1))
                _repository.Delete(1);
        }

        [Test]
        public void SaveLoadDelete_RoundTripsSlotData()
        {
            var save = new ProgressionSave
            {
                campaignName = "Test Campaign",
                allies = new[] { new ShipSnapshot("Frigate") },
                credits = "0",
                enemiesKilled = 2
            };

            _repository.Save(1, save);

            Assert.That(_repository.SlotHasSave(1), Is.True);
            Assert.That(_repository.Model.Slots[1].CampaignName, Is.EqualTo("Test Campaign"));

            var loaded = _repository.Load(1);
            Assert.That(loaded.campaignName, Is.EqualTo("Test Campaign"));
            Assert.That(loaded.enemiesKilled, Is.EqualTo(2));
            Assert.That(loaded.allies, Has.Length.EqualTo(1));
            Assert.That(loaded.allies[0].shipName, Is.EqualTo("Frigate"));

            _repository.Delete(1);

            Assert.That(_repository.SlotHasSave(1), Is.False);
            Assert.That(_repository.Model.Slots[1].HasSave, Is.False);
        }
    }
}