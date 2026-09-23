namespace MagicalGirl.Core.Flight
{
    /// <summary>
    /// FlightModelが1フレーム分の入力から算出した出力。
    /// </summary>
    public readonly struct FlightOutput
    {
        /// <summary>前進速度(単位/秒)。</summary>
        public float Speed { get; }

        /// <summary>旋回速度(度/秒)。</summary>
        public float YawRateDegPerSecond { get; }

        /// <summary>機首の上下角(度)。正の値で機首上げ。</summary>
        public float PitchAngleDeg { get; }

        /// <summary>バンク角(度)。旋回方向に応じた視覚的な傾き。</summary>
        public float BankAngleDeg { get; }

        /// <summary>
        /// 引数: speed, yawRateDegPerSecond, pitchAngleDeg, bankAngleDeg - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public FlightOutput(float speed, float yawRateDegPerSecond, float pitchAngleDeg, float bankAngleDeg)
        {
            Speed = speed;
            YawRateDegPerSecond = yawRateDegPerSecond;
            PitchAngleDeg = pitchAngleDeg;
            BankAngleDeg = bankAngleDeg;
        }
    }
}
