using System;
using UnityEngine;

namespace ForestWatch
{
    /// <summary>The shared source for combat statistics and lexicon descriptions.</summary>
    public sealed class EnemyDefinition
    {
        public readonly int kind, goldReward, experience, gateDamage, firstWave;
        public readonly string id, displayName, role, description, tactics;
        public readonly float baseHealth, moveSpeed, heroDamage, attackInterval, attackRange, armor;
        public readonly float healPerPulse, healRadius, healInterval, healthBarHeight;
        public readonly bool neutral, boss;
        public readonly Color accent;

        public EnemyDefinition(int kind, string id, string displayName, string role, string description,
            string tactics, float health, float speed, float damage, float attackInterval, float range,
            float armor, int gold, int experience, int gateDamage, int firstWave, Color accent,
            float healthBarHeight = 2f, bool neutral = false, bool boss = false,
            float healPerPulse = 0, float healRadius = 0, float healInterval = 0)
        {
            this.kind = kind; this.id = id; this.displayName = displayName; this.role = role;
            this.description = description; this.tactics = tactics; baseHealth = health; moveSpeed = speed;
            heroDamage = damage; this.attackInterval = attackInterval; attackRange = range;
            this.armor = Mathf.Clamp(armor, 0, .9f); goldReward = gold; this.experience = experience;
            this.gateDamage = gateDamage; this.firstWave = firstWave; this.accent = accent;
            this.healthBarHeight = healthBarHeight; this.neutral = neutral; this.boss = boss;
            this.healPerPulse = healPerPulse; this.healRadius = healRadius; this.healInterval = healInterval;
        }
    }

    public static class EnemyCatalog
    {
        public static EnemyDefinition[] All { get; } = new[] {
            new EnemyDefinition(0, "goblin-raider", "Goblin Raider", "Raider",
                "A reliable foot soldier with balanced health, speed, and melee damage.",
                "A good early target for Arrow Watches. Keep several towers covering the same stretch of road.",
                48, 1.85f, 9, 1.15f, 1.7f, 0, 10, 8, 1, 1, Hex(0x87AD5C), 1.9f),
            new EnemyDefinition(1, "bone-runner", "Bone Runner", "Runner",
                "A quick skeleton that slips through gaps in tower coverage. Its attack becomes stronger in later waves.",
                "Frost Spires buy time for your defenses. Farthest Along targeting helps catch runners before the gate.",
                65, 2.3f, 9, 1.15f, 1.7f, 0, 10, 8, 1, 1, Hex(0xDCD5BA), 1.9f),
            new EnemyDefinition(2, "iron-brute", "Iron Brute", "Armored bruiser",
                "A slow, armored melee fighter. Its plating reduces incoming hero and tower damage by 20%.",
                "Use Tankiest targeting to concentrate damage. Upgraded towers and your ranger can bring it down together.",
                150, 1.3f, 16, 1.35f, 1.8f, .2f, 22, 14, 1, 3, Hex(0x8799A3), 2.65f),
            new EnemyDefinition(3, "siege-ogre", "Siege Ogre", "Final-wave boss",
                "A towering siege beast with a huge health pool and a punishing melee attack. Reaching the gate costs five lives.",
                "Save Nature Nova for its approach, upgrade your strongest positions, and keep the ranger out of melee reach.",
                1400, 1.3f, 32, 1.5f, 2.1f, .1f, 150, 100, 5, 0, Hex(0xB9A16A), 3.75f, boss: true),
            new EnemyDefinition(4, "ancient-guardian", "Ancient Guardian", "Wild guardian",
                "The optional guardian protects a treasure camp. It fights when challenged and retreats if the ranger leaves its territory.",
                "Challenge it between waves. Keep moving and use Nature Nova; defeating it rewards gold, equipment, and experience.",
                420, 1.3f, 18, 1.3f, 2.1f, .1f, 100, 65, 0, 0, Hex(0xB7A17B), 3.3f, neutral: true),
            new EnemyDefinition(5, "bloodblade", "Bloodblade", "Glass cannon",
                "A lightly armored skirmisher with twin blades. Very low health, very fast movement, and rapid, high-damage strikes against the ranger.",
                "Do not let it surround the ranger. Slow it with frost or destroy it with rapid fire before it reaches melee range.",
                32, 3.5f, 28, .8f, 1.7f, 0, 14, 10, 1, 2, Hex(0xD65349), 1.9f),
            new EnemyDefinition(6, "stoneguard", "Stoneguard", "Tank",
                "A heavily plated living bulwark. It moves slowly, absorbs 35% of incoming damage, and costs two gate lives if it gets through.",
                "Tankiest targeting accounts for its armor as well as its maximum health. Focus several upgraded towers on it early.",
                260, .95f, 12, 1.6f, 1.9f, .35f, 26, 20, 2, 3, Hex(0xA7B5C0), 3f),
            new EnemyDefinition(7, "hexcaster", "Hexcaster", "Ranged attacker",
                "A fragile spellcaster that can strike the ranger from a distance. It stops to cast whenever the ranger is within its reach.",
                "Avoid standing beside its route while distracted. Close in with the ranger or let overlapping towers remove it.",
                55, 1.5f, 20, 1.7f, 5.5f, 0, 18, 14, 1, 4, Hex(0xB58BD4), 2.35f),
            new EnemyDefinition(8, "moss-mender", "Moss Mender", "Healer",
                "A woodland support creature that restores nearby allies' health in repeated pulses. Healing cannot exceed an ally's maximum health.",
                "Remove it before attacking a durable group. Cannon splash and Nature Nova can damage the healer and its escorts together.",
                80, 1.35f, 6, 1.4f, 1.7f, 0, 20, 16, 1, 5, Hex(0x78CA93), 2.45f,
                healPerPulse: 12, healRadius: 3.5f, healInterval: 3)
        };

        public static EnemyDefinition Get(int kind)
        {
            foreach (var definition in All) if (definition.kind == kind) return definition;
            return All[0];
        }

        public static EnemyDefinition Get(string id)
        {
            foreach (var definition in All)
                if (string.Equals(definition.id, id, StringComparison.Ordinal)) return definition;
            return null;
        }

        public static float HealthFor(EnemyDefinition definition, int wave, float healthMultiplier)
        {
            float waveScale = definition.neutral ? 1 : 1 + (Mathf.Max(1, wave) - 1) * .25f;
            return definition.baseHealth * waveScale * Mathf.Max(.1f, healthMultiplier);
        }

        public static float DamageFor(EnemyDefinition definition, int wave)
        {
            float scaling = definition.kind == 0 || definition.kind == 1 ? 1 : definition.kind == 5 || definition.kind == 7 ? .5f : 0;
            return definition.heroDamage + (Mathf.Max(1, wave) - 1) * scaling;
        }

        public static float SpeedFor(EnemyDefinition definition, float multiplier) => definition.moveSpeed * Mathf.Max(.1f, multiplier);
        public static int GoldFor(EnemyDefinition definition, int wave) => definition.goldReward + (definition.boss || definition.neutral ? 0 : Mathf.Max(1, wave));
        public static float EffectiveHealthFor(EnemyDefinition definition, int wave, float healthMultiplier) => HealthFor(definition, wave, healthMultiplier) / (1 - definition.armor);

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
