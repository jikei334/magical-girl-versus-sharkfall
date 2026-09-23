using System;

namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// 単一軸の生入力(-1〜1)に対して、デッドゾーンの除去と非線形感度カーブを適用するユーティリティ。
    /// Beam Proの傾き入力(ロール/ピッチ)など、Unityに依存しない箇所で共通利用する想定。
    /// UnityEngineへの参照を持たないため、Unityを起動せずに単体テストできる。
    /// </summary>
    public static class AxisInputCurve
    {
        /// <summary>
        /// デッドゾーンを除去し、べき乗カーブで正規化した値を返す。
        /// 引数:
        ///   rawValue - 生の入力値。想定範囲は-1〜1(範囲外はクランプされる)
        ///   deadzone - この絶対値未満の入力を0として扱う閾値。0以上1未満である必要がある
        ///   exponent - 感度カーブの指数。1で線形、1より大きいほど「入力の小さいうちは緩やかで、
        ///              後半にいくほど効きが強くなる」カーブになる。0より大きい必要がある
        /// 返り値: -1〜1に正規化・カーブ適用済みの値
        /// 例外: deadzoneまたはexponentが許容範囲外の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public static float Apply(float rawValue, float deadzone, float exponent)
        {
            if (deadzone < 0f || deadzone >= 1f)
                throw new ArgumentOutOfRangeException(nameof(deadzone), deadzone, "deadzoneは0以上1未満である必要があります。");
            if (exponent <= 0f)
                throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "exponentは0より大きい必要があります。");

            var clamped = Math.Clamp(rawValue, -1f, 1f);
            var sign = Math.Sign(clamped);
            var magnitude = Math.Abs(clamped);

            if (magnitude <= deadzone)
                return 0f;

            // デッドゾーン分を切り詰めたうえで、残りの範囲を0〜1に再スケールしてからカーブを適用する
            var normalized = (magnitude - deadzone) / (1f - deadzone);
            var curved = (float)Math.Pow(normalized, exponent);
            return sign * curved;
        }
    }
}
