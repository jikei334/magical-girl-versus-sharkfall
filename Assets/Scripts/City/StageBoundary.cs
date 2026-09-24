using UnityEngine;

namespace MagicalGirl.City
{
    /// <summary>
    /// 生成した街の外側に、プレイヤーがステージ外へ出られないようにする境界壁を配置する
    /// ユーティリティ。衝突判定(BoxCollider)に加えて、近づくと見えてくるよう薄い半透明の
    /// 色を付けたメッシュも生成する(完全に見えない壁だと、ぶつかった理由が分かりにくいため)。
    /// </summary>
    public static class StageBoundary
    {
        const float WallThickness = 20f;
        static readonly Color WallColor = new Color(0.3f, 0.7f, 1f, 0.12f); // 薄い水色、近づくほど視界に占める割合が増えて見えやすくなる

        /// <summary>
        /// 中心(0,0)を基準に、一辺2*halfExtentの正方形の外周へ4枚の壁を配置する。
        /// 引数:
        ///   halfExtent - 中心から境界までの距離(0より大きい値)
        ///   wallHeight - 壁の高さ(地面からの高さ、0より大きい値)
        ///   parent - 配置先の親Transform(nullなら未配置)
        /// 返り値: 生成した境界のルートGameObject("StageBoundary")
        /// </summary>
        public static GameObject Build(float halfExtent, float wallHeight, Transform parent)
        {
            var root = new GameObject("StageBoundary");
            if (parent != null)
                root.transform.SetParent(parent, false);

            var material = CreateTranslucentMaterial(WallColor);

            var centerY = wallHeight / 2f;
            var fullSpan = halfExtent * 2f + WallThickness;

            BuildWall(root.transform, material, "North", new Vector3(0f, centerY, halfExtent), new Vector3(fullSpan, wallHeight, WallThickness));
            BuildWall(root.transform, material, "South", new Vector3(0f, centerY, -halfExtent), new Vector3(fullSpan, wallHeight, WallThickness));
            BuildWall(root.transform, material, "East", new Vector3(halfExtent, centerY, 0f), new Vector3(WallThickness, wallHeight, fullSpan));
            BuildWall(root.transform, material, "West", new Vector3(-halfExtent, centerY, 0f), new Vector3(WallThickness, wallHeight, fullSpan));

            return root;
        }

        /// <summary>
        /// 壁1枚を生成する(BoxColliderは自動付与、半透明マテリアルを割り当てる)。
        /// 引数: parent - 親Transform / material - 割り当てる半透明マテリアル
        ///        / name - GameObject名のサフィックス / position, size - 配置位置とサイズ
        /// 返り値: なし
        /// </summary>
        static void BuildWall(Transform parent, Material material, string name, Vector3 position, Vector3 size)
        {
            var wallGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGo.name = $"Wall_{name}";
            wallGo.transform.SetParent(parent, false);
            wallGo.transform.localPosition = position;
            wallGo.transform.localScale = size;
            wallGo.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>
        /// Standardシェーダーを半透明(Alpha Blend)モードに設定したマテリアルを作る。
        /// 引数: color - 表示色(alphaが透明度)
        /// 返り値: 半透明設定済みのMaterial
        /// </summary>
        static Material CreateTranslucentMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.SetFloat("_Mode", 3); // Transparent
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.color = color;
            return material;
        }
    }
}
