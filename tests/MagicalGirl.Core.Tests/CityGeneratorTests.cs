using System;
using System.Linq;
using MagicalGirl.Core.City;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class CityGeneratorTests
    {
        static CityGenerationConfig DefaultConfig(int seed = 42, int gridExtent = 3) => new CityGenerationConfig(
            gridExtent: gridExtent,
            blockSize: 20f,
            roadWidth: 10f,
            minBuildingHeight: 5f,
            maxBuildingHeight: 40f,
            minFootprintRatio: 0.4f,
            maxFootprintRatio: 0.8f,
            landmarkHeight: 80f,
            landmarkFootprintRatio: 0.6f,
            seed: seed);

        [Fact]
        public void Generate_ProducesExpectedBuildingCount()
        {
            var layout = CityGenerator.Generate(DefaultConfig(gridExtent: 3));

            // (2*3+1)^2 - 1(中心はランドマーク) = 48
            Assert.Equal(48, layout.Buildings.Count);
        }

        [Fact]
        public void Generate_LandmarkIsAtCenterWithConfiguredHeight()
        {
            var config = DefaultConfig();
            var layout = CityGenerator.Generate(config);

            Assert.Equal(0f, layout.Landmark.X);
            Assert.Equal(0f, layout.Landmark.Z);
            Assert.Equal(config.LandmarkHeight, layout.Landmark.Blocks[0].Height);
        }

        [Fact]
        public void Generate_NoRegularBuildingAtCenter()
        {
            var layout = CityGenerator.Generate(DefaultConfig());

            Assert.DoesNotContain(layout.Buildings, b => b.X == 0f && b.Z == 0f);
        }

        [Fact]
        public void Generate_AllBuildingHeights_AreWithinConfiguredRange()
        {
            var config = DefaultConfig();
            var layout = CityGenerator.Generate(config);

            foreach (var building in layout.Buildings)
            {
                foreach (var block in building.Blocks)
                {
                    Assert.True(block.Height <= config.MaxBuildingHeight + 0.001f,
                        $"height {block.Height} exceeds max {config.MaxBuildingHeight}");
                }
            }

            // 各建物の合計高さ(積み上げ含む)の下限は、少なくとも最下段のブロックがMinBuildingHeight未満に
            // ならないことを確認する(Stepped等は上段が低くなるのは仕様通り)。
            foreach (var building in layout.Buildings)
            {
                var totalHeight = building.Blocks.Sum(b => b.Height);
                Assert.True(totalHeight >= config.MinBuildingHeight - 0.001f,
                    $"total height {totalHeight} below min {config.MinBuildingHeight}");
            }
        }

        [Fact]
        public void Generate_SameSeed_ProducesIdenticalLayout()
        {
            var config = DefaultConfig(seed: 123);

            var layoutA = CityGenerator.Generate(config);
            var layoutB = CityGenerator.Generate(config);

            Assert.Equal(layoutA.Buildings.Count, layoutB.Buildings.Count);
            for (var i = 0; i < layoutA.Buildings.Count; i++)
            {
                Assert.Equal(layoutA.Buildings[i].X, layoutB.Buildings[i].X);
                Assert.Equal(layoutA.Buildings[i].Z, layoutB.Buildings[i].Z);
                Assert.Equal(layoutA.Buildings[i].Shape, layoutB.Buildings[i].Shape);
                Assert.Equal(layoutA.Buildings[i].Blocks[0].Height, layoutB.Buildings[i].Blocks[0].Height);
            }
        }

        [Fact]
        public void Generate_DifferentSeed_ProducesDifferentLayout()
        {
            var layoutA = CityGenerator.Generate(DefaultConfig(seed: 1));
            var layoutB = CityGenerator.Generate(DefaultConfig(seed: 2));

            var anyDifferent = layoutA.Buildings
                .Zip(layoutB.Buildings, (a, b) => a.Shape != b.Shape || Math.Abs(a.Blocks[0].Height - b.Blocks[0].Height) > 0.001f)
                .Any(different => different);

            Assert.True(anyDifferent, "異なるシードなのに全ての建物が完全に一致した(乱数の使い方に問題がある可能性)");
        }

        [Fact]
        public void Generate_DowntownBuildings_AreTallerOnAverageThanOutskirts()
        {
            // 十分広いグリッドで、中心付近と最外周の平均高さを比較し、都心の方が高いことを確認する。
            var config = DefaultConfig(gridExtent: 5);
            var layout = CityGenerator.Generate(config);

            var downtownAvg = layout.Buildings
                .Where(b => Math.Max(Math.Abs(b.X), Math.Abs(b.Z)) <= config.BlockSize + config.RoadWidth)
                .Average(b => b.Blocks[0].Height);

            var outskirtsAvg = layout.Buildings
                .Where(b => Math.Max(Math.Abs(b.X), Math.Abs(b.Z)) >= (config.GridExtent - 1) * (config.BlockSize + config.RoadWidth))
                .Average(b => b.Blocks[0].Height);

            Assert.True(downtownAvg > outskirtsAvg,
                $"downtown avg {downtownAvg} should be greater than outskirts avg {outskirtsAvg}");
        }
    }

    public class BuildingDataTests
    {
        [Fact]
        public void Constructor_EmptyBlocks_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BuildingData(0f, 0f, BuildingShape.Box, Array.Empty<BuildingBlock>()));
        }

        [Fact]
        public void Constructor_NullBlocks_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BuildingData(0f, 0f, BuildingShape.Box, null!));
        }
    }

    public class CityLayoutTests
    {
        [Fact]
        public void Constructor_NullLandmark_Throws()
        {
            var buildings = Array.Empty<BuildingData>();
            Assert.Throws<ArgumentNullException>(() => new CityLayout(null!, buildings));
        }

        [Fact]
        public void Constructor_NullBuildings_Throws()
        {
            var landmark = new BuildingData(0f, 0f, BuildingShape.Box, new[] { new BuildingBlock(0f, 0f, 0f, 1f, 1f, 1f) });
            Assert.Throws<ArgumentNullException>(() => new CityLayout(landmark, null!));
        }
    }

    public class CityGenerationConfigTests
    {
        [Theory]
        [InlineData(0, 20f, 10f, 5f, 40f, 0.4f, 0.8f, 80f, 0.6f)] // gridExtent=0
        [InlineData(3, 0f, 10f, 5f, 40f, 0.4f, 0.8f, 80f, 0.6f)] // blockSize=0
        [InlineData(3, 20f, -1f, 5f, 40f, 0.4f, 0.8f, 80f, 0.6f)] // roadWidth負
        [InlineData(3, 20f, 10f, 0f, 40f, 0.4f, 0.8f, 80f, 0.6f)] // minBuildingHeight=0
        [InlineData(3, 20f, 10f, 50f, 40f, 0.4f, 0.8f, 80f, 0.6f)] // maxBuildingHeight<minBuildingHeight
        [InlineData(3, 20f, 10f, 5f, 40f, 0f, 0.8f, 80f, 0.6f)] // minFootprintRatio=0
        [InlineData(3, 20f, 10f, 5f, 40f, 0.8f, 0.4f, 80f, 0.6f)] // maxFootprintRatio<minFootprintRatio
        [InlineData(3, 20f, 10f, 5f, 40f, 0.4f, 0.8f, 30f, 0.6f)] // landmarkHeight<=maxBuildingHeight
        [InlineData(3, 20f, 10f, 5f, 40f, 0.4f, 0.8f, 80f, 0f)] // landmarkFootprintRatio=0
        public void Constructor_InvalidValues_Throws(
            int gridExtent, float blockSize, float roadWidth, float minBuildingHeight, float maxBuildingHeight,
            float minFootprintRatio, float maxFootprintRatio, float landmarkHeight, float landmarkFootprintRatio)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityGenerationConfig(
                gridExtent, blockSize, roadWidth, minBuildingHeight, maxBuildingHeight,
                minFootprintRatio, maxFootprintRatio, landmarkHeight, landmarkFootprintRatio, seed: 0));
        }
    }
}
