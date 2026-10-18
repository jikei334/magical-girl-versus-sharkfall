using System;
using System.Linq;
using MagicalGirl.Core.City;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class RoadPropPlacerTests
    {
        static CityGenerationConfig DefaultConfig(int gridExtent = 1, float blockSize = 20f, float roadWidth = 10f) => new CityGenerationConfig(
            gridExtent: gridExtent,
            blockSize: blockSize,
            roadWidth: roadWidth,
            minBuildingHeight: 5f,
            maxBuildingHeight: 40f,
            minFootprintRatio: 0.4f,
            maxFootprintRatio: 0.8f,
            landmarkHeight: 80f,
            landmarkFootprintRatio: 0.6f,
            seed: 42);

        [Fact]
        public void GenerateAlongRoadside_NullLayout_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RoadPropPlacer.GenerateAlongRoadside(null, 10f, 5f));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void GenerateAlongRoadside_InvalidSpacing_Throws(float spacing)
        {
            var layout = RoadGenerator.Generate(DefaultConfig());
            Assert.Throws<ArgumentOutOfRangeException>(() => RoadPropPlacer.GenerateAlongRoadside(layout, spacing, 5f));
        }

        [Fact]
        public void GenerateAlongRoadside_ProducesAtLeastOnePointPerSegment()
        {
            var layout = RoadGenerator.Generate(DefaultConfig(gridExtent: 1));

            // spacingを区間の全長より大きくしても、各区間最低1点は生成される。
            var points = RoadPropPlacer.GenerateAlongRoadside(layout, spacing: 100000f, sideOffset: 5f);

            Assert.Equal(layout.Segments.Count, points.Count);
        }

        [Fact]
        public void GenerateAlongRoadside_OffsetsPointsToOneSideOfRoad()
        {
            var config = DefaultConfig(gridExtent: 1, blockSize: 20f, roadWidth: 10f);
            var layout = RoadGenerator.Generate(config);
            var horizontalSegment = layout.Segments.First(s => s.RunsAlongX);

            var points = RoadPropPlacer.GenerateAlongRoadside(layout, spacing: 100000f, sideOffset: 7f);
            var point = points[layout.Segments.ToList().IndexOf(horizontalSegment)];

            // X方向に伸びる区間は、幅方向(Z)にオフセットされる。
            Assert.Equal(horizontalSegment.CenterZ + 7f, point.Z);
        }

        [Fact]
        public void GenerateAlongRoadside_IsDeterministic()
        {
            var layout = RoadGenerator.Generate(DefaultConfig());

            var a = RoadPropPlacer.GenerateAlongRoadside(layout, 15f, 5f);
            var b = RoadPropPlacer.GenerateAlongRoadside(layout, 15f, 5f);

            Assert.Equal(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].X, b[i].X);
                Assert.Equal(a[i].Z, b[i].Z);
            }
        }
    }
}
