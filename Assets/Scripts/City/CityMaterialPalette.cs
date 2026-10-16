using UnityEngine;

namespace MagicalGirl.City
{
    /// <summary>
    /// 街の外壁・屋上・ランドマーク用マテリアルの一式。CityBuilder.Build()に渡すと、
    /// 建物ごとに(位置から決定的に)外壁マテリアルを選んで割り当てる。
    /// </summary>
    public readonly struct CityMaterialPalette
    {
        /// <summary>通常の建物に使う外壁マテリアルの候補(建物ごとにこの中から1つ選ばれる)。</summary>
        public Material[] FacadeMaterials { get; }

        /// <summary>建物の屋上(各ブロック最上面)に使うマテリアル。</summary>
        public Material RoofMaterial { get; }

        /// <summary>ランドマークタワー専用の外壁マテリアル。</summary>
        public Material LandmarkMaterial { get; }

        /// <summary>
        /// 引数: facadeMaterials, roofMaterial, landmarkMaterial - 各プロパティの値
        /// 返り値: なし(コンストラクタ)
        /// </summary>
        public CityMaterialPalette(Material[] facadeMaterials, Material roofMaterial, Material landmarkMaterial)
        {
            FacadeMaterials = facadeMaterials;
            RoofMaterial = roofMaterial;
            LandmarkMaterial = landmarkMaterial;
        }
    }
}
