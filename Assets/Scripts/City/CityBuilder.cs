using System;
using MagicalGirl.Core.City;
using UnityEngine;

namespace MagicalGirl.City
{
    /// <summary>
    /// CityGeneratorが生成したCityLayoutを、実際のUnity GameObject(直方体プリミティブの組み合わせ)
    /// として配置するユーティリティ。各ブロックはGameObject.CreatePrimitive(Cube)で生成するため、
    /// MeshFilter・MeshRenderer・BoxColliderが自動的に付与される。
    /// </summary>
    public static class CityBuilder
    {
        /// <summary>
        /// CityLayoutからGameObject群を生成し、指定した親の下に配置する。
        /// 引数: layout - 生成済みの都市レイアウト / parent - 配置先の親Transform(nullなら未配置)
        /// 返り値: 生成した街のルートGameObject("GeneratedCity")
        /// 例外: layoutがnullの場合、ArgumentNullExceptionを投げる
        /// </summary>
        public static GameObject Build(CityLayout layout, Transform parent)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var root = new GameObject("GeneratedCity");
            if (parent != null)
                root.transform.SetParent(parent, false);

            foreach (var building in layout.Buildings)
                BuildBuilding(building, root.transform, "Building");

            BuildBuilding(layout.Landmark, root.transform, "Landmark");

            return root;
        }

        /// <summary>
        /// 建物1棟分のGameObjectを生成する(基準点用の空GameObjectの下に、構成ブロックごとの
        /// Cubeプリミティブをぶら下げる)。
        /// 引数: building - 生成する建物データ / parent - 配置先の親Transform / namePrefix - GameObject名の接頭辞
        /// 返り値: なし
        /// </summary>
        static void BuildBuilding(BuildingData building, Transform parent, string namePrefix)
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
            }
        }
    }
}
