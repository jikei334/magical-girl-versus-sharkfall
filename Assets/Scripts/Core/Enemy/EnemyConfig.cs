using System;

namespace MagicalGirl.Core.Enemy
{
    /// <summary>
    /// 敵1体分のステータス。HP・速度はウェーブ倍率(Issue #12)を適用した後の最終値を渡す想定
    /// (このクラス自体は倍率計算を行わない)。全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct EnemyConfig
    {
        /// <summary>最大HP(0より大きい値)。</summary>
        public float MaxHealth { get; }

        /// <summary>直進速度(単位/秒、0より大きい値)。</summary>
        public float Speed { get; }

        /// <summary>
        /// 引数: maxHealth, speed - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が0以下の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public EnemyConfig(float maxHealth, float speed)
        {
            if (maxHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "maxHealthは0より大きい必要があります。");
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "speedは0より大きい必要があります。");

            MaxHealth = maxHealth;
            Speed = speed;
        }
    }
}
