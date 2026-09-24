namespace MagicalGirl.Core.Gesture
{
    /// <summary>
    /// 認識対象のジェスチャー種別。ジグザグ(雷撃)・長押し(チャージ)は別Issueで追加する。
    /// </summary>
    public enum GestureType
    {
        /// <summary>直線に素早くフリック → 火球(直進弾)。</summary>
        Line,

        /// <summary>円を描く → バリア展開/範囲攻撃。</summary>
        Circle,
    }
}
