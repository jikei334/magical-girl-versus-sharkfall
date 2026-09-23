using System;

namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// Beam Proの生の傾き角度(度)から、キャリブレーション・デッドゾーン・非線形カーブ・
    /// ブレーキゾーン判定までを行うUnity非依存のロジック。
    /// センサー値の取得自体はUnity側(例: MagicalGirl.Controls.BeamProTiltController)が
    /// 担当し、このクラスには角度(度)だけを渡す。
    /// </summary>
    public sealed class TiltInputProcessor
    {
        readonly TiltInputConfig m_Config;
        float m_NeutralRoll;
        float m_NeutralPitch;
        bool m_IsCalibrated;

        /// <summary>
        /// 引数: config - 動作パラメータ
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public TiltInputProcessor(TiltInputConfig config)
        {
            m_Config = config;
        }

        /// <summary>
        /// 現在の生角度をニュートラル位置として記録する。
        /// 引数: rawRoll, rawPitch - キャリブレーション時点の生角度(度)
        /// 返り値: なし
        /// </summary>
        public void Calibrate(float rawRoll, float rawPitch)
        {
            m_NeutralRoll = rawRoll;
            m_NeutralPitch = rawPitch;
            m_IsCalibrated = true;
        }

        /// <summary>
        /// 生角度から現在の入力状態を算出する。
        /// 引数: rawRoll, rawPitch - 現在の生角度(度)
        /// 返り値: デッドゾーン・カーブ・ブレーキ判定を適用したTiltInputState
        /// 例外: Calibrate()を未実行の場合、InvalidOperationExceptionを投げる
        /// </summary>
        public TiltInputState Process(float rawRoll, float rawPitch)
        {
            if (!m_IsCalibrated)
                throw new InvalidOperationException("Calibrate()を先に呼び出してニュートラル位置を設定してください。");

            var rollOffset = Normalize(rawRoll - m_NeutralRoll, m_Config.RollFullScaleDegrees);
            var pitchOffset = Normalize(rawPitch - m_NeutralPitch, m_Config.PitchFullScaleDegrees);

            var roll = AxisInputCurve.Apply(rollOffset, m_Config.Deadzone, m_Config.Exponent);
            var pitch = AxisInputCurve.Apply(pitchOffset, m_Config.Deadzone, m_Config.Exponent);

            // ブレーキは「機首を上げる」方向(正のピッチ)にのみ存在する。
            var zone = pitch > 0f
                ? PitchZoneClassifier.Classify(pitch, m_Config.PitchNormalMax, m_Config.PitchBrakeMin)
                : PitchZone.Normal;

            return new TiltInputState(roll, pitch, zone);
        }

        /// <summary>
        /// ニュートラル位置からのずれをfullScaleDegreesで正規化し、-1〜1にクランプする。
        /// 引数: offsetDegrees - ニュートラルからのずれ(度) / fullScaleDegrees - ±1とみなす角度(度)
        /// 返り値: -1〜1に正規化された値
        /// </summary>
        static float Normalize(float offsetDegrees, float fullScaleDegrees)
        {
            var normalized = offsetDegrees / fullScaleDegrees;
            return Math.Clamp(normalized, -1f, 1f);
        }
    }
}
