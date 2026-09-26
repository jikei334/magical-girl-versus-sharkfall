using MagicalGirl.Controls;
using MagicalGirl.Core.Combat;
using MagicalGirl.Enemy;
using UnityEngine;

namespace MagicalGirl.Combat
{
    /// <summary>
    /// 火球(直進弾)1発分のUnity側コンポーネント。Unity非依存のFireballStateで
    /// 飛翔距離・最大射程を管理しつつ、実際の移動はTransformで、命中判定はTrigger
    /// コライダーで行う。
    ///
    /// 生成直後はプレイヤー自身のCharacterControllerと重なった状態で発射されるため、
    /// BroomFlightControllerを持つ相手との衝突は無視する(自爆防止)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Fireball : MonoBehaviour
    {
        FireballState m_State;

        /// <summary>
        /// ダメージ・速度・最大射程を設定して初期化する。SpellCasterが生成直後に呼び出す。
        /// 引数: config - この火球のパラメータ設定
        /// 返り値: なし
        /// </summary>
        public void Initialize(FireballConfig config)
        {
            m_State = new FireballState(config);
        }

        /// <summary>
        /// TriggerとRigidbody(判定用、キネマティック)の設定を行う。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            var rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            GetComponent<SphereCollider>().isTrigger = true;
        }

        /// <summary>
        /// 毎フレーム直進させ、最大射程に達したら消滅させる。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_State == null)
                return;

            transform.position += transform.forward * (m_State.Config.Speed * Time.deltaTime);
            m_State.Advance(Time.deltaTime);

            if (m_State.IsExpired)
                Destroy(gameObject);
        }

        /// <summary>
        /// 何かに接触したときの命中判定。敵ならダメージを与えて消滅、プレイヤー自身なら無視、
        /// それ以外(建物・地面・境界壁など)にぶつかった場合もそこで消滅させる。
        /// 引数: other - 接触した相手のCollider
        /// 返り値: なし
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (m_State == null)
                return;

            if (other.GetComponent<BroomFlightController>() != null)
                return; // 発射直後の自爆防止

            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.TakeDamage(m_State.Config.Damage);

            Destroy(gameObject);
        }
    }
}
