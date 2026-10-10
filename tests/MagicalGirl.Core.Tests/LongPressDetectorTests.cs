using System;
using MagicalGirl.Core.Gesture;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class LongPressConfigTests
    {
        [Theory]
        [InlineData(0f, 10f)] // holdDurationSeconds=0
        [InlineData(-1f, 10f)] // holdDurationSeconds負
        [InlineData(1.2f, 0f)] // maxMovement=0
        [InlineData(1.2f, -1f)] // maxMovement負
        public void Constructor_InvalidValues_Throws(float holdDurationSeconds, float maxMovement)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LongPressConfig(holdDurationSeconds, maxMovement));
        }

        [Fact]
        public void Constructor_ValidValues_SetsProperties()
        {
            var config = new LongPressConfig(holdDurationSeconds: 1.2f, maxMovement: 24f);

            Assert.Equal(1.2f, config.HoldDurationSeconds);
            Assert.Equal(24f, config.MaxMovement);
        }
    }

    public class LongPressDetectorTests
    {
        static LongPressConfig DefaultConfig(float holdDurationSeconds = 1.2f, float maxMovement = 24f) =>
            new LongPressConfig(holdDurationSeconds, maxMovement);

        [Fact]
        public void IsTriggered_EnoughTimeAndNoMovement_ReturnsTrue()
        {
            var result = LongPressDetector.IsTriggered(DefaultConfig(), elapsedSeconds: 1.5f, maxMovement: 0f);

            Assert.True(result);
        }

        [Fact]
        public void IsTriggered_ExactlyAtThresholds_ReturnsTrue()
        {
            var result = LongPressDetector.IsTriggered(DefaultConfig(holdDurationSeconds: 1.2f, maxMovement: 24f), elapsedSeconds: 1.2f, maxMovement: 24f);

            Assert.True(result);
        }

        [Fact]
        public void IsTriggered_NotEnoughTime_ReturnsFalse()
        {
            var result = LongPressDetector.IsTriggered(DefaultConfig(holdDurationSeconds: 1.2f), elapsedSeconds: 0.5f, maxMovement: 0f);

            Assert.False(result);
        }

        [Fact]
        public void IsTriggered_MovedTooMuch_ReturnsFalse()
        {
            var result = LongPressDetector.IsTriggered(DefaultConfig(maxMovement: 24f), elapsedSeconds: 2f, maxMovement: 50f);

            Assert.False(result);
        }

        [Fact]
        public void IsTriggered_NegativeElapsedSeconds_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LongPressDetector.IsTriggered(DefaultConfig(), elapsedSeconds: -0.1f, maxMovement: 0f));
        }

        [Fact]
        public void IsTriggered_NegativeMaxMovement_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LongPressDetector.IsTriggered(DefaultConfig(), elapsedSeconds: 1.5f, maxMovement: -1f));
        }
    }
}
