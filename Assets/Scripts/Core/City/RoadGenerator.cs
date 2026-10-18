using System.Collections.Generic;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 手続き型都市生成のグリッドに沿った道路網を生成するUnity非依存ロジック。
    /// CityGeneratorが生成する建物配置と同じグリッド(BlockSize+RoadWidth間隔)を前提に、
    /// 各ブロック境界に道路区間を1本ずつ配置する。同じCityGenerationConfigを与えれば、
    /// 常に同じ結果を返す(決定論的)。
    /// </summary>
    public static class RoadGenerator
    {
        /// <summary>
        /// 道路網を生成する。
        /// 引数: config - 生成パラメータ(CityGeneratorと共有)
        /// 返り値: 生成された道路網
        /// </summary>
        public static RoadLayout Generate(CityGenerationConfig config)
        {
            var spacing = config.BlockSize + config.RoadWidth;
            var citySpan = (2 * config.GridExtent + 1) * spacing;

            var segments = new List<RoadSegment>();

            // 縦方向(Z軸方向に伸びる)の道路。ブロック列の間、つまりbx列とbx+1列の境界に1本ずつ配置する。
            for (var bx = -config.GridExtent; bx < config.GridExtent; bx++)
            {
                var centerX = (bx + 0.5f) * spacing;
                segments.Add(new RoadSegment(centerX, 0f, citySpan, config.RoadWidth, runsAlongX: false));
            }

            // 横方向(X軸方向に伸びる)の道路。ブロック行の間に1本ずつ配置する。
            for (var bz = -config.GridExtent; bz < config.GridExtent; bz++)
            {
                var centerZ = (bz + 0.5f) * spacing;
                segments.Add(new RoadSegment(0f, centerZ, citySpan, config.RoadWidth, runsAlongX: true));
            }

            return new RoadLayout(segments);
        }
    }
}
