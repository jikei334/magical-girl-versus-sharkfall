namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// ブレーキ判定に使う軸(現在はロール)の現在のゾーン。
    /// 通常域→緩衝ゾーン→ブレーキ(極端域)の順に遷移する。
    /// </summary>
    public enum BrakeZone
    {
        /// <summary>通常の操作として扱う範囲。</summary>
        Normal,

        /// <summary>通常域とブレーキの間の緩衝ゾーン。誤ってブレーキが発動しないための領域。</summary>
        Buffer,

        /// <summary>ロールを倒し切った(タッチパネル面がほぼ地面に垂直になる)急減速(スキー止め)の範囲。</summary>
        Brake,
    }
}
