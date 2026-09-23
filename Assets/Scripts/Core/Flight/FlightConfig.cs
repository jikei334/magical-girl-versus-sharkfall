using System;

namespace MagicalGirl.Core.Flight
{
    /// <summary>
    /// FlightModelの動作パラメータ。全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct FlightConfig
    {
        /// <summary>ピッチ・ロールがニュートラルなときの基準前進速度(単位/秒、0より大きい値)。</summary>
        public float BaseSpeed { get; }

        /// <summary>前進速度の下限(0以上、BaseSpeed以下)。</summary>
        public float MinSpeed { get; }

        /// <summary>前進速度の上限(BaseSpeed以上)。</summary>
        public float MaxSpeed { get; }

        /// <summary>climbAmount(-1〜1、+1で機首を目一杯上げた状態)1あたりの速度変化量(単位/秒、0以上)。
        /// 機首下げ(climbAmountが負)で加速、機首上げ(climbAmountが正)で減速する。</summary>
        public float PitchSpeedSensitivity { get; }

        /// <summary>ロール入力1あたりの最大旋回速度(度/秒、0以上)。</summary>
        public float MaxYawRateDegPerSecond { get; }

        /// <summary>ロール入力1あたりの最大バンク角(度、0以上)。</summary>
        public float MaxBankAngleDeg { get; }

        /// <summary>climbAmount1あたりの最大機首上げ/下げ角(度、0以上)。実際の上昇/降下軌道もこの角度で決まる。</summary>
        public float MaxPitchAngleDeg { get; }

        /// <summary>通常時、現在速度が目標速度へ追従する加速度(単位/秒^2、0より大きい値)。</summary>
        public float SpeedAcceleration { get; }

        /// <summary>バンク角・機首上げ下げ角が目標値へ追従する角速度(度/秒、0より大きい値)。</summary>
        public float AttitudeRateDegPerSecond { get; }

        /// <summary>ブレーキ(失速)中、現在速度が目標速度へ追従する加速度(単位/秒^2、0より大きい値。
        /// 通常はSpeedAccelerationより大きい値にして急減速を表現する)。</summary>
        public float BrakeDeceleration { get; }

        /// <summary>ブレーキ(失速)中の目標速度(単位/秒、0以上MinSpeed以下)。</summary>
        public float StallSpeed { get; }

        /// <summary>
        /// 設定値を検証しつつ構築する。各引数の意味は同名プロパティのコメントを参照。
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が許容範囲外の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public FlightConfig(
            float baseSpeed,
            float minSpeed,
            float maxSpeed,
            float pitchSpeedSensitivity,
            float maxYawRateDegPerSecond,
            float maxBankAngleDeg,
            float maxPitchAngleDeg,
            float speedAcceleration,
            float attitudeRateDegPerSecond,
            float brakeDeceleration,
            float stallSpeed)
        {
            if (baseSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(baseSpeed), baseSpeed, "baseSpeedは0より大きい必要があります。");
            if (minSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(minSpeed), minSpeed, "minSpeedは0以上である必要があります。");
            if (minSpeed > baseSpeed)
                throw new ArgumentOutOfRangeException(nameof(minSpeed), minSpeed, "minSpeedはbaseSpeed以下である必要があります。");
            if (maxSpeed < baseSpeed)
                throw new ArgumentOutOfRangeException(nameof(maxSpeed), maxSpeed, "maxSpeedはbaseSpeed以上である必要があります。");
            if (pitchSpeedSensitivity < 0f)
                throw new ArgumentOutOfRangeException(nameof(pitchSpeedSensitivity), pitchSpeedSensitivity, "pitchSpeedSensitivityは0以上である必要があります。");
            if (maxYawRateDegPerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxYawRateDegPerSecond), maxYawRateDegPerSecond, "maxYawRateDegPerSecondは0以上である必要があります。");
            if (maxBankAngleDeg < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxBankAngleDeg), maxBankAngleDeg, "maxBankAngleDegは0以上である必要があります。");
            if (maxPitchAngleDeg < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxPitchAngleDeg), maxPitchAngleDeg, "maxPitchAngleDegは0以上である必要があります。");
            if (speedAcceleration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speedAcceleration), speedAcceleration, "speedAccelerationは0より大きい必要があります。");
            if (attitudeRateDegPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(attitudeRateDegPerSecond), attitudeRateDegPerSecond, "attitudeRateDegPerSecondは0より大きい必要があります。");
            if (brakeDeceleration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(brakeDeceleration), brakeDeceleration, "brakeDecelerationは0より大きい必要があります。");
            if (stallSpeed < 0f || stallSpeed > minSpeed)
                throw new ArgumentOutOfRangeException(nameof(stallSpeed), stallSpeed, "stallSpeedは0以上minSpeed以下である必要があります。");

            BaseSpeed = baseSpeed;
            MinSpeed = minSpeed;
            MaxSpeed = maxSpeed;
            PitchSpeedSensitivity = pitchSpeedSensitivity;
            MaxYawRateDegPerSecond = maxYawRateDegPerSecond;
            MaxBankAngleDeg = maxBankAngleDeg;
            MaxPitchAngleDeg = maxPitchAngleDeg;
            SpeedAcceleration = speedAcceleration;
            AttitudeRateDegPerSecond = attitudeRateDegPerSecond;
            BrakeDeceleration = brakeDeceleration;
            StallSpeed = stallSpeed;
        }
    }
}
