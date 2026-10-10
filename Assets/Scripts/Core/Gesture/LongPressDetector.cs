using System;

namespace MagicalGirl.Core.Gesture
{
    /// <summary>
    /// 長押し判定の純粋な条件式。実際のタッチ座標・経過時間の計測はUnity側
    /// (GestureInputController)が行い、このクラスは「その結果が長押し条件を
    /// 満たすか」だけを判定する。
    /// </summary>
    public static class LongPressDetector
    {
        /// <summary>
        /// 保持時間と開始位置からの最大移動量(これまでの最大値)から、長押し条件を満たすか判定する。
        /// 引数: config - 判定パラメータ / elapsedSeconds - タッチ開始からの経過時間(秒、0以上)
        ///        / maxMovement - 開始位置からの移動量の最大値(0以上)
        /// 返り値: 長押し条件(経過時間が十分で、かつ動かしていない)を満たすかどうか
        /// 例外: elapsedSecondsまたはmaxMovementが負の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public static bool IsTriggered(LongPressConfig config, float elapsedSeconds, float maxMovement)
        {
            if (elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds, "elapsedSecondsは0以上である必要があります。");
            if (maxMovement < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxMovement), maxMovement, "maxMovementは0以上である必要があります。");

            return elapsedSeconds >= config.HoldDurationSeconds && maxMovement <= config.MaxMovement;
        }
    }
}
