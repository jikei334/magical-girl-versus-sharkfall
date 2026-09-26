using System;

namespace MagicalGirl.Core.Enemy
{
    /// <summary>
    /// スクール(小型・群れ)のメンバー1体分の、リーダーからの相対オフセット。
    /// Unity側で、進行方向を基準にした座標系(Right=進行方向に対して右、Forward=進行方向側、
    /// 負の値でリーダーより後方)へ変換して使う想定。高度(上下)方向は編隊内で揃えたままにする。
    /// </summary>
    public readonly struct FormationOffset
    {
        /// <summary>進行方向に対して右方向のオフセット。</summary>
        public float Right { get; }

        /// <summary>進行方向に沿ったオフセット。負の値はリーダーより後方(編隊が追従する側)。</summary>
        public float Forward { get; }

        /// <summary>
        /// 引数: right, forward - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public FormationOffset(float right, float forward)
        {
            Right = right;
            Forward = forward;
        }
    }

    /// <summary>
    /// 編隊パターンから、各メンバーのリーダーに対する相対オフセットを生成するUnity非依存のロジック。
    /// </summary>
    public static class SchoolFormation
    {
        /// <summary>
        /// 編隊内の各メンバーのオフセットを生成する。0番目は常にリーダー自身(0,0)。
        /// 引数: config - 編隊の生成パラメータ
        /// 返り値: MemberCount個のFormationOffset配列
        /// </summary>
        public static FormationOffset[] GenerateOffsets(SchoolFormationConfig config)
        {
            switch (config.Pattern)
            {
                case FormationPattern.Line:
                    return GenerateLine(config);
                case FormationPattern.V:
                    return GenerateV(config);
                case FormationPattern.Grid:
                    return GenerateGrid(config);
                default:
                    throw new ArgumentOutOfRangeException(nameof(config), config.Pattern, "未対応のFormationPatternです。");
            }
        }

        /// <summary>進行方向に対して横一列に並べる(全員同じ奥行き)。</summary>
        static FormationOffset[] GenerateLine(SchoolFormationConfig config)
        {
            var offsets = new FormationOffset[config.MemberCount];
            var center = (config.MemberCount - 1) / 2f;
            for (var i = 0; i < config.MemberCount; i++)
                offsets[i] = new FormationOffset((i - center) * config.Spacing, 0f);
            return offsets;
        }

        /// <summary>先頭(リーダー)を頂点に、左右交互に後方へ広がるV字(渡り鳥型)。</summary>
        static FormationOffset[] GenerateV(SchoolFormationConfig config)
        {
            var offsets = new FormationOffset[config.MemberCount];
            offsets[0] = new FormationOffset(0f, 0f);

            for (var i = 1; i < config.MemberCount; i++)
            {
                var rank = (i + 1) / 2; // 1,1,2,2,3,3,...
                var side = i % 2 == 1 ? 1f : -1f;
                offsets[i] = new FormationOffset(side * rank * config.Spacing, -rank * config.Spacing);
            }
            return offsets;
        }

        /// <summary>できるだけ正方形に近い格子状に並べる(行ごとに奥行きをずらす)。</summary>
        static FormationOffset[] GenerateGrid(SchoolFormationConfig config)
        {
            var columns = (int)Math.Ceiling(Math.Sqrt(config.MemberCount));
            var offsets = new FormationOffset[config.MemberCount];

            for (var i = 0; i < config.MemberCount; i++)
            {
                var row = i / columns;
                var col = i % columns;

                // 最終行はメンバーが足りず偏ることがあるため、その行だけ列数に応じて中央寄せする。
                var itemsInRow = Math.Min(columns, config.MemberCount - row * columns);
                var rowCenter = (itemsInRow - 1) / 2f;

                offsets[i] = new FormationOffset((col - rowCenter) * config.Spacing, -row * config.Spacing);
            }
            return offsets;
        }
    }
}
