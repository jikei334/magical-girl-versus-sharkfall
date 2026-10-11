using System.Linq;
using MagicalGirl.Controls;
using MagicalGirl.Core.Combat;
using MagicalGirl.Enemy;
using UnityEngine;

namespace MagicalGirl.Combat
{
    /// <summary>
    /// 火球(直進弾)1発分のUnity側コンポーネント。Unity非依存のFireballStateで
    /// 飛翔距離・最大射程を管理しつつ、実際の移動はTransformで、命中判定はTrigger
    /// コライダーで行う。見た目はfireball.glb(Spawn/Fly/Impactのアニメーションクリップ付き)で、
    /// Animatorがあれば発射時にSpawn→Fly(ループ)、消滅時にImpactを再生する
    /// (Animatorが無い場合、たとえばエディタで直接テストする場合などは何も再生せず移動のみ行う)。
    ///
    /// 生成直後はプレイヤー自身のCharacterControllerと重なった状態で発射されるため、
    /// BroomFlightControllerを持つ相手との衝突は無視する(自爆防止)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Fireball : MonoBehaviour
    {
        const string k_SpawnStateName = "Spawn";
        const string k_FlyStateName = "Fly";
        const string k_ImpactStateName = "Impact";

        FireballState m_State;
        Animator m_Animator;
        float m_SpawnElapsed;
        bool m_SwitchedToFly;
        bool m_IsImpacting;

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
        /// Trigger・Rigidbody(判定用、キネマティック)の設定と、Spawnアニメーションの再生開始を行う。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            var rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            GetComponent<SphereCollider>().isTrigger = true;

            m_Animator = GetComponent<Animator>();
            m_Animator?.Play(k_SpawnStateName);
        }

        /// <summary>
        /// 毎フレーム直進させ、最大射程に達したら着弾演出のうえ消滅させる。
        /// Spawnアニメーションの再生時間が経過したらFly(ループ)へ切り替える。
        /// 着弾演出中は移動・判定を止める。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_State == null || m_IsImpacting)
                return;

            if (m_Animator != null && !m_SwitchedToFly)
            {
                m_SpawnElapsed += Time.deltaTime;
                if (m_SpawnElapsed >= GetClipLength(k_SpawnStateName))
                {
                    m_Animator.Play(k_FlyStateName);
                    m_SwitchedToFly = true;
                }
            }

            transform.position += transform.forward * (m_State.Config.Speed * Time.deltaTime);
            m_State.Advance(Time.deltaTime);

            if (m_State.IsExpired)
                Impact();
        }

        /// <summary>
        /// 何かに接触したときの命中判定。敵ならダメージを与えて着弾演出のうえ消滅、
        /// プレイヤー自身なら無視、それ以外(建物・地面・境界壁など)にぶつかった場合も
        /// そこで着弾演出のうえ消滅させる。
        /// 引数: other - 接触した相手のCollider
        /// 返り値: なし
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (m_State == null || m_IsImpacting)
                return;

            if (other.GetComponent<BroomFlightController>() != null)
                return; // 発射直後の自爆防止

            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.TakeDamage(m_State.Config.Damage);

            Impact();
        }

        /// <summary>
        /// 着弾演出(Impactアニメーション)を再生し、再生時間分だけ待って消滅させる。
        /// Animatorが無い場合は即座に消滅させる。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Impact()
        {
            m_IsImpacting = true;

            if (m_Animator == null)
            {
                Destroy(gameObject);
                return;
            }

            m_Animator.Play(k_ImpactStateName);
            Destroy(gameObject, GetClipLength(k_ImpactStateName));
        }

        /// <summary>
        /// AnimatorControllerに登録されたクリップから、指定した名前のクリップの再生時間(秒)を取得する。
        /// 見つからない場合は0を返す(Animator未設定時や想定外のコントローラー構成への対処)。
        /// 引数: clipName - クリップ名
        /// 返り値: クリップの再生時間(秒)。見つからない場合は0
        /// </summary>
        float GetClipLength(string clipName)
        {
            if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
                return 0f;

            return m_Animator.runtimeAnimatorController.animationClips
                .FirstOrDefault(clip => clip.name == clipName)?.length ?? 0f;
        }
    }
}
