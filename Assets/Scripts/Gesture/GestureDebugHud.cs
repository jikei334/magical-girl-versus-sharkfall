using MagicalGirl.Core.Gesture;
using UnityEngine;

namespace MagicalGirl.Gesture
{
    /// <summary>
    /// GestureInputControllerのイベントを購読し、直近の認識結果をテキストに表示する、
    /// 実機確認用のデバッグHUD。
    /// </summary>
    public class GestureDebugHud : MonoBehaviour
    {
        [SerializeField]
        GestureInputController m_InputController;

        [SerializeField]
        TextMesh m_Text;

        /// <summary>
        /// GestureInputControllerのイベントを購読する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnEnable()
        {
            if (m_InputController == null)
                return;

            m_InputController.CastingStarted += HandleCastingStarted;
            m_InputController.GestureRecognized += HandleGestureRecognized;
            m_InputController.GestureFailed += HandleGestureFailed;
        }

        /// <summary>
        /// GestureInputControllerのイベント購読を解除する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnDisable()
        {
            if (m_InputController == null)
                return;

            m_InputController.CastingStarted -= HandleCastingStarted;
            m_InputController.GestureRecognized -= HandleGestureRecognized;
            m_InputController.GestureFailed -= HandleGestureFailed;
        }

        /// <summary>
        /// タッチ開始時に「詠唱中」表示に切り替える。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void HandleCastingStarted()
        {
            if (m_Text != null)
                m_Text.text = "Casting...";
        }

        /// <summary>
        /// ジェスチャー認識成功時に、種別とスコアを表示する。
        /// 引数: result - 認識結果
        /// 返り値: なし
        /// </summary>
        void HandleGestureRecognized(GestureRecognitionResult result)
        {
            if (m_Text != null)
                m_Text.text = $"Gesture: {result.Type}\nScore: {result.Score:F2}";
        }

        /// <summary>
        /// ジェスチャー認識失敗時に、失敗した旨を表示する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void HandleGestureFailed()
        {
            if (m_Text != null)
                m_Text.text = "Gesture: Failed";
        }
    }
}
