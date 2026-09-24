namespace MagicalGirl.Controls
{
    /// <summary>
    /// 実機での動作確認を依頼するたびにインクリメントするビルド番号。
    /// 画面上に表示することで、古いAPKのまま確認していないかを一目で見分けられるようにする。
    /// </summary>
    public static class BuildInfo
    {
        /// <summary>現在のビルド番号。動作確認を依頼するたびに1ずつ増やすこと。</summary>
        public const int Version = 1;
    }
}
