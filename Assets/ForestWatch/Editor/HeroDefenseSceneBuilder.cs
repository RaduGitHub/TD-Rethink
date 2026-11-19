using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForestWatch.Editor
{
    public static class HeroDefenseSceneBuilder
    {
        public const string ScenePath = "Assets/ForestWatch/Scenes/ForestWatch.unity";

        [MenuItem("Tools/Forest Watch/Open Playable Prototype")]
        public static void OpenPrototype()
        {
            DefenseLevelEditorTools.EnsureSampleLevels();
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else CreateScene();
        }

        public static void CreateScene()
        {
            DefenseLevelEditorTools.EnsureSampleLevels();
            RealmVisuals.ApplyLevel(DefenseLevelCatalog.SelectedLevel);
            EnsureFolder("Assets/ForestWatch/Scenes");
            EnsureFolder("Assets/ForestWatch/Art/Materials");
            EnsureFolder("Assets/ForestWatch/Art/Meshes");
            var previous = SceneManager.GetActiveScene();
            if (previous.IsValid() && string.IsNullOrEmpty(previous.path) && previous.rootCount > 0)
                EditorSceneManager.SaveScene(previous, "Assets/ForestWatch/Scenes/PreviousSetup.unity");
            else if (previous.IsValid() && previous.isDirty && !string.IsNullOrEmpty(previous.path))
                EditorSceneManager.SaveScene(previous);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Forest Watch");
            var world = new GameObject("Environment").transform;
            world.SetParent(root.transform, false);
            RealmVisuals.BuildMap(world);
            root.AddComponent<HeroDefenseGame>();
            PersistArt(world);
            PlayerSettings.companyName = "Forest Watch Prototype";
            PlayerSettings.productName = "Forest Watch";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 90;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0, 0), Quaternion.Euler(50, -35, 0), 35);
            Debug.Log("Forest Watch ready. Open ForestWatch.unity and press Play.");
        }

        static void PersistArt(Transform root)
        {
            EnsureFolder("Assets/ForestWatch/Art/Materials");
            EnsureFolder("Assets/ForestWatch/Art/Meshes");
            var materials = new Dictionary<Material, Material>();
            var meshes = new Dictionary<Mesh, Mesh>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    var material = assigned[i];
                    if (material == null || AssetDatabase.Contains(material)) continue;
                    if (!materials.TryGetValue(material, out var saved))
                    {
                        string key = SafeName(material.name + "-" + material.shader.name) + "-" + ColorUtility.ToHtmlStringRGBA(material.color);
                        string path = "Assets/ForestWatch/Art/Materials/" + key + ".mat";
                        saved = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (saved == null) { AssetDatabase.CreateAsset(material, path); saved = material; }
                        materials[material] = saved;
                    }
                    assigned[i] = saved;
                }
                renderer.sharedMaterials = assigned;
            }
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || AssetDatabase.Contains(mesh)) continue;
                if (!meshes.TryGetValue(mesh, out var saved))
                {
                    string path = "Assets/ForestWatch/Art/Meshes/Generated-" + MeshKey(mesh) + ".asset";
                    saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
                    meshes[mesh] = saved;
                }
                filter.sharedMesh = saved;
            }
        }

        static string MeshKey(Mesh mesh)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                foreach (var vertex in mesh.vertices) { writer.Write(vertex.x); writer.Write(vertex.y); writer.Write(vertex.z); }
                foreach (int triangle in mesh.triangles) writer.Write(triangle);
                foreach (var uv in mesh.uv) { writer.Write(uv.x); writer.Write(uv.y); }
                writer.Flush();
                using (var hash = SHA256.Create())
                    return System.BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").Substring(0, 20);
            }
        }

        public static void PreviewLevel(DefenseLevel level)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Stop Play mode before previewing a level."); return; }
            if (level == null) return;
            DefenseLevelCatalog.SelectLevel(level);
            RealmVisuals.ApplyLevel(level);
            if (!File.Exists(ScenePath)) CreateScene();
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                var current = SceneManager.GetActiveScene();
                if (current.isDirty && !string.IsNullOrEmpty(current.path)) EditorSceneManager.SaveScene(current);
                EditorSceneManager.OpenScene(ScenePath);
            }
            var game = Object.FindAnyObjectByType<HeroDefenseGame>();
            if (game == null) { CreateScene(); game = Object.FindAnyObjectByType<HeroDefenseGame>(); }
            var oldWorld = game.transform.Find("Environment");
            if (oldWorld != null) Undo.DestroyObjectImmediate(oldWorld.gameObject);
            var world = new GameObject("Environment").transform;
            world.SetParent(game.transform, false);
            Undo.RegisterCreatedObjectUndo(world.gameObject, "Preview defense level");
            RealmVisuals.BuildMap(world);
            PersistArt(world);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeObject = level;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) { sceneView.LookAt(Vector3.zero, Quaternion.Euler(62, 0, 0), 30); sceneView.Repaint(); }
        }

        static string SafeName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value;
        }

        public static void ApplyPresentation()
        {
            var camera = Camera.main;
            var target = new Vector3(0, 0, -3.8f);
            camera.transform.position = target + new Vector3(0, 36, -25);
            camera.transform.LookAt(target);
            camera.orthographicSize = 15.8f;
            RenderSettings.ambientLight = new Color(.27f, .32f, .32f);
            var sun = RenderSettings.sun;
            sun.intensity = .85f;
            sun.color = new Color(1, .95f, .85f);
            sun.shadowStrength = .72f;
            var colors = new Dictionary<string, string> {
                { "Meadow Surface", "#5B813E" }, { "Path Shoulder", "#4F7137" },
                { "Golden Earth Path", "#AD935F" }, { "Path Center", "#B9A16D" }
            };
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
                if (colors.TryGetValue(renderer.name, out string color))
                {
                    ColorUtility.TryParseHtmlString(color, out Color value);
                    renderer.sharedMaterial.color = value;
                    EditorUtility.SetDirty(renderer.sharedMaterial);
                }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        [MenuItem("Tools/Forest Watch/Build Windows Playable")]
        public static void BuildWindows()
        {
            DefenseLevelEditorTools.EnsureSampleLevels();
            if (!File.Exists(ScenePath)) CreateScene();
            Directory.CreateDirectory("Builds/ForestWatch");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/ForestWatch/ForestWatch.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Forest Watch build failed: " + report.summary.result);
            Debug.Log("Playable built: Builds/ForestWatch/ForestWatch.exe");
        }
    }
}
