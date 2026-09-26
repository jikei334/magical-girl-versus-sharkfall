using System;

namespace MagicalGirl.Core.Combat
{
    /// <summary>
    /// バリアの展開状態(残り時間の追跡)を管理するUnity非依存のロジック。
    /// 展開した瞬間の範囲攻撃(バーストダメージ)自体はUnity側(Barrierコンポーネント)が
    /// Physics.OverlapSphereで対象を探して行う。このクラスは「今バリアが展開中かどうか」
    /// だけを扱う。
    /// </summary>
    public sealed class BarrierState
    {
        /// <summary>このバリアのパラメータ設定。</summary>
        public BarrierConfig Config { get; }

        /// <summary>展開中かどうか。</summary>
        public bool IsActive { get; private set; }

        /// <summary>展開が終わるまでの残り時間(秒)。展開中でない場合は0。</summary>
        public float RemainingDuration { get; private set; }

        /// <summary>
        /// 引数: config - このバリアのパラメータ設定
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public BarrierState(BarrierConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// バリアを展開する(既に展開中の場合は残り時間をDurationへリセットする)。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            RemainingDuration = Config.Duration;
        }

        /// <summary>
        /// 経過時間の分だけ残り時間を減らす。展開中でない場合は何もしない。
        /// 引数: deltaTime - 経過時間(秒、0以上である必要がある)
        /// 返り値: なし
        /// 例外: deltaTimeが負の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "deltaTimeは0以上である必要があります。");

            if (!IsActive)
                return;

            RemainingDuration = Math.Max(0f, RemainingDuration - deltaTime);
            if (RemainingDuration <= 0f)
                IsActive = false;
        }
    }
}
