using MagicalGirl.Core.Combat;
using MagicalGirl.Enemy;
using UnityEngine;

namespace MagicalGirl.Combat
{
    /// <summary>
    /// バリアのUnity側コンポーネント。展開した瞬間に周囲の敵へ範囲ダメージ(バースト)を
    /// 与えつつ、Unity非依存のBarrierStateが管理する時間だけ半透明の球体を表示する。
    /// 常にプレイヤー(このGameObjectの親であるBroomRig)に追従する想定で、あらかじめ
    /// シーンに1つだけ配置しておき、Circleジェスチャー成功のたびにActivate()を呼び出す。
    /// </summary>
    public class Barrier : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("バリア球体に使うマテリアル(MagicalGirl/DoubleSidedGlowシェーダー、両面描画で" +
            "プレイヤーが内側にいても見える)。実行時にShader.Find()で作ると、ビルドに" +
            "シェーダーが含まれない/未保存のマテリアル参照が失われる問題があるため、" +
            "アセットとして保存済みのものを割り当てる")]
        Material m_VisualMaterial;

        [SerializeField]
        [Tooltip("展開状態が持続する時間(秒)")]
        float m_Duration = 2f;

        [SerializeField]
        [Tooltip("展開した瞬間に範囲内の敵へ与えるダメージ量")]
        float m_BurstDamage = 15f;

        [SerializeField]
        [Tooltip("範囲攻撃が届く半径(単位)")]
        float m_BurstRadius = 15f;

        BarrierState m_State;
        Transform m_Visual;

        /// <summary>
        /// BarrierStateの構築と、半透明の球体ビジュアル(初期は非表示)の生成を行う。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            m_State = new BarrierState(new BarrierConfig(m_Duration, m_BurstDamage, m_BurstRadius));
            m_Visual = BuildVisual();
            m_Visual.gameObject.SetActive(false);
        }

        /// <summary>
        /// バリアを展開する。範囲内の敵へ即座にダメージを与え、Duration秒だけビジュアルを表示する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        public void Activate()
        {
            m_State.Activate();
            m_Visual.gameObject.SetActive(true);

            foreach (var hit in Physics.OverlapSphere(transform.position, m_BurstRadius))
            {
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                    enemy.TakeDamage(m_BurstDamage);
            }
        }

        /// <summary>
        /// 毎フレーム展開状態を更新し、時間切れになったらビジュアルを隠す。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_State == null || !m_State.IsActive)
                return;

            m_State.Tick(Time.deltaTime);
            if (!m_State.IsActive)
                m_Visual.gameObject.SetActive(false);
        }

        /// <summary>
        /// 半透明の球体ビジュアルを子として生成する(コライダーは不要なので取り除く)。
        /// 引数: なし
        /// 返り値: 生成した球体のTransform
        /// </summary>
        Transform BuildVisual()
        {
            var visualGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualGo.name = "BarrierVisual";
            visualGo.transform.SetParent(transform, false);
            visualGo.transform.localScale = Vector3.one * (m_BurstRadius * 2f);
            Destroy(visualGo.GetComponent<Collider>());

            if (m_VisualMaterial != null)
                visualGo.GetComponent<Renderer>().sharedMaterial = m_VisualMaterial;

            return visualGo.transform;
        }
    }
}
