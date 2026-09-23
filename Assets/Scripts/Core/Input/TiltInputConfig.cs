using System;

namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// TiltInputProcessorの動作パラメータ。全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct TiltInputConfig
    {
        /// <summary>この絶対値未満の入力を0として扱うデッドゾーン(0以上1未満)。</summary>
        public float Deadzone { get; }

        /// <summary>感度カーブの指数(0より大きい値。1で線形、大きいほど非線形が強まる)。</summary>
        public float Exponent { get; }

        /// <summary>ロール軸で正規化値が±1に達するとみなす傾き角度(度、0より大きい値)。</summary>
        public float RollFullScaleDegrees { get; }

        /// <summary>ピッチ軸で正規化値が±1に達するとみなす傾き角度(度、0より大きい値)。</summary>
        public float PitchFullScaleDegrees { get; }

        /// <summary>ピッチの「通常域」の上限(0より大きく1以下)。これを超えると緩衝ゾーンに入る。</summary>
        public float PitchNormalMax { get; }

        /// <summary>ピッチの「ブレーキ(極端域)」の下限(PitchNormalMaxより大きく1以下)。</summary>
        public float PitchBrakeMin { get; }

        /// <summary>
        /// 設定値を検証しつつ構築する。各引数の意味は同名プロパティのコメントを参照。
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が許容範囲外の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public TiltInputConfig(
            float deadzone,
            float exponent,
            float rollFullScaleDegrees,
            float pitchFullScaleDegrees,
            float pitchNormalMax,
            float pitchBrakeMin)
        {
            if (deadzone < 0f || deadzone >= 1f)
                throw new ArgumentOutOfRangeException(nameof(deadzone), deadzone, "deadzoneは0以上1未満である必要があります。");
            if (exponent <= 0f)
                throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "exponentは0より大きい必要があります。");
            if (rollFullScaleDegrees <= 0f)
                throw new ArgumentOutOfRangeException(nameof(rollFullScaleDegrees), rollFullScaleDegrees, "rollFullScaleDegreesは0より大きい必要があります。");
            if (pitchFullScaleDegrees <= 0f)
                throw new ArgumentOutOfRangeException(nameof(pitchFullScaleDegrees), pitchFullScaleDegrees, "pitchFullScaleDegreesは0より大きい必要があります。");
            if (pitchNormalMax <= 0f || pitchNormalMax > 1f)
                throw new ArgumentOutOfRangeException(nameof(pitchNormalMax), pitchNormalMax, "pitchNormalMaxは0より大きく1以下である必要があります。");
            if (pitchBrakeMin <= pitchNormalMax || pitchBrakeMin > 1f)
                throw new ArgumentOutOfRangeException(nameof(pitchBrakeMin), pitchBrakeMin, "pitchBrakeMinはpitchNormalMaxより大きく1以下である必要があります。");

            Deadzone = deadzone;
            Exponent = exponent;
            RollFullScaleDegrees = rollFullScaleDegrees;
            PitchFullScaleDegrees = pitchFullScaleDegrees;
            PitchNormalMax = pitchNormalMax;
            PitchBrakeMin = pitchBrakeMin;
        }
    }
}
