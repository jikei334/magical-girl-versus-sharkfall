using System;
using MagicalGirl.Core.Combat;
using MagicalGirl.Core.Gesture;
using MagicalGirl.Gesture;
using UnityEngine;

namespace MagicalGirl.Combat
{
    /// <summary>
    /// GestureInputControllerの認識結果を実際の攻撃(火球・バリア)に変換するコンポーネント。
    /// 直線フリック(GestureType.Line)で火球を発射、円(GestureType.Circle)でバリアを展開する。
    /// 照準は頭部トラッキングで動くカメラ(m_AimOrigin)の向き・位置を使う。
    /// </summary>
    public class SpellCaster : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("ジェスチャー入力の取得元")]
        GestureInputController m_GestureInput;

        [SerializeField]
        [Tooltip("照準・発射origin(頭部トラッキングで動くカメラのTransform)")]
        Transform m_AimOrigin;

        [SerializeField]
        [Tooltip("火球のプレハブ(Fireballコンポーネントを持つこと)")]
        Fireball m_FireballPrefab;

        [SerializeField]
        [Tooltip("バリア(あらかじめシーンに1つ配置しておく)")]
        Barrier m_Barrier;

        [SerializeField]
        [Tooltip("火球の発射地点を照準originからどれだけ前方にずらすか(自爆防止)。" +
            "火球本体の半径(プレハブのスケールから決まる)より確実に大きくすること。" +
            "小さいと発射直後カメラが火球の内部に埋まり、描画されず見えなくなる")]
        float m_FireballSpawnOffset = 3f;

        [SerializeField]
        [Tooltip("火球の直進速度(単位/秒)")]
        float m_FireballSpeed = 40f;

        [SerializeField]
        [Tooltip("火球の命中ダメージ")]
        float m_FireballDamage = 10f;

        [SerializeField]
        [Tooltip("火球の最大射程(単位)")]
        float m_FireballMaxRange = 200f;

        /// <summary>火球を発射したときに発火する(デバッグ表示用)。</summary>
        public event Action FireballCast;

        /// <summary>バリアを展開したときに発火する(デバッグ表示用)。</summary>
        public event Action BarrierCast;

        /// <summary>
        /// GestureInputControllerのGestureRecognizedイベントを購読する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnEnable()
        {
            if (m_GestureInput != null)
                m_GestureInput.GestureRecognized += HandleGestureRecognized;
        }

        /// <summary>
        /// イベント購読を解除する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void OnDisable()
        {
            if (m_GestureInput != null)
                m_GestureInput.GestureRecognized -= HandleGestureRecognized;
        }

        /// <summary>
        /// 認識されたジェスチャー種別に応じて攻撃を発動する。
        /// 引数: result - ジェスチャー認識結果
        /// 返り値: なし
        /// </summary>
        void HandleGestureRecognized(GestureRecognitionResult result)
        {
            switch (result.Type)
            {
                case GestureType.Line:
                    CastFireball();
                    break;
                case GestureType.Circle:
                    CastBarrier();
                    break;
            }
        }

        /// <summary>
        /// 照準originの前方へ火球を生成し、速度・ダメージ・射程を設定して発射する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void CastFireball()
        {
            if (m_FireballPrefab == null || m_AimOrigin == null)
                return;

            var spawnPosition = m_AimOrigin.position + m_AimOrigin.forward * m_FireballSpawnOffset;
            var fireball = Instantiate(m_FireballPrefab, spawnPosition, Quaternion.LookRotation(m_AimOrigin.forward));
            fireball.Initialize(new FireballConfig(m_FireballSpeed, m_FireballDamage, m_FireballMaxRange));

            FireballCast?.Invoke();
        }

        /// <summary>
        /// バリアを展開する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void CastBarrier()
        {
            if (m_Barrier == null)
                return;

            m_Barrier.Activate();
            BarrierCast?.Invoke();
        }
    }
}
