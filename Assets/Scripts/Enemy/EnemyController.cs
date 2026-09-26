using System;
using MagicalGirl.Core.Enemy;
using UnityEngine;

namespace MagicalGirl.Enemy
{
    /// <summary>
    /// 敵1体分のUnity側コンポーネント。Unity非依存のEnemyStateを保持し、直進移動・被弾・
    /// 撃破処理を行う。実際の攻撃手段(火球・雷撃)はIssue #9で実装され、TakeDamage()を
    /// 呼び出す形で連携する想定。
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("最大HP(ウェーブ倍率適用前のデフォルト値。SpawnerからConfigure()で上書きされる)")]
        float m_MaxHealth = 20f;

        [SerializeField]
        [Tooltip("直進速度(単位/秒。デフォルト値。SpawnerからConfigure()で上書きされる)")]
        float m_Speed = 8f;

        EnemyState m_State;
        Vector3 m_Direction = Vector3.forward;

        /// <summary>撃破されたときに発火するイベント(スコア加算・雑魚召喚カウント等で使う想定)。</summary>
        public event Action<EnemyController> Defeated;

        /// <summary>現在のHP。EnemyState未初期化の場合は0。</summary>
        public float CurrentHealth => m_State?.CurrentHealth ?? 0f;

        /// <summary>撃破済みかどうか。EnemyState未初期化の場合はtrue扱い。</summary>
        public bool IsDefeated => m_State?.IsDefeated ?? true;

        /// <summary>
        /// Inspectorのデフォルト値でEnemyStateを構築する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Awake()
        {
            Configure(m_MaxHealth, m_Speed);
        }

        /// <summary>
        /// ステータスを設定(または上書き)する。Spawnerがウェーブ倍率適用後の値で
        /// 呼び出すことを想定している。HPは呼び出し時点でMaxHealthにリセットされる。
        /// 引数: maxHealth, speed - 適用するステータス
        /// 返り値: なし
        /// </summary>
        public void Configure(float maxHealth, float speed)
        {
            m_State = new EnemyState(new EnemyConfig(maxHealth, speed));
        }

        /// <summary>
        /// 進行方向を設定する(編隊出現時にSpawnerから呼ばれる)。
        /// 引数: direction - 進行方向ベクトル(正規化されていなくてもよい)
        /// 返り値: なし
        /// </summary>
        public void SetDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                m_Direction = direction.normalized;
        }

        /// <summary>
        /// 毎フレーム、設定された方向へ直進する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Update()
        {
            if (m_State == null || m_State.IsDefeated)
                return;

            transform.position += m_Direction * (m_State.Config.Speed * Time.deltaTime);
        }

        /// <summary>
        /// ダメージを与える。この呼び出しで撃破に至った場合、Defeatedイベントを発火して
        /// このGameObjectを破棄する。
        /// 引数: amount - ダメージ量(0以上である必要がある)
        /// 返り値: なし
        /// 例外: EnemyStateが未初期化(Awake未実行)の場合、InvalidOperationExceptionを投げる
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (m_State == null)
                throw new InvalidOperationException("EnemyStateが初期化されていません(Awake前にTakeDamageが呼ばれました)。");

            var wasDefeated = m_State.IsDefeated;
            m_State.TakeDamage(amount);

            if (!wasDefeated && m_State.IsDefeated)
            {
                Defeated?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
