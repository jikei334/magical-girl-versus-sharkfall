using System;
using MagicalGirl.Core.Input;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class BrakeZoneClassifierTests
    {
        const float NormalMax = 0.75f;
        const float BrakeMin = 0.9f;

        [Theory]
        [InlineData(0f)]
        [InlineData(0.5f)]
        [InlineData(0.74f)]
        public void Classify_BelowNormalMax_ReturnsNormal(float magnitude)
        {
            Assert.Equal(BrakeZone.Normal, BrakeZoneClassifier.Classify(magnitude, NormalMax, BrakeMin));
        }

        [Theory]
        [InlineData(0.75f)]
        [InlineData(0.8f)]
        [InlineData(0.89f)]
        public void Classify_BetweenNormalMaxAndBrakeMin_ReturnsBuffer(float magnitude)
        {
            Assert.Equal(BrakeZone.Buffer, BrakeZoneClassifier.Classify(magnitude, NormalMax, BrakeMin));
        }

        [Theory]
        [InlineData(0.9f)]
        [InlineData(1f)]
        public void Classify_AtOrAboveBrakeMin_ReturnsBrake(float magnitude)
        {
            Assert.Equal(BrakeZone.Brake, BrakeZoneClassifier.Classify(magnitude, NormalMax, BrakeMin));
        }

        [Fact]
        public void Classify_BrakeMinNotGreaterThanNormalMax_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BrakeZoneClassifier.Classify(0.5f, normalMax: 0.8f, brakeMin: 0.8f));
        }

        [Fact]
        public void Classify_NormalMaxOutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BrakeZoneClassifier.Classify(0.5f, normalMax: 0f, brakeMin: 0.9f));
        }
    }
}
