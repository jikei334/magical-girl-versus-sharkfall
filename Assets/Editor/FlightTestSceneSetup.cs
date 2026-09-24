using System.IO;
using MagicalGirl.City;
using MagicalGirl.Controls;
using MagicalGirl.Core.City;
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
        static readonly CityGenerationConfig k_CityConfig = new CityGenerationConfig(
            gridExtent: 4,
            blockSize: 20f,
            roadWidth: 10f,
            minBuildingHeight: 5f,
            maxBuildingHeight: 40f,
            minFootprintRatio: 0.4f,
            maxFootprintRatio: 0.8f,
            landmarkHeight: 80f,
            landmarkFootprintRatio: 0.5f,
            seed: 12345);

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
            BuildBroomRig();

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
            ground.transform.localScale = new Vector3(50f, 1f, 50f); // Planeは10x10単位なので500x500相当
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
            rigGo.transform.position = new Vector3(0f, 50f, -100f);

            rigGo.AddComponent<BeamProTiltController>();
            rigGo.AddComponent<BroomFlightController>();

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
    }
}
