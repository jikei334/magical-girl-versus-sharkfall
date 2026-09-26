using UnityEngine;

namespace MagicalGirl.Combat
{
    /// <summary>
    /// SpellCasterのイベントを購読し、直近に発動した攻撃をテキストに表示する、
    /// 実機確認用のデバッグHUD。
    /// </summary>
    public class CombatDebugHud : MonoBehaviour
    {
        [SerializeField]
        SpellCaster m_SpellCaster;

        [SerializeField]
        TextMesh m_Text;

        /// <summary>
        /// SpellCasterのイベントを購読する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnEnable()
        {
            if (m_SpellCaster == null)
                return;

            m_SpellCaster.FireballCast += HandleFireballCast;
            m_SpellCaster.BarrierCast += HandleBarrierCast;
        }

        /// <summary>
        /// SpellCasterのイベント購読を解除する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnDisable()
        {
            if (m_SpellCaster == null)
                return;

            m_SpellCaster.FireballCast -= HandleFireballCast;
            m_SpellCaster.BarrierCast -= HandleBarrierCast;
        }

        /// <summary>火球発射時に表示を更新する。引数: なし / 返り値: なし</summary>
        void HandleFireballCast()
        {
            if (m_Text != null)
                m_Text.text = "Attack: Fireball";
        }

        /// <summary>バリア展開時に表示を更新する。引数: なし / 返り値: なし</summary>
        void HandleBarrierCast()
        {
            if (m_Text != null)
                m_Text.text = "Attack: Barrier";
        }
    }
}
