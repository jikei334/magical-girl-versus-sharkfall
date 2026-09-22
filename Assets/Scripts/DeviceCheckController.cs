using System;
using Unity.XR.XREAL;
using UnityEngine;

namespace MagicalGirl
{
    /// <summary>
    /// XREALグラスとSDKが正しく通信できているかを確認するための動作確認用コンポーネント。
    /// 頭部トラッキングを3DoFに切り替え、検出したデバイス種別とトラッキングモードを
    /// 3Dテキストに表示する。
    /// </summary>
    public class DeviceCheckController : MonoBehaviour
    {
        [SerializeField]
        TextMesh m_Text;

        /// <summary>
        /// 起動時にトラッキングモードを3DoFへ切り替え、結果を画面に表示する。
        /// 引数: なし
        /// 返り値: なし(async void。Unityのライフサイクルメソッドのため)
        /// </summary>
        async void Start()
        {
            if (m_Text == null)
            {
                Debug.LogError("[DeviceCheck] m_Text が設定されていません。");
                return;
            }

            string error = null;
            try
            {
                // Air 2 Pro単体は3DoFのみ対応。位置トラッキングは使わない設計。
                await XREALPlugin.SwitchTrackingTypeAsync(TrackingType.MODE_3DOF);
            }
            catch (Exception e)
            {
                // Unityエディタ上など、実機以外では失敗しうる。握りつぶさず記録して画面にも出す。
                Debug.LogException(e);
                error = e.GetType().Name;
            }

            ShowStatus(error);
        }

        /// <summary>
        /// デバイス種別とトラッキングモードをテキストに反映する。
        /// 引数: error - トラッキング切り替え時に発生した例外名。成功時はnull
        /// 返り値: なし
        /// </summary>
        void ShowStatus(string error)
        {
            var device = XREALPlugin.GetDeviceType();
            var mode = XREALPlugin.GetTrackingType();
            var status = $"Magical Girl versus Sharkfall\nDevice: {device}\nTracking: {mode}";
            if (error != null)
                status += $"\nError: {error}";

            m_Text.text = status;
            Debug.Log($"[DeviceCheck] Device={device}, Tracking={mode}, Error={error ?? "none"}");
        }
    }
}
