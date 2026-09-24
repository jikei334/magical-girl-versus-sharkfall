using UnityEngine;

namespace MagicalGirl.Controls
{
    /// <summary>
    /// BuildInfo.Versionを画面上に表示するだけの簡易HUD。実機で古いAPKのまま
    /// 動作確認していないかを見分けるための目印。
    /// </summary>
    public class BuildVersionHud : MonoBehaviour
    {
        [SerializeField]
        TextMesh m_Text;

        /// <summary>
        /// ビルド番号をテキストに反映する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Start()
        {
            if (m_Text != null)
                m_Text.text = $"Build v{BuildInfo.Version}";
        }
    }
}
