using System;

namespace MagicalGirl.Core.Combat
{
    /// <summary>
    /// バリア(展開時に周囲へ範囲攻撃しつつ、一定時間展開状態が続く)のパラメータ。
    /// 全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct BarrierConfig
    {
        /// <summary>展開状態が持続する時間(秒、0より大きい値)。</summary>
        public float Duration { get; }

        /// <summary>展開した瞬間に範囲内の敵へ与えるダメージ量(0より大きい値)。</summary>
        public float BurstDamage { get; }

        /// <summary>範囲攻撃が届く半径(単位、0より大きい値)。</summary>
        public float BurstRadius { get; }

        /// <summary>
        /// 引数: duration, burstDamage, burstRadius - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が0以下の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public BarrierConfig(float duration, float burstDamage, float burstRadius)
        {
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "durationは0より大きい必要があります。");
            if (burstDamage <= 0f)
                throw new ArgumentOutOfRangeException(nameof(burstDamage), burstDamage, "burstDamageは0より大きい必要があります。");
            if (burstRadius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(burstRadius), burstRadius, "burstRadiusは0より大きい必要があります。");

            Duration = duration;
            BurstDamage = burstDamage;
            BurstRadius = burstRadius;
        }
    }
}
