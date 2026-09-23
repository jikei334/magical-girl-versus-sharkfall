using System;
using System.Collections.Generic;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// 街に配置される建物1棟分のデータ。1棟は1〜複数個のBuildingBlockで構成される。
    /// </summary>
    public sealed class BuildingData
    {
        /// <summary>建物基準点のワールドX座標。</summary>
        public float X { get; }

        /// <summary>建物基準点のワールドZ座標。</summary>
        public float Z { get; }

        /// <summary>形状バリエーション。</summary>
        public BuildingShape Shape { get; }

        /// <summary>この建物を構成するブロック一覧(1個以上)。</summary>
        public IReadOnlyList<BuildingBlock> Blocks { get; }

        /// <summary>
        /// 引数: x, z - 建物基準点のワールド座標 / shape - 形状バリエーション / blocks - 構成ブロック一覧
        /// 返り値: なし(コンストラクタ)
        /// 例外: blocksがnullまたは空の場合、ArgumentExceptionを投げる
        /// </summary>
        public BuildingData(float x, float z, BuildingShape shape, IReadOnlyList<BuildingBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0)
                throw new ArgumentException("blocksは1個以上のBuildingBlockを含む必要があります。", nameof(blocks));

            X = x;
            Z = z;
            Shape = shape;
            Blocks = blocks;
        }
    }
}
