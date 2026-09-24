using System;
using System.Collections.Generic;

namespace MagicalGirl.Core.Gesture
{
    /// <summary>
    /// $1 Unistroke Recognizer(Wobbrock, Wilson &amp; Li, 2007)によるジェスチャー認識。
    /// タッチ軌跡を「再標本化 → 回転で始点方向を揃える → 正方形へ拡大縮小 → 重心を原点へ平行移動」
    /// の順で正規化し、登録済みテンプレートとの平均点間距離が最小になる回転角(黄金分割探索)を
    /// 求めて、最も距離が近いテンプレートを一致候補として返す。
    /// Unityに依存しないため、Unityを起動せずに単体テストできる。
    /// </summary>
    public sealed class UnistrokeRecognizer
    {
        const int ResamplePointCount = 64;
        const float SquareSize = 250f;
        // 縦一直線・横一直線の軌跡は、もう片方の辺の長さに対してこの比率未満なら「ほぼ0」とみなし、
        // その軸は拡大縮小しない(0除算や、回転処理の浮動小数点誤差で生じる極小値による
        // スケールの桁違いの爆発を防ぐ)。絶対値ではなく相対比率で判定することで、
        // テンプレート自体が小さい座標系で定義されていても正しく拡大縮小できるようにしている。
        const float FlatnessRatio = 0.01f;
        const float MinPathLength = 1e-6f;
        const int SmoothingWindowSize = 3;

        static readonly float GoldenRatio = 0.5f * (-1f + MathF.Sqrt(5f));
        // $1の原論文では±45°が一般的だが、円のように「どこから描き始めても正しい」閉じた形状は
        // ユーザーの描き始め位置(=始点の向き)がテンプレートの基準角度から45°以上ずれることが
        // 多く、探索範囲外になって一致しなくなる(直線フリックが逆方向のときも同様)。
        // 全周を探索範囲にすることで、描き始めの位置に依らず最適な回転を見つけられるようにする。
        static readonly float AngleSearchRangeRad = 180f * MathF.PI / 180f;
        static readonly float AngleSearchPrecisionRad = 2f * MathF.PI / 180f;
        static readonly float HalfDiagonal = 0.5f * MathF.Sqrt(SquareSize * SquareSize + SquareSize * SquareSize);

        readonly List<(GestureType Type, GesturePoint[] Points)> m_Templates = new();
        readonly float m_MatchThreshold;

        /// <summary>
        /// 組み込みテンプレート(直線・円)を登録して構築する。
        /// $1アルゴリズムは軌跡をなぞる向き(点の並び順)も一致条件に含まれるため、
        /// 直線・円それぞれについて、順方向と逆方向(右から左/時計回り)の両方を
        /// テンプレートとして登録し、なぞる向きに依らず同じコマンドとして認識されるようにする。
        /// 引数: matchThreshold - この値未満のスコアは不一致(詠唱失敗)として扱う(0〜1)
        /// 返り値: なし(コンストラクタ)
        /// 例外: matchThresholdが0〜1の範囲外の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public UnistrokeRecognizer(float matchThreshold = 0.7f)
        {
            if (matchThreshold < 0f || matchThreshold > 1f)
                throw new ArgumentOutOfRangeException(nameof(matchThreshold), matchThreshold, "matchThresholdは0以上1以下である必要があります。");

            m_MatchThreshold = matchThreshold;

            var lineTemplate = BuildLineTemplate();
            AddTemplate(GestureType.Line, lineTemplate);
            AddTemplate(GestureType.Line, Reversed(lineTemplate));

            var circleTemplate = BuildCircleTemplate();
            AddTemplate(GestureType.Circle, circleTemplate);
            AddTemplate(GestureType.Circle, Reversed(circleTemplate));
        }

        /// <summary>点列の並び順を逆にした配列を返す(なぞる向きが逆のテンプレートを作るため)。</summary>
        static GesturePoint[] Reversed(GesturePoint[] points)
        {
            var reversed = new GesturePoint[points.Length];
            for (var i = 0; i < points.Length; i++)
                reversed[i] = points[points.Length - 1 - i];
            return reversed;
        }

        /// <summary>
        /// テンプレートを追加する(正規化してから保存する)。将来のジェスチャー種別追加(ジグザグ等)に使う。
        /// 引数: type - ジェスチャー種別 / rawPoints - テンプレートの生の点列(2点以上必要)
        /// 返り値: なし
        /// 例外: rawPointsがnullまたは2点未満の場合、ArgumentExceptionを投げる
        /// </summary>
        public void AddTemplate(GestureType type, IReadOnlyList<GesturePoint> rawPoints)
        {
            if (rawPoints == null || rawPoints.Count < 2)
                throw new ArgumentException("rawPointsは2点以上である必要があります。", nameof(rawPoints));

            var normalized = Normalize(rawPoints);
            m_Templates.Add((type, normalized));
        }

        /// <summary>
        /// タッチ軌跡を認識する。
        /// 引数: rawPoints - タッチ開始から終了までの生の点列(2点以上必要)
        /// 返り値: 最も近いテンプレートとの一致結果。閾値未満、または軌跡がほぼ動いていない
        ///          (タップ等)場合はIsMatch=falseになる
        /// 例外: rawPointsがnullまたは2点未満の場合、ArgumentExceptionを投げる
        /// </summary>
        public GestureRecognitionResult Recognize(IReadOnlyList<GesturePoint> rawPoints)
        {
            if (rawPoints == null || rawPoints.Count < 2)
                throw new ArgumentException("rawPointsは2点以上である必要があります。", nameof(rawPoints));

            // ほぼ動いていない(タップ等)軌跡は、再標本化の数値不安定を避けるため早期に不一致として扱う。
            if (PathLength(rawPoints) < MinPathLength)
                return new GestureRecognitionResult(false, default, 0f);

            var candidate = Normalize(rawPoints);

            var bestDistance = float.PositiveInfinity;
            var bestType = default(GestureType);

            foreach (var (type, templatePoints) in m_Templates)
            {
                var distance = DistanceAtBestAngle(candidate, templatePoints);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestType = type;
                }
            }

            var score = 1f - bestDistance / HalfDiagonal;
            return new GestureRecognitionResult(score >= m_MatchThreshold, bestType, score);
        }

        /// <summary>
        /// 平滑化・再標本化・回転・拡大縮小・平行移動を行い、比較可能な形に正規化する。
        /// 引数: rawPoints - 正規化前の点列(2点以上、経路長0より大きいことを呼び出し側で保証する)
        /// 返り値: 正規化された点列(ResamplePointCount個)
        /// </summary>
        static GesturePoint[] Normalize(IReadOnlyList<GesturePoint> rawPoints)
        {
            var smoothed = Smooth(rawPoints, SmoothingWindowSize);
            var points = Resample(smoothed, ResamplePointCount);
            var indicativeAngle = IndicativeAngle(points);
            points = RotateBy(points, -indicativeAngle);
            points = ScaleToSquare(points, SquareSize);
            points = TranslateToOrigin(points);
            return points;
        }

        /// <summary>
        /// 移動平均で軌跡を軽く平滑化する。短いフリックほど、指のわずかなブレが軌跡全体に対して
        /// 相対的に大きくなり直線らしさのスコアが下がりやすいため、細かいブレをならして
        /// 長さに依らず安定して認識できるようにする。ウィンドウは小さめにし、意図した形状の
        /// 大きな変化(円のカーブ等)までなだらかにし過ぎないようにしている。
        /// 引数: points - 平滑化前の点列 / windowSize - 移動平均の窓幅(奇数を想定)
        /// 返り値: 平滑化後の点列(点数は変えない)
        /// </summary>
        static GesturePoint[] Smooth(IReadOnlyList<GesturePoint> points, int windowSize)
        {
            if (windowSize <= 1 || points.Count <= 2)
                return ToArray(points);

            var half = windowSize / 2;
            var result = new GesturePoint[points.Count];

            for (var i = 0; i < points.Count; i++)
            {
                var start = Math.Max(0, i - half);
                var end = Math.Min(points.Count - 1, i + half);

                var sumX = 0f;
                var sumY = 0f;
                for (var j = start; j <= end; j++)
                {
                    sumX += points[j].X;
                    sumY += points[j].Y;
                }
                var count = end - start + 1;
                result[i] = new GesturePoint(sumX / count, sumY / count);
            }

            return result;
        }

        /// <summary>IReadOnlyList&lt;GesturePoint&gt;を配列にコピーする。</summary>
        static GesturePoint[] ToArray(IReadOnlyList<GesturePoint> points)
        {
            var result = new GesturePoint[points.Count];
            for (var i = 0; i < points.Count; i++)
                result[i] = points[i];
            return result;
        }

        /// <summary>
        /// 点列を、始点から等間隔になるようn個の点に再標本化する。
        /// 引数: points - 元の点列 / n - 再標本化後の点数
        /// 返り値: 再標本化された点列(n個)
        /// </summary>
        static GesturePoint[] Resample(IReadOnlyList<GesturePoint> points, int n)
        {
            var interval = PathLength(points) / (n - 1);
            var accumulated = 0f;

            var source = new List<GesturePoint>(points);
            var result = new List<GesturePoint>(n) { source[0] };

            for (var i = 1; i < source.Count; i++)
            {
                var d = Distance(source[i - 1], source[i]);
                if (d > 0f && accumulated + d >= interval)
                {
                    var t = (interval - accumulated) / d;
                    var q = new GesturePoint(
                        source[i - 1].X + t * (source[i].X - source[i - 1].X),
                        source[i - 1].Y + t * (source[i].Y - source[i - 1].Y));
                    result.Add(q);
                    source.Insert(i, q); // 次の区間の始点として使うため、qをこの位置に割り込ませる
                    accumulated = 0f;
                }
                else
                {
                    accumulated += d;
                }
            }

            // 丸め誤差で1点足りないことがあるため、最後の点で埋める
            while (result.Count < n)
                result.Add(source[source.Count - 1]);

            return result.ToArray();
        }

        /// <summary>
        /// 重心から始点へ向かう角度(始点の「向き」)を求める。
        /// 引数: points - 点列
        /// 返り値: 角度(ラジアン)
        /// </summary>
        static float IndicativeAngle(IReadOnlyList<GesturePoint> points)
        {
            var c = Centroid(points);
            return MathF.Atan2(c.Y - points[0].Y, c.X - points[0].X);
        }

        /// <summary>
        /// 点列を重心を中心にθラジアン回転させる。
        /// 引数: points - 点列 / theta - 回転角(ラジアン)
        /// 返り値: 回転後の点列
        /// </summary>
        static GesturePoint[] RotateBy(IReadOnlyList<GesturePoint> points, float theta)
        {
            var c = Centroid(points);
            var cos = MathF.Cos(theta);
            var sin = MathF.Sin(theta);

            var result = new GesturePoint[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var dx = points[i].X - c.X;
                var dy = points[i].Y - c.Y;
                result[i] = new GesturePoint(
                    dx * cos - dy * sin + c.X,
                    dx * sin + dy * cos + c.Y);
            }
            return result;
        }

        /// <summary>
        /// 点列をバウンディングボックスに合わせて正方形サイズへ拡大縮小する(縦横独立、アスペクト比は保持しない)。
        /// 幅または高さがほぼ0(縦一直線・横一直線の軌跡)の場合、その軸は拡大縮小せずそのまま保つ
        /// (0除算・無限大化を避けるため)。
        /// 引数: points - 点列 / size - 目標サイズ
        /// 返り値: 拡大縮小後の点列
        /// </summary>
        static GesturePoint[] ScaleToSquare(IReadOnlyList<GesturePoint> points, float size)
        {
            var (minX, minY, maxX, maxY) = BoundingBox(points);
            var width = maxX - minX;
            var height = maxY - minY;
            var flatnessEpsilon = Math.Max(width, height) * FlatnessRatio;

            var scaleX = width > flatnessEpsilon ? size / width : 1f;
            var scaleY = height > flatnessEpsilon ? size / height : 1f;

            var result = new GesturePoint[points.Count];
            for (var i = 0; i < points.Count; i++)
                result[i] = new GesturePoint(points[i].X * scaleX, points[i].Y * scaleY);
            return result;
        }

        /// <summary>
        /// 点列の重心が原点(0,0)になるよう平行移動する。
        /// 引数: points - 点列
        /// 返り値: 平行移動後の点列
        /// </summary>
        static GesturePoint[] TranslateToOrigin(IReadOnlyList<GesturePoint> points)
        {
            var c = Centroid(points);
            var result = new GesturePoint[points.Count];
            for (var i = 0; i < points.Count; i++)
                result[i] = new GesturePoint(points[i].X - c.X, points[i].Y - c.Y);
            return result;
        }

        /// <summary>
        /// 黄金分割探索で、pointsをどれだけ回転させればtemplateに最も近づくかを求め、そのときの距離を返す。
        /// 引数: points - 正規化済みの候補点列 / template - 正規化済みのテンプレート点列
        /// 返り値: 探索範囲内で最小の平均点間距離
        /// </summary>
        static float DistanceAtBestAngle(GesturePoint[] points, GesturePoint[] template)
        {
            var thetaA = -AngleSearchRangeRad;
            var thetaB = AngleSearchRangeRad;

            var x1 = GoldenRatio * thetaA + (1f - GoldenRatio) * thetaB;
            var f1 = DistanceAtAngle(points, template, x1);
            var x2 = (1f - GoldenRatio) * thetaA + GoldenRatio * thetaB;
            var f2 = DistanceAtAngle(points, template, x2);

            while (MathF.Abs(thetaB - thetaA) > AngleSearchPrecisionRad)
            {
                if (f1 < f2)
                {
                    thetaB = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = GoldenRatio * thetaA + (1f - GoldenRatio) * thetaB;
                    f1 = DistanceAtAngle(points, template, x1);
                }
                else
                {
                    thetaA = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = (1f - GoldenRatio) * thetaA + GoldenRatio * thetaB;
                    f2 = DistanceAtAngle(points, template, x2);
                }
            }

            return MathF.Min(f1, f2);
        }

        /// <summary>
        /// pointsをtheta回転させた状態でtemplateとの距離を求める。
        /// 引数: points, template - 比較する2つの点列(同じ点数) / theta - 回転角(ラジアン)
        /// 返り値: 平均点間距離
        /// </summary>
        static float DistanceAtAngle(GesturePoint[] points, GesturePoint[] template, float theta)
        {
            var rotated = RotateBy(points, theta);
            return PathDistance(rotated, template);
        }

        /// <summary>
        /// 同じ点数を持つ2つの点列について、対応する点同士の距離の平均を求める。
        /// 引数: a, b - 比較する2つの点列(同じ点数)
        /// 返り値: 平均点間距離
        /// </summary>
        static float PathDistance(IReadOnlyList<GesturePoint> a, IReadOnlyList<GesturePoint> b)
        {
            var sum = 0f;
            for (var i = 0; i < a.Count; i++)
                sum += Distance(a[i], b[i]);
            return sum / a.Count;
        }

        /// <summary>点列全体の重心を求める。</summary>
        static GesturePoint Centroid(IReadOnlyList<GesturePoint> points)
        {
            var sumX = 0f;
            var sumY = 0f;
            foreach (var p in points)
            {
                sumX += p.X;
                sumY += p.Y;
            }
            return new GesturePoint(sumX / points.Count, sumY / points.Count);
        }

        /// <summary>点列のバウンディングボックス(minX, minY, maxX, maxY)を求める。</summary>
        static (float minX, float minY, float maxX, float maxY) BoundingBox(IReadOnlyList<GesturePoint> points)
        {
            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;

            foreach (var p in points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            return (minX, minY, maxX, maxY);
        }

        /// <summary>点列の総経路長(各点間の距離の合計)を求める。</summary>
        static float PathLength(IReadOnlyList<GesturePoint> points)
        {
            var length = 0f;
            for (var i = 1; i < points.Count; i++)
                length += Distance(points[i - 1], points[i]);
            return length;
        }

        /// <summary>2点間のユークリッド距離を求める。</summary>
        static float Distance(GesturePoint a, GesturePoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 直線(横一直線)の組み込みテンプレートを生成する。回転正規化されるため、
        /// テンプレート自体の絶対的な向きは認識結果に影響しない。
        /// </summary>
        static GesturePoint[] BuildLineTemplate()
        {
            return new[] { new GesturePoint(0f, 0f), new GesturePoint(1f, 0f) };
        }

        /// <summary>円(1周)の組み込みテンプレートを生成する。</summary>
        static GesturePoint[] BuildCircleTemplate()
        {
            const int count = 32;
            var points = new GesturePoint[count];
            for (var i = 0; i < count; i++)
            {
                var angle = (float)i / (count - 1) * MathF.PI * 2f;
                points[i] = new GesturePoint(MathF.Cos(angle), MathF.Sin(angle));
            }
            return points;
        }
    }
}
