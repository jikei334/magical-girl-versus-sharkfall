using System;
using System.Collections.Generic;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 手続き型都市生成のUnity非依存ロジック。グリッド状の道路網を前提に、中心ブロックへ
    /// ランドマークタワーを、それ以外のブロックへ建物を配置する。
    /// 同じCityGenerationConfig(特にSeed)を与えれば、常に同じ結果を返す(決定論的)。
    /// </summary>
    public static class CityGenerator
    {
        /// <summary>
        /// 街のレイアウトを生成する。
        /// 引数: config - 生成パラメータ
        /// 返り値: 生成された街のレイアウト(ランドマーク+建物一覧)
        /// </summary>
        public static CityLayout Generate(CityGenerationConfig config)
        {
            var random = new Random(config.Seed);
            var spacing = config.BlockSize + config.RoadWidth;

            var landmark = CreateLandmark(config);
            var buildings = new List<BuildingData>();

            for (var bx = -config.GridExtent; bx <= config.GridExtent; bx++)
            {
                for (var bz = -config.GridExtent; bz <= config.GridExtent; bz++)
                {
                    if (bx == 0 && bz == 0)
                        continue; // 中心はランドマーク用に予約済み

                    var centerX = bx * spacing;
                    var centerZ = bz * spacing;

                    // 中心からの距離(0〜1に正規化、チェビシェフ距離)が大きいほど郊外とみなし、
                    // 建物の高さを低くする。都心(中心付近)ほど高層ビルが集まる自然な起伏を作る。
                    var chebyshevDistance = Math.Max(Math.Abs(bx), Math.Abs(bz));
                    var normalizedDistance = (float)chebyshevDistance / config.GridExtent;

                    buildings.Add(CreateBuilding(config, random, centerX, centerZ, normalizedDistance));
                }
            }

            return new CityLayout(landmark, buildings);
        }

        /// <summary>
        /// 中心ブロックに配置するランドマークタワーを生成する。
        /// 引数: config - 生成パラメータ
        /// 返り値: ランドマークのBuildingData(単純な直方体1つ)
        /// </summary>
        static BuildingData CreateLandmark(CityGenerationConfig config)
        {
            var footprint = config.BlockSize * config.LandmarkFootprintRatio;
            var block = new BuildingBlock(0f, 0f, 0f, footprint, footprint, config.LandmarkHeight);
            return new BuildingData(0f, 0f, BuildingShape.Box, new[] { block });
        }

        /// <summary>
        /// 1敷地分の建物を生成する。
        /// 引数:
        ///   config - 生成パラメータ / random - 共有の乱数生成器
        ///   centerX, centerZ - 敷地の中心ワールド座標
        ///   normalizedDistance - 中心からの正規化距離(0=中心、1=最外周)
        /// 返り値: 生成されたBuildingData
        /// </summary>
        static BuildingData CreateBuilding(CityGenerationConfig config, Random random, float centerX, float centerZ, float normalizedDistance)
        {
            var footprintRatio = Lerp(config.MinFootprintRatio, config.MaxFootprintRatio, (float)random.NextDouble());
            var footprint = config.BlockSize * footprintRatio;

            // 都心(距離0)ほどMaxBuildingHeightに近く、郊外(距離1)ほどMinBuildingHeightに近い基準高さを
            // 決め、そこにランダムなばらつき(±30%)を加えてMin〜Maxにクランプする。
            var baseHeight = Lerp(config.MaxBuildingHeight, config.MinBuildingHeight, normalizedDistance);
            var jitter = Lerp(0.7f, 1.3f, (float)random.NextDouble());
            var height = Math.Clamp(baseHeight * jitter, config.MinBuildingHeight, config.MaxBuildingHeight);

            var shape = (BuildingShape)random.Next(0, 3);
            var blocks = BuildBlocks(shape, footprint, height);

            return new BuildingData(centerX, centerZ, shape, blocks);
        }

        /// <summary>
        /// 形状バリエーションに応じたブロック構成を作る。
        /// 引数: shape - 形状 / footprint - 設置面積の一辺の長さ / height - 建物全体の高さ
        /// 返り値: 構成ブロックの配列
        /// </summary>
        static BuildingBlock[] BuildBlocks(BuildingShape shape, float footprint, float height)
        {
            switch (shape)
            {
                case BuildingShape.Stepped:
                {
                    // 3段の階段状(ウェディングケーキ型)。上に行くほど幅・奥行きが小さくなる。
                    var h1 = height * 0.5f;
                    var h2 = height * 0.3f;
                    var h3 = height - h1 - h2;
                    return new[]
                    {
                        new BuildingBlock(0f, 0f, 0f, footprint, footprint, h1),
                        new BuildingBlock(0f, 0f, h1, footprint * 0.7f, footprint * 0.7f, h2),
                        new BuildingBlock(0f, 0f, h1 + h2, footprint * 0.45f, footprint * 0.45f, h3),
                    };
                }

                case BuildingShape.LShape:
                {
                    // 2つの直方体を1/4ずつずらして重ね、L字型のシルエットを作る。
                    var half = footprint * 0.5f;
                    return new[]
                    {
                        new BuildingBlock(-half * 0.5f, 0f, 0f, half, footprint, height),
                        new BuildingBlock(half * 0.25f, -half * 0.5f, 0f, half, half, height),
                    };
                }

                case BuildingShape.Box:
                default:
                    return new[] { new BuildingBlock(0f, 0f, 0f, footprint, footprint, height) };
            }
        }

        /// <summary>
        /// aとbの線形補間を返す。
        /// 引数: a, b - 補間する2値 / t - 補間係数(0でa、1でb。範囲外もそのまま外挿する)
        /// 返り値: 補間結果
        /// </summary>
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
