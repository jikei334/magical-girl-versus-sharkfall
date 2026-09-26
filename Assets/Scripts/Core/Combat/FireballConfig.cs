using System;

namespace MagicalGirl.Core.Combat
{
    /// <summary>
    /// 火球(直進弾)1発分のパラメータ。全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct FireballConfig
    {
        /// <summary>直進速度(単位/秒、0より大きい値)。</summary>
        public float Speed { get; }

        /// <summary>命中時に与えるダメージ量(0より大きい値)。</summary>
        public float Damage { get; }

        /// <summary>この距離だけ飛翔したら消滅する最大射程(0より大きい値)。</summary>
        public float MaxRange { get; }

        /// <summary>
        /// 引数: speed, damage, maxRange - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が0以下の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public FireballConfig(float speed, float damage, float maxRange)
        {
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "speedは0より大きい必要があります。");
            if (damage <= 0f)
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "damageは0より大きい必要があります。");
            if (maxRange <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxRange), maxRange, "maxRangeは0より大きい必要があります。");

            Speed = speed;
            Damage = damage;
            MaxRange = maxRange;
        }
    }
}
