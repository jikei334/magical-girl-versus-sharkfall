namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 建物を構成する直方体1つ分のデータ。建物はこのブロックを1〜複数個組み合わせて表現する
    /// (Box=1個、Stepped=積み上げ3個、LShape=並べて2個)。座標は建物の基準点(地面レベル)からの
    /// ローカルオフセット。
    /// </summary>
    public readonly struct BuildingBlock
    {
        /// <summary>建物基準点からのX方向オフセット。</summary>
        public float OffsetX { get; }

        /// <summary>建物基準点からのZ方向オフセット。</summary>
        public float OffsetZ { get; }

        /// <summary>このブロックの底面の高さ(地面からの積み上げ量)。0以上。</summary>
        public float BaseHeight { get; }

        /// <summary>ブロックの幅(X方向、0より大きい値)。</summary>
        public float Width { get; }

        /// <summary>ブロックの奥行き(Z方向、0より大きい値)。</summary>
        public float Depth { get; }

        /// <summary>ブロックの高さ(Y方向、0より大きい値)。</summary>
        public float Height { get; }

        /// <summary>
        /// 引数: offsetX, offsetZ, baseHeight, width, depth, height - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public BuildingBlock(float offsetX, float offsetZ, float baseHeight, float width, float depth, float height)
        {
            OffsetX = offsetX;
            OffsetZ = offsetZ;
            BaseHeight = baseHeight;
            Width = width;
            Depth = depth;
            Height = height;
        }
    }
}
