using System;
using System.IO;
using MagicalGirl.Controls;
using Unity.XR.XREAL;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace MagicalGirl.EditorTools
{
    /// <summary>
    /// XREAL Air 2 Pro + Beam Pro 向けのプロジェクト設定とビルドを自動化するエディタスクリプト。
    ///  - XREAL XR Pluginのバリデータが要求するAndroid Player Settingsの設定
    ///  - XR Plug-in ManagementへのXREALローダー登録
    ///  - XREALSettingsアセットの生成とSupportMultiResumeのOFF
    ///  - 動作確認用シーンの生成
    /// バッチモードから -executeMethod で呼び出せる。失敗時は終了コード1で終了する。
    /// </summary>
    public static class ProjectSetup
    {
        const string k_ScenePath = "Assets/Scenes/DeviceCheck.unity";
        const string k_ApplicationId = "com.jikei334.magicalgirlversussharkfall";
        const string k_ApkPath = "Builds/MagicalGirl.apk";

        /// <summary>
        /// プロジェクトの初期セットアップを行う(メニュー・バッチモード共通)。
        /// 引数: なし
        /// 返り値: なし。失敗時は例外をログに出し、バッチモードでは終了コード1で終了する
        /// </summary>
        [MenuItem("XREAL/Setup Project")]
        public static void Setup()
        {
            RunOrExit("Setup", () =>
            {
                ConfigurePlayerSettings();
                EnableXrealLoader();
                EnsureXrealSettingsAsset();
                BuildScene();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[ProjectSetup] Setup complete.");
            });
        }

        /// <summary>
        /// Android向けにAPKをビルドする(メニュー・バッチモード共通)。
        /// 引数: なし
        /// 返り値: なし。ビルド失敗時は終了コード1で終了する
        /// </summary>
        [MenuItem("XREAL/Build APK")]
        public static void Build()
        {
            RunOrExit("Build", () =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(k_ApkPath));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { k_ScenePath },
                    locationPathName = k_ApkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });

                var summary = report.summary;
                Debug.Log($"[ProjectSetup] Build result: {summary.result}, size: {summary.totalSize} bytes, errors: {summary.totalErrors}, warnings: {summary.totalWarnings}, output: {summary.outputPath}");

                if (summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"ビルドに失敗しました: {summary.result}");
            });
        }

        /// <summary>
        /// 処理を実行し、例外が発生したらログに出力する。バッチモードでは終了コード1で終了する。
        /// 引数: name - ログに表示する処理名 / action - 実行する処理
        /// 返り値: なし
        /// </summary>
        static void RunOrExit(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectSetup] {name} failed: {e.Message}");
                Debug.LogException(e);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        /// <summary>
        /// XREAL XR Pluginのバリデータ(XREALProjectValidator)が要求するPlayer Settingsを設定する。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "jikei334";
            PlayerSettings.productName = "Magical Girl versus Sharkfall";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, k_ApplicationId);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[1] { GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            Debug.Log("[ProjectSetup] Player Settings configured for Android.");
        }

        /// <summary>
        /// XR Plug-in Management(Android)にXREALローダーを登録する。
        /// 引数: なし
        /// 返り値: なし。登録に失敗した場合は例外を投げる
        /// </summary>
        static void EnableXrealLoader()
        {
            XRGeneralSettingsPerBuildTarget buildTargetSettings;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings == null)
            {
                buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            }

            var settings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = "XR Settings Android";
                buildTargetSettings.SetSettingsForBuildTarget(BuildTargetGroup.Android, settings);
                AssetDatabase.AddObjectToAsset(settings, buildTargetSettings);
            }

            if (settings.Manager == null)
            {
                settings.Manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                settings.Manager.name = "XR Manager Settings Android";
                AssetDatabase.AddObjectToAsset(settings.Manager, buildTargetSettings);
            }

            // 既に登録済みの場合もfalseが返るため、登録後の状態を見て成否を判定する
            XRPackageMetadataStore.AssignLoader(
                settings.Manager, typeof(XREALXRLoader).FullName, BuildTargetGroup.Android);

            var assigned = false;
            foreach (var loader in settings.Manager.activeLoaders)
            {
                if (loader is XREALXRLoader)
                    assigned = true;
            }

            if (!assigned)
                throw new InvalidOperationException("XREALXRLoader をAndroidのXR Plug-in Managementに登録できませんでした。");

            EditorUtility.SetDirty(buildTargetSettings);
            Debug.Log("[ProjectSetup] XREALXRLoader assigned to Android XR Plug-in Management.");
        }

        /// <summary>
        /// XREALSettingsアセットを生成し、SupportMultiResumeをOFFにする。
        /// GUIでXREALタブを開かないとアセットが作られないSDKの不具合への対処。
        /// Beam Proはマルチウィンドウ対応スマホではなく、ONだとグラス側が
        /// NebulaOSの画面のまま切り替わらないため、必ずOFFにする。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void EnsureXrealSettingsAsset()
        {
            XREALSettings settings;
            if (!EditorBuildSettings.TryGetConfigObject(XREALSettings.k_SettingsKey, out settings) || settings == null)
            {
                const string dir = "Assets/XR/Settings";
                Directory.CreateDirectory(dir);
                settings = ScriptableObject.CreateInstance<XREALSettings>();
                AssetDatabase.CreateAsset(settings, $"{dir}/XREAL Settings.asset");
                EditorBuildSettings.AddConfigObject(XREALSettings.k_SettingsKey, settings, true);
                Debug.Log("[ProjectSetup] Created XREAL Settings asset at " + dir + "/XREAL Settings.asset");
            }

            if (settings.SupportMultiResume)
            {
                settings.SupportMultiResume = false;
                EditorUtility.SetDirty(settings);
                Debug.Log("[ProjectSetup] Disabled SupportMultiResume (single-Activity launch path for Beam Pro).");
            }
        }

        /// <summary>
        /// 動作確認用のシーン(カメラ+デバイス情報テキスト)を生成して保存する。
        /// テキストはグラスの狭いFOV(対角約46度)に収まるよう、距離3mで縦約5.5度に抑える。
        /// TextMeshの前面法線は-Zなので、カメラが原点で+Zを向く場合は回転不要。
        /// 引数: なし
        /// 返り値: なし
        /// </summary>
        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();

            var textGo = new GameObject("DeviceCheckText");
            textGo.transform.position = new Vector3(0f, 0f, 3f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "Magical Girl versus Sharkfall";
            tm.fontSize = 48;
            tm.characterSize = 0.02f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.cyan;

            var controllerGo = new GameObject("DeviceCheckController");
            var controller = controllerGo.AddComponent<DeviceCheckController>();
            var so = new SerializedObject(controller);
            so.FindProperty("m_Text").objectReferenceValue = tm;
            so.ApplyModifiedProperties();

            // 傾き入力(ロール/ピッチ/ブレーキゾーン)を実機で確認するためのデバッグ表示。
            // 1つ目のテキストの下に配置し、FOV(対角約46度)に収まる範囲に収める。
            var tiltTextGo = new GameObject("TiltDebugText");
            tiltTextGo.transform.position = new Vector3(0f, -0.25f, 3f);
            var tiltTm = tiltTextGo.AddComponent<TextMesh>();
            tiltTm.text = "Roll: -\nPitch: -\nZone: -";
            tiltTm.fontSize = 36;
            tiltTm.characterSize = 0.015f;
            tiltTm.anchor = TextAnchor.MiddleCenter;
            tiltTm.alignment = TextAlignment.Center;
            tiltTm.color = Color.yellow;

            var tiltControllerGo = new GameObject("BeamProTiltController");
            var tiltController = tiltControllerGo.AddComponent<BeamProTiltController>();
            var tiltSo = new SerializedObject(tiltController);
            tiltSo.FindProperty("m_DebugText").objectReferenceValue = tiltTm;
            tiltSo.ApplyModifiedProperties();

            Directory.CreateDirectory(Path.GetDirectoryName(k_ScenePath));
            if (!EditorSceneManager.SaveScene(scene, k_ScenePath))
                throw new IOException("シーンの保存に失敗しました: " + k_ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(k_ScenePath, true) };

            Debug.Log("[ProjectSetup] Scene created at " + k_ScenePath);
        }
    }
}
