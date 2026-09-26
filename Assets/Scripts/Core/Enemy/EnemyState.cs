using System;

namespace MagicalGirl.Core.Enemy
{
    /// <summary>
    /// 敵1体分の被弾・撃破状態を管理するUnity非依存のロジック。
    /// 火球の直撃・雷撃のチェインなど、複数の攻撃手段から独立して呼び出せるよう
    /// TakeDamage()をシンプルな1メソッドにしている。
    /// </summary>
    public sealed class EnemyState
    {
        // 浮動小数点の減算を繰り返すと、本来ちょうど0になるはずのHPが丸め誤差で
        // ごく小さい正の値(例: 0.0000003)として残ることがある。CurrentHealthは
        // Math.Maxで負にはならないようクランプ済みだが、逆に「本来0のはずが0にならない」
        // ケースを救うため、この閾値以下は撃破済みとみなす。
        const float DefeatEpsilon = 0.0001f;

        /// <summary>この敵のステータス設定。</summary>
        public EnemyConfig Config { get; }

        /// <summary>現在のHP(0以上MaxHealth以下)。</summary>
        public float CurrentHealth { get; private set; }

        /// <summary>HPが0(またはDefeatEpsilon以下)になり撃破済みかどうか。</summary>
        public bool IsDefeated => CurrentHealth <= DefeatEpsilon;

        /// <summary>
        /// HPをMaxHealthに設定して構築する。
        /// 引数: config - この敵のステータス設定
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public EnemyState(EnemyConfig config)
        {
            Config = config;
            CurrentHealth = config.MaxHealth;
        }

        /// <summary>
        /// ダメージを与える。撃破済みの場合は何もしない(多重撃破の二重処理を防ぐ)。
        /// 引数: amount - ダメージ量(0以上である必要がある)
        /// 返り値: なし
        /// 例外: amountが負の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "amountは0以上である必要があります。");

            if (IsDefeated)
                return;

            CurrentHealth = Math.Max(0f, CurrentHealth - amount);
        }
    }
}
