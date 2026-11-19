using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestWatch
{
    public static class DefenseLevelCatalog
    {
        const string PreferenceKey = "ForestWatch.SelectedLevelId";
        static DefenseLevel selected;
        static DefenseLevel fallback;
        static readonly HashSet<string> ReportedProblems = new HashSet<string>();

        public static DefenseLevel[] GetLevels()
        {
            var candidates = Resources.LoadAll<DefenseLevel>("ForestWatch/Levels")
                .Where(level => level != null)
                .OrderBy(level => level.sortOrder)
                .ThenBy(level => level.displayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(level => level.name, StringComparer.Ordinal).ToArray();
            var levels = new List<DefenseLevel>();
            var identifiers = new Dictionary<string, DefenseLevel>(StringComparer.Ordinal);
            foreach (var level in candidates)
            {
                var errors = level.GetValidationErrors();
                if (errors.Count > 0)
                {
                    if (ReportedProblems.Add("invalid:" + level.name + ":" + level.levelId))
                        Debug.LogWarning("Level '" + level.name + "' is hidden from Choose Level until fixed: " + string.Join("; ", errors), level);
                    continue;
                }
                if (identifiers.TryGetValue(level.levelId, out var first))
                {
                    if (ReportedProblems.Add("duplicate:" + level.levelId))
                        Debug.LogWarning("Levels '" + first.name + "' and '" + level.name + "' share Level Id '" + level.levelId + "'. Only the first is shown. Change the duplicate's Level Id in its Inspector.", level);
                    continue;
                }
                identifiers.Add(level.levelId, level);
                levels.Add(level);
            }
            return levels.Count > 0 ? levels.ToArray() : new[] { Fallback };
        }

        public static DefenseLevel SelectedLevel
        {
            get
            {
                if (selected != null && selected.IsPlayable) return selected;
                var levels = GetLevels();
                string id = PlayerPrefs.GetString(PreferenceKey, "forest-crossing");
                selected = Array.Find(levels, level => level.levelId == id) ?? levels[0];
                return selected;
            }
        }

        public static void SelectLevel(DefenseLevel level)
        {
            if (level == null || !level.IsPlayable) return;
            selected = level;
            PlayerPrefs.SetString(PreferenceKey, level.levelId);
            PlayerPrefs.Save();
        }

        public static void Refresh() { selected = null; ReportedProblems.Clear(); }

        static DefenseLevel Fallback
        {
            get
            {
                if (fallback != null) return fallback;
                fallback = ScriptableObject.CreateInstance<DefenseLevel>();
                fallback.name = "Forest Crossing (Fallback)";
                fallback.levelId = "forest-crossing";
                fallback.displayName = "Forest Crossing";
                fallback.description = "Hold the forest road and defend the sanctuary.";
                fallback.sortOrder = 0;
                fallback.hideFlags = HideFlags.DontSave;
                return fallback;
            }
        }
    }
}
