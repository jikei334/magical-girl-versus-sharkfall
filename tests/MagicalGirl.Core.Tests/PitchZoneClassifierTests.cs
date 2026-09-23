using System;
using MagicalGirl.Core.Input;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class PitchZoneClassifierTests
    {
        const float NormalMax = 0.75f;
        const float BrakeMin = 0.9f;

        [Theory]
        [InlineData(0f)]
        [InlineData(0.5f)]
        [InlineData(0.74f)]
        public void Classify_BelowNormalMax_ReturnsNormal(float magnitude)
        {
            Assert.Equal(PitchZone.Normal, PitchZoneClassifier.Classify(magnitude, NormalMax, BrakeMin));
        }

        [Theory]
        [InlineData(0.75f)]
        [InlineData(0.8f)]
        [InlineData(0.89f)]
        public void Classify_BetweenNormalMaxAndBrakeMin_ReturnsBuffer(float magnitude)
        {
            Assert.Equal(PitchZone.Buffer, PitchZoneClassifier.Classify(magnitude, NormalMax, BrakeMin));
        }

        [Theory]
        [InlineData(0.9f)]
        [InlineData(1f)]
        public void Classify_AtOrAboveBrakeMin_ReturnsBrake(float magnitude)
        {
            Assert.Equal(PitchZone.Brake, PitchZoneClassifier.Classify(magnitude, NormalMax, BrakeMin));
        }

        [Fact]
        public void Classify_BrakeMinNotGreaterThanNormalMax_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PitchZoneClassifier.Classify(0.5f, normalMax: 0.8f, brakeMin: 0.8f));
        }

        [Fact]
        public void Classify_NormalMaxOutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PitchZoneClassifier.Classify(0.5f, normalMax: 0f, brakeMin: 0.9f));
        }
    }
}
