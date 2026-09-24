namespace MagicalGirl.Core.Gesture
{
    /// <summary>
    /// UnistrokeRecognizerによるジェスチャー認識結果。
    /// </summary>
    public readonly struct GestureRecognitionResult
    {
        /// <summary>閾値以上のスコアでテンプレートに一致したかどうか。falseなら詠唱失敗として扱う。</summary>
        public bool IsMatch { get; }

        /// <summary>最も近いテンプレートの種別(IsMatchがfalseの場合は参考値)。</summary>
        public GestureType Type { get; }

        /// <summary>最も近いテンプレートとの一致度(0〜1、1に近いほど一致)。</summary>
        public float Score { get; }

        /// <summary>
        /// 引数: isMatch, type, score - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public GestureRecognitionResult(bool isMatch, GestureType type, float score)
        {
            IsMatch = isMatch;
            Type = type;
            Score = score;
        }
    }
}
