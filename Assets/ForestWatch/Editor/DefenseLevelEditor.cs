using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestWatch.Editor
{
    [CustomEditor(typeof(DefenseLevel))]
    public sealed class DefenseLevelEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var level = (DefenseLevel)target;
            root.Add(new HelpBox("This asset defines a complete level. Duplicate it with the button below, change its name and layout, then Preview. Levels saved in Resources/ForestWatch/Levels appear in Choose Level automatically.", HelpBoxMessageType.Info));
            if (!AssetDatabase.GetAssetPath(level).StartsWith(DefenseLevelEditorTools.LevelsFolder + "/", StringComparison.OrdinalIgnoreCase))
                root.Add(new HelpBox("Move this asset into Assets/ForestWatch/Resources/ForestWatch/Levels to include it in Choose Level and in the Windows build.", HelpBoxMessageType.Warning));
            var preview = new Button(() => DefenseLevelEditorTools.Preview(level)) { text = "Preview Level in Scene" };
            var duplicate = new Button(() => DefenseLevelEditorTools.Duplicate(level)) { text = "Duplicate as a New Level" };
            var handles = new Toggle("Edit layout handles in Scene view") { value = DefenseLevelSceneHandles.Enabled };
            handles.RegisterValueChangedCallback(evt => { DefenseLevelSceneHandles.Enabled = evt.newValue; SceneView.RepaintAll(); });
            root.Add(preview); root.Add(duplicate); root.Add(handles);
            var validation = new HelpBox();
            root.Add(validation);
            void Refresh()
            {
                var errors = level.GetValidationErrors();
                string duplicateId = DefenseLevelEditorTools.DuplicateIdMessage(level);
                if (!string.IsNullOrEmpty(duplicateId)) errors.Add(duplicateId);
                validation.text = errors.Count == 0 ? "Ready to play. Blue dots = route; gold rings = foundations; green = hero; orange = wild camp. Click a marker, then drag its move gizmo. Preview again after changing the layout." : string.Join("\n", errors);
                validation.messageType = errors.Count == 0 ? HelpBoxMessageType.Info : HelpBoxMessageType.Error;
                preview.SetEnabled(errors.Count == 0 && !EditorApplication.isPlayingOrWillChangePlaymode);
                duplicate.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                handles.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            }
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            root.TrackSerializedObjectValue(serializedObject, _ => { Refresh(); SceneView.RepaintAll(); });
            root.schedule.Execute(Refresh).Every(350);
            Refresh();
            return root;
        }
    }

    public static class DefenseLevelEditorTools
    {
        public const string LevelsFolder = "Assets/ForestWatch/Resources/ForestWatch/Levels";

        public static void EnsureSampleLevels()
        {
            EnsureFolder(LevelsFolder);
            CreateIfMissing("01_ForestCrossing", level => {
                level.levelId = "forest-crossing"; level.displayName = "Forest Crossing"; level.sortOrder = 0;
                level.description = "The original greenwood crossing. Learn to combine towers and your ranger across ten waves.";
            });
            CreateIfMissing("02_AmberHollow", level => {
                level.levelId = "amber-hollow"; level.displayName = "Amber Hollow"; level.sortOrder = 1;
                level.description = "An autumn switchback through golden woodland. Eight waves, stronger raiders, and a generous opening treasury.";
                level.biome = DefenseBiome.Autumn; level.seed = 207314;
                level.startingGold = 350; level.waveCount = 8; level.enemyHealthMultiplier = 1.15f; level.enemySpeedMultiplier = 1.05f;
                level.grassColor = Hex(0x8D8144); level.pathColor = Hex(0xA37C4C); level.pathCenterColor = Hex(0xC09D64);
                level.foliageDark = Hex(0x92562E); level.foliageMid = Hex(0xC47C35); level.foliageLight = Hex(0xD9AE4B); level.backgroundColor = Hex(0x312D28);
                level.route = Points(-19,8, -13,8, -13,-5, -7,-8, -1,-8, -1,6, 6,6, 6,-5, 13,-5, 17,-1, 17,8);
                level.buildSites = Points(-16,4, -10,4, -16,-3, -9,-4, -6,-4, -4,1, 2,1, 2,-4, 9,2, 11,-1, 14,3, 13,-8);
                level.heroStart = new Vector3(-8,.2f,1); level.campPosition = new Vector3(-6,.2f,7);
            });
            CreateIfMissing("03_FrostboundPass", level => {
                level.levelId = "frostbound-pass"; level.displayName = "Frostbound Pass"; level.sortOrder = 2;
                level.description = "A frozen pass with long turns and tougher enemies. Position your ranger carefully and guard the final approach.";
                level.biome = DefenseBiome.Winter; level.seed = 900318;
                level.startingGold = 400; level.waveCount = 8; level.enemyHealthMultiplier = 1.3f; level.enemySpeedMultiplier = 1.1f;
                level.grassColor = Hex(0x8EAAB3); level.pathColor = Hex(0x667E8C); level.pathCenterColor = Hex(0x879AA5);
                level.foliageDark = Hex(0x385B63); level.foliageMid = Hex(0x507C82); level.foliageLight = Hex(0xBBD9DD); level.backgroundColor = Hex(0x1D303F);
                level.route = Points(-19,-8, -12,-8, -12,5, -5,8, 2,8, 2,-5, 9,-5, 9,4, 16,7, 18,3, 18,-8);
                level.buildSites = Points(-16,-4, -9,-4, -16,3, -9,3, -5,4, -2,0, 5,2, 5,-2, 13,0, 15,-4, 14,-8);
                level.heroStart = new Vector3(-5,.2f,-2); level.campPosition = new Vector3(-5,.2f,-8);
            });
            AssetDatabase.SaveAssets();
            DefenseLevelCatalog.Refresh();
        }

        static void CreateIfMissing(string name, Action<DefenseLevel> configure)
        {
            string path = LevelsFolder + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<DefenseLevel>(path) != null) return;
            var level = ScriptableObject.CreateInstance<DefenseLevel>();
            configure(level);
            var errors = level.GetValidationErrors();
            if (errors.Count > 0) throw new InvalidOperationException(level.displayName + ": " + string.Join("; ", errors));
            AssetDatabase.CreateAsset(level, path);
        }

        public static void Preview(DefenseLevel level)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || level == null || !level.IsPlayable) return;
            var preview = typeof(HeroDefenseSceneBuilder).GetMethod("PreviewLevel");
            if (preview == null) { Debug.LogError("The prototype scene builder does not yet expose PreviewLevel."); return; }
            preview.Invoke(null, new object[] { level });
            Selection.activeObject = level;
            SceneView.RepaintAll();
        }

        public static void Duplicate(DefenseLevel source)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || source == null) return;
            EnsureFolder(LevelsFolder);
            var copy = UnityEngine.Object.Instantiate(source);
            copy.levelId = "level-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            copy.displayName = source.displayName + " Copy";
            copy.sortOrder = source.sortOrder + 1;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string fileName = string.IsNullOrEmpty(sourcePath) ? "NewLevel" : Path.GetFileNameWithoutExtension(sourcePath);
            AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath(LevelsFolder + "/" + fileName + " Copy.asset"));
            AssetDatabase.SaveAssets();
            DefenseLevelCatalog.Refresh();
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
        }

        public static string DuplicateIdMessage(DefenseLevel level)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DefenseLevel", new[] { LevelsFolder }))
            {
                var other = AssetDatabase.LoadAssetAtPath<DefenseLevel>(AssetDatabase.GUIDToAssetPath(guid));
                if (other != null && other != level && other.levelId == level.levelId)
                    return "Level Id is also used by " + other.displayName + ". Give this level a different ID so the selected level can be remembered correctly.";
            }
            return null;
        }

        [MenuItem("Tools/Forest Watch/Preview Selected Level")]
        static void PreviewSelected() => Preview(Selection.activeObject as DefenseLevel);
        [MenuItem("Tools/Forest Watch/Preview Selected Level", true)]
        static bool CanPreview() => Selection.activeObject is DefenseLevel && !EditorApplication.isPlayingOrWillChangePlaymode;

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
        static Vector3[] Points(params float[] coordinates)
        {
            var points = new Vector3[coordinates.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector3(coordinates[i * 2], .2f, coordinates[i * 2 + 1]);
            return points;
        }
        static Color Hex(int value) => new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
    }

    [InitializeOnLoad]
    static class DefenseLevelSceneHandles
    {
        static int selectedKind, selectedIndex;
        static DefenseLevel previous;
        public static bool Enabled { get => SessionState.GetBool("ForestWatch.LevelHandles", true); set => SessionState.SetBool("ForestWatch.LevelHandles", value); }
        static DefenseLevelSceneHandles() { SceneView.duringSceneGui += Draw; }

        static void Draw(SceneView view)
        {
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode || !(Selection.activeObject is DefenseLevel level)) return;
            if (previous != level) { previous = level; selectedKind = 0; selectedIndex = 0; }
            if (selectedKind == 0)
            {
                if (level.route == null || level.route.Length == 0) selectedKind = 2;
                else selectedIndex = Mathf.Clamp(selectedIndex, 0, level.route.Length - 1);
            }
            else if (selectedKind == 1)
            {
                if (level.buildSites == null || level.buildSites.Length == 0) selectedKind = 2;
                else selectedIndex = Mathf.Clamp(selectedIndex, 0, level.buildSites.Length - 1);
            }
            Handles.color = new Color(.4f,.7f,.8f,.5f);
            Handles.DrawWireCube(new Vector3(0,.2f,0), new Vector3(40,0,20));
            if (level.route != null && level.route.Length >= 2)
            {
                Handles.color = Color.cyan;
                Handles.DrawAAPolyLine(4, level.route);
            }
            Markers(level.route, 0, Color.cyan, "Route ");
            Markers(level.buildSites, 1, new Color(1,.75f,.25f), "Build ");
            Markers(new[] { level.heroStart }, 2, Color.green, "Hero");
            Markers(new[] { level.campPosition }, 3, new Color(1,.4f,.15f), "Camp");
            Vector3 point = selectedKind == 0 ? Get(level.route, selectedIndex) : selectedKind == 1 ? Get(level.buildSites, selectedIndex) : selectedKind == 2 ? level.heroStart : level.campPosition;
            Vector3 moved = Handles.PositionHandle(point, Quaternion.identity);
            if (moved != point)
            {
                Undo.RecordObject(level, "Move level marker");
                moved = new Vector3(Mathf.Clamp(moved.x,-20,20), .2f, Mathf.Clamp(moved.z,-10,10));
                if (selectedKind == 0 && level.route != null && selectedIndex < level.route.Length) level.route[selectedIndex] = moved;
                else if (selectedKind == 1 && level.buildSites != null && selectedIndex < level.buildSites.Length) level.buildSites[selectedIndex] = moved;
                else if (selectedKind == 2) level.heroStart = moved;
                else if (selectedKind == 3) level.campPosition = moved;
                EditorUtility.SetDirty(level);
                view.Repaint();
            }
        }

        static Vector3 Get(Vector3[] points, int index) => points != null && points.Length > 0 ? points[Mathf.Clamp(index,0,points.Length-1)] : new Vector3(0,.2f,0);

        static void Markers(Vector3[] points, int kind, Color color, string label)
        {
            if (points == null) return;
            Handles.color = color;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 point = points[i];
                if (kind == 1) Handles.DrawWireDisc(point, Vector3.up, 1.1f);
                if (kind == 3) Handles.DrawWireDisc(point, Vector3.up, 2.4f);
                if (Handles.Button(point, Quaternion.identity, .22f, .3f, Handles.SphereHandleCap)) { selectedKind = kind; selectedIndex = i; SceneView.RepaintAll(); }
                Handles.Label(point + Vector3.up * .4f, label + (kind < 2 ? (i + 1).ToString() : ""));
            }
        }
    }
}
