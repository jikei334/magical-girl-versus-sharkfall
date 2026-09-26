using System.IO;
using MagicalGirl.City;
using MagicalGirl.Controls;
using MagicalGirl.Core.City;
using MagicalGirl.Core.Enemy;
using MagicalGirl.Enemy;
using MagicalGirl.Gesture;
using UnityEditor;
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
            CityBuilder.Build(layout, null);
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

            BuildGestureInput(rigGo, cameraGo);
        }

        /// <summary>
        /// タッチパッドのジェスチャー入力(GestureInputController)と、認識結果を確認する
        /// デバッグ表示を配置する。
        /// 引数: rigGo - 箒リグのGameObject / cameraGo - カメラのGameObject(デバッグ表示の親)
        /// 返り値: なし
        /// </summary>
        static void BuildGestureInput(GameObject rigGo, GameObject cameraGo)
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
