namespace MagicalGirl.Core.Gesture
{
    /// <summary>
    /// タッチ軌跡上の1点。Beam Proのタッチパッド座標系での位置を表す。
    /// </summary>
    public readonly struct GesturePoint
    {
        /// <summary>タッチパッド上のX座標。</summary>
        public float X { get; }

        /// <summary>タッチパッド上のY座標。</summary>
        public float Y { get; }

        /// <summary>
        /// 引数: x, y - 座標値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public GesturePoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
