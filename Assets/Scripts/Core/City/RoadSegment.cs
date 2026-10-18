using System;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 道路1区間分のデータ。X軸方向またはZ軸方向のどちらかに長く伸びる帯状の領域を表す。
    /// </summary>
    public readonly struct RoadSegment
    {
        /// <summary>区間中心のワールドX座標。</summary>
        public float CenterX { get; }

        /// <summary>区間中心のワールドZ座標。</summary>
        public float CenterZ { get; }

        /// <summary>区間が伸びる方向の長さ(0より大きい値)。</summary>
        public float Length { get; }

        /// <summary>道路の幅(0より大きい値)。</summary>
        public float Width { get; }

        /// <summary>trueならX軸方向に伸びる(幅はZ方向)、falseならZ軸方向に伸びる(幅はX方向)。</summary>
        public bool RunsAlongX { get; }

        /// <summary>
        /// 引数: centerX, centerZ - 区間中心のワールド座標 / length - 伸びる方向の長さ
        ///        / width - 道路の幅 / runsAlongX - 伸びる方向(true=X軸方向、false=Z軸方向)
        /// 返り値: なし(コンストラクタ)
        /// 例外: lengthまたはwidthが0以下の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public RoadSegment(float centerX, float centerZ, float length, float width, bool runsAlongX)
        {
            if (length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length), length, "lengthは0より大きい必要があります。");
            if (width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width), width, "widthは0より大きい必要があります。");

            CenterX = centerX;
            CenterZ = centerZ;
            Length = length;
            Width = width;
            RunsAlongX = runsAlongX;
        }
    }
}
