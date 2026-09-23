using System;

namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// ピッチの正規化された大きさ(0〜1)から、通常域・緩衝ゾーン・ブレーキのどれに
    /// 該当するかを判定するユーティリティ。
    /// </summary>
    public static class PitchZoneClassifier
    {
        /// <summary>
        /// ピッチの大きさをゾーンに分類する。
        /// 引数:
        ///   magnitude - ピッチの正規化された大きさ(0〜1を想定)
        ///   normalMax - 通常域の上限(0より大きく1以下)
        ///   brakeMin - ブレーキ域の下限(normalMaxより大きく1以下)
        /// 返り値: 該当するPitchZone(magnitude &lt; normalMaxならNormal、
        ///          normalMax〜brakeMin未満ならBuffer、brakeMin以上ならBrake)
        /// 例外: normalMax/brakeMinの大小関係が不正な場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public static PitchZone Classify(float magnitude, float normalMax, float brakeMin)
        {
            if (normalMax <= 0f || normalMax > 1f)
                throw new ArgumentOutOfRangeException(nameof(normalMax), normalMax, "normalMaxは0より大きく1以下である必要があります。");
            if (brakeMin <= normalMax || brakeMin > 1f)
                throw new ArgumentOutOfRangeException(nameof(brakeMin), brakeMin, "brakeMinはnormalMaxより大きく1以下である必要があります。");

            if (magnitude < normalMax)
                return PitchZone.Normal;
            if (magnitude < brakeMin)
                return PitchZone.Buffer;
            return PitchZone.Brake;
        }
    }
}
