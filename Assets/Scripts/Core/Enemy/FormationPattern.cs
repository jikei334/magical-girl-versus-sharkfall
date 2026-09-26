namespace MagicalGirl.Core.Enemy
{
    /// <summary>
    /// スクール(小型・群れ)の編隊パターン。
    /// </summary>
    public enum FormationPattern
    {
        /// <summary>進行方向に対して横一列に並ぶ。</summary>
        Line,

        /// <summary>V字(先頭1体+左右に広がる)に並ぶ。</summary>
        V,

        /// <summary>格子状に並ぶ。</summary>
        Grid,
    }
}
