namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// ピッチ入力の現在のゾーン。通常域→緩衝ゾーン→ブレーキ(極端域)の順に遷移する。
    /// </summary>
    public enum PitchZone
    {
        /// <summary>通常の降下/上昇操作として扱う範囲。</summary>
        Normal,

        /// <summary>通常域とブレーキの間の緩衝ゾーン。誤ってブレーキが発動しないための領域。</summary>
        Buffer,

        /// <summary>ピッチを引き切った失速ブレーキの範囲。</summary>
        Brake,
    }
}
