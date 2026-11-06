using System.IO;
using SHI.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SHI.EditorTools
{
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/SHI/Scenes/Main.unity";
        const string ResourcesDir = "Assets/SHI/Resources/SHI";
        const string BuildPath = "Builds/SHI/SHI.exe";

        [MenuItem("SHI/메인 씬 다시 만들기")]
        public static void Setup()
        {
            Directory.CreateDirectory(ResourcesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();

            MakeMaterial("Lit", Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            MakeMaterial("Glow", AssetDatabase.LoadAssetAtPath<Shader>("Assets/SHI/Shaders/SHIGlow.shader"));
            var profile = MakeProfile();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.06f);
            cameraObject.transform.position = new Vector3(0f, -2.95f, -10f);
            cameraObject.AddComponent<AudioListener>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            var light = new GameObject("Global Light").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.95f;

            new GameObject("SHI Game").AddComponent<GameController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SHI] 메인 씬을 만들었습니다: " + ScenePath);
        }

        static void MakeMaterial(string name, Shader shader)
        {
            if (shader == null) throw new System.InvalidOperationException("셰이더를 찾을 수 없습니다: " + name);
            string path = $"{ResourcesDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        static VolumeProfile MakeProfile()
        {
            string path = $"{ResourcesDir}/PostFX.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.72f);
            bloom.intensity.Override(1.15f);
            bloom.scatter.Override(0.68f);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.5f);
            AssetDatabase.AddObjectToAsset(vignette, profile);

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(14f);
            color.saturation.Override(8f);
            AssetDatabase.AddObjectToAsset(color, profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        [MenuItem("SHI/Windows 빌드")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[SHI] 빌드 결과: {report.summary.result}, 오류 {report.summary.totalErrors}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        public static void SetupAndBuild()
        {
            Setup();
            Build();
        }
    }
}
