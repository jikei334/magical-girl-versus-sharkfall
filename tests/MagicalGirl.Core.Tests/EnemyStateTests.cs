using System;
using MagicalGirl.Core.Enemy;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class EnemyStateTests
    {
        static EnemyConfig DefaultConfig(float maxHealth = 100f, float speed = 10f) => new EnemyConfig(maxHealth, speed);

        [Fact]
        public void Constructor_StartsAtMaxHealth_NotDefeated()
        {
            var state = new EnemyState(DefaultConfig(maxHealth: 100f));

            Assert.Equal(100f, state.CurrentHealth);
            Assert.False(state.IsDefeated);
        }

        [Fact]
        public void TakeDamage_ReducesHealth()
        {
            var state = new EnemyState(DefaultConfig(maxHealth: 100f));

            state.TakeDamage(30f);

            Assert.Equal(70f, state.CurrentHealth);
            Assert.False(state.IsDefeated);
        }

        [Fact]
        public void TakeDamage_ExceedingHealth_ClampsToZeroAndDefeats()
        {
            var state = new EnemyState(DefaultConfig(maxHealth: 100f));

            state.TakeDamage(150f);

            Assert.Equal(0f, state.CurrentHealth);
            Assert.True(state.IsDefeated);
        }

        [Fact]
        public void TakeDamage_ExactlyDepletingHealth_Defeats()
        {
            var state = new EnemyState(DefaultConfig(maxHealth: 100f));

            state.TakeDamage(100f);

            Assert.True(state.IsDefeated);
        }

        [Fact]
        public void TakeDamage_AfterDefeated_IsIgnored()
        {
            // 雷撃チェイン等で同時に複数回ダメージが入っても、撃破後は多重処理されないことを確認する
            var state = new EnemyState(DefaultConfig(maxHealth: 100f));
            state.TakeDamage(100f);

            state.TakeDamage(50f); // 撃破済みなので無視されるはず

            Assert.Equal(0f, state.CurrentHealth);
            Assert.True(state.IsDefeated);
        }

        [Fact]
        public void TakeDamage_Negative_Throws()
        {
            var state = new EnemyState(DefaultConfig());
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TakeDamage(-1f));
        }

        [Fact]
        public void TakeDamage_RepeatedSmallAmountsWithFloatingPointRounding_EventuallyDefeats()
        {
            // 1/3ずつのような割り切れないダメージを繰り返すと、浮動小数点の丸め誤差で
            // CurrentHealthが厳密な0ではなくごく小さい正の値として残ることがある。
            // IsDefeatedがepsilon込みで判定され、正しく撃破扱いになることを確認する回帰テスト。
            var state = new EnemyState(DefaultConfig(maxHealth: 1f));

            for (var i = 0; i < 3; i++)
                state.TakeDamage(1f / 3f);

            Assert.True(state.IsDefeated, $"CurrentHealth={state.CurrentHealth}であり、撃破と判定されるべき");
        }
    }

    public class EnemyConfigTests
    {
        [Theory]
        [InlineData(0f, 10f)] // maxHealth=0
        [InlineData(-1f, 10f)] // maxHealth負
        [InlineData(100f, 0f)] // speed=0
        [InlineData(100f, -1f)] // speed負
        public void Constructor_InvalidValues_Throws(float maxHealth, float speed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyConfig(maxHealth, speed));
        }
    }
}
