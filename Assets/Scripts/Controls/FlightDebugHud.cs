using UnityEngine;

namespace MagicalGirl.Controls
{
    /// <summary>
    /// BroomFlightControllerの現在の出力(速度・旋回速度・機首角・バンク角)を
    /// テキストに表示する、実機確認用のデバッグHUD。
    /// </summary>
    public class FlightDebugHud : MonoBehaviour
    {
        [SerializeField]
        BroomFlightController m_FlightController;

        [SerializeField]
        TextMesh m_Text;

        /// <summary>
        /// 毎フレーム、飛行コントローラーの現在値をテキストに反映する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_FlightController == null || m_Text == null)
                return;

            var output = m_FlightController.Current;
            m_Text.text =
                $"Speed: {output.Speed:F1}\n" +
                $"YawRate: {output.YawRateDegPerSecond:F1}\n" +
                $"PitchAngle: {output.PitchAngleDeg:F1}\n" +
                $"BankAngle: {output.BankAngleDeg:F1}";
        }
    }
}
