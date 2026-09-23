using System;
using MagicalGirl.Core.Input;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class TiltInputProcessorTests
    {
        static TiltInputConfig DefaultConfig(bool brakeOnNegativePitch = false) => new TiltInputConfig(
            deadzone: 0.05f,
            exponent: 1f,
            rollFullScaleDegrees: 45f,
            pitchFullScaleDegrees: 45f,
            brakeNormalMax: 0.75f,
            brakeMin: 0.9f,
            brakeOnNegativePitch: brakeOnNegativePitch);

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
            Assert.Equal(BrakeZone.Normal, state.Zone);
        }

        [Fact]
        public void Process_PositivePitchBrake_PulledToLimit_EntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig(brakeOnNegativePitch: false));
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            // フルスケール45度に対して44度引き上げ。デッドゾーン控除後も0.9以上になり、ブレーキ域に入る想定
            var state = processor.Process(rawRoll: 0f, rawPitch: 44f);

            Assert.Equal(BrakeZone.Brake, state.Zone);
        }

        [Fact]
        public void Process_PositivePitchBrake_PushedDownToLimit_NeverEntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig(brakeOnNegativePitch: false));
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            var state = processor.Process(rawRoll: 0f, rawPitch: -60f);

            Assert.Equal(BrakeZone.Normal, state.Zone);
            Assert.True(state.Pitch < 0f);
        }

        [Fact]
        public void Process_NegativePitchBrake_PushedDownToLimit_EntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig(brakeOnNegativePitch: true));
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            var state = processor.Process(rawRoll: 0f, rawPitch: -44f);

            Assert.Equal(BrakeZone.Brake, state.Zone);
        }

        [Fact]
        public void Process_NegativePitchBrake_PulledUpToLimit_NeverEntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig(brakeOnNegativePitch: true));
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            var state = processor.Process(rawRoll: 0f, rawPitch: 60f);

            Assert.Equal(BrakeZone.Normal, state.Zone);
            Assert.True(state.Pitch > 0f);
        }

        [Fact]
        public void Process_RollAtExtreme_NeverEntersBrakeZone()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            // ブレーキはピッチ基準なので、ロールがどれだけ極端でもブレーキには入らない
            var statePositive = processor.Process(rawRoll: 44f, rawPitch: 0f);
            var stateNegative = processor.Process(rawRoll: -44f, rawPitch: 0f);

            Assert.Equal(BrakeZone.Normal, statePositive.Zone);
            Assert.Equal(BrakeZone.Normal, stateNegative.Zone);
        }

        [Fact]
        public void Process_RollBeyondFullScale_ClampsToOne()
        {
            var processor = new TiltInputProcessor(DefaultConfig());
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            var state = processor.Process(rawRoll: 90f, rawPitch: 0f);

            Assert.Equal(1f, state.Roll, 0.00001f);
        }

        [Fact]
        public void UpdateConfig_ChangesBrakeDirection_WithoutRequiringRecalibration()
        {
            var processor = new TiltInputProcessor(DefaultConfig(brakeOnNegativePitch: false));
            processor.Calibrate(rawRoll: 0f, rawPitch: 0f);

            // 変更前: 正のピッチでブレーキ
            var before = processor.Process(rawRoll: 0f, rawPitch: 44f);
            Assert.Equal(BrakeZone.Brake, before.Zone);

            processor.UpdateConfig(DefaultConfig(brakeOnNegativePitch: true));

            // 変更後: 同じ正のピッチではブレーキに入らず、負のピッチでブレーキに入る。
            // 再キャリブレーション不要でニュートラル位置(0度)は維持されている。
            var afterPositive = processor.Process(rawRoll: 0f, rawPitch: 44f);
            var afterNegative = processor.Process(rawRoll: 0f, rawPitch: -44f);
            Assert.Equal(BrakeZone.Normal, afterPositive.Zone);
            Assert.Equal(BrakeZone.Brake, afterNegative.Zone);
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
        [InlineData(0.05f, 1f, 45f, 45f, 0f, 0.9f)] // brakeNormalMax=0
        [InlineData(0.05f, 1f, 45f, 45f, 0.9f, 0.9f)] // brakeMin <= brakeNormalMax
        public void Constructor_InvalidValues_Throws(
            float deadzone, float exponent, float rollFullScale, float pitchFullScale, float normalMax, float brakeMin)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TiltInputConfig(deadzone, exponent, rollFullScale, pitchFullScale, normalMax, brakeMin, brakeOnNegativePitch: false));
        }
    }
}
