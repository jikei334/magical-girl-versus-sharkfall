namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 道路沿いの小物(街灯・標識など)を置く1点分のワールド座標(X,Z)。
    /// </summary>
    public readonly struct RoadPropPoint
    {
        /// <summary>ワールドX座標。</summary>
        public float X { get; }

        /// <summary>ワールドZ座標。</summary>
        public float Z { get; }

        /// <summary>
        /// 引数: x, z - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public RoadPropPoint(float x, float z)
        {
            X = x;
            Z = z;
        }
    }
}
