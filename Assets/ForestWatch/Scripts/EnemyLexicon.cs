using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestWatch
{
    [Serializable]
    public sealed class LexiconRecord
    {
        public int encounterCount;
        public int killCount;
        public int lastWave;
        public float lastHealth;
        public float lastDamage;
        public float lastSpeed;
        public float highestHealth;
    }

    /// <summary>Persistent discoveries, updated only by actual encounters and kills.</summary>
    public static class EnemyLexicon
    {
        public const string PersistenceKeyPrefix = "ForestWatch.EnemyLexicon.v1.";
        static readonly Dictionary<string, LexiconRecord> Records = new Dictionary<string, LexiconRecord>(StringComparer.Ordinal);

        public static int DiscoveredCount
        {
            get
            {
                int count = 0;
                foreach (var definition in EnemyCatalog.All)
                    if (definition != null && IsDiscovered(definition.id)) count++;
                return count;
            }
        }

        public static bool IsDiscovered(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && Load(id).encounterCount > 0;
        }

        public static LexiconRecord GetRecord(string id)
        {
            var record = string.IsNullOrWhiteSpace(id) ? new LexiconRecord() : Load(id);
            return new LexiconRecord {
                encounterCount = record.encounterCount,
                killCount = record.killCount,
                lastWave = record.lastWave,
                lastHealth = record.lastHealth,
                lastDamage = record.lastDamage,
                lastSpeed = record.lastSpeed,
                highestHealth = record.highestHealth
            };
        }

        public static void RegisterEncounter(EnemyDefinition definition, int wave, float healthMultiplier, float speedMultiplier)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id)) return;
            var record = Load(definition.id);
            if (record.encounterCount < int.MaxValue) record.encounterCount++;
            record.lastWave = definition.neutral ? Mathf.Max(0, wave) : Mathf.Max(1, wave);
            record.lastHealth = Positive(EnemyCatalog.HealthFor(definition, record.lastWave, Positive(healthMultiplier, 1)), Positive(definition.baseHealth, 1));
            record.lastDamage = NonNegative(EnemyCatalog.DamageFor(definition, record.lastWave), NonNegative(definition.heroDamage));
            record.lastSpeed = Positive(EnemyCatalog.SpeedFor(definition, Positive(speedMultiplier, 1)), Positive(definition.moveSpeed, 1));
            record.highestHealth = Mathf.Max(record.highestHealth, record.lastHealth);
            Save(definition.id, record);
        }

        public static void RegisterKill(EnemyDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id)) return;
            var record = Load(definition.id);
            // A kill cannot reveal an entry that was never encountered in gameplay.
            if (record.encounterCount <= 0) return;
            if (record.killCount < record.encounterCount) record.killCount++;
            Save(definition.id, record);
        }

        /// <summary>Re-read saved discoveries without deleting or changing them.</summary>
        public static void Reload() => Records.Clear();

        static LexiconRecord Load(string id)
        {
            if (Records.TryGetValue(id, out var cached)) return cached;
            LexiconRecord record = null;
            string json = PlayerPrefs.GetString(PersistenceKeyPrefix + id, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try { record = JsonUtility.FromJson<LexiconRecord>(json); }
                catch (Exception) { record = null; }
            }
            if (record == null) record = new LexiconRecord();
            Normalize(record);
            Records[id] = record;
            return record;
        }

        static void Save(string id, LexiconRecord record)
        {
            Normalize(record);
            PlayerPrefs.SetString(PersistenceKeyPrefix + id, JsonUtility.ToJson(record));
            PlayerPrefs.Save();
        }

        static void Normalize(LexiconRecord record)
        {
            record.encounterCount = Mathf.Max(0, record.encounterCount);
            record.killCount = Mathf.Clamp(record.killCount, 0, record.encounterCount);
            record.lastWave = Mathf.Max(0, record.lastWave);
            record.lastHealth = NonNegative(record.lastHealth);
            record.lastDamage = NonNegative(record.lastDamage);
            record.lastSpeed = NonNegative(record.lastSpeed);
            record.highestHealth = Mathf.Max(NonNegative(record.highestHealth), record.lastHealth);
        }

        static float NonNegative(float value, float fallback = 0)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 ? value : fallback;
        }

        static float Positive(float value, float fallback)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0 ? value : fallback;
        }
    }
}
