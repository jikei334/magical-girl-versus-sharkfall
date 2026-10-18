using System.IO;
using System.Linq;
using MagicalGirl.City;
using MagicalGirl.Combat;
using MagicalGirl.Controls;
using MagicalGirl.Core.City;
using MagicalGirl.Core.Enemy;
using MagicalGirl.Enemy;
using MagicalGirl.Gesture;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;

namespace MagicalGirl.EditorTools
{
    /// <summary>
    /// 疑似飛行コントローラー(BroomFlightController)と手続き型都市生成を実機で確認するための
    /// テストシーンを生成するエディタスクリプト。箒の機体役のリグに傾き入力・飛行制御・頭部
    /// トラッキング用カメラをまとめ、地面と生成した街(建物+ランドマークタワー)を配置する。
    /// </summary>
    public static class FlightTestSceneSetup
    {
        const string k_ScenePath = "Assets/Scenes/FlightTest.unity";

        // テストシーン用の都市生成パラメータ。シードは固定し、毎回同じ街が再現されるようにする。
        // 実機確認で「ビルが小さい気がする」とのフィードバックを受け、ブロックサイズ・
        // 建物の高さを大きめに調整してある。
        static readonly CityGenerationConfig k_CityConfig = new CityGenerationConfig(
            gridExtent: 4,
            blockSize: 40f,
            roadWidth: 20f,
            minBuildingHeight: 15f,
            maxBuildingHeight: 100f,
            minFootprintRatio: 0.4f,
            maxFootprintRatio: 0.8f,
            landmarkHeight: 250f,
            landmarkFootprintRatio: 0.5f,
            seed: 12345);

        // 境界壁の配置パラメータ。街のグリッド1ブロック分外側に境界を置く。
        static float CityBlockSpacing => k_CityConfig.BlockSize + k_CityConfig.RoadWidth;
        static float StageHalfExtent => k_CityConfig.GridExtent * CityBlockSpacing + CityBlockSpacing;
        const float StageWallHeight = 400f;

        const string k_SharkModelPath = "Assets/Art/Enemies/School/shark.fbx";
        const string k_SharkPrefabPath = "Assets/Prefabs/Enemies/SharkSchool.prefab";
        const string k_CityMaterialsFolder = "Assets/Materials/City";
        const string k_FireballModelPath = "Assets/Art/Sample/fireball.glb";
        const string k_FireballPrefabPath = "Assets/Prefabs/Combat/Fireball.prefab";
        const string k_FireballAnimatorControllerPath = "Assets/Prefabs/Combat/FireballAnimator.controller";
        const string k_BarrierMaterialPath = "Assets/Prefabs/Combat/BarrierMaterial.mat";

        /// <summary>
        /// テストシーンを生成して保存する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        [MenuItem("XREAL/Build Flight Test Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildCity();
            BuildRoads();
            BuildBoundary();
            BuildBroomRig();
            BuildEnemySchool();

            var light = new GameObject("Directional Light");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Directory.CreateDirectory(Path.GetDirectoryName(k_ScenePath));
            if (!EditorSceneManager.SaveScene(scene, k_ScenePath))
                throw new IOException("シーンの保存に失敗しました: " + k_ScenePath);

            RegisterInBuildSettings();

            Debug.Log("[FlightTestSceneSetup] Scene created at " + k_ScenePath);
        }

        /// <summary>
        /// このシーンをビルド対象の先頭(=起動シーン)に登録する。既存のシーン
        /// (DeviceCheck等)は残しつつ、重複だけ避ける。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void RegisterInBuildSettings()
        {
            var existing = EditorBuildSettings.scenes;
            var withoutSelf = System.Array.FindAll(existing, s => s.path != k_ScenePath);

            var updated = new EditorBuildSettingsScene[withoutSelf.Length + 1];
            updated[0] = new EditorBuildSettingsScene(k_ScenePath, true);
            System.Array.Copy(withoutSelf, 0, updated, 1, withoutSelf.Length);

            EditorBuildSettings.scenes = updated;
        }

        /// <summary>
        /// 移動速度・高度の目安がわかるよう、大きめの地面を配置する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, 0f, 0f);
            // Planeは10x10単位なので、境界壁(StageHalfExtent)より確実に広くなるスケールにする。
            var scale = StageHalfExtent / 5f + 10f;
            ground.transform.localScale = new Vector3(scale, 1f, scale);

            // デフォルトの白のままだと視認性が悪いため、道路・ビルと同じトーンの
            // 控えめなグレーに変える(道路や建物の隙間から見える部分)。
            ground.GetComponent<Renderer>().sharedMaterial = EnsureGroundMaterial();
        }

        /// <summary>
        /// 地面用の控えめなグレーのマテリアルを作る(既に作成済みならそれを再利用する)。
        /// 引数: なし
        /// 返り値: 生成(または既存)のマテリアル
        /// </summary>
        static Material EnsureGroundMaterial()
        {
            var materialPath = $"{k_CityMaterialsFolder}/Ground.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
                return existing;

            var material = new Material(Shader.Find("Unlit/Color"));
            material.color = new Color(0.55f, 0.58f, 0.55f);

            Directory.CreateDirectory(k_CityMaterialsFolder);
            AssetDatabase.CreateAsset(material, materialPath);

            return material;
        }

        /// <summary>
        /// 街の外周に、ステージ外へ出られないようにする境界壁(StageBoundary)を配置する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildBoundary()
        {
            StageBoundary.Build(StageHalfExtent, StageWallHeight, null);
        }

        /// <summary>
        /// 手続き型都市生成(CityGenerator)で街のレイアウトを作り、CityBuilderで実際の
        /// GameObject(建物+ランドマークタワー)として配置する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildCity()
        {
            var layout = CityGenerator.Generate(k_CityConfig);
            var materials = EnsureCityMaterials();
            CityBuilder.Build(layout, null, materials);
        }

        /// <summary>
        /// 街の外壁・屋上・ランドマーク用マテリアル一式を作る(既に作成済みならそれを再利用する)。
        /// 引数: なし
        /// 返り値: 生成(または既存)のCityMaterialPalette
        /// </summary>
        static CityMaterialPalette EnsureCityMaterials()
        {
            var facadeMaterials = new[]
            {
                EnsureCityMaterial("Assets/Art/Sample/building_facade_glass.png", "FacadeGlass"),
                EnsureCityMaterial("Assets/Art/Sample/building_facade_concrete.png", "FacadeConcrete"),
                EnsureCityMaterial("Assets/Art/Sample/building_facade_brick.png", "FacadeBrick"),
            };
            var roofMaterial = EnsureCityMaterial("Assets/Art/Sample/building_roof.png", "Roof");
            var landmarkMaterial = EnsureCityMaterial("Assets/Art/Sample/landmark_facade.png", "Landmark");

            return new CityMaterialPalette(facadeMaterials, roofMaterial, landmarkMaterial);
        }

        /// <summary>
        /// 街の見た目用マテリアルを1つ作る(既に作成済みならそれを再利用する)。トゥーン調の
        /// ベタ塗りテクスチャなので、シーンのライティングの影響を受けないUnlitシェーダーを使う
        /// (Standardシェーダーだと陰影が付いてフラットな絵柄が崩れてしまう)。テクスチャの
        /// Wrap ModeはRepeatに揃える(タイリングにはRepeatが必須で、Clampだと端のピクセルが
        /// 伸びるだけになってしまう)。
        /// 引数: texturePath - 元テクスチャのパス / materialName - 保存するマテリアル名
        /// 返り値: 生成(または既存)のマテリアル。テクスチャが見つからない場合はnull
        /// </summary>
        static Material EnsureCityMaterial(string texturePath, string materialName)
        {
            var materialPath = $"{k_CityMaterialsFolder}/{materialName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
                return existing;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogError("[FlightTestSceneSetup] テクスチャが見つかりません: " + texturePath);
                return null;
            }

            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer && importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            var material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = texture;

            Directory.CreateDirectory(k_CityMaterialsFolder);
            AssetDatabase.CreateAsset(material, materialPath);

            return material;
        }

        /// <summary>
        /// グリッド道路網(RoadGenerator)を生成し、路面・センターラインを配置する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildRoads()
        {
            var layout = RoadGenerator.Generate(k_CityConfig);
            var asphaltMaterial = EnsureCityMaterial("Assets/Art/Sample/road_asphalt.png", "RoadAsphalt");
            var centerlineMaterial = EnsureCenterlineMaterial();
            RoadBuilder.Build(layout, null, asphaltMaterial, centerlineMaterial);
        }

        /// <summary>
        /// センターライン用マテリアルを作る(既に作成済みならそれを再利用する)。
        /// 引数: なし
        /// 返り値: 生成(または既存)のマテリアル
        /// </summary>
        static Material EnsureCenterlineMaterial()
        {
            var materialPath = $"{k_CityMaterialsFolder}/Centerline.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
                return existing;

            var texture = EnsureCenterlineTexture();

            // 破線はアルファで切り抜くため、半透明描画に対応したUnlitシェーダーを使う。
            var material = new Material(Shader.Find("Unlit/Transparent"));
            material.mainTexture = texture;

            Directory.CreateDirectory(k_CityMaterialsFolder);
            AssetDatabase.CreateAsset(material, materialPath);

            return material;
        }

        /// <summary>
        /// センターラインの破線パターンを持つ透過PNGをコードで生成する(既に作成済みならそれを
        /// 再利用する)。AI画像生成では「道路」という概念自体が中央線を連想させてしまい、
        /// 意図しない線が混入する問題が解決しづらかったため、Unity側で確実にピクセルを
        /// 制御できるこの方式にした。縦長の画像で、上から一定割合を白(破線部分)・残りを
        /// 透明(隙間部分)にする。RoadBuilder側でローカルZ(長さ方向)に沿ってタイリングする。
        /// 引数: なし
        /// 返り値: 生成(または既存)のテクスチャ
        /// </summary>
        static Texture2D EnsureCenterlineTexture()
        {
            const string texturePath = "Assets/Materials/City/CenterlineDash.png";

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (existing != null)
                return existing;

            const int width = 8;
            const int height = 64;
            const float dashRatio = 0.6f; // 画像の上から60%を破線(白)、残り40%を隙間(透明)にする
            var dashRows = Mathf.RoundToInt(height * dashRatio);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (var y = 0; y < height; y++)
            {
                // Texture2Dはy=0が画像の下端。「上からdashRows行」を白にしたいので、
                // 上端からの行数に変換してから判定する。
                var rowsFromTop = height - 1 - y;
                var color = rowsFromTop < dashRows ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0f);
                for (var x = 0; x < width; x++)
                    texture.SetPixel(x, y, color);
            }
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(texturePath));
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);

            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point; // トゥーン調に合わせ、破線の境界をくっきりさせる
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        /// <summary>
        /// スクール(小型・群れ)の編隊を生成するSchoolSpawnerを配置する。箒の開始位置
        /// (z=-250)より奥から、プレイヤーに向かって直進してくるよう設定する。
        ///
        /// 生成(Instantiate)自体はSchoolSpawner.Start()で実行時に行われる。ここで
        /// Spawn()を直接呼んでしまうと、EnemyControllerの進行方向・ステータスは
        /// [SerializeField]でないため、シーン保存→実機での再ロード時に失われてしまう
        /// (実際にこの問題が起き、サメが意図と逆方向へ飛んでいくバグになった)。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildEnemySchool()
        {
            var prefab = EnsureSharkPrefab();
            if (prefab == null)
                return;

            var spawnerGo = new GameObject("SchoolSpawner");
            // 箒の開始位置(0, 150, -250)より奥に置く。SchoolSpawnerはこの位置を基準に生成する。
            spawnerGo.transform.position = new Vector3(0f, 150f, 100f);

            var spawner = spawnerGo.AddComponent<SchoolSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("m_EnemyPrefab").objectReferenceValue = prefab;
            so.FindProperty("m_Pattern").enumValueIndex = (int)FormationPattern.V;
            so.FindProperty("m_MemberCount").intValue = 5;
            so.FindProperty("m_Spacing").floatValue = 8f;
            so.FindProperty("m_EnemyMaxHealth").floatValue = 20f;
            so.FindProperty("m_EnemySpeed").floatValue = 12f;
            so.FindProperty("m_SpawnDirection").vector3Value = Vector3.back; // -Z方向、プレイヤー側へ
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// shark.fbxからスクール用の敵プレハブを作る(既に作成済みならそれを再利用する)。
        /// EnemyControllerとモデルのバウンディングに合わせたBoxColliderを付与する。
        /// 引数: なし
        /// 返り値: 生成(または既存)のプレハブ。shark.fbxが見つからない場合はnull
        /// </summary>
        static EnemyController EnsureSharkPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(k_SharkPrefabPath);
            if (existing != null)
                return existing.GetComponent<EnemyController>();

            var sharkModel = AssetDatabase.LoadAssetAtPath<GameObject>(k_SharkModelPath);
            if (sharkModel == null)
            {
                Debug.LogError("[FlightTestSceneSetup] shark.fbxが見つかりません: " + k_SharkModelPath);
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(sharkModel);
            instance.name = "SharkSchool";
            instance.AddComponent<EnemyController>();

            // 階層を組み替える(子の付け替え)にはプレハブインスタンスのままでは
            // SetParent()が拒否されるため、先にプレハブ接続を解除しておく。
            // (このインスタンスはこの後SaveAsPrefabAssetで新規プレハブとして保存するため問題ない)
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // モデルは鼻先がローカルZ-方向、尾びれがローカルZ+方向を向いている(頂点分布で確認済み)。
            // SchoolSpawnerはこのプレハブのルートに対しQuaternion.LookRotation(進行方向)で
            // 絶対回転を設定するため、ルート自体には回転を焼き込めない。
            // ルート直下のArmature/メッシュは、Blender→Unity変換由来のベース回転(X軸270度)を
            // 既に持っているため、それらに直接Space.Selfで回転を加えると、親から見た実際の
            // 回転軸がベース回転によってねじれてしまい、意図した前後反転ではなく上下・左右の
            // 反転になってしまう(実際に発生した不具合)。
            // そこで無回転の空の親(OrientationFix)でラップし、そちらをワールド基準で180度
            // (Y軸)回転することで、既存の姿勢を保ったまま前後だけを正しく反転する。
            var originalChildren = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in instance.transform)
                originalChildren.Add(child);

            var orientationFix = new GameObject("OrientationFix").transform;
            orientationFix.SetParent(instance.transform, false);
            foreach (var child in originalChildren)
                child.SetParent(orientationFix, true);

            orientationFix.Rotate(0f, 180f, 0f, Space.Self);

            var renderer = instance.GetComponentInChildren<Renderer>();
            var collider = instance.AddComponent<BoxCollider>();
            if (renderer != null)
            {
                collider.center = instance.transform.InverseTransformPoint(renderer.bounds.center);
                collider.size = renderer.bounds.size;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(k_SharkPrefabPath));
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, k_SharkPrefabPath);
            Object.DestroyImmediate(instance);

            return savedPrefab.GetComponent<EnemyController>();
        }

        /// <summary>
        /// 箒の機体役のリグ(傾き入力+飛行制御)と、その子として頭部トラッキング付きの
        /// カメラ・デバッグ表示テキストを配置する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildBroomRig()
        {
            var rigGo = new GameObject("BroomRig");
            // 通常のビル群(最大MaxBuildingHeight)の上、ランドマークタワー(LandmarkHeight)より
            // 低い高度から開始し、スカイラインを見下ろしつつランドマークが遠くに見える構図にする。
            rigGo.transform.position = new Vector3(0f, 150f, -250f);

            rigGo.AddComponent<BeamProTiltController>();
            rigGo.AddComponent<BroomFlightController>();

            // 箒+ライダー相当のサイズの当たり判定にする(CharacterControllerの既定値は
            // 接地キャラクター向けの小さめのサイズのため、街のスケールに合わせて広げる)。
            var characterController = rigGo.GetComponent<CharacterController>();
            characterController.radius = 2f;
            characterController.height = 3f;

            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(rigGo.transform, false);
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cameraGo.AddComponent<AudioListener>();

            // Air 2 Proは3DoF(回転のみ)のため、TrackingTypeはRotationOnlyにする。
            // 箒本体(BroomRig)の位置・旋回とは独立に、カメラのローカル回転だけが頭部の
            // 向きに応じて動く。
            var poseDriver = cameraGo.AddComponent<TrackedPoseDriver>();
            poseDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;

            BuildVersionDisplay(cameraGo);

            // 飛行状態(速度・旋回・機首角)を確認するデバッグ表示。カメラの子にして常に視界内に置く。
            var debugTextGo = new GameObject("FlightDebugText");
            debugTextGo.transform.SetParent(cameraGo.transform, false);
            debugTextGo.transform.localPosition = new Vector3(0f, -0.3f, 3f);
            var debugTm = debugTextGo.AddComponent<TextMesh>();
            debugTm.text = "Speed: -\nYawRate: -\nPitchAngle: -\nBankAngle: -";
            debugTm.fontSize = 32;
            debugTm.characterSize = 0.013f;
            debugTm.anchor = TextAnchor.MiddleCenter;
            debugTm.alignment = TextAlignment.Center;
            debugTm.color = Color.green;

            var hudGo = new GameObject("FlightDebugHud");
            hudGo.transform.SetParent(rigGo.transform, false);
            var hud = hudGo.AddComponent<FlightDebugHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("m_FlightController").objectReferenceValue = rigGo.GetComponent<BroomFlightController>();
            so.FindProperty("m_Text").objectReferenceValue = debugTm;
            so.ApplyModifiedProperties();

            var gestureInput = BuildGestureInput(rigGo, cameraGo);
            BuildRecalibration(rigGo, cameraGo, gestureInput);
            BuildCombat(rigGo, cameraGo, gestureInput);
        }

        /// <summary>
        /// タッチパッドの長押しで再キャリブレーションするRecalibrationControllerを配置する。
        /// 引数: rigGo - 箒リグのGameObject / cameraGo - デバッグ表示の親にするカメラGameObject
        ///        / gestureInput - 長押し検知元
        /// 返り値: なし
        /// </summary>
        static void BuildRecalibration(GameObject rigGo, GameObject cameraGo, GestureInputController gestureInput)
        {
            var debugTextGo = new GameObject("CalibrationDebugText");
            debugTextGo.transform.SetParent(cameraGo.transform, false);
            debugTextGo.transform.localPosition = new Vector3(0.7f, 0.4f, 3f);
            var debugTm = debugTextGo.AddComponent<TextMesh>();
            debugTm.text = "Calibration: -";
            debugTm.fontSize = 28;
            debugTm.characterSize = 0.011f;
            debugTm.anchor = TextAnchor.MiddleCenter;
            debugTm.alignment = TextAlignment.Center;
            debugTm.color = Color.cyan;

            var controller = rigGo.AddComponent<RecalibrationController>();
            var so = new SerializedObject(controller);
            so.FindProperty("m_GestureInput").objectReferenceValue = gestureInput;
            so.FindProperty("m_DebugText").objectReferenceValue = debugTm;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// タッチパッドのジェスチャー入力(GestureInputController)と、認識結果を確認する
        /// デバッグ表示を配置する。
        /// 引数: rigGo - 箒リグのGameObject / cameraGo - カメラのGameObject(デバッグ表示の親)
        /// 返り値: 生成したGestureInputController(SpellCasterの入力元として使う)
        /// </summary>
        static GestureInputController BuildGestureInput(GameObject rigGo, GameObject cameraGo)
        {
            var inputGo = new GameObject("GestureInputController");
            inputGo.transform.SetParent(rigGo.transform, false);
            var input = inputGo.AddComponent<GestureInputController>();

            var debugTextGo = new GameObject("GestureDebugText");
            debugTextGo.transform.SetParent(cameraGo.transform, false);
            debugTextGo.transform.localPosition = new Vector3(0f, 0.4f, 3f);
            var debugTm = debugTextGo.AddComponent<TextMesh>();
            debugTm.text = "Gesture: -";
            debugTm.fontSize = 32;
            debugTm.characterSize = 0.013f;
            debugTm.anchor = TextAnchor.MiddleCenter;
            debugTm.alignment = TextAlignment.Center;
            debugTm.color = Color.magenta;

            var hudGo = new GameObject("GestureDebugHud");
            hudGo.transform.SetParent(rigGo.transform, false);
            var hud = hudGo.AddComponent<GestureDebugHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("m_InputController").objectReferenceValue = input;
            so.FindProperty("m_Text").objectReferenceValue = debugTm;
            so.ApplyModifiedProperties();

            return input;
        }

        /// <summary>
        /// シューティング基本ループ(火球・バリア)を配置する。ジェスチャー認識結果を
        /// SpellCasterで攻撃に変換し、照準は頭部トラッキングで動くカメラの向きを使う。
        /// 引数: rigGo - 箒リグのGameObject / cameraGo - 照準originにするカメラのGameObject
        ///        / gestureInput - ジェスチャー入力元
        /// 返り値: なし
        /// </summary>
        static void BuildCombat(GameObject rigGo, GameObject cameraGo, GestureInputController gestureInput)
        {
            var fireballPrefab = EnsureFireballPrefab();
            if (fireballPrefab == null)
                return;

            var barrierGo = new GameObject("Barrier");
            barrierGo.transform.SetParent(rigGo.transform, false);
            var barrier = barrierGo.AddComponent<Barrier>();
            var barrierSo = new SerializedObject(barrier);
            barrierSo.FindProperty("m_VisualMaterial").objectReferenceValue = EnsureBarrierVisualMaterial();
            barrierSo.ApplyModifiedProperties();

            var casterGo = new GameObject("SpellCaster");
            casterGo.transform.SetParent(rigGo.transform, false);
            var caster = casterGo.AddComponent<SpellCaster>();
            var casterSo = new SerializedObject(caster);
            casterSo.FindProperty("m_GestureInput").objectReferenceValue = gestureInput;
            casterSo.FindProperty("m_AimOrigin").objectReferenceValue = cameraGo.transform;
            casterSo.FindProperty("m_FireballPrefab").objectReferenceValue = fireballPrefab;
            casterSo.FindProperty("m_Barrier").objectReferenceValue = barrier;
            casterSo.ApplyModifiedProperties();

            // 直近に発動した攻撃種別を確認するデバッグ表示。
            // 実機確認で「Attackの文字列が全く見えない」との報告があり、Y座標を高く
            // (0.65)しすぎてXREAL Air 2 Proの縦方向視野角の外に出ていた可能性が高いため、
            // 既に視認できているGestureDebugText(0, 0.4, 3)と同じ高さで横に並べる位置にする。
            var debugTextGo = new GameObject("CombatDebugText");
            debugTextGo.transform.SetParent(cameraGo.transform, false);
            debugTextGo.transform.localPosition = new Vector3(-0.7f, 0.4f, 3f);
            var debugTm = debugTextGo.AddComponent<TextMesh>();
            debugTm.text = "Attack: -";
            debugTm.fontSize = 28;
            debugTm.characterSize = 0.011f;
            debugTm.anchor = TextAnchor.MiddleCenter;
            debugTm.alignment = TextAlignment.Center;
            debugTm.color = Color.red;

            var hudGo = new GameObject("CombatDebugHud");
            hudGo.transform.SetParent(rigGo.transform, false);
            var hud = hudGo.AddComponent<CombatDebugHud>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("m_SpellCaster").objectReferenceValue = caster;
            hudSo.FindProperty("m_Text").objectReferenceValue = debugTm;
            hudSo.ApplyModifiedProperties();
        }

        /// <summary>
        /// 火球のプレハブを作る(既に作成済みならそれを再利用する)。fireball.glb
        /// (Spawn/Fly/Impactのアニメーション付き)を元に、当たり判定(コア相当のSphereCollider)と
        /// Fireballコンポーネント(RequireComponentでRigidbodyも付与)を追加する。
        /// モデルの頂点分布を確認したところ、炎の尾はローカルZ-方向、進行方向(ヘッド側)が
        /// ローカルZ+方向で、Quaternion.LookRotation()の規約と既に一致していたため、
        /// サメ(shark.fbx)で必要だった向きの補正は不要だった。
        /// 引数: なし
        /// 返り値: 生成(または既存)のプレハブ。fireball.glbが見つからない場合はnull
        /// </summary>
        static Fireball EnsureFireballPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(k_FireballPrefabPath);
            if (existing != null)
                return existing.GetComponent<Fireball>();

            var fireballModel = AssetDatabase.LoadAssetAtPath<GameObject>(k_FireballModelPath);
            if (fireballModel == null)
            {
                Debug.LogError("[FlightTestSceneSetup] fireball.glbが見つかりません: " + k_FireballModelPath);
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fireballModel);
            instance.name = "Fireball";

            // コア(本体の球)相当の当たり判定。炎の尾(最大で約2.9先まで伸びる)は判定に含めない。
            var collider = instance.AddComponent<SphereCollider>();
            collider.center = Vector3.zero;
            collider.radius = 0.5f;

            var animator = instance.GetComponent<Animator>();
            if (animator != null)
                animator.runtimeAnimatorController = EnsureFireballAnimatorController();

            instance.AddComponent<Fireball>();

            Directory.CreateDirectory(Path.GetDirectoryName(k_FireballPrefabPath));
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, k_FireballPrefabPath);
            Object.DestroyImmediate(instance);

            return savedPrefab.GetComponent<Fireball>();
        }

        /// <summary>
        /// fireball.glb内のSpawn/Fly/ImpactクリップをそれぞれStateとして持つAnimatorControllerを
        /// 作る(既に作成済みならそれを再利用する)。クリップ間の自動遷移は組まず、
        /// Fireball.cs側からAnimator.Play(ステート名)で明示的に切り替える想定。
        /// 引数: なし
        /// 返り値: 生成(または既存)のAnimatorController
        /// </summary>
        static AnimatorController EnsureFireballAnimatorController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(k_FireballAnimatorControllerPath);
            if (existing != null)
                return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(k_FireballAnimatorControllerPath));
            var controller = AnimatorController.CreateAnimatorControllerAtPath(k_FireballAnimatorControllerPath);

            var clips = AssetDatabase.LoadAllAssetsAtPath(k_FireballModelPath).OfType<AnimationClip>().ToList();
            foreach (var clipName in new[] { "Spawn", "Fly", "Impact" })
            {
                var clip = clips.FirstOrDefault(c => c.name == clipName);
                if (clip == null)
                {
                    Debug.LogError($"[FlightTestSceneSetup] fireball.glbに{clipName}クリップが見つかりません");
                    continue;
                }

                var state = controller.AddMotion(clip);
                state.name = clipName;
            }

            return controller;
        }

        /// <summary>
        /// バリア球体用の両面描画マテリアルを作る(既に作成済みならそれを再利用する)。
        /// Shader.Find()を実行時に呼ぶと、ビルドにシェーダーが含まれず取得に失敗する
        /// 可能性があるため、編集時にアセットとして保存しておく(Fireballの
        /// マテリアル紛失不具合と同根の対策)。
        /// 引数: なし
        /// 返り値: 生成(または既存)のマテリアル
        /// </summary>
        static Material EnsureBarrierVisualMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(k_BarrierMaterialPath);
            if (existing != null)
                return existing;

            var material = new Material(Shader.Find("MagicalGirl/DoubleSidedGlow"));
            material.color = new Color(0.4f, 0.8f, 1f, 0.6f); // 水色、加算気味の半透明

            Directory.CreateDirectory(Path.GetDirectoryName(k_BarrierMaterialPath));
            AssetDatabase.CreateAsset(material, k_BarrierMaterialPath);

            return material;
        }

        /// <summary>
        /// カメラ視界の右上に、実機で確認しているビルドが最新かどうかの目印になる
        /// バージョン番号(BuildInfo.Version)を表示する。
        /// 引数: cameraGo - 表示先のカメラGameObject
        /// 返り値: なし
        /// </summary>
        static void BuildVersionDisplay(GameObject cameraGo)
        {
            var textGo = new GameObject("BuildVersionText");
            textGo.transform.SetParent(cameraGo.transform, false);
            textGo.transform.localPosition = new Vector3(0.9f, 0.55f, 3f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "Build v-";
            tm.fontSize = 28;
            tm.characterSize = 0.011f;
            tm.anchor = TextAnchor.MiddleRight;
            tm.alignment = TextAlignment.Right;
            tm.color = Color.yellow;

            var hud = textGo.AddComponent<BuildVersionHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("m_Text").objectReferenceValue = tm;
            so.ApplyModifiedProperties();
        }
    }
}
