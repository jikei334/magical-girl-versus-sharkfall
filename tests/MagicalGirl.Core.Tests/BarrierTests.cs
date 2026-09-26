using System;
using MagicalGirl.Core.Combat;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class BarrierConfigTests
    {
        [Theory]
        [InlineData(0f, 10f, 5f)] // duration=0
        [InlineData(-1f, 10f, 5f)] // duration負
        [InlineData(3f, 0f, 5f)] // burstDamage=0
        [InlineData(3f, -1f, 5f)] // burstDamage負
        [InlineData(3f, 10f, 0f)] // burstRadius=0
        [InlineData(3f, 10f, -1f)] // burstRadius負
        public void Constructor_InvalidValues_Throws(float duration, float burstDamage, float burstRadius)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BarrierConfig(duration, burstDamage, burstRadius));
        }

        [Fact]
        public void Constructor_ValidValues_SetsProperties()
        {
            var config = new BarrierConfig(duration: 3f, burstDamage: 10f, burstRadius: 5f);

            Assert.Equal(3f, config.Duration);
            Assert.Equal(10f, config.BurstDamage);
            Assert.Equal(5f, config.BurstRadius);
        }
    }

    public class BarrierStateTests
    {
        static BarrierConfig DefaultConfig(float duration = 3f, float burstDamage = 10f, float burstRadius = 5f) =>
            new BarrierConfig(duration, burstDamage, burstRadius);

        [Fact]
        public void Constructor_StartsInactive()
        {
            var state = new BarrierState(DefaultConfig());

            Assert.False(state.IsActive);
            Assert.Equal(0f, state.RemainingDuration);
        }

        [Fact]
        public void Activate_BecomesActiveWithFullDuration()
        {
            var state = new BarrierState(DefaultConfig(duration: 3f));

            state.Activate();

            Assert.True(state.IsActive);
            Assert.Equal(3f, state.RemainingDuration);
        }

        [Fact]
        public void Tick_ReducesRemainingDuration()
        {
            var state = new BarrierState(DefaultConfig(duration: 3f));
            state.Activate();

            state.Tick(1f);

            Assert.True(state.IsActive);
            Assert.Equal(2f, state.RemainingDuration);
        }

        [Fact]
        public void Tick_ExceedingDuration_DeactivatesAndClampsToZero()
        {
            var state = new BarrierState(DefaultConfig(duration: 3f));
            state.Activate();

            state.Tick(10f);

            Assert.False(state.IsActive);
            Assert.Equal(0f, state.RemainingDuration);
        }

        [Fact]
        public void Tick_WhileInactive_DoesNothing()
        {
            var state = new BarrierState(DefaultConfig());

            state.Tick(1f);

            Assert.False(state.IsActive);
            Assert.Equal(0f, state.RemainingDuration);
        }

        [Fact]
        public void Activate_WhileAlreadyActive_ResetsRemainingDuration()
        {
            var state = new BarrierState(DefaultConfig(duration: 3f));
            state.Activate();
            state.Tick(2f); // 残り1秒

            state.Activate(); // 再詠唱でリセット

            Assert.Equal(3f, state.RemainingDuration);
        }

        [Fact]
        public void Tick_NegativeDeltaTime_Throws()
        {
            var state = new BarrierState(DefaultConfig());
            state.Activate();
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(-0.1f));
        }
    }
}
