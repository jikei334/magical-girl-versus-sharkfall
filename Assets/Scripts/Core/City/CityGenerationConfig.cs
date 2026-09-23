using System;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// CityGeneratorの動作パラメータ。全ての値はコンストラクタで検証される。
    /// </summary>
    public readonly struct CityGenerationConfig
    {
        /// <summary>中心ブロックから数えたグリッドの半径(ブロック数)。1以上。
        /// 例えば3なら、-3〜3の範囲で7x7個のブロックが生成される(中心はランドマーク用に予約)。</summary>
        public int GridExtent { get; }

        /// <summary>1ブロックの一辺の長さ(0より大きい値)。</summary>
        public float BlockSize { get; }

        /// <summary>ブロック間の道路幅(0以上)。ブロックの中心間隔はBlockSize+RoadWidthになる。</summary>
        public float RoadWidth { get; }

        /// <summary>建物の最低高さ(0より大きい値)。</summary>
        public float MinBuildingHeight { get; }

        /// <summary>建物の最高高さ(MinBuildingHeight以上)。</summary>
        public float MaxBuildingHeight { get; }

        /// <summary>ブロックサイズに対する建物設置面積の最小比率(0より大きく1以下)。</summary>
        public float MinFootprintRatio { get; }

        /// <summary>ブロックサイズに対する建物設置面積の最大比率(MinFootprintRatio以上1以下)。</summary>
        public float MaxFootprintRatio { get; }

        /// <summary>ランドマークタワーの高さ(MaxBuildingHeightより大きい値)。</summary>
        public float LandmarkHeight { get; }

        /// <summary>ブロックサイズに対するランドマークタワーの設置面積の比率(0より大きく1以下)。</summary>
        public float LandmarkFootprintRatio { get; }

        /// <summary>生成シード。同じ値を指定すれば同じ街が再現される。</summary>
        public int Seed { get; }

        /// <summary>
        /// 設定値を検証しつつ構築する。各引数の意味は同名プロパティのコメントを参照。
        /// 返り値: なし(コンストラクタ)
        /// 例外: いずれかの値が許容範囲外の場合、ArgumentOutOfRangeExceptionを投げる
        /// </summary>
        public CityGenerationConfig(
            int gridExtent,
            float blockSize,
            float roadWidth,
            float minBuildingHeight,
            float maxBuildingHeight,
            float minFootprintRatio,
            float maxFootprintRatio,
            float landmarkHeight,
            float landmarkFootprintRatio,
            int seed)
        {
            if (gridExtent < 1)
                throw new ArgumentOutOfRangeException(nameof(gridExtent), gridExtent, "gridExtentは1以上である必要があります。");
            if (blockSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "blockSizeは0より大きい必要があります。");
            if (roadWidth < 0f)
                throw new ArgumentOutOfRangeException(nameof(roadWidth), roadWidth, "roadWidthは0以上である必要があります。");
            if (minBuildingHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(minBuildingHeight), minBuildingHeight, "minBuildingHeightは0より大きい必要があります。");
            if (maxBuildingHeight < minBuildingHeight)
                throw new ArgumentOutOfRangeException(nameof(maxBuildingHeight), maxBuildingHeight, "maxBuildingHeightはminBuildingHeight以上である必要があります。");
            if (minFootprintRatio <= 0f || minFootprintRatio > 1f)
                throw new ArgumentOutOfRangeException(nameof(minFootprintRatio), minFootprintRatio, "minFootprintRatioは0より大きく1以下である必要があります。");
            if (maxFootprintRatio < minFootprintRatio || maxFootprintRatio > 1f)
                throw new ArgumentOutOfRangeException(nameof(maxFootprintRatio), maxFootprintRatio, "maxFootprintRatioはminFootprintRatio以上1以下である必要があります。");
            if (landmarkHeight <= maxBuildingHeight)
                throw new ArgumentOutOfRangeException(nameof(landmarkHeight), landmarkHeight, "landmarkHeightはmaxBuildingHeightより大きい必要があります。");
            if (landmarkFootprintRatio <= 0f || landmarkFootprintRatio > 1f)
                throw new ArgumentOutOfRangeException(nameof(landmarkFootprintRatio), landmarkFootprintRatio, "landmarkFootprintRatioは0より大きく1以下である必要があります。");

            GridExtent = gridExtent;
            BlockSize = blockSize;
            RoadWidth = roadWidth;
            MinBuildingHeight = minBuildingHeight;
            MaxBuildingHeight = maxBuildingHeight;
            MinFootprintRatio = minFootprintRatio;
            MaxFootprintRatio = maxFootprintRatio;
            LandmarkHeight = landmarkHeight;
            LandmarkFootprintRatio = landmarkFootprintRatio;
            Seed = seed;
        }
    }
}
