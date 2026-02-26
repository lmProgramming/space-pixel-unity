using System;
using NUnit.Framework;
using Pixelation;
using UnityEngine;

namespace Pixelation.Tests
{
    [TestFixture]
    public class PixelTests
    {
        [Test]
        public void Constructor_InitializesHealthToMaxHealth()
        {
            var pixel = new Pixel(Color.red, 100f);

            Assert.AreEqual(100f, pixel.Health);
            Assert.AreEqual(100f, pixel.MaxHealth);
        }

        [Test]
        public void Constructor_SetsColor()
        {
            var pixel = new Pixel(Color.blue, 50f);

            Assert.AreEqual(Color.blue, pixel.Color);
        }

        [Test]
        public void RepairToMaxHealth_RestoresHealthToMax()
        {
            var pixel = new Pixel(Color.red, 100f);

            pixel.RepairToMaxHealth();

            Assert.AreEqual(pixel.MaxHealth, pixel.Health);
        }

        [Test]
        public void Pixel_IsSerializable()
        {
            Assert.IsTrue(typeof(Pixel).IsDefined(typeof(SerializableAttribute), false),
                "Pixel class must have [Serializable] attribute");
        }
    }
}
