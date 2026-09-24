using MagicalGirl.Core.Flight;
using UnityEngine;

namespace MagicalGirl.Controls
{
    /// <summary>
    /// 傾き入力(BeamProTiltController)を使って箒(このGameObjectのTransform)を飛行させる
    /// コンポーネント。実際の数値計算はUnity非依存のMagicalGirl.Core.Flight.FlightModelに委譲する。
    /// 移動はCharacterController.Move()経由で行い、建物や境界壁のColliderとの衝突を検出・
    /// ブロックする(Transform.positionを直接書き換えるとColliderをすり抜けてしまうため)。
    ///
    /// 頭部トラッキングによるカメラの視線制御とは独立している。カメラはこのTransformの子として
    /// 配置し、TrackedPoseDriver(3DoF)でカメラ自身のローカル回転を駆動する想定。
    /// このコンポーネントは箒本体(機体)の位置・旋回・姿勢だけを扱う。
    /// </summary>
    [RequireComponent(typeof(BeamProTiltController))]
    [RequireComponent(typeof(CharacterController))]
    public class BroomFlightController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("ニュートラル時の基準前進速度(単位/秒)")]
        float m_BaseSpeed = 10f;

        [SerializeField]
        [Tooltip("前進速度の下限")]
        float m_MinSpeed = 2f;

        [SerializeField]
        [Tooltip("前進速度の上限")]
        float m_MaxSpeed = 20f;

        [SerializeField]
        [Tooltip("ピッチ入力1あたりの速度変化量。機首下げで加速、機首上げで減速する")]
        float m_PitchSpeedSensitivity = 8f;

        [SerializeField]
        [Tooltip("ロール入力1あたりの最大旋回速度(度/秒)")]
        float m_MaxYawRateDegPerSecond = 90f;

        [SerializeField]
        [Tooltip("ロール入力1あたりの最大バンク角(度)")]
        float m_MaxBankAngleDeg = 30f;

        [SerializeField]
        [Tooltip("ピッチ入力1あたりの最大機首上げ/下げ角(度)。上昇/降下の軌道もこの角度で決まる")]
        float m_MaxPitchAngleDeg = 25f;

        [SerializeField]
        [Tooltip("通常時、速度が目標値へ追従する加速度(単位/秒^2)")]
        float m_SpeedAcceleration = 15f;

        [SerializeField]
        [Tooltip("バンク角・機首上げ下げ角が目標値へ追従する角速度(度/秒)")]
        float m_AttitudeRateDegPerSecond = 120f;

        [SerializeField]
        [Tooltip("ブレーキ(失速)中、速度が目標値へ追従する加速度(単位/秒^2)")]
        float m_BrakeDeceleration = 40f;

        [SerializeField]
        [Tooltip("ブレーキ(失速)中の目標速度")]
        float m_StallSpeed = 1f;

        BeamProTiltController m_TiltController;
        CharacterController m_CharacterController;
        FlightModel m_Model;
        float m_YawDeg;

        /// <summary>直近のUpdateで算出した飛行出力。</summary>
        public FlightOutput Current { get; private set; }

        /// <summary>
        /// TiltController・CharacterControllerの取得とFlightModelの構築を行う。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            m_TiltController = GetComponent<BeamProTiltController>();

            m_CharacterController = GetComponent<CharacterController>();
            // 飛行中は「立っている」概念がないため、コライダーの中心を原点に置く
            // (CharacterControllerのデフォルトは接地キャラクター向けにY+1オフセットされている)。
            m_CharacterController.center = Vector3.zero;

            var config = new FlightConfig(
                baseSpeed: m_BaseSpeed,
                minSpeed: m_MinSpeed,
                maxSpeed: m_MaxSpeed,
                pitchSpeedSensitivity: m_PitchSpeedSensitivity,
                maxYawRateDegPerSecond: m_MaxYawRateDegPerSecond,
                maxBankAngleDeg: m_MaxBankAngleDeg,
                maxPitchAngleDeg: m_MaxPitchAngleDeg,
                speedAcceleration: m_SpeedAcceleration,
                attitudeRateDegPerSecond: m_AttitudeRateDegPerSecond,
                brakeDeceleration: m_BrakeDeceleration,
                stallSpeed: m_StallSpeed);

            m_Model = new FlightModel(config);
            m_YawDeg = transform.eulerAngles.y;
        }

        /// <summary>
        /// 毎フレーム、現在の傾き入力から飛行状態を更新し、Transformに反映する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_TiltController == null || m_Model == null)
                return;

            var output = m_Model.Step(m_TiltController.Current, m_TiltController.BrakeOnNegativePitch, Time.deltaTime);
            Current = output;

            // ヨーは蓄積、ピッチ・バンクは毎フレーム目標角度をそのまま姿勢として適用する
            // (蓄積するとオイラー角の順序次第でジンバルロック的な破綻が起きるため)。
            m_YawDeg += output.YawRateDegPerSecond * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(-output.PitchAngleDeg, m_YawDeg, -output.BankAngleDeg);

            // Transform.positionを直接書き換えるとColliderをすり抜けるため、
            // CharacterController.Move()で移動し、建物・境界壁との衝突を検出させる。
            m_CharacterController.Move(transform.forward * (output.Speed * Time.deltaTime));
        }
    }
}
