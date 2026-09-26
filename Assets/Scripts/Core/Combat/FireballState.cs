using System;

namespace MagicalGirl.Core.Combat
{
    /// <summary>
    /// 火球1発分の飛翔状態(直進した距離の追跡と、最大射程に達したかどうかの判定)を管理する
    /// Unity非依存のロジック。実際の3D移動・当たり判定はUnity側(Fireballコンポーネント)が
    /// このクラスと並行して行う。
    /// </summary>
    public sealed class FireballState
    {
        /// <summary>この火球のパラメータ設定。</summary>
        public FireballConfig Config { get; }

        /// <summary>発射地点からの直進距離(単位)。</summary>
        public float DistanceTraveled { get; private set; }

        /// <summary>最大射程に達し、消滅すべきかどうか。</summary>
        public bool IsExpired => DistanceTraveled >= Config.MaxRange;

        /// <summary>
        /// 引数: config - この火球のパラメータ設定
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public FireballState(FireballConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// 経過時間の分だけ直進させる。既に最大射程に達している場合は何もしない。
        /// 引数: deltaTime - 経過時間(秒、0以上である必要がある)
        /// 返り値: なし
        /// 例外: deltaTimeが負の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "deltaTimeは0以上である必要があります。");

            if (IsExpired)
                return;

            DistanceTraveled += Config.Speed * deltaTime;
        }
    }
}
