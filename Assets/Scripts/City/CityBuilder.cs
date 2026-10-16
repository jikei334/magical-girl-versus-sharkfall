using System;
using MagicalGirl.Core.City;
using UnityEngine;

namespace MagicalGirl.City
{
    /// <summary>
    /// CityGeneratorが生成したCityLayoutを、実際のUnity GameObject(直方体プリミティブの組み合わせ)
    /// として配置するユーティリティ。各ブロックはGameObject.CreatePrimitive(Cube)で生成するため、
    /// MeshFilter・MeshRenderer・BoxColliderが自動的に付与される。CityMaterialPaletteを渡すと、
    /// 建物ごとに(位置から決定的に)外壁マテリアルを選んで割り当て、各ブロックの屋上に屋上用
    /// マテリアルの薄い天板(RoofCap)を追加する。
    /// </summary>
    public static class CityBuilder
    {
        // 外壁テクスチャ1タイルあたりの目安サイズ(単位)。ブロックの幅・高さに応じてタイリング
        // 回数を変え、窓が間延び/圧縮して見えないようにする。
        const float FacadeTileSize = 8f;

        // 屋上キャップ(天板)の厚み。
        const float RoofCapThickness = 0.5f;

        /// <summary>
        /// CityLayoutからGameObject群を生成し、指定した親の下に配置する。
        /// 引数: layout - 生成済みの都市レイアウト / parent - 配置先の親Transform(nullなら未配置)
        ///        / materials - 外壁・屋上・ランドマーク用マテリアル一式
        /// 返り値: 生成した街のルートGameObject("GeneratedCity")
        /// 例外: layoutがnullの場合、ArgumentNullExceptionを投げる
        /// </summary>
        public static GameObject Build(CityLayout layout, Transform parent, CityMaterialPalette materials)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var root = new GameObject("GeneratedCity");
            if (parent != null)
                root.transform.SetParent(parent, false);

            foreach (var building in layout.Buildings)
                BuildBuilding(building, root.transform, "Building", SelectFacadeMaterial(building, materials), materials.RoofMaterial);

            BuildBuilding(layout.Landmark, root.transform, "Landmark", materials.LandmarkMaterial, materials.RoofMaterial);

            return root;
        }

        /// <summary>
        /// 建物の位置から、外壁マテリアル候補の中から1つを決定的に選ぶ(CityGenerator側のシードで
        /// 建物配置自体が決定的なため、同じ街なら毎回同じ組み合わせになる)。
        /// 引数: building - 対象の建物データ / materials - マテリアル一式
        /// 返り値: 選ばれた外壁マテリアル(候補が無ければnull)
        /// </summary>
        static Material SelectFacadeMaterial(BuildingData building, CityMaterialPalette materials)
        {
            if (materials.FacadeMaterials == null || materials.FacadeMaterials.Length == 0)
                return null;

            var hash = HashCode.Combine(building.X, building.Z);
            var index = (int)((uint)hash % (uint)materials.FacadeMaterials.Length);
            return materials.FacadeMaterials[index];
        }

        /// <summary>
        /// 建物1棟分のGameObjectを生成する(基準点用の空GameObjectの下に、構成ブロックごとの
        /// Cubeプリミティブと屋上キャップをぶら下げる)。
        /// 引数: building - 生成する建物データ / parent - 配置先の親Transform / namePrefix - GameObject名の接頭辞
        ///        / facadeMaterial - この建物の外壁マテリアル(nullなら未設定のまま=デフォルトの白)
        ///        / roofMaterial - 屋上キャップのマテリアル(nullなら屋上キャップを生成しない)
        /// 返り値: なし
        /// </summary>
        static void BuildBuilding(BuildingData building, Transform parent, string namePrefix, Material facadeMaterial, Material roofMaterial)
        {
            var buildingGo = new GameObject($"{namePrefix}_{building.X:F0}_{building.Z:F0}");
            buildingGo.transform.SetParent(parent, false);
            buildingGo.transform.localPosition = new Vector3(building.X, 0f, building.Z);

            foreach (var block in building.Blocks)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Block";
                cube.transform.SetParent(buildingGo.transform, false);
                cube.transform.localPosition = new Vector3(block.OffsetX, block.BaseHeight + block.Height / 2f, block.OffsetZ);
                cube.transform.localScale = new Vector3(block.Width, block.Height, block.Depth);

                if (facadeMaterial != null)
                    ApplyFacadeMaterial(cube, block, facadeMaterial);

                if (roofMaterial != null)
                    BuildRoofCap(buildingGo.transform, block, roofMaterial);
            }
        }

        /// <summary>
        /// ブロックに外壁マテリアルを割り当て、ブロックのサイズに応じたタイリング回数を
        /// MaterialPropertyBlock経由で設定する(マテリアル自体はブロック間で共有したいため、
        /// インスタンスごとにMaterialを複製するのではなくPropertyBlockでタイリングだけ上書きする)。
        /// 引数: cube - 対象のブロックGameObject / block - サイズ情報 / facadeMaterial - 割り当てる外壁マテリアル
        /// 返り値: なし
        /// </summary>
        static void ApplyFacadeMaterial(GameObject cube, BuildingBlock block, Material facadeMaterial)
        {
            var renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = facadeMaterial;

            var tilingX = Mathf.Max(1f, Mathf.Round(Mathf.Max(block.Width, block.Depth) / FacadeTileSize));
            var tilingY = Mathf.Max(1f, Mathf.Round(block.Height / FacadeTileSize));

            var props = new MaterialPropertyBlock();
            props.SetVector("_MainTex_ST", new Vector4(tilingX, tilingY, 0f, 0f));
            renderer.SetPropertyBlock(props);
        }

        /// <summary>
        /// ブロックの真上に、屋上マテリアルを貼った薄いCubeを重ねる(天板)。単一のCubeメッシュは
        /// 面ごとに別マテリアルを持てないため、別オブジェクトとして重ねることで屋上だけ違う
        /// 見た目にする。
        /// 引数: buildingParent - 建物基準点のTransform(block.OffsetX/Z等はこれ基準のローカル座標)
        ///        / block - 対象ブロック / roofMaterial - 屋上マテリアル
        /// 返り値: なし
        /// </summary>
        static void BuildRoofCap(Transform buildingParent, BuildingBlock block, Material roofMaterial)
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "RoofCap";
            cap.transform.SetParent(buildingParent, false);
            cap.transform.localPosition = new Vector3(block.OffsetX, block.BaseHeight + block.Height + RoofCapThickness / 2f, block.OffsetZ);
            cap.transform.localScale = new Vector3(block.Width, RoofCapThickness, block.Depth);

            var renderer = cap.GetComponent<Renderer>();
            renderer.sharedMaterial = roofMaterial;

            var tilingX = Mathf.Max(1f, Mathf.Round(block.Width / FacadeTileSize));
            var tilingZ = Mathf.Max(1f, Mathf.Round(block.Depth / FacadeTileSize));

            var props = new MaterialPropertyBlock();
            props.SetVector("_MainTex_ST", new Vector4(tilingX, tilingZ, 0f, 0f));
            renderer.SetPropertyBlock(props);
        }
    }
}
