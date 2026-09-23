namespace MagicalGirl.Core.Input
{
    /// <summary>
    /// TiltInputProcessorが1フレーム分の入力から算出した状態。
    /// </summary>
    public readonly struct TiltInputState
    {
        /// <summary>ロール入力。-1〜1。</summary>
        public float Roll { get; }

        /// <summary>ピッチ入力。-1〜1。正の値を「機首を上げる」方向として扱う。</summary>
        public float Pitch { get; }

        /// <summary>ピッチの現在のゾーン。</summary>
        public PitchZone Zone { get; }

        /// <summary>
        /// 引数: roll, pitch - 正規化・カーブ適用済みの入力値 / zone - ピッチの現在のゾーン
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public TiltInputState(float roll, float pitch, PitchZone zone)
        {
            Roll = roll;
            Pitch = pitch;
            Zone = zone;
        }
    }
}
