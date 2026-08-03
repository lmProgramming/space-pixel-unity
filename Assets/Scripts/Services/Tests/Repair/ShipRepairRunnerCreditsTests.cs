using Core.Constants;
using Core.Services.Repair;
using NSubstitute;
using NUnit.Framework;
using Services.Repair;
using UnityEngine;

namespace Services.Tests.Repair
{
    [TestFixture]
    public class ShipRepairRunnerCreditsTests
    {
        [Test]
        public void RepairAllAsync_StopsWhenCreditsAreExhausted()
        {
            var constants = ScriptableObject.CreateInstance<ProgressionConstants>();
            constants.creditsPerRepairedPixel = 5;
            constants.repairedPixelsPerFrame = 10;

            var repairService = Substitute.For<IShipRepairService>();
            var remaining = 3;
            repairService.HasWorkRemaining.Returns(_ => remaining > 0);
            repairService.TryRestoreOnePixel(out Arg.Any<string>()).Returns(call =>
            {
                if (remaining <= 0)
                {
                    call[0] = "done";
                    return false;
                }

                remaining--;
                call[0] = null;
                return true;
            });

            var runner = new ShipRepairRunner(repairService, constants);
            var credits = 8;
            var result = runner.RepairAllAsync(() => credits, spent => credits -= spent, default)
                .GetAwaiter().GetResult();

            Assert.AreEqual(ShipRepairStopReason.OutOfCredits, result.Reason);
            Assert.AreEqual(1, result.PixelsRestored);
            Assert.AreEqual(3, credits);
            Assert.AreEqual("Not enough credits", result.Message);
        }
    }
}