using Core.Gameplay;
using Core.Gameplay.Progression;
using Core.Services;
using Core.Ships;
using Core.State;
using NSubstitute;
using NUnit.Framework;

namespace Services.Tests
{
    public class ProgressionBattleSpawnConfigurationProviderTests
    {
        [Test]
        public void GetConfiguration_SplitsSelectedAllyFromRemainingAllies()
        {
            var repository = Substitute.For<IProgressionRepository>();
            repository.Load(0).Returns(new ProgressionSave
            {
                campaignName = "Campaign",
                allies = new[]
                {
                    new ShipSnapshot("Alpha"),
                    new ShipSnapshot("Bravo"),
                    new ShipSnapshot("Charlie")
                }
            });

            SaveState.Mode = GameSessionMode.Progression;
            SaveState.ProgressionSlotIndex = 0;
            SaveState.SelectedAllyIndex = 1;
            SaveState.EnemySnapshots = new[] { new ShipSnapshot("Enemy") };
            SaveState.AsteroidCount = 3;

            var provider = new ProgressionBattleSpawnConfigurationProvider(repository);
            var configuration = provider.GetConfiguration();

            Assert.That(configuration.PlayerShipSnapshot!.shipName, Is.EqualTo("Bravo"));
            Assert.That(configuration.AllySnapshots, Has.Count.EqualTo(2));
            Assert.That(configuration.AllySnapshots[0].shipName, Is.EqualTo("Alpha"));
            Assert.That(configuration.AllySnapshots[1].shipName, Is.EqualTo("Charlie"));
            Assert.That(configuration.EnemySnapshots, Has.Count.EqualTo(1));
            Assert.That(configuration.EnemySnapshots[0].shipName, Is.EqualTo("Enemy"));
            Assert.That(configuration.AsteroidCount, Is.EqualTo(3));
        }
    }
}