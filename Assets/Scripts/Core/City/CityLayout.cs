using System;
using System.Collections.Generic;

namespace MagicalGirl.Core.City
{
    /// <summary>
    /// CityGeneratorが生成した街全体のレイアウト。
    /// </summary>
    public sealed class CityLayout
    {
        /// <summary>中心ブロックに配置されるランドマークタワー。ボス戦の舞台。</summary>
        public BuildingData Landmark { get; }

        /// <summary>ランドマーク以外の建物一覧。</summary>
        public IReadOnlyList<BuildingData> Buildings { get; }

        /// <summary>
        /// 引数: landmark - ランドマークタワー / buildings - ランドマーク以外の建物一覧
        /// 返り値: なし(コンストラクタ)
        /// 例外: landmarkまたはbuildingsがnullの場合、ArgumentNullExceptionを投げる
        /// </summary>
        public CityLayout(BuildingData landmark, IReadOnlyList<BuildingData> buildings)
        {
            Landmark = landmark ?? throw new ArgumentNullException(nameof(landmark));
            Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        }
    }
}
