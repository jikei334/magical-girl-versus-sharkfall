using MagicalGirl.Core.Input;
using UnityEngine;

namespace MagicalGirl.Controls
{
    /// <summary>
    /// Beam Pro本体の物理的な傾きを加速度センサーから取得し、ロール・ピッチの
    /// 制御入力に変換するコンポーネント。
    /// 実際の数値計算(キャリブレーション・デッドゾーン・非線形カーブ・ブレーキ判定)は
    /// Unity非依存のMagicalGirl.Core.Input.TiltInputProcessorに委譲する。
    ///
    /// 注意: 加速度センサーからのロール/ピッチ算出式(ReadRawAngles)の軸の向き・符号は
    /// Beam Pro実機での検証がまだ行われていない。実機でロール/ピッチが意図通りの方向に
    /// 反応するかを確認し、必要なら符号や軸の対応を調整すること。
    /// </summary>
    public class BeamProTiltController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("この絶対値未満の傾きを0として扱うデッドゾーン(0〜1未満)")]
        float m_Deadzone = 0.08f;

        [SerializeField]
        [Tooltip("感度カーブの指数。1で線形、大きいほど非線形が強まる")]
        float m_Exponent = 2.0f;

        [SerializeField]
        [Tooltip("ロール軸で入力が±1になるとみなす傾き角度(度)")]
        float m_RollFullScaleDegrees = 45f;

        [SerializeField]
        [Tooltip("ピッチ軸で入力が±1になるとみなす傾き角度(度)")]
        float m_PitchFullScaleDegrees = 45f;

        [SerializeField]
        [Tooltip("ピッチの通常域の上限(0〜1)。これを超えると緩衝ゾーンに入る")]
        float m_PitchNormalMax = 0.75f;

        [SerializeField]
        [Tooltip("ピッチのブレーキ域の下限(m_PitchNormalMaxより大きい0〜1)")]
        float m_PitchBrakeMin = 0.92f;

        [SerializeField]
        [Tooltip("デバッグ用に現在の入力状態を表示するテキスト(任意)")]
        TextMesh m_DebugText;

        TiltInputProcessor m_Processor;

        /// <summary>直近のUpdateで算出した入力状態。</summary>
        public TiltInputState Current { get; private set; }

        /// <summary>
        /// 加速度センサーの対応状況を確認し、TiltInputProcessorを構築する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            if (!SystemInfo.supportsAccelerometer)
            {
                Debug.LogError("[BeamProTiltController] この端末は加速度センサーに対応していません。傾き入力を無効化します。");
                enabled = false;
                return;
            }

            var config = new TiltInputConfig(
                deadzone: m_Deadzone,
                exponent: m_Exponent,
                rollFullScaleDegrees: m_RollFullScaleDegrees,
                pitchFullScaleDegrees: m_PitchFullScaleDegrees,
                pitchNormalMax: m_PitchNormalMax,
                pitchBrakeMin: m_PitchBrakeMin);

            m_Processor = new TiltInputProcessor(config);
        }

        /// <summary>
        /// 起動時の姿勢をニュートラル位置としてキャリブレーションする。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Start()
        {
            Calibrate();
        }

        /// <summary>
        /// 現在の傾きをニュートラル位置として記録し直す。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        public void Calibrate()
        {
            if (m_Processor == null)
                return;

            var (roll, pitch) = ReadRawAngles();
            m_Processor.Calibrate(roll, pitch);
            Debug.Log($"[BeamProTiltController] キャリブレーション完了: roll={roll:F1}deg, pitch={pitch:F1}deg");
        }

        /// <summary>
        /// 毎フレーム、現在の傾きから入力状態を更新する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_Processor == null)
                return;

            var (roll, pitch) = ReadRawAngles();
            Current = m_Processor.Process(roll, pitch);

            if (m_DebugText != null)
                m_DebugText.text = $"Roll: {Current.Roll:F2}\nPitch: {Current.Pitch:F2}\nZone: {Current.Zone}";
        }

        /// <summary>
        /// 加速度センサー(重力方向)からロール・ピッチ角(度)を算出する。
        /// 引数: なし
        /// 返り値: (ロール角, ピッチ角) のタプル(度)。
        /// ピッチは実機確認により前後が逆だったため符号を反転済み(正=機首を上げる方向)。
        /// ロールの符号・軸の対応は未確認
        /// </summary>
        (float roll, float pitch) ReadRawAngles()
        {
            var accel = UnityEngine.Input.acceleration;
            var roll = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(accel.y, Mathf.Sqrt(accel.x * accel.x + accel.z * accel.z)) * Mathf.Rad2Deg;
            return (roll, pitch);
        }
    }
}
