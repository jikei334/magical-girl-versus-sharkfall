using System;
using MagicalGirl.Core.Combat;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class FireballConfigTests
    {
        [Theory]
        [InlineData(0f, 10f, 100f)] // speed=0
        [InlineData(-1f, 10f, 100f)] // speed負
        [InlineData(30f, 0f, 100f)] // damage=0
        [InlineData(30f, -1f, 100f)] // damage負
        [InlineData(30f, 10f, 0f)] // maxRange=0
        [InlineData(30f, 10f, -1f)] // maxRange負
        public void Constructor_InvalidValues_Throws(float speed, float damage, float maxRange)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FireballConfig(speed, damage, maxRange));
        }

        [Fact]
        public void Constructor_ValidValues_SetsProperties()
        {
            var config = new FireballConfig(speed: 30f, damage: 10f, maxRange: 100f);

            Assert.Equal(30f, config.Speed);
            Assert.Equal(10f, config.Damage);
            Assert.Equal(100f, config.MaxRange);
        }
    }

    public class FireballStateTests
    {
        static FireballConfig DefaultConfig(float speed = 10f, float damage = 5f, float maxRange = 50f) =>
            new FireballConfig(speed, damage, maxRange);

        [Fact]
        public void Constructor_StartsAtZeroDistance_NotExpired()
        {
            var state = new FireballState(DefaultConfig());

            Assert.Equal(0f, state.DistanceTraveled);
            Assert.False(state.IsExpired);
        }

        [Fact]
        public void Advance_AccumulatesDistanceBySpeedTimesDeltaTime()
        {
            var state = new FireballState(DefaultConfig(speed: 10f));

            state.Advance(0.5f);

            Assert.Equal(5f, state.DistanceTraveled);
        }

        [Fact]
        public void Advance_ReachingMaxRange_Expires()
        {
            var state = new FireballState(DefaultConfig(speed: 10f, maxRange: 20f));

            state.Advance(2f);

            Assert.True(state.IsExpired);
        }

        [Fact]
        public void Advance_AfterExpired_DoesNotAccumulateFurther()
        {
            var state = new FireballState(DefaultConfig(speed: 10f, maxRange: 20f));
            state.Advance(2f); // ちょうど射程到達

            state.Advance(5f); // 追加で進めても増えないはず

            Assert.Equal(20f, state.DistanceTraveled);
        }

        [Fact]
        public void Advance_NegativeDeltaTime_Throws()
        {
            var state = new FireballState(DefaultConfig());
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Advance(-0.1f));
        }
    }
}
