using System;
using MagicalGirl.Core.Enemy;
using UnityEngine;

namespace MagicalGirl.Enemy
{
    /// <summary>
    /// スクール(小型・群れ)の編隊を生成するコンポーネント。Unity非依存のSchoolFormationで
    /// メンバーの相対オフセット(進行方向基準)を求め、ワールド座標へ変換してプレハブを配置する。
    ///
    /// 生成自体はStart()で実行時に行う(このGameObjectの位置を編隊の基準位置として使う)。
    /// EnemyController.Configure()/SetDirection()が設定する値は[SerializeField]でない
    /// プライベートフィールドのため、エディタでシーン保存時に生成してしまうと実機でシーンを
    /// ロードし直したときに失われてしまう(Awake()の初期値に戻る)。必ず実行時に生成すること。
    /// </summary>
    public class SchoolSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("敵のプレハブ(EnemyControllerを持つこと)")]
        EnemyController m_EnemyPrefab;

        [SerializeField]
        FormationPattern m_Pattern = FormationPattern.V;

        [SerializeField]
        [Tooltip("編隊のメンバー数")]
        int m_MemberCount = 5;

        [SerializeField]
        [Tooltip("メンバー間の間隔")]
        float m_Spacing = 6f;

        [SerializeField]
        [Tooltip("敵のHP(ウェーブ倍率適用後の最終値を想定)")]
        float m_EnemyMaxHealth = 20f;

        [SerializeField]
        [Tooltip("敵の直進速度(単位/秒、ウェーブ倍率適用後の最終値を想定)")]
        float m_EnemySpeed = 8f;

        [SerializeField]
        [Tooltip("編隊の進行方向(正規化不要)")]
        Vector3 m_SpawnDirection = Vector3.back;

        /// <summary>
        /// シーン開始時に、このGameObjectの位置を基準として編隊を生成する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        void Start()
        {
            Spawn(transform.position, m_SpawnDirection);
        }

        /// <summary>
        /// 指定した位置・進行方向に編隊を生成する。
        /// 引数: origin - 編隊の基準位置(リーダーの初期位置) / direction - 進行方向(正規化不要)
        /// 返り値: 生成したEnemyControllerの配列(m_EnemyPrefab未設定の場合は空配列)
        /// </summary>
        public EnemyController[] Spawn(Vector3 origin, Vector3 direction)
        {
            if (m_EnemyPrefab == null)
            {
                Debug.LogError("[SchoolSpawner] m_EnemyPrefabが設定されていません。編隊を生成できません。");
                return Array.Empty<EnemyController>();
            }

            var forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right; // 進行方向が真上/真下向きのときのフォールバック

            var formationConfig = new SchoolFormationConfig(m_Pattern, m_MemberCount, m_Spacing);
            var offsets = SchoolFormation.GenerateOffsets(formationConfig);

            var members = new EnemyController[offsets.Length];
            var rotation = Quaternion.LookRotation(forward, Vector3.up);

            for (var i = 0; i < offsets.Length; i++)
            {
                var worldOffset = right * offsets[i].Right + forward * offsets[i].Forward;
                var enemy = Instantiate(m_EnemyPrefab, origin + worldOffset, rotation, transform);
                enemy.Configure(m_EnemyMaxHealth, m_EnemySpeed);
                enemy.SetDirection(forward);
                members[i] = enemy;
            }

            return members;
        }
    }
}
