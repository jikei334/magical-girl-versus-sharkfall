using System;
using MagicalGirl.Core.Input;

namespace MagicalGirl.Core.Flight
{
    /// <summary>
    /// 傾き入力(TiltInputState)から、箒の飛行に必要な速度・旋回速度・姿勢角を算出する
    /// Unity非依存のロジック。実際のTransform操作(移動・回転の適用)はUnity側が行う。
    /// </summary>
    public sealed class FlightModel
    {
        readonly FlightConfig m_Config;
        float m_CurrentSpeed;
        float m_CurrentPitchAngle;
        float m_CurrentBankAngle;

        /// <summary>
        /// 引数: config - 動作パラメータ
        /// 返り値: なし(コンストラクタ)。速度は基準速度から開始する
        /// </summary>
        public FlightModel(FlightConfig config)
        {
            m_Config = config;
            m_CurrentSpeed = config.BaseSpeed;
        }

        /// <summary>
        /// 1フレーム分の傾き入力から飛行状態を更新する。
        /// 引数:
        ///   tilt - TiltInputProcessorが算出した現在の入力状態
        ///   brakeOnNegativePitch - ブレーキが負のピッチで発動する設定かどうか
        ///     (どちらの符号が「機首上げ」に対応するかの判定に使う)
        ///   deltaTime - 前回更新からの経過秒数(0以上である必要がある)
        /// 返り値: 現在の速度・旋回速度・姿勢角をまとめたFlightOutput
        /// 例外: deltaTimeが負の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public FlightOutput Step(TiltInputState tilt, bool brakeOnNegativePitch, float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "deltaTimeは0以上である必要があります。");

            // climbAmount: +1で機首を目一杯上げた状態(ブレーキ方向)、-1で目一杯下げた状態。
            // ブレーキの発動方向(BrakeOnNegativePitch)から、どちらの符号が「機首上げ」かを逆算する。
            var climbAmount = brakeOnNegativePitch ? -tilt.Pitch : tilt.Pitch;

            var isBraking = tilt.Zone == BrakeZone.Brake;

            float targetSpeed;
            if (isBraking)
            {
                targetSpeed = m_Config.StallSpeed;
            }
            else
            {
                targetSpeed = m_Config.BaseSpeed - climbAmount * m_Config.PitchSpeedSensitivity;
                targetSpeed = Math.Clamp(targetSpeed, m_Config.MinSpeed, m_Config.MaxSpeed);
            }

            var speedAccel = isBraking ? m_Config.BrakeDeceleration : m_Config.SpeedAcceleration;
            m_CurrentSpeed = MoveTowards(m_CurrentSpeed, targetSpeed, speedAccel * deltaTime);

            var targetPitchAngle = climbAmount * m_Config.MaxPitchAngleDeg;
            m_CurrentPitchAngle = MoveTowards(m_CurrentPitchAngle, targetPitchAngle, m_Config.AttitudeRateDegPerSecond * deltaTime);

            var targetBankAngle = tilt.Roll * m_Config.MaxBankAngleDeg;
            m_CurrentBankAngle = MoveTowards(m_CurrentBankAngle, targetBankAngle, m_Config.AttitudeRateDegPerSecond * deltaTime);

            var yawRate = tilt.Roll * m_Config.MaxYawRateDegPerSecond;

            return new FlightOutput(m_CurrentSpeed, yawRate, m_CurrentPitchAngle, m_CurrentBankAngle);
        }

        /// <summary>
        /// currentからtargetへ、maxDeltaを超えない範囲で近づけた値を返す。
        /// 引数: current, target - 現在値・目標値 / maxDelta - 1回で近づける最大量(0以上)
        /// 返り値: 近づけた後の値
        /// </summary>
        static float MoveTowards(float current, float target, float maxDelta)
        {
            var diff = target - current;
            if (Math.Abs(diff) <= maxDelta)
                return target;
            return current + Math.Sign(diff) * maxDelta;
        }
    }
}
