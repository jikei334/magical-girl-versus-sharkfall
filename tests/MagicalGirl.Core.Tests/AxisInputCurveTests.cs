using System;
using MagicalGirl.Core.Input;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class AxisInputCurveTests
    {
        [Theory]
        [InlineData(0f)]
        [InlineData(0.05f)]
        [InlineData(-0.05f)]
        public void Apply_WithinDeadzone_ReturnsZero(float rawValue)
        {
            var result = AxisInputCurve.Apply(rawValue, deadzone: 0.1f, exponent: 1f);
            Assert.Equal(0f, result);
        }

        [Fact]
        public void Apply_AtFullScale_ReturnsOne()
        {
            var result = AxisInputCurve.Apply(1f, deadzone: 0.1f, exponent: 1f);
            Assert.Equal(1f, result, 0.00001f);
        }

        [Fact]
        public void Apply_NegativeFullScale_ReturnsNegativeOne()
        {
            var result = AxisInputCurve.Apply(-1f, deadzone: 0.1f, exponent: 1f);
            Assert.Equal(-1f, result, 0.00001f);
        }

        [Fact]
        public void Apply_ValueBeyondRange_IsClamped()
        {
            var result = AxisInputCurve.Apply(2f, deadzone: 0.1f, exponent: 1f);
            Assert.Equal(1f, result, 0.00001f);
        }

        [Fact]
        public void Apply_LinearMidpoint_IsHalfway()
        {
            // deadzone=0のとき、線形カーブ(exponent=1)なら入力の半分は出力も半分になる
            var result = AxisInputCurve.Apply(0.5f, deadzone: 0f, exponent: 1f);
            Assert.Equal(0.5f, result, 0.00001f);
        }

        [Fact]
        public void Apply_ExponentGreaterThanOne_DampensMidRange()
        {
            var linear = AxisInputCurve.Apply(0.5f, deadzone: 0f, exponent: 1f);
            var curved = AxisInputCurve.Apply(0.5f, deadzone: 0f, exponent: 2f);
            Assert.True(curved < linear, "指数が1より大きい場合、中間値では線形より出力が小さくなるはず");
        }

        [Theory]
        [InlineData(-0.1f)]
        [InlineData(1f)]
        public void Apply_InvalidDeadzone_Throws(float deadzone)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AxisInputCurve.Apply(0.5f, deadzone, exponent: 1f));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void Apply_InvalidExponent_Throws(float exponent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AxisInputCurve.Apply(0.5f, deadzone: 0.1f, exponent));
        }
    }
}
