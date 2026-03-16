using NUnit.Framework;
using ShipFactory.Serialization;

namespace ShipFactory.Tests.Serialization
{
    [TestFixture]
    public class SnapshotNameUtilityTests
    {
        [Test]
        public void GetNextCopyName_WhenBaseNameIsFree_ReturnsBaseName()
        {
            var result = SnapshotNameUtility.GetNextCopyName("Ship Alpha", _ => false);

            Assert.That(result, Is.EqualTo("Ship Alpha"));
        }

        [Test]
        public void GetNextCopyName_WhenBaseNameExists_ReturnsFirstFreeCopyName()
        {
            var result = SnapshotNameUtility.GetNextCopyName("Ship Alpha", name =>
                name is "Ship Alpha" or "Ship Alpha (2)");

            Assert.That(result, Is.EqualTo("Ship Alpha (3)"));
        }

        [Test]
        public void SanitizeFileName_WhenNameIsEmpty_ReturnsDefaultShipName()
        {
            var result = SnapshotNameUtility.SanitizeFileName("  ");

            Assert.That(result, Is.EqualTo("Ship"));
        }
    }
}

