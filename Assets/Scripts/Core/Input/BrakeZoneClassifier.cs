using System;

namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// ブレーキ判定に使う軸の正規化された大きさ(0〜1)から、通常域・緩衝ゾーン・ブレーキの
    /// どれに該当するかを判定するユーティリティ。
    /// </summary>
    public static class BrakeZoneClassifier
    {
        /// <summary>
        /// 大きさをゾーンに分類する。
        /// 引数:
        ///   magnitude - 正規化された大きさ(0〜1を想定)
        ///   normalMax - 通常域の上限(0より大きく1以下)
        ///   brakeMin - ブレーキ域の下限(normalMaxより大きく1以下)
        /// 返り値: 該当するBrakeZone(magnitude &lt; normalMaxならNormal、
        ///          normalMax〜brakeMin未満ならBuffer、brakeMin以上ならBrake)
        /// 例外: normalMax/brakeMinの大小関係が不正な場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public static BrakeZone Classify(float magnitude, float normalMax, float brakeMin)
        {
            if (normalMax <= 0f || normalMax > 1f)
                throw new ArgumentOutOfRangeException(nameof(normalMax), normalMax, "normalMaxは0より大きく1以下である必要があります。");
            if (brakeMin <= normalMax || brakeMin > 1f)
                throw new ArgumentOutOfRangeException(nameof(brakeMin), brakeMin, "brakeMinはnormalMaxより大きく1以下である必要があります。");

            if (magnitude < normalMax)
                return BrakeZone.Normal;
            if (magnitude < brakeMin)
                return BrakeZone.Buffer;
            return BrakeZone.Brake;
        }
    }
}
