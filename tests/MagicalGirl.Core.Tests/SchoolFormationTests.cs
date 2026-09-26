using System;
using System.Linq;
using MagicalGirl.Core.Enemy;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class SchoolFormationTests
    {
        [Fact]
        public void GenerateOffsets_Line_ProducesEvenlySpacedRowWithZeroForward()
        {
            var config = new SchoolFormationConfig(FormationPattern.Line, memberCount: 5, spacing: 3f);

            var offsets = SchoolFormation.GenerateOffsets(config);

            Assert.Equal(5, offsets.Length);
            Assert.All(offsets, o => Assert.Equal(0f, o.Forward));

            var rights = offsets.Select(o => o.Right).OrderBy(r => r).ToArray();
            for (var i = 1; i < rights.Length; i++)
                Assert.Equal(3f, rights[i] - rights[i - 1], 0.0001f);

            // 中心(平均)が0付近(左右対称)であることを確認する
            Assert.Equal(0f, rights.Average(), 0.0001f);
        }

        [Fact]
        public void GenerateOffsets_Line_SingleMember_IsAtOrigin()
        {
            var config = new SchoolFormationConfig(FormationPattern.Line, memberCount: 1, spacing: 3f);

            var offsets = SchoolFormation.GenerateOffsets(config);

            Assert.Single(offsets);
            Assert.Equal(0f, offsets[0].Right);
            Assert.Equal(0f, offsets[0].Forward);
        }

        [Fact]
        public void GenerateOffsets_V_LeaderIsAtOrigin()
        {
            var config = new SchoolFormationConfig(FormationPattern.V, memberCount: 7, spacing: 2f);

            var offsets = SchoolFormation.GenerateOffsets(config);

            Assert.Equal(0f, offsets[0].Right);
            Assert.Equal(0f, offsets[0].Forward);
        }

        [Fact]
        public void GenerateOffsets_V_MembersTrailBehindLeaderAndAlternateSides()
        {
            var config = new SchoolFormationConfig(FormationPattern.V, memberCount: 5, spacing: 2f);

            var offsets = SchoolFormation.GenerateOffsets(config);

            // リーダー以外は全員リーダーより後方(Forwardが負)にいる
            for (var i = 1; i < offsets.Length; i++)
                Assert.True(offsets[i].Forward < 0f, $"member {i} should trail the leader");

            // 1番目と2番目は同じランク(同じ後方距離)で、左右反対側にいる
            Assert.Equal(offsets[1].Forward, offsets[2].Forward, 0.0001f);
            Assert.Equal(-offsets[1].Right, offsets[2].Right, 0.0001f);
        }

        [Fact]
        public void GenerateOffsets_Grid_ProducesRequestedMemberCount()
        {
            var config = new SchoolFormationConfig(FormationPattern.Grid, memberCount: 10, spacing: 4f);

            var offsets = SchoolFormation.GenerateOffsets(config);

            Assert.Equal(10, offsets.Length);
        }

        [Fact]
        public void GenerateOffsets_Grid_AllPositionsAreDistinct()
        {
            var config = new SchoolFormationConfig(FormationPattern.Grid, memberCount: 12, spacing: 4f);

            var offsets = SchoolFormation.GenerateOffsets(config);

            var distinctCount = offsets.Select(o => (o.Right, o.Forward)).Distinct().Count();
            Assert.Equal(offsets.Length, distinctCount);
        }
    }

    public class SchoolFormationConfigTests
    {
        [Theory]
        [InlineData(0, 3f)] // memberCount=0
        [InlineData(-1, 3f)] // memberCount負
        [InlineData(5, 0f)] // spacing=0
        [InlineData(5, -1f)] // spacing負
        public void Constructor_InvalidValues_Throws(int memberCount, float spacing)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SchoolFormationConfig(FormationPattern.Line, memberCount, spacing));
        }
    }
}
