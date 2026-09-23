using MagicalGirl.Core.Input;
using UnityEngine;

namespace MagicalGirl.Controls
{
    /// <summary>
    /// Beam Pro本体の物理的な傾きを加速度センサーから取得し、ロール・ピッチの
    /// 制御入力に変換するコンポーネント。ブレーキはピッチを引き切ったときに発動する
    /// (Beamの長辺軸を回転軸にして画面が地面に垂直に近づく方向)。どちら向き
    /// (機首上げ/機首下げ)で発動するかはm_BrakeOnNegativePitchで切り替えられ、
    /// SetBrakeOnNegativePitch()経由でランタイムにも変更できる(設定画面からの想定。
    /// 実際の設定UIはまだ無く、当面はInspectorとPlayerPrefsでの切り替えのみ)。
    /// 実際の数値計算(キャリブレーション・デッドゾーン・非線形カーブ・ブレーキ判定)は
    /// Unity非依存のMagicalGirl.Core.Input.TiltInputProcessorに委譲する。
    ///
    /// 注意: 加速度センサーからのロール/ピッチ算出式(ReadRawAngles)は、両手で横持ち
    /// (ランドスケープ)する持ち方を前提に組んでいるが、実機での軸マッピングの検証が
    /// まだ途中。特にRollは長辺・短辺どちらの傾きにも反応してしまう(軸が分離できていない)
    /// 疑いがあり、修正が必要。デバッグ表示のAccel行(生の加速度値)を見ながら、
    /// 意図した方向でRoll/Pitchが反応するか確認し、必要なら式を調整すること。
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
        [Tooltip("ブレーキ判定(ピッチ)の通常域の上限(0〜1)。これを超えると緩衝ゾーンに入る")]
        float m_BrakeNormalMax = 0.75f;

        [SerializeField]
        [Tooltip("ブレーキ判定(ピッチ)のブレーキ域の下限(m_BrakeNormalMaxより大きい0〜1)")]
        float m_BrakeMin = 0.92f;

        [SerializeField]
        [Tooltip("trueなら機首を下げる方向、falseなら機首を上げる方向でブレーキを発動する。" +
                 "設定画面などからSetBrakeOnNegativePitch()で変更した値がPlayerPrefsに保存され、" +
                 "この初期値より優先される")]
        bool m_BrakeOnNegativePitch = true;

        [SerializeField]
        [Tooltip("デバッグ用に現在の入力状態を表示するテキスト(任意)")]
        TextMesh m_DebugText;

        const string k_BrakeOnNegativePitchPrefsKey = "MagicalGirl.BrakeOnNegativePitch";

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

            // PlayerPrefsに保存済みの値があれば、Inspectorの初期値より優先する
            // (設定画面などでプレイヤーが変更した内容を次回起動時にも反映するため)。
            if (PlayerPrefs.HasKey(k_BrakeOnNegativePitchPrefsKey))
                m_BrakeOnNegativePitch = PlayerPrefs.GetInt(k_BrakeOnNegativePitchPrefsKey) != 0;

            var config = BuildConfig();
            m_Processor = new TiltInputProcessor(config);
        }

        /// <summary>
        /// Inspectorのフィールドから動作パラメータを組み立てる。
        /// 引数: なし
        /// 返り値: 現在のフィールド値を反映したTiltInputConfig
        /// </summary>
        TiltInputConfig BuildConfig()
        {
            return new TiltInputConfig(
                deadzone: m_Deadzone,
                exponent: m_Exponent,
                rollFullScaleDegrees: m_RollFullScaleDegrees,
                pitchFullScaleDegrees: m_PitchFullScaleDegrees,
                brakeNormalMax: m_BrakeNormalMax,
                brakeMin: m_BrakeMin,
                brakeOnNegativePitch: m_BrakeOnNegativePitch);
        }

        /// <summary>
        /// ブレーキを発動させるピッチの向きを切り替える。設定画面などUIから呼び出す想定。
        /// PlayerPrefsに保存し、次回起動時にも反映される。キャリブレーション状態は維持される。
        /// 引数: brakeOnNegativePitch - trueなら機首を下げる方向、falseなら機首を上げる方向でブレーキを発動する
        /// 返り値: なし
        /// </summary>
        public void SetBrakeOnNegativePitch(bool brakeOnNegativePitch)
        {
            m_BrakeOnNegativePitch = brakeOnNegativePitch;
            PlayerPrefs.SetInt(k_BrakeOnNegativePitchPrefsKey, brakeOnNegativePitch ? 1 : 0);
            PlayerPrefs.Save();

            m_Processor?.UpdateConfig(BuildConfig());
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
            {
                // Accel行は軸マッピングの調査用。実機での調整が済んだら削除してよい。
                var accel = UnityEngine.Input.acceleration;
                m_DebugText.text =
                    $"Accel: ({accel.x:F2}, {accel.y:F2}, {accel.z:F2})\n" +
                    $"Roll: {Current.Roll:F2}\nPitch: {Current.Pitch:F2}\nZone: {Current.Zone}";
            }
        }

        /// <summary>
        /// 加速度センサー(重力方向)からロール・ピッチ角(度)を算出する。
        /// 両手で横持ち(ランドスケープ)する持ち方を前提にした式で、まだ実機での
        /// 軸マッピング検証が途中(詳細はクラス冒頭のコメント参照)。
        /// 引数: なし
        /// 返り値: (ロール角, ピッチ角) のタプル(度)
        /// </summary>
        (float roll, float pitch) ReadRawAngles()
        {
            var accel = UnityEngine.Input.acceleration;
            var roll = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;
            var pitch = Mathf.Atan2(accel.y, Mathf.Sqrt(accel.x * accel.x + accel.z * accel.z)) * Mathf.Rad2Deg;
            return (roll, pitch);
        }
    }
}
