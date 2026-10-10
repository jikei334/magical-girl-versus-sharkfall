using System;

namespace MagicalGirl.Core.Gesture
{
    /// <summary>
    /// 長押し判定(タッチしたままほぼ動かさずに一定時間経過)のパラメータ。
    /// 全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct LongPressConfig
    {
        /// <summary>長押しと判定するまでの保持時間(秒、0より大きい値)。</summary>
        public float HoldDurationSeconds { get; }

        /// <summary>開始位置からの移動量がこの値以下なら「動かしていない」とみなす(0より大きい値)。</summary>
        public float MaxMovement { get; }

        /// <summary>
        /// 引数: holdDurationSeconds, maxMovement - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が0以下の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public LongPressConfig(float holdDurationSeconds, float maxMovement)
        {
            if (holdDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(holdDurationSeconds), holdDurationSeconds, "holdDurationSecondsは0より大きい必要があります。");
            if (maxMovement <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxMovement), maxMovement, "maxMovementは0より大きい必要があります。");

            HoldDurationSeconds = holdDurationSeconds;
            MaxMovement = maxMovement;
        }
    }
}
