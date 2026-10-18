using System;
using System.Linq;
using MagicalGirl.Core.City;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class RoadSegmentTests
    {
        [Theory]
        [InlineData(0f, 10f)] // length=0
        [InlineData(-1f, 10f)] // length負
        [InlineData(10f, 0f)] // width=0
        [InlineData(10f, -1f)] // width負
        public void Constructor_InvalidValues_Throws(float length, float width)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoadSegment(0f, 0f, length, width, runsAlongX: true));
        }
    }

    public class RoadGeneratorTests
    {
        static CityGenerationConfig DefaultConfig(int gridExtent = 3, float blockSize = 20f, float roadWidth = 10f) => new CityGenerationConfig(
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
        public void Generate_ProducesExpectedSegmentCount()
        {
            var layout = RoadGenerator.Generate(DefaultConfig(gridExtent: 3));

            // 縦方向2*3本 + 横方向2*3本 = 12本
            Assert.Equal(12, layout.Segments.Count);
        }

        [Fact]
        public void Generate_SegmentsHaveConfiguredWidthAndSpanFullCity()
        {
            var config = DefaultConfig(gridExtent: 3, blockSize: 20f, roadWidth: 10f);
            var layout = RoadGenerator.Generate(config);

            var spacing = config.BlockSize + config.RoadWidth;
            var expectedSpan = (2 * config.GridExtent + 1) * spacing;

            Assert.All(layout.Segments, segment =>
            {
                Assert.Equal(config.RoadWidth, segment.Width);
                Assert.Equal(expectedSpan, segment.Length);
            });
        }

        [Fact]
        public void Generate_HasEqualNumberOfHorizontalAndVerticalSegments()
        {
            var layout = RoadGenerator.Generate(DefaultConfig(gridExtent: 3));

            var alongX = layout.Segments.Count(s => s.RunsAlongX);
            var alongZ = layout.Segments.Count(s => !s.RunsAlongX);

            Assert.Equal(6, alongX);
            Assert.Equal(6, alongZ);
        }

        [Fact]
        public void Generate_VerticalSegmentPositions_AreCenteredOnBlockBoundaries()
        {
            var config = DefaultConfig(gridExtent: 1, blockSize: 20f, roadWidth: 10f);
            var layout = RoadGenerator.Generate(config);

            var spacing = config.BlockSize + config.RoadWidth; // 30
            var verticalXPositions = layout.Segments.Where(s => !s.RunsAlongX).Select(s => s.CenterX).OrderBy(x => x).ToArray();

            // gridExtent=1 -> bx in [-1, 0] -> boundary at (bx+0.5)*spacing = -15, 15
            Assert.Equal(new[] { -0.5f * spacing, 0.5f * spacing }, verticalXPositions);
        }

        [Fact]
        public void Generate_IsDeterministic()
        {
            var config = DefaultConfig();
            var layoutA = RoadGenerator.Generate(config);
            var layoutB = RoadGenerator.Generate(config);

            Assert.Equal(layoutA.Segments.Count, layoutB.Segments.Count);
            for (var i = 0; i < layoutA.Segments.Count; i++)
            {
                Assert.Equal(layoutA.Segments[i].CenterX, layoutB.Segments[i].CenterX);
                Assert.Equal(layoutA.Segments[i].CenterZ, layoutB.Segments[i].CenterZ);
            }
        }
    }
}
