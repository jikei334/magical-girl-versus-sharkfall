using System;
using MagicalGirl.Core.Input;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class TiltInputProcessorTests
    {
        static TiltInputConfig DefaultConfig() => new TiltInputConfig(
            deadzone: 0.05f,
            exponent: 1f,
            rollFullScaleDegrees: 45f,
            pitchFullScaleDegrees: 45f,
            pitchNormalMax: 0.75f,
            pitchBrakeMin: 0.9f);

        [Fact]
        public void Process_WithoutCalibrate_Throws()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            Assert.Throws<InvalidOperationException>(() => processor.Process(0f, 0f));
        }

        [Fact]
        public void Process_AtNeutral_ReturnsZeroAndNormalZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            processor.Calibrate(rawRoll: 3f, rawPitch: -2f);

            var state = processor.Process(rawRoll: 3f, rawPitch: -2f);

            Assert.Equal(0f, state.Roll);
            Assert.Equal(0f, state.Pitch);
            Assert.Equal(PitchZone.Normal, state.Zone);
        }

        [Fact]
        public void Process_PitchPulledToLimit_EntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            // フルスケール45度に対して44度引き上げ。デッドゾーン控除後も0.9以上になり、ブレーキ域に入る想定
            var state = processor.Process(rawRoll: 0f, rawPitch: 44f);

            Assert.Equal(PitchZone.Brake, state.Zone);
        }

        [Fact]
        public void Process_PitchPushedDownToLimit_NeverEntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            // 機首を下げる方向(負のピッチ)はどれだけ大きくてもブレーキ対象外
            var state = processor.Process(rawRoll: 0f, rawPitch: -60f);

            Assert.Equal(PitchZone.Normal, state.Zone);
            Assert.True(state.Pitch < 0f);
        }

        [Fact]
        public void Process_RollBeyondFullScale_ClampsToOne()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            var state = processor.Process(rawRoll: 90f, rawPitch: 0f);

            Assert.Equal(1f, state.Roll, 0.00001f);
        }
    }

    public class TiltInputConfigTests
    {
        [Theory]
        [InlineData(-0.1f, 1f, 45f, 45f, 0.75f, 0.9f)] // deadzoneが負
        [InlineData(1f, 1f, 45f, 45f, 0.75f, 0.9f)] // deadzone=1
        [InlineData(0.05f, 0f, 45f, 45f, 0.75f, 0.9f)] // exponent=0
        [InlineData(0.05f, 1f, 0f, 45f, 0.75f, 0.9f)] // rollFullScaleDegrees=0
        [InlineData(0.05f, 1f, 45f, 0f, 0.75f, 0.9f)] // pitchFullScaleDegrees=0
        [InlineData(0.05f, 1f, 45f, 45f, 0f, 0.9f)] // pitchNormalMax=0
        [InlineData(0.05f, 1f, 45f, 45f, 0.9f, 0.9f)] // pitchBrakeMin <= pitchNormalMax
        public void Constructor_InvalidValues_Throws(
            float deadzone, float exponent, float rollFullScale, float pitchFullScale, float normalMax, float brakeMin)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TiltInputConfig(deadzone, exponent, rollFullScale, pitchFullScale, normalMax, brakeMin));
        }
    }
}
