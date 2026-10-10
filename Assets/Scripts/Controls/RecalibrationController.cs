using MagicalGirl.Gesture;
using UnityEngine;

namespace MagicalGirl.Controls
{
    /// <summary>
    /// タッチパッドの長押し(GestureInputController.LongPressDetected)を検知すると、
    /// その場の姿勢をニュートラル位置としてBeamProTiltController.Calibrate()を呼び直す
    /// (再キャリブレーション)。起動時にコントローラーを傾けたまま起動してしまい、
    /// ニュートラル位置がずれる問題への対策。
    ///
    /// 当初はBeam Proの物理ボタンを使う案だったが、実機でどのボタンを押しても
    /// Unity側にキー入力として届かないことが確認されたため、既に動作確認済みの
    /// タッチパッド入力(長押し)を使う方式に変更した。
    /// </summary>
    [RequireComponent(typeof(BeamProTiltController))]
    public class RecalibrationController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("長押しの検知元")]
        GestureInputController m_GestureInput;

        [SerializeField]
        [Tooltip("再キャリブレーション完了を知らせるデバッグ表示(任意)")]
        TextMesh m_DebugText;

        BeamProTiltController m_TiltController;

        /// <summary>
        /// 同じGameObjectのBeamProTiltControllerを取得する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            m_TiltController = GetComponent<BeamProTiltController>();
        }

        /// <summary>
        /// GestureInputControllerのLongPressDetectedイベントを購読する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnEnable()
        {
            if (m_GestureInput != null)
                m_GestureInput.LongPressDetected += HandleLongPressDetected;
        }

        /// <summary>
        /// イベント購読を解除する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnDisable()
        {
            if (m_GestureInput != null)
                m_GestureInput.LongPressDetected -= HandleLongPressDetected;
        }

        /// <summary>
        /// 長押し検知時に再キャリブレーションを実行する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void HandleLongPressDetected()
        {
            m_TiltController.Calibrate();

            if (m_DebugText != null)
                m_DebugText.text = "Calibrated!";
        }
    }
}
