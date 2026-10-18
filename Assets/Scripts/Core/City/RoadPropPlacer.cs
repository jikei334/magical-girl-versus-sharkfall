using System;
using System.Collections.Generic;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 道路網(RoadLayout)に沿って、街灯・標識などの小物を置く位置を決めるUnity非依存ロジック。
    /// 各道路区間の長さ方向に一定間隔で点を並べ、道路の片側へ一定距離オフセットする。
    /// </summary>
    public static class RoadPropPlacer
    {
        /// <summary>
        /// 道路の片側に沿って、一定間隔で点を生成する。
        /// 引数:
        ///   layout - 対象の道路網 / spacing - 区間の長さ方向の間隔(0より大きい値)
        ///   sideOffset - 道路中心からのオフセット(道路の幅方向。正負で左右どちら側かを表す)
        /// 返り値: 生成された点の一覧(区間ごとに、区間の中心を基準に対称に並ぶ)
        /// 例外: layoutがnullの場合、ArgumentNullException。spacingが0以下の場合、ArgumentOutOfRangeException
        /// </summary>
        public static IReadOnlyList<RoadPropPoint> GenerateAlongRoadside(RoadLayout layout, float spacing, float sideOffset)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (spacing <= 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "spacingは0より大きい必要があります。");

            var points = new List<RoadPropPoint>();

            foreach (var segment in layout.Segments)
            {
                var count = Math.Max(1, (int)(segment.Length / spacing));
                var startOffset = -(count - 1) * spacing / 2f;

                for (var i = 0; i < count; i++)
                {
                    var alongOffset = startOffset + i * spacing;

                    points.Add(segment.RunsAlongX
                        ? new RoadPropPoint(segment.CenterX + alongOffset, segment.CenterZ + sideOffset)
                        : new RoadPropPoint(segment.CenterX + sideOffset, segment.CenterZ + alongOffset));
                }
            }

            return points;
        }
    }
}
