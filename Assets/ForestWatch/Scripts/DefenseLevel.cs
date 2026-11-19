using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestWatch
{
    public enum DefenseBiome { Forest, Autumn, Winter }

    [CreateAssetMenu(fileName = "NewLevel", menuName = "Forest Watch/Level", order = 10)]
    public sealed class DefenseLevel : ScriptableObject
    {
        [Header("Menu")]
        [Tooltip("Unique identifier used to remember the selected level. Give duplicated levels a new ID.")]
        public string levelId = "level-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        public string displayName = "New Level";
        [TextArea(2, 4)] public string description = "Defend the sanctuary against the incoming raiders.";
        [Tooltip("Lower numbers appear first in Choose Level.")] public int sortOrder = 10;

        [Header("Battle")]
        [Min(0)] public int startingGold = 300;
        [Min(1)] public int startingLives = 20;
        [Range(1, 30)] public int waveCount = 10;
        [Range(.25f, 4)] public float enemyHealthMultiplier = 1;
        [Range(.5f, 2)] public float enemySpeedMultiplier = 1;

        [Header("Landscape")]
        [Tooltip("Changes the deterministic placement of trees, rocks, and flowers.")] public int seed = 41027;
        public DefenseBiome biome = DefenseBiome.Forest;
        public Color grassColor = new Color32(91, 129, 62, 255);
        public Color pathColor = new Color32(173, 147, 95, 255);
        public Color pathCenterColor = new Color32(185, 161, 109, 255);
        public Color foliageDark = new Color32(46, 110, 74, 255);
        public Color foliageMid = new Color32(63, 137, 82, 255);
        public Color foliageLight = new Color32(107, 156, 88, 255);
        public Color backgroundColor = new Color32(24, 46, 52, 255);

        [Header("Layout — X/Z coordinates; ground height is 0.2")]
        [Tooltip("Enemy waypoints in travel order. First point is the portal; last point is the sanctuary.")]
        public Vector3[] route = DefaultRoute();
        [Tooltip("One round foundation per point. Keep centers at least 2.2 units from the road and 2.3 units apart.")]
        public Vector3[] buildSites = DefaultBuildSites();
        public Vector3 heroStart = new Vector3(-5, .2f, -2);
        public Vector3 campPosition = new Vector3(-3, .2f, 7);

        public bool IsPlayable => GetValidationErrors().Count == 0;

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(levelId)) errors.Add("Give this level a unique Level Id.");
            if (string.IsNullOrWhiteSpace(displayName)) errors.Add("Enter a Display Name for the level menu.");
            if (route == null || route.Length < 2) errors.Add("Add at least two route points: portal first, sanctuary last.");
            if (buildSites == null || buildSites.Length == 0) errors.Add("Add at least one build site.");
            if (startingGold < 0 || startingLives < 1 || waveCount < 1 || waveCount > 30) errors.Add("Gold must be non-negative, lives positive, and waves between 1 and 30.");
            if (!(enemyHealthMultiplier > 0) || !(enemySpeedMultiplier > 0) || float.IsInfinity(enemyHealthMultiplier) || float.IsInfinity(enemySpeedMultiplier)) errors.Add("Enemy health and speed multipliers must be finite and positive.");
            if (route != null)
                for (int i = 0; i < route.Length; i++)
                {
                    if (!InsideMap(route[i])) errors.Add("Route point " + (i + 1) + " is outside X ±20 / Z ±10, or is not a finite position.");
                    if (i > 0 && FlatDistance(route[i - 1], route[i]) < .5f) errors.Add("Route points " + i + " and " + (i + 1) + " must be at least 0.5 units apart.");
                }
            if (buildSites != null)
                for (int i = 0; i < buildSites.Length; i++)
                {
                    if (!InsideMap(buildSites[i])) errors.Add("Build site " + (i + 1) + " is outside the map.");
                    if (route != null && route.Length >= 2 && DistanceToRoute(buildSites[i]) < 2.15f) errors.Add("Build site " + (i + 1) + " overlaps the road. Move it at least 2.2 units from the route line.");
                    for (int j = 0; j < i; j++)
                        if (FlatDistance(buildSites[i], buildSites[j]) < 2.3f) { errors.Add("Build sites " + (j + 1) + " and " + (i + 1) + " overlap."); break; }
                }
            if (!InsideMap(heroStart)) errors.Add("Hero Start must be inside the map.");
            if (!InsideMap(campPosition)) errors.Add("Camp Position must be inside the map.");
            return errors;
        }

        public float DistanceToRoute(Vector3 point)
        {
            float distance = float.MaxValue;
            if (route == null) return distance;
            point.y = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector3 a = route[i], b = route[i + 1]; a.y = b.y = 0;
                Vector3 direction = b - a;
                if (direction.sqrMagnitude < .001f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(point - a, direction) / direction.sqrMagnitude);
                distance = Mathf.Min(distance, Vector3.Distance(point, a + direction * t));
            }
            return distance;
        }

        static bool InsideMap(Vector3 point) => !float.IsNaN(point.x) && !float.IsNaN(point.z) && !float.IsInfinity(point.x) && !float.IsInfinity(point.z) && Mathf.Abs(point.x) <= 20 && Mathf.Abs(point.z) <= 10;
        static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

        public static Vector3[] DefaultRoute() => new[] {
            new Vector3(-19,.2f,7), new Vector3(-12,.2f,7), new Vector3(-10,.2f,5), new Vector3(-10,.2f,-3),
            new Vector3(-7,.2f,-6), new Vector3(0,.2f,-6), new Vector3(3,.2f,-3), new Vector3(3,.2f,3),
            new Vector3(6,.2f,6), new Vector3(13,.2f,6), new Vector3(16,.2f,3), new Vector3(16,.2f,-3), new Vector3(13,.2f,-6)
        };

        public static Vector3[] DefaultBuildSites() => new[] {
            new Vector3(-14,.2f,4), new Vector3(-7,.2f,3), new Vector3(-13,.2f,-1), new Vector3(-7,.2f,-2),
            new Vector3(-5,.2f,-9), new Vector3(-2,.2f,-3), new Vector3(0,.2f,0), new Vector3(6,.2f,0),
            new Vector3(7,.2f,3), new Vector3(11,.2f,9), new Vector3(13,.2f,1), new Vector3(12,.2f,-3)
        };
    }
}
