namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 建物の形状バリエーション。単調さを減らすため、CityGeneratorがランダムに割り当てる。
    /// </summary>
    public enum BuildingShape
    {
        /// <summary>単純な直方体1つ。</summary>
        Box,

        /// <summary>上に行くほど細くなる階段状(ウェディングケーキ型)。</summary>
        Stepped,

        /// <summary>2つの直方体を組み合わせたL字型。</summary>
        LShape,
    }
}
