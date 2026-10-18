using System;
using MagicalGirl.Core.City;
using UnityEngine;

namespace MagicalGirl.City
{
    /// <summary>
    /// RoadGeneratorが生成したRoadLayoutを、実際のUnity GameObject(薄いCubeプリミティブ)
    /// として配置するユーティリティ。各区間ごとに、路面(アスファルト)と、その少し上に重ねる
    /// センターライン(破線パターンの透過テクスチャをタイル貼りした薄いCube)を生成する。
    ///
    /// Cubeプリミティブの上面UVは常にU=ローカルX・V=ローカルZという固定の対応になっている
    /// (ワールド軸とは無関係)。そのため、道路をワールドX方向・Z方向のどちらに伸ばす場合でも、
    /// オブジェクト自体は常に「長さ=ローカルZ、幅=ローカルX」という向きで組み立て、
    /// X方向に伸ばす区間だけY軸90度回転で向きを変える。こうすることで、テクスチャの
    /// タイリング軸(ローカルZ=長さ方向)が常に一定になり、センターラインの破線模様の
    /// 向きが道路の向きによって崩れることがなくなる。
    /// </summary>
    public static class RoadBuilder
    {
        // 路面・センターラインの厚み。地面Plane(y=0)とのz-fighting回避のため少し浮かせる。
        const float RoadSurfaceThickness = 0.02f;
        const float CenterlineThickness = 0.02f;
        const float CenterlineHeightOffset = 0.03f; // 路面の上面からさらに少し浮かせる

        // 路面テクスチャ1タイルあたりの目安サイズ(単位)。
        const float RoadTileSize = 10f;

        // センターラインのテクスチャ1タイル(破線1本+隙間)あたりの目安の長さ(単位)。
        const float CenterlineTilePitch = 8f;

        // センターラインの見た目の幅(単位)。
        const float CenterlineWidth = 1f;

        // 縦方向・横方向の道路は交差点で同じ高さに重なるため、そのままだとZファイティング
        // (面の奥行きがほぼ同値になり、どちらを手前に描くかチラつく現象)が起きる。
        // X方向に伸びる区間だけ、ごくわずかに高さをずらして重なりを解消する
        // (街のスケールに対して無視できる差で、見た目には段差として現れない)。
        const float XAxisZFightOffset = 0.005f;

        /// <summary>
        /// RoadLayoutからGameObject群を生成し、指定した親の下に配置する。
        /// 引数: layout - 生成済みの道路網 / parent - 配置先の親Transform(nullなら未配置)
        ///        / asphaltMaterial - 路面マテリアル(nullなら路面を生成しない)
        ///        / centerlineMaterial - センターライン用マテリアル(nullならセンターラインを生成しない)
        /// 返り値: 生成した道路網のルートGameObject("GeneratedRoads")
        /// 例外: layoutがnullの場合、ArgumentNullExceptionを投げる
        /// </summary>
        public static GameObject Build(RoadLayout layout, Transform parent, Material asphaltMaterial, Material centerlineMaterial)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var root = new GameObject("GeneratedRoads");
            if (parent != null)
                root.transform.SetParent(parent, false);

            foreach (var segment in layout.Segments)
            {
                if (asphaltMaterial != null)
                    BuildStrip(segment, root.transform, "RoadSurface", asphaltMaterial,
                        RoadSurfaceThickness, RoadSurfaceThickness / 2f, segment.Width, RoadTileSize, removeCollider: false);

                if (centerlineMaterial != null)
                    BuildStrip(segment, root.transform, "Centerline", centerlineMaterial,
                        CenterlineThickness, RoadSurfaceThickness + CenterlineHeightOffset, CenterlineWidth, CenterlineTilePitch, removeCollider: true);
            }

            return root;
        }

        /// <summary>
        /// 区間1本分の帯状オブジェクト(路面またはセンターライン)を生成する。常に
        /// 「長さ=ローカルZ、幅=ローカルX」の向きで組み立て、区間がワールドX方向に伸びる場合は
        /// Y軸90度回転で向きを合わせる(クラス冒頭のコメント参照)。
        /// 引数:
        ///   segment - 対象区間 / parent - 配置先の親Transform / name - GameObject名
        ///   material - 割り当てるマテリアル / thickness - 高さ方向(Y)の厚み
        ///   centerY - 中心の高さ(ローカルY座標) / stripWidth - 帯の幅(ローカルX方向のサイズ)
        ///   tileSize - タイリング計算用の1タイルあたりの目安サイズ / removeCollider - 自動付与されるBoxColliderを削除するか
        /// 返り値: なし
        /// </summary>
        static void BuildStrip(RoadSegment segment, Transform parent, string name, Material material,
            float thickness, float centerY, float stripWidth, float tileSize, bool removeCollider)
        {
            var zFightOffset = segment.RunsAlongX ? XAxisZFightOffset : 0f;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(segment.CenterX, centerY + zFightOffset, segment.CenterZ);
            go.transform.localRotation = segment.RunsAlongX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            go.transform.localScale = new Vector3(stripWidth, thickness, segment.Length);

            if (removeCollider)
                UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            // ローカルZ(=長さ方向)に沿ってタイリングする。U=ローカルX(幅)・V=ローカルZ(長さ)の
            // 対応がCubeの上面UVの標準仕様のため、_MainTex_STのy成分(V方向)に長さ方向の
            // タイリング回数を設定する。
            var tilingAlongWidth = Mathf.Max(1f, Mathf.Round(stripWidth / tileSize));
            var tilingAlongLength = Mathf.Max(1f, Mathf.Round(segment.Length / tileSize));

            var props = new MaterialPropertyBlock();
            props.SetVector("_MainTex_ST", new Vector4(tilingAlongWidth, tilingAlongLength, 0f, 0f));
            renderer.SetPropertyBlock(props);
        }
    }
}
