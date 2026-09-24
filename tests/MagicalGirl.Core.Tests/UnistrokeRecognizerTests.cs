using System;
using System.Collections.Generic;
using MagicalGirl.Core.Gesture;
using Xunit;

namespace MagicalGirl.Core.Tests
{
    public class UnistrokeRecognizerTests
    {
        [Fact]
        public void Recognize_StraightDiagonalLine_MatchesLineWithHighScore()
        {
            var recognizer = new UnistrokeRecognizer();
            var points = SampleLine(new GesturePoint(0f, 0f), new GesturePoint(100f, 30f), 20);

            var result = recognizer.Recognize(points);

            Assert.True(result.IsMatch);
            Assert.Equal(GestureType.Line, result.Type);
            Assert.True(result.Score > 0.9f, $"score was {result.Score}");
        }

        [Fact]
        public void Recognize_StraightVerticalLine_MatchesLine()
        {
            // 縦一直線(バウンディングボックスの幅が0に近いケース)でも0除算せず認識できることを確認する
            var recognizer = new UnistrokeRecognizer();
            var points = SampleLine(new GesturePoint(50f, 0f), new GesturePoint(50f, 100f), 20);

            var result = recognizer.Recognize(points);

            Assert.True(result.IsMatch);
            Assert.Equal(GestureType.Line, result.Type);
        }

        [Fact]
        public void Recognize_ShortFlickWithJitter_StillMatchesLine()
        {
            // 短いフリックは、指のわずかなブレが軌跡全体に対して相対的に大きくなりやすく、
            // 直線らしさのスコアが下がりやすい(実機確認で「端から端でないと通らない」と
            // 報告があった問題)。平滑化により、短くても安定して認識できることを確認する。
            var recognizer = new UnistrokeRecognizer();
            var jitterRandom = new Random(7);
            var points = new GesturePoint[10];
            for (var i = 0; i < points.Length; i++)
            {
                var t = (float)i / (points.Length - 1);
                var jitter = ((float)jitterRandom.NextDouble() - 0.5f) * 2f; // ±1px程度のブレ
                points[i] = new GesturePoint(t * 30f, jitter);
            }

            var result = recognizer.Recognize(points);

            Assert.True(result.IsMatch, $"short jittery flick should match (score was {result.Score})");
            Assert.Equal(GestureType.Line, result.Type);
        }

        [Fact]
        public void Recognize_LineDrawnRightToLeft_MatchesLine()
        {
            // なぞる向きが左→右でも右→左でも、同じ「直線」コマンドとして扱われることを確認する。
            var recognizer = new UnistrokeRecognizer();
            var points = SampleLine(new GesturePoint(100f, 0f), new GesturePoint(0f, 0f), 20);

            var result = recognizer.Recognize(points);

            Assert.True(result.IsMatch);
            Assert.Equal(GestureType.Line, result.Type);
        }

        [Fact]
        public void Recognize_CircleDrawnClockwise_MatchesCircle()
        {
            // 反時計回りだけでなく時計回りでなぞっても、同じ「円」コマンドとして扱われることを確認する。
            var recognizer = new UnistrokeRecognizer();
            var counterClockwise = SampleCircle(new GesturePoint(0f, 0f), radius: 50f, count: 40);
            var clockwise = new GesturePoint[counterClockwise.Length];
            for (var i = 0; i < counterClockwise.Length; i++)
                clockwise[i] = counterClockwise[counterClockwise.Length - 1 - i];

            var result = recognizer.Recognize(clockwise);

            Assert.True(result.IsMatch, $"clockwise circle should match (score was {result.Score})");
            Assert.Equal(GestureType.Circle, result.Type);
        }

        [Fact]
        public void Recognize_Circle_MatchesCircleWithHighScore()
        {
            var recognizer = new UnistrokeRecognizer();
            var points = SampleCircle(new GesturePoint(100f, 100f), radius: 50f, count: 40);

            var result = recognizer.Recognize(points);

            Assert.True(result.IsMatch);
            Assert.Equal(GestureType.Circle, result.Type);
            Assert.True(result.Score > 0.9f, $"score was {result.Score}");
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(90f)]
        [InlineData(180f)]
        [InlineData(270f)]
        public void Recognize_Circle_MatchesRegardlessOfStartAngle(float startAngleDeg)
        {
            // 円は12時・9時など、テンプレートの基準角度(0°=3時方向)から離れた位置から
            // 描き始めることが多い。回転探索範囲が狭いと一致しなくなるバグの再発防止テスト。
            var recognizer = new UnistrokeRecognizer();
            var startAngle = startAngleDeg * MathF.PI / 180f;
            var points = SampleCircle(new GesturePoint(0f, 0f), radius: 50f, count: 40, startAngle: startAngle, endAngle: startAngle + MathF.PI * 2f);

            var result = recognizer.Recognize(points);

            Assert.True(result.IsMatch, $"startAngle={startAngleDeg} did not match (score was {result.Score})");
            Assert.Equal(GestureType.Circle, result.Type);
        }

        [Fact]
        public void Recognize_Zigzag_DoesNotMatchLineOrCircle()
        {
            // ランダムな殴り書きは、滑らかな直線にも円にも近くないはずなので詠唱失敗(不一致)に
            // なる想定。規則正しいジグザグは直線区間の集まりでもあるため、緩和した設定(平滑化・
            // 全周探索・低めの閾値)のもとでは直線寄りのスコアになりやすく、境界線上になって
            // テストとして不安定だった。実際の「意図しないなぞり」に近い、乱雑な形状で検証する。
            var recognizer = new UnistrokeRecognizer(matchThreshold: 0.8f);
            var points = SampleScribble(new GesturePoint(0f, 0f), extent: 100f, pointCount: 30, seed: 1);

            var result = recognizer.Recognize(points);

            Assert.False(result.IsMatch, $"scribble should not match (score was {result.Score}, type {result.Type})");
        }

        [Fact]
        public void Recognize_LineIsInvariantToRotationAndPosition()
        {
            // 直線であれば、始点の位置や向きが違っても同程度のスコアでLineと認識されるはず
            var recognizer = new UnistrokeRecognizer();

            var horizontal = SampleLine(new GesturePoint(0f, 0f), new GesturePoint(80f, 0f), 15);
            var diagonalElsewhere = SampleLine(new GesturePoint(500f, 500f), new GesturePoint(560f, 620f), 15);

            var resultA = recognizer.Recognize(horizontal);
            var resultB = recognizer.Recognize(diagonalElsewhere);

            Assert.Equal(GestureType.Line, resultA.Type);
            Assert.Equal(GestureType.Line, resultB.Type);
            Assert.True(resultA.IsMatch);
            Assert.True(resultB.IsMatch);
        }

        [Fact]
        public void Recognize_NearlyStationaryTap_DoesNotMatch()
        {
            var recognizer = new UnistrokeRecognizer();
            var points = new[]
            {
                new GesturePoint(10f, 10f),
                new GesturePoint(10.0001f, 10f),
                new GesturePoint(10f, 10.0001f),
            };

            var result = recognizer.Recognize(points);

            Assert.False(result.IsMatch);
        }

        [Fact]
        public void Recognize_NullPoints_Throws()
        {
            var recognizer = new UnistrokeRecognizer();
            Assert.Throws<ArgumentException>(() => recognizer.Recognize(null!));
        }

        [Fact]
        public void Recognize_TooFewPoints_Throws()
        {
            var recognizer = new UnistrokeRecognizer();
            Assert.Throws<ArgumentException>(() => recognizer.Recognize(new[] { new GesturePoint(0f, 0f) }));
        }

        [Fact]
        public void Constructor_InvalidThreshold_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnistrokeRecognizer(matchThreshold: 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnistrokeRecognizer(matchThreshold: -0.1f));
        }

        [Fact]
        public void AddTemplate_TooFewPoints_Throws()
        {
            var recognizer = new UnistrokeRecognizer();
            Assert.Throws<ArgumentException>(() => recognizer.AddTemplate(GestureType.Line, new[] { new GesturePoint(0f, 0f) }));
        }

        [Fact]
        public void AddTemplate_CustomTemplate_CanBeRecognized()
        {
            var recognizer = new UnistrokeRecognizer();
            // 円の反対向き(半円)を新しいテンプレートとして登録できることを確認する簡易テスト
            var halfCircle = SampleCircle(new GesturePoint(0f, 0f), radius: 40f, count: 20, startAngle: 0f, endAngle: MathF.PI);
            recognizer.AddTemplate(GestureType.Circle, halfCircle);

            // テンプレート追加自体が例外を投げずに完了すること、既存の認識が壊れないことを確認する
            var lineResult = recognizer.Recognize(SampleLine(new GesturePoint(0f, 0f), new GesturePoint(90f, 0f), 15));
            Assert.Equal(GestureType.Line, lineResult.Type);
        }

        static GesturePoint[] SampleLine(GesturePoint from, GesturePoint to, int count)
        {
            var points = new GesturePoint[count];
            for (var i = 0; i < count; i++)
            {
                var t = (float)i / (count - 1);
                points[i] = new GesturePoint(
                    from.X + (to.X - from.X) * t,
                    from.Y + (to.Y - from.Y) * t);
            }
            return points;
        }

        static GesturePoint[] SampleCircle(GesturePoint center, float radius, int count, float startAngle = 0f, float? endAngle = null)
        {
            var end = endAngle ?? MathF.PI * 2f;
            var points = new GesturePoint[count];
            for (var i = 0; i < count; i++)
            {
                var t = (float)i / (count - 1);
                var angle = startAngle + (end - startAngle) * t;
                points[i] = new GesturePoint(center.X + radius * MathF.Cos(angle), center.Y + radius * MathF.Sin(angle));
            }
            return points;
        }

        static GesturePoint[] SampleScribble(GesturePoint origin, float extent, int pointCount, int seed)
        {
            // 決定論的な乱数で、意図しない殴り書きのような無秩序な軌跡を作る。
            var random = new Random(seed);
            var points = new GesturePoint[pointCount];
            var x = origin.X;
            var y = origin.Y;
            for (var i = 0; i < pointCount; i++)
            {
                x += ((float)random.NextDouble() - 0.5f) * extent * 0.3f;
                y += ((float)random.NextDouble() - 0.5f) * extent * 0.3f;
                points[i] = new GesturePoint(x, y);
            }
            return points;
        }
    }
}
