using System;
using MagicalGirl.Core.Flight;
using MagicalGirl.Core.Input;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class FlightModelTests
    {
        static FlightConfig DefaultConfig(
            float baseSpeed = 10f,
            float minSpeed = 2f,
            float maxSpeed = 20f,
            float pitchSpeedSensitivity = 8f,
            float maxYawRateDegPerSecond = 90f,
            float maxBankAngleDeg = 30f,
            float maxPitchAngleDeg = 25f,
            float speedAcceleration = 15f,
            float attitudeRateDegPerSecond = 120f,
            float brakeDeceleration = 40f,
            float stallSpeed = 1f) => new FlightConfig(
                baseSpeed, minSpeed, maxSpeed, pitchSpeedSensitivity, maxYawRateDegPerSecond,
                maxBankAngleDeg, maxPitchAngleDeg, speedAcceleration, attitudeRateDegPerSecond,
                brakeDeceleration, stallSpeed);

        static TiltInputState Neutral => new TiltInputState(0f, 0f, BrakeZone.Normal);

        [Fact]
        public void Step_NegativeDeltaTime_Throws()
        {
            var model = new FlightModel(DefaultConfig());
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Step(Neutral, brakeOnNegativePitch: true, deltaTime: -0.01f));
        }

        [Fact]
        public void Step_AtNeutral_SpeedStaysAtBaseSpeed()
        {
            var model = new FlightModel(DefaultConfig());

            var output = model.Step(Neutral, brakeOnNegativePitch: true, deltaTime: 1f);

            Assert.Equal(10f, output.Speed, 0.001f);
            Assert.Equal(0f, output.YawRateDegPerSecond, 0.001f);
            Assert.Equal(0f, output.PitchAngleDeg, 0.001f);
            Assert.Equal(0f, output.BankAngleDeg, 0.001f);
        }

        [Fact]
        public void Step_PositiveRoll_ProducesPositiveYawAndBank()
        {
            var model = new FlightModel(DefaultConfig());
            var tilt = new TiltInputState(roll: 1f, pitch: 0f, BrakeZone.Normal);

            // 十分な時間を与えてバンク角が目標値に収束することを確認
            var output = model.Step(tilt, brakeOnNegativePitch: true, deltaTime: 1f);

            Assert.Equal(90f, output.YawRateDegPerSecond, 0.001f);
            Assert.Equal(30f, output.BankAngleDeg, 0.001f);
        }

        [Fact]
        public void Step_NoseDownDive_IncreasesSpeed()
        {
            var model = new FlightModel(DefaultConfig());
            // brakeOnNegativePitch=trueの設定では、正のピッチが「機首下げ(ダイブ)」に対応する
            var tilt = new TiltInputState(roll: 0f, pitch: 1f, BrakeZone.Normal);

            var output = model.Step(tilt, brakeOnNegativePitch: true, deltaTime: 1f);

            Assert.Equal(18f, output.Speed, 0.001f); // 10 + 1*8
            Assert.Equal(-25f, output.PitchAngleDeg, 0.001f); // climbAmount=-1 → 機首下げ方向
        }

        [Fact]
        public void Step_NoseUpClimb_DecreasesSpeed()
        {
            var model = new FlightModel(DefaultConfig());
            var tilt = new TiltInputState(roll: 0f, pitch: -1f, BrakeZone.Normal);

            var output = model.Step(tilt, brakeOnNegativePitch: true, deltaTime: 1f);

            Assert.Equal(2f, output.Speed, 0.001f); // 10 - 1*8 = 2 = minSpeed
            Assert.Equal(25f, output.PitchAngleDeg, 0.001f); // climbAmount=+1 → 機首上げ方向
        }

        [Fact]
        public void Step_SpeedBeyondSensitivityRange_ClampsToConfiguredBounds()
        {
            // pitchSpeedSensitivityが大きく、単純計算だと範囲を超える設定でクランプを確認
            var config = DefaultConfig(pitchSpeedSensitivity: 50f);
            var model = new FlightModel(config);
            var diveTilt = new TiltInputState(roll: 0f, pitch: 1f, BrakeZone.Normal);

            var output = model.Step(diveTilt, brakeOnNegativePitch: true, deltaTime: 10f);

            Assert.Equal(20f, output.Speed, 0.001f); // maxSpeedでクランプ
        }

        [Fact]
        public void Step_BrakeZone_DecelatesTowardStallSpeed()
        {
            var model = new FlightModel(DefaultConfig());
            var tilt = new TiltInputState(roll: 0f, pitch: -1f, BrakeZone.Brake);

            var output = model.Step(tilt, brakeOnNegativePitch: true, deltaTime: 1f);

            Assert.Equal(1f, output.Speed, 0.001f); // stallSpeedに到達(brakeDeceleration=40 * 1s は十分大きい)
        }

        [Fact]
        public void Step_SpeedConvergesGradually_NotInstantly()
        {
            var model = new FlightModel(DefaultConfig());
            var diveTilt = new TiltInputState(roll: 0f, pitch: 1f, BrakeZone.Normal);

            // deltaTimeが小さいと、1フレームでは目標速度(18)にまだ到達しない
            var output = model.Step(diveTilt, brakeOnNegativePitch: true, deltaTime: 0.1f);

            Assert.True(output.Speed > 10f);
            Assert.True(output.Speed < 18f);
        }

        [Fact]
        public void Step_ZeroDeltaTime_KeepsCurrentState()
        {
            var model = new FlightModel(DefaultConfig());
            var diveTilt = new TiltInputState(roll: 1f, pitch: 1f, BrakeZone.Normal);

            var output = model.Step(diveTilt, brakeOnNegativePitch: true, deltaTime: 0f);

            Assert.Equal(10f, output.Speed, 0.001f);
            Assert.Equal(0f, output.PitchAngleDeg, 0.001f);
            Assert.Equal(0f, output.BankAngleDeg, 0.001f);
            // ヨーレートは目標値への追従ではなく毎回即時反映
            Assert.Equal(90f, output.YawRateDegPerSecond, 0.001f);
        }
    }

    public class FlightConfigTests
    {
        [Theory]
        [InlineData(0f, 2f, 20f, 8f, 90f, 30f, 25f, 15f, 120f, 40f, 1f)] // baseSpeed=0
        [InlineData(10f, -1f, 20f, 8f, 90f, 30f, 25f, 15f, 120f, 40f, 1f)] // minSpeed負
        [InlineData(10f, 15f, 20f, 8f, 90f, 30f, 25f, 15f, 120f, 40f, 1f)] // minSpeed>baseSpeed
        [InlineData(10f, 2f, 5f, 8f, 90f, 30f, 25f, 15f, 120f, 40f, 1f)] // maxSpeed<baseSpeed
        [InlineData(10f, 2f, 20f, -1f, 90f, 30f, 25f, 15f, 120f, 40f, 1f)] // pitchSpeedSensitivity負
        [InlineData(10f, 2f, 20f, 8f, -1f, 30f, 25f, 15f, 120f, 40f, 1f)] // maxYawRate負
        [InlineData(10f, 2f, 20f, 8f, 90f, -1f, 25f, 15f, 120f, 40f, 1f)] // maxBankAngle負
        [InlineData(10f, 2f, 20f, 8f, 90f, 30f, -1f, 15f, 120f, 40f, 1f)] // maxPitchAngle負
        [InlineData(10f, 2f, 20f, 8f, 90f, 30f, 25f, 0f, 120f, 40f, 1f)] // speedAcceleration=0
        [InlineData(10f, 2f, 20f, 8f, 90f, 30f, 25f, 15f, 0f, 40f, 1f)] // attitudeRate=0
        [InlineData(10f, 2f, 20f, 8f, 90f, 30f, 25f, 15f, 120f, 0f, 1f)] // brakeDeceleration=0
        [InlineData(10f, 2f, 20f, 8f, 90f, 30f, 25f, 15f, 120f, 40f, 5f)] // stallSpeed>minSpeed
        public void Constructor_InvalidValues_Throws(
            float baseSpeed, float minSpeed, float maxSpeed, float pitchSpeedSensitivity,
            float maxYawRateDegPerSecond, float maxBankAngleDeg, float maxPitchAngleDeg,
            float speedAcceleration, float attitudeRateDegPerSecond, float brakeDeceleration, float stallSpeed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FlightConfig(
                baseSpeed, minSpeed, maxSpeed, pitchSpeedSensitivity, maxYawRateDegPerSecond,
                maxBankAngleDeg, maxPitchAngleDeg, speedAcceleration, attitudeRateDegPerSecond,
                brakeDeceleration, stallSpeed));
        }
    }
}
