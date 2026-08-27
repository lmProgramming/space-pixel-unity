using System.Collections.Generic;
using System.IO;
using Core.Constants;
using Core.Ships;
using NUnit.Framework;
using Services;
using UnityEngine;
using ZLinq;

namespace Services.Tests
{
    public class ShipSnapshotRepositoryTests
    {
        private static string SnapshotsFolder => Constants.ShipSnapshotsFolder;

        private readonly List<string> _createdFilePaths = new();
        private bool _snapshotsFolderExistedBeforeSetUp;

        [SetUp]
        public void SetUp()
        {
            _snapshotsFolderExistedBeforeSetUp = Directory.Exists(SnapshotsFolder);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var filePath in _createdFilePaths)
                if (File.Exists(filePath))
                    File.Delete(filePath);

            _createdFilePaths.Clear();

            if (!_snapshotsFolderExistedBeforeSetUp && Directory.Exists(SnapshotsFolder))
                Directory.Delete(SnapshotsFolder, true);
        }

        [Test]
        public void Constructor_FirstLaunch_SeedsBuiltInAllyShip()
        {
            if (_snapshotsFolderExistedBeforeSetUp)
                Assert.Ignore(
                    "Snapshots folder already exists on this machine; skipping first-launch seeding check.");

            var repository = new ShipSnapshotRepository();

            Assert.That(File.Exists(Path.Combine(SnapshotsFolder, "Ally.json")), Is.True);
            Assert.That(repository.Model.Snapshots, Has.Count.EqualTo(1));
            Assert.That(repository.Model.Snapshots[0].DisplayName, Is.EqualTo("Ally"));
        }

        [Test]
        public void Constructor_ExistingSnapshotsFolder_DoesNotReseedBuiltInShips()
        {
            if (_snapshotsFolderExistedBeforeSetUp &&
                File.Exists(Path.Combine(SnapshotsFolder, "Ally.json")))
                Assert.Ignore(
                    "A saved 'Ally' ship already exists on this machine; cannot verify absence of reseeding.");

            Directory.CreateDirectory(SnapshotsFolder);
            var dummySnapshotPath = Path.Combine(SnapshotsFolder, "Dummy.json");
            File.WriteAllText(dummySnapshotPath, JsonUtility.ToJson(new ShipSnapshot("Dummy")));
            _createdFilePaths.Add(dummySnapshotPath);

            var repository = new ShipSnapshotRepository();

            Assert.That(
                repository.Model.Snapshots.AsValueEnumerable().Any(snapshot => snapshot.DisplayName == "Dummy"),
                Is.True);
            Assert.That(
                repository.Model.Snapshots.AsValueEnumerable().Any(snapshot => snapshot.DisplayName == "Ally"),
                Is.False);
        }
    }
}
