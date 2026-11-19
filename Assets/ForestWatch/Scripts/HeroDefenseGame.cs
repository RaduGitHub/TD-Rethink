using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForestWatch
{
    public enum TowerTargetMode { FarthestAlongRoad, Tankiest }

    /// <summary>Runs the Forest Watch tower defense game, including hero combat, towers, waves, and progression.</summary>
    public sealed class HeroDefenseGame : MonoBehaviour
    {
        [Serializable]
        public class TowerDefinition
        {
            public string title;
            public int cost;
            public float damage, range, attackInterval;
            public Color shotColor;
            public TowerDefinition(string title, int cost, float damage, float range, float interval, Color color)
            { this.title = title; this.cost = cost; this.damage = damage; this.range = range; attackInterval = interval; shotColor = color; }
        }

        [Header("First playable — freely tune these values")]
        [SerializeField] int startingGold = 300;
        [SerializeField] int startingLives = 20;
        [SerializeField] int waveCount = 10;
        [SerializeField] float heroMoveSpeed = 7f;
        [SerializeField] float heroBaseDamage = 24f;
        [SerializeField] float enemyHealthMultiplier = 1f;
        [SerializeField] float enemySpeedMultiplier = 1f;
        [SerializeField] TowerDefinition[] towerDefinitions = {
            new TowerDefinition("Arrow Watch", 75, 24, 6.8f, .72f, new Color(1f,.77f,.3f)),
            new TowerDefinition("Frost Spire", 100, 16, 5.8f, .95f, new Color(.35f,.9f,1f)),
            new TowerDefinition("Cannon Keep", 125, 48, 6.4f, 1.7f, new Color(1f,.42f,.18f))
        };

        public int Gold { get; private set; }
        public int Lives { get; private set; }
        public int Wave { get; private set; }
        public int TotalWaves => waveCount;
        public int EnemiesAlive => enemies.FindAll(e => !e.dead && !e.neutral).Count;
        public int HeroLevel { get; private set; } = 1;
        public int Shards { get; private set; }
        public int EquippedItems { get; private set; }
        public int CraftCost => legendary ? 7 : 5;
        public float HeroHealth { get; private set; }
        public float HeroMaxHealth => 180 + (HeroLevel - 1) * 25 + armorBonus;
        public float HeroExperience { get; private set; }
        public float HeroExperienceRequired => 55 + (HeroLevel - 1) * 35;
        public float SkillCooldown { get; private set; }
        public float SkillMaxCooldown => 12f;
        public bool IsPaused { get; private set; }
        public bool WaveRunning { get; private set; }
        public bool GameOver { get; private set; }
        public bool Victory { get; private set; }
        public bool InMainMenu { get; private set; }
        public bool SettingsOpen { get; private set; }
        public bool LexiconOpen { get; private set; }
        public DefenseLevel ActiveLevel { get; private set; }
        public string LevelName => ActiveLevel != null ? ActiveLevel.displayName : "Forest Crossing";
        public int Speed { get; private set; } = 1;
        public string Message { get; private set; } = "Choose a stone foundation to build your first tower.";
        public bool HasSelectedSite => selectedSite >= 0;
        public bool SelectedHasTower => HasSelectedSite && sites[selectedSite].tower != null;
        public string SelectedTitle => !HasSelectedSite ? "BUILD YOUR DEFENSE" : SelectedHasTower ? towerDefinitions[sites[selectedSite].tower.kind].title.ToUpperInvariant() : "FOUNDATION " + (selectedSite + 1).ToString("00");
        public string SelectedDescription => !HasSelectedSite ? "Select a stone foundation on the battlefield." : SelectedHasTower ? "Upgrade its damage and range, or reclaim part of its gold." : "Choose a tower. Arrow: rapid fire. Frost: slows. Cannon: area damage.";
        public int SelectedTowerLevel => SelectedHasTower ? sites[selectedSite].tower.level : 0;
        public int SelectedUpgradeCost => SelectedHasTower ? UpgradeCost(sites[selectedSite].tower) : 0;
        public int SelectedSellValue => SelectedHasTower ? Mathf.RoundToInt(sites[selectedSite].tower.invested * .7f) : 0;
        public TowerTargetMode SelectedTargetMode => SelectedHasTower ? sites[selectedSite].tower.targetMode : TowerTargetMode.FarthestAlongRoad;
        public int GetTowerCost(int kind) => towerDefinitions[Mathf.Clamp(kind, 0, towerDefinitions.Length - 1)].cost;
        public string EquipmentSummary => legendary ? "Sunforged Bow  +45 ATK  /  Warden Mail  +" + armorBonus + " HP" : EquippedItems == 0 ? "No equipment yet · collect glowing drops" : "Ranger gear  +" + damageBonus + " ATK  /  +" + armorBonus + " HP";

        sealed class Site { public GameObject pad; public Tower tower; }
        sealed class Tower { public int kind, level = 1, invested; public float cooldown; public GameObject model; public TowerTargetMode targetMode; }
        sealed class Enemy
        {
            public GameObject model;
            public EnemyDefinition definition;
            public Transform healthBar, healthFill;
            public float health, maxHealth, speed, damage, attackTimer, slowTimer, phase, healTimer;
            public int routeIndex, kind, reward;
            public bool dead, neutral, awake;
        }
        sealed class Shot
        {
            public GameObject model;
            public Enemy target;
            public Vector3 destination;
            public float damage, speed;
            public int kind;
            public bool hero;
        }
        sealed class Drop { public GameObject model; public float phase; public int quality; }
        sealed class HostileShot { public GameObject model; public float damage, remaining = 3.5f; }
        sealed class Effect { public GameObject model; public float remaining, duration, expansion; public Vector3 scale; }

        readonly List<Site> sites = new List<Site>();
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Drop> drops = new List<Drop>();
        readonly List<Effect> effects = new List<Effect>();
        readonly List<HostileShot> hostileShots = new List<HostileShot>();
        float[] routeDistances;
        Transform actors;
        GameObject hero, destinationMarker, selectionRing, rangeRing;
        Transform heroHealthBar, heroHealthFill;
        HeroDefenseHud hud;
        ForestWatchMenu menu;
        EnemyLexiconView lexicon;
        static bool startOnNextLoad;
        Camera view;
        Vector3 heroDestination;
        Enemy commandedEnemy;
        int selectedSite = -1, spawnRemaining, spawnIndex, kills, damageBonus, armorBonus;
        float spawnTimer, heroAttackTimer, heroRespawnTimer, messageTimer, elapsed;
        bool legendary;
        float initialCameraSize;
        AudioSource audioSource;
        AudioClip buildSound, lootSound, novaSound, waveSound;

        void Awake()
        {
            Time.timeScale = 1;
            Application.runInBackground = true;
            ForestWatchSettings.Initialize();
            ActiveLevel = DefenseLevelCatalog.SelectedLevel;
            RealmVisuals.ApplyLevel(ActiveLevel);
            routeDistances = new float[RealmVisuals.Route.Length];
            for (int i = 1; i < routeDistances.Length; i++)
                routeDistances[i] = routeDistances[i - 1] + FlatDistance(RealmVisuals.Route[i - 1], RealmVisuals.Route[i]);
            if (ActiveLevel != null)
            {
                startingGold = Mathf.Max(0, ActiveLevel.startingGold);
                startingLives = Mathf.Max(1, ActiveLevel.startingLives);
                waveCount = Mathf.Max(1, ActiveLevel.waveCount);
                enemyHealthMultiplier = Mathf.Max(.1f, ActiveLevel.enemyHealthMultiplier);
                enemySpeedMultiplier = Mathf.Max(.1f, ActiveLevel.enemySpeedMultiplier);
            }
            InMainMenu = !startOnNextLoad;
            startOnNextLoad = false;
            Gold = startingGold;
            Lives = startingLives;
            HeroHealth = HeroMaxHealth;
            var world = transform.Find("Environment");
            if (world != null)
            {
                world.gameObject.SetActive(false);
                Destroy(world.gameObject);
            }
            world = new GameObject("Environment").transform;
            world.SetParent(transform, false);
            RealmVisuals.BuildMap(world);
            actors = new GameObject("Battlefield Actors").transform;
            actors.SetParent(transform, false);
            foreach (var position in RealmVisuals.BuildSites)
                sites.Add(new Site { pad = RealmVisuals.CreatePad(actors, position, sites.Count) });
            hero = RealmVisuals.CreateUnit(actors, RealmVisuals.HeroStart, 0, true);
            hero.name = "Ranger Hero";
            heroDestination = hero.transform.position;
            MakeHealthBar(hero.transform, 2.4f, out heroHealthBar, out heroHealthFill);
            view = Camera.main;
            if (view != null) initialCameraSize = view.orthographicSize;
            destinationMarker = RealmVisuals.Ring(actors, heroDestination + Vector3.up * .1f, .45f, new Color(.35f, 1, .72f), .05f);
            destinationMarker.name = "Hero move target";
            destinationMarker.SetActive(false);
            SpawnEnemy(4, true);
            hud = gameObject.AddComponent<HeroDefenseHud>();
            hud.Initialize(this);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = .22f;
            buildSound = Tone(520, .16f);
            lootSound = Tone(880, .22f);
            novaSound = Tone(150, .45f);
            waveSound = Tone(330, .34f);
            var menuObject = new GameObject("Main Menu");
            menu = menuObject.AddComponent<ForestWatchMenu>();
            menu.Initialize(this);
            var lexiconObject = new GameObject("Enemy Lexicon");
            lexicon = lexiconObject.AddComponent<EnemyLexiconView>();
            lexicon.Initialize(this);
            lexicon.Hide();
            hud.SetVisible(!InMainMenu);
            if (InMainMenu) { menu.ShowMainMenu(); Time.timeScale = 0; }
            else menu.Hide();
        }

        void Update()
        {
            if (LexiconOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B)) CloseLexicon();
                return;
            }
            if (InMainMenu || SettingsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (SettingsOpen) CloseSettings();
                    else menu.NavigateBack();
                }
                return;
            }
            HandleInput();
            if (IsPaused || GameOver || LexiconOpen) return;
            float dt = Time.deltaTime;
            elapsed += dt;
            if (messageTimer > 0) messageTimer -= dt;
            if (messageTimer <= 0 && Wave > 0)
                Message = WaveRunning ? "Defend the gate. Your hero fights nearby enemies automatically." : "Wave cleared. Build, upgrade, explore the wild camp, then send the next wave.";
            TickHero(dt);
            TickWaves(dt);
            TickEnemies(dt);
            TickTowers(dt);
            TickShots(dt);
            TickHostileShots(dt);
            TickLoot(dt);
            TickEffects(dt);
        }

        void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.B)) { OpenLexicon(); return; }
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Escape)) { if (HasSelectedSite) DismissSelection(); else TogglePause(); }
            if (GameOver) { if (Input.GetKeyDown(KeyCode.R)) RestartGame(); return; }
            if (Input.GetKeyDown(KeyCode.Q)) CastNova();
            if (Input.GetKeyDown(KeyCode.R)) StartWave();
            if (Input.GetKeyDown(KeyCode.F)) CraftEquipment();
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleSpeed();
            if (IsPaused || view == null) return;
            if (Mathf.Abs(Input.mouseScrollDelta.y) > .01f && (hud == null || !hud.IsPointerOverUi()))
                view.orthographicSize = Mathf.Clamp(view.orthographicSize - Input.mouseScrollDelta.y, initialCameraSize * .72f, initialCameraSize * 1.2f);
            if (Input.GetMouseButtonDown(0) && !hud.IsPointerOverUi() && TryGroundPoint(out var point))
            {
                int closest = -1;
                float distance = 1.8f;
                for (int i = 0; i < sites.Count; i++)
                {
                    float d = FlatDistance(point, sites[i].pad.transform.position);
                    if (d < distance) { distance = d; closest = i; }
                }
                if (closest >= 0) SelectSite(closest);
                else
                {
                    foreach (var enemy in enemies)
                        if (!enemy.dead && FlatDistance(point, enemy.model.transform.position) < 1.6f)
                        { commandedEnemy = enemy; heroDestination = enemy.model.transform.position; Tell(enemy.neutral ? "Wild guardian challenged. Defeat it for gold, equipment, and crafting shards." : "Target marked. Your ranger is moving to engage."); break; }
                }
            }
            if (Input.GetMouseButtonDown(1) && !hud.IsPointerOverUi() && TryGroundPoint(out var destination))
            {
                heroDestination = ClampToMap(destination);
                commandedEnemy = null;
                destinationMarker.transform.position = heroDestination + Vector3.up * .12f;
                destinationMarker.SetActive(true);
            }
        }

        bool TryGroundPoint(out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = view.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter)) { point = ray.GetPoint(enter); return true; }
            point = Vector3.zero; return false;
        }

        void TickHero(float dt)
        {
            SkillCooldown = Mathf.Max(0, SkillCooldown - dt);
            if (HeroHealth <= 0)
            {
                heroRespawnTimer -= dt;
                if (heroRespawnTimer <= 0)
                {
                    hero.transform.position = RealmVisuals.HeroStart;
                    heroDestination = hero.transform.position;
                    HeroHealth = HeroMaxHealth;
                    hero.SetActive(true);
                    Tell("Your ranger has returned to the battlefield.");
                }
                return;
            }
            HeroHealth = Mathf.Min(HeroMaxHealth, HeroHealth + dt * (WaveRunning ? 1.6f : 12f));
            var keyboard = new Vector3((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0), 0,
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            if (keyboard.sqrMagnitude > .01f)
            {
                var forward = view.transform.forward; forward.y = 0; forward.Normalize();
                var right = view.transform.right; right.y = 0; right.Normalize();
                heroDestination = ClampToMap(hero.transform.position + (right * keyboard.x + forward * keyboard.z).normalized * heroMoveSpeed * dt * 1.2f);
                commandedEnemy = null;
                destinationMarker.SetActive(false);
            }
            if (commandedEnemy != null && !commandedEnemy.dead)
            {
                if (commandedEnemy.neutral) commandedEnemy.awake = true;
                var toward = commandedEnemy.model.transform.position - hero.transform.position;
                heroDestination = toward.magnitude > 3.8f ? commandedEnemy.model.transform.position - toward.normalized * 3.3f : hero.transform.position;
            }
            Vector3 old = hero.transform.position;
            hero.transform.position = Vector3.MoveTowards(old, heroDestination, heroMoveSpeed * dt);
            if ((hero.transform.position - old).sqrMagnitude > .0001f) Face(hero.transform, hero.transform.position - old, dt);
            if (FlatDistance(hero.transform.position, heroDestination) < .2f) destinationMarker.SetActive(false);
            heroAttackTimer -= dt;
            Enemy target = commandedEnemy != null && !commandedEnemy.dead && FlatDistance(hero.transform.position, commandedEnemy.model.transform.position) < 5.6f ? commandedEnemy : FindEnemy(hero.transform.position, 5.6f, true);
            if (target != null && heroAttackTimer <= 0)
            {
                heroAttackTimer = .7f;
                Face(hero.transform, target.model.transform.position - hero.transform.position, 1);
                Fire(hero.transform.position + Vector3.up * 1.25f, target, heroBaseDamage + (HeroLevel - 1) * 5 + damageBonus, -1, true, new Color(.55f, 1f, .6f));
            }
            UpdateHealthBar(heroHealthBar, heroHealthFill, HeroHealth / HeroMaxHealth);
        }

        void TickWaves(float dt)
        {
            if (!WaveRunning) return;
            spawnTimer -= dt;
            if (spawnRemaining > 0 && spawnTimer <= 0)
            {
                int kind = Wave == TotalWaves && spawnRemaining == 1 ? 3 :
                    Wave >= 5 && spawnIndex % 9 == 7 ? 8 :
                    Wave >= 4 && spawnIndex % 8 == 6 ? 7 :
                    Wave >= 3 && spawnIndex % 7 == 5 ? 6 :
                    Wave >= 2 && spawnIndex % 5 == 3 ? 5 :
                    Wave >= 3 && spawnIndex % 6 == 4 ? 2 : spawnIndex % 3 == 2 ? 1 : 0;
                SpawnEnemy(kind, false);
                spawnRemaining--; spawnIndex++;
                spawnTimer = Mathf.Max(.42f, 1.05f - Wave * .045f);
            }
            if (spawnRemaining == 0 && EnemiesAlive == 0)
            {
                WaveRunning = false;
                int reward = 45 + Wave * 12;
                Gold += reward;
                Shards++;
                Tell("WAVE " + Wave + " CLEARED  ·  +" + reward + " gold  ·  +1 crafting shard");
                if (Wave == TotalWaves) FinishGame(true);
            }
        }

        void SpawnEnemy(int kind, bool neutral)
        {
            var definition = EnemyCatalog.Get(kind);
            neutral = definition.neutral;
            Vector3 position = neutral ? RealmVisuals.CampPosition : RealmVisuals.Route[0] + Vector3.right * UnityEngine.Random.Range(-.25f, .25f);
            float health = EnemyCatalog.HealthFor(definition, Wave, enemyHealthMultiplier);
            var enemy = new Enemy {
                model = RealmVisuals.CreateUnit(actors, position, kind), definition = definition, kind = kind, neutral = neutral,
                maxHealth = health, health = health, routeIndex = 1, speed = EnemyCatalog.SpeedFor(definition, enemySpeedMultiplier),
                damage = EnemyCatalog.DamageFor(definition, Wave), healTimer = definition.healInterval,
                reward = EnemyCatalog.GoldFor(definition, Wave), phase = UnityEngine.Random.value * 6.28f
            };
            enemy.model.name = definition.displayName;
            MakeHealthBar(enemy.model.transform, definition.healthBarHeight, out enemy.healthBar, out enemy.healthFill);
            if (neutral) RealmVisuals.Ring(enemy.model.transform, new Vector3(0, .08f, 0), 1.3f, new Color(1, .7f, .2f), .05f);
            enemies.Add(enemy);
            if (!InMainMenu) EnemyLexicon.RegisterEncounter(definition, Wave, enemyHealthMultiplier, enemySpeedMultiplier);
        }

        void TickEnemies(float dt)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.dead) continue;
                enemy.slowTimer = Mathf.Max(0, enemy.slowTimer - dt);
                enemy.attackTimer -= dt;
                TickHealer(enemy, dt);
                var pos = enemy.model.transform.position;
                float heroDistance = HeroHealth > 0 ? FlatDistance(pos, hero.transform.position) : 1000;
                if (enemy.neutral)
                {
                    if (!enemy.awake) { UpdateHealthBar(enemy.healthBar, enemy.healthFill, enemy.health / enemy.maxHealth); continue; }
                    if (HeroHealth <= 0 || FlatDistance(hero.transform.position, RealmVisuals.CampPosition) > 10)
                    { enemy.awake = false; enemy.model.transform.position = RealmVisuals.CampPosition; enemy.health = enemy.maxHealth; continue; }
                }
                bool attackHero = heroDistance < enemy.definition.attackRange;
                if (attackHero)
                {
                    Face(enemy.model.transform, hero.transform.position - pos, dt);
                    if (enemy.attackTimer <= 0)
                    {
                        enemy.attackTimer = enemy.definition.attackInterval;
                        if (enemy.definition.attackRange > 2.5f)
                        {
                            var bolt = RealmVisuals.Orb(actors, pos + Vector3.up * 1.5f, .17f, enemy.definition.accent);
                            bolt.name = "Hostile hex bolt";
                            hostileShots.Add(new HostileShot { model = bolt, damage = enemy.damage });
                        }
                        else HurtHero(enemy.damage);
                    }
                }
                else
                {
                    Vector3 goal = enemy.neutral ? hero.transform.position : RealmVisuals.Route[enemy.routeIndex];
                    Vector3 next = Vector3.MoveTowards(pos, goal, enemy.speed * (enemy.slowTimer > 0 ? .45f : 1) * dt);
                    Face(enemy.model.transform, next - pos, dt);
                    enemy.model.transform.position = next;
                    if (!enemy.neutral && FlatDistance(next, goal) < .1f)
                    {
                        enemy.routeIndex++;
                        if (enemy.routeIndex >= RealmVisuals.Route.Length)
                        {
                            Lives = Mathf.Max(0, Lives - enemy.definition.gateDamage);
                            enemy.dead = true;
                            Destroy(enemy.model);
                            Burst(pos + Vector3.up, 1.4f, new Color(1, .2f, .1f), .45f);
                            if (Lives <= 0) { FinishGame(false); return; }
                            Tell("An enemy reached the gate. " + Lives + " lives remain.");
                            continue;
                        }
                    }
                }
                UpdateHealthBar(enemy.healthBar, enemy.healthFill, enemy.health / enemy.maxHealth);
            }
            enemies.RemoveAll(e => e.dead);
        }

        void TickTowers(float dt)
        {
            foreach (var site in sites)
            {
                var tower = site.tower;
                if (tower == null) continue;
                tower.cooldown -= dt;
                var definition = towerDefinitions[tower.kind];
                if (tower.cooldown > 0) continue;
                Enemy target = FindEnemy(site.pad.transform.position, definition.range + (tower.level - 1) * .65f, false, tower.targetMode);
                if (target == null) continue;
                tower.cooldown = definition.attackInterval / (1 + (tower.level - 1) * .12f);
                Fire(site.pad.transform.position + Vector3.up * (2.9f + .25f * tower.level), target, definition.damage * (1 + (tower.level - 1) * .65f), tower.kind, false, definition.shotColor);
            }
        }

        Enemy FindEnemy(Vector3 position, float range, bool includeAwakeNeutral, TowerTargetMode mode = TowerTargetMode.FarthestAlongRoad)
        {
            Enemy best = null;
            float score = float.NegativeInfinity;
            float bestProgress = float.NegativeInfinity;
            float bestDistance = float.PositiveInfinity;
            foreach (var enemy in enemies)
            {
                if (enemy.dead || enemy.neutral && (!includeAwakeNeutral || !enemy.awake)) continue;
                float distance = FlatDistance(position, enemy.model.transform.position);
                if (distance > range) continue;
                float progress = RoadProgress(enemy);
                float priority = mode == TowerTargetMode.Tankiest ? enemy.maxHealth / (1 - enemy.definition.armor) : progress;
                if (priority > score + .001f || Mathf.Abs(priority - score) <= .001f &&
                    (progress > bestProgress + .001f || Mathf.Abs(progress - bestProgress) <= .001f && distance < bestDistance))
                { score = priority; bestProgress = progress; bestDistance = distance; best = enemy; }
            }
            return best;
        }

        float RoadProgress(Enemy enemy)
        {
            if (enemy.neutral) return -FlatDistance(enemy.model.transform.position, hero.transform.position);
            int segment = Mathf.Clamp(enemy.routeIndex - 1, 0, RealmVisuals.Route.Length - 2);
            Vector3 a = RealmVisuals.Route[segment], b = RealmVisuals.Route[segment + 1];
            a.y = b.y = 0;
            Vector3 position = enemy.model.transform.position; position.y = 0;
            Vector3 direction = b - a;
            return routeDistances[segment] + Mathf.Clamp(Vector3.Dot(position - a, direction.normalized), 0, direction.magnitude);
        }

        void HurtHero(float damage)
        {
            if (HeroHealth <= 0) return;
            HeroHealth = Mathf.Max(0, HeroHealth - damage);
            Burst(hero.transform.position + Vector3.up, .3f, new Color(1, .25f, .15f), .2f);
            if (HeroHealth > 0) return;
            heroRespawnTimer = 5;
            hero.SetActive(false);
            commandedEnemy = null;
            Tell("Your ranger fell in battle. Returning in 5 seconds — towers still defend.");
        }

        void TickHostileShots(float dt)
        {
            for (int i = hostileShots.Count - 1; i >= 0; i--)
            {
                var shot = hostileShots[i];
                shot.remaining -= dt;
                if (HeroHealth > 0 && shot.remaining > 0)
                {
                    Vector3 destination = hero.transform.position + Vector3.up;
                    shot.model.transform.position = Vector3.MoveTowards(shot.model.transform.position, destination, 14 * dt);
                    if (Vector3.Distance(shot.model.transform.position, destination) > .25f) continue;
                    HurtHero(shot.damage);
                }
                Destroy(shot.model);
                hostileShots.RemoveAt(i);
            }
        }

        void TickHealer(Enemy healer, float dt)
        {
            var definition = healer.definition;
            if (definition.healPerPulse <= 0) return;
            healer.healTimer -= dt;
            if (healer.healTimer > 0) return;
            healer.healTimer = definition.healInterval;
            bool healed = false;
            foreach (var ally in enemies)
            {
                if (ally == healer || ally.dead || ally.neutral || ally.health >= ally.maxHealth ||
                    FlatDistance(ally.model.transform.position, healer.model.transform.position) > definition.healRadius) continue;
                ally.health = Mathf.Min(ally.maxHealth, ally.health + definition.healPerPulse);
                healed = true;
            }
            if (!healed) return;
            var pulse = RealmVisuals.Ring(actors, healer.model.transform.position + Vector3.up * .3f, 1, definition.accent, .1f);
            effects.Add(new Effect { model = pulse, remaining = .6f, duration = .6f, expansion = definition.healRadius, scale = Vector3.one });
        }

        void Fire(Vector3 from, Enemy target, float damage, int kind, bool fromHero, Color color)
        {
            var model = RealmVisuals.Orb(actors, from, kind == 2 ? .22f : .11f, color);
            model.name = fromHero ? "Ranger bolt" : "Tower projectile";
            shots.Add(new Shot { model = model, target = target, destination = target.model.transform.position + Vector3.up * .9f, damage = damage, kind = kind, hero = fromHero, speed = kind == 2 ? 14 : 24 });
        }

        void TickShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                if (!shot.target.dead && shot.target.model != null) shot.destination = shot.target.model.transform.position + Vector3.up * .9f;
                shot.model.transform.position = Vector3.MoveTowards(shot.model.transform.position, shot.destination, shot.speed * dt);
                if (Vector3.Distance(shot.model.transform.position, shot.destination) > .2f) continue;
                if (shot.kind == 2)
                {
                    var targets = enemies.ToArray();
                    foreach (var enemy in targets)
                        if (!enemy.dead && !enemy.neutral && FlatDistance(enemy.model.transform.position, shot.destination) < 2.6f) Damage(enemy, shot.damage, shot.hero);
                    Burst(shot.destination, 1.4f, new Color(1, .45f, .12f), .25f);
                }
                else if (!shot.target.dead)
                {
                    if (shot.kind == 1) shot.target.slowTimer = 2.5f;
                    Damage(shot.target, shot.damage, shot.hero);
                    Burst(shot.destination, .3f, shot.kind == 1 ? Color.cyan : new Color(1, .8f, .35f), .18f);
                }
                Destroy(shot.model);
                shots.RemoveAt(i);
            }
        }

        void Damage(Enemy enemy, float damage, bool fromHero)
        {
            if (enemy.dead) return;
            enemy.health -= damage * (1 - enemy.definition.armor);
            if (enemy.neutral) enemy.awake = true;
            if (enemy.health > 0) return;
            enemy.dead = true;
            EnemyLexicon.RegisterKill(enemy.definition);
            Gold += enemy.reward;
            kills++;
            GiveExperience(enemy.definition.experience);
            Vector3 location = enemy.model.transform.position;
            Burst(location + Vector3.up * .7f, .6f, enemy.neutral ? new Color(1, .75f, .2f) : new Color(.4f, 1, .6f), .28f);
            if (enemy.neutral || kills % 5 == 0 || enemy.kind == 3)
            {
                drops.Add(new Drop { model = RealmVisuals.CreateLoot(actors, location), phase = UnityEngine.Random.value * 6, quality = enemy.neutral || enemy.kind == 3 ? 2 : 1 });
                Tell(enemy.neutral ? "WILD GUARDIAN DEFEATED  ·  +100 gold! Move near the chest to collect its relics." : "Equipment dropped! Move your ranger near the glowing chest.");
            }
            Destroy(enemy.model);
            if (commandedEnemy == enemy) commandedEnemy = null;
        }

        void GiveExperience(float amount)
        {
            HeroExperience += amount;
            while (HeroExperience >= HeroExperienceRequired)
            {
                HeroExperience -= HeroExperienceRequired;
                HeroLevel++;
                if (HeroHealth > 0) HeroHealth = HeroMaxHealth;
                Burst(hero.transform.position + Vector3.up, 1.3f, new Color(.8f, 1, .35f), .6f);
                Tell("LEVEL " + HeroLevel + "  ·  Your ranger gains health and attack power.");
            }
        }

        void TickLoot(float dt)
        {
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                drop.model.transform.Rotate(0, 35 * dt, 0);
                if (HeroHealth <= 0 || FlatDistance(hero.transform.position, drop.model.transform.position) > 2.3f) continue;
                EquippedItems++;
                Shards += drop.quality;
                damageBonus += drop.quality * 3;
                armorBonus += drop.quality * 8;
                HeroHealth = Mathf.Min(HeroMaxHealth, HeroHealth + 25);
                Tell("GEAR EQUIPPED  ·  +" + drop.quality * 3 + " attack  ·  +" + drop.quality * 8 + " health  ·  +" + drop.quality + " shards");
                audioSource.PlayOneShot(lootSound);
                Destroy(drop.model); drops.RemoveAt(i);
            }
        }

        public void StartWave()
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || GameOver || IsPaused || WaveRunning) return;
            if (Wave >= TotalWaves) return;
            Wave++;
            WaveRunning = true;
            spawnRemaining = 9 + Wave * 3;
            spawnIndex = 0;
            spawnTimer = .5f;
            Tell(Wave == TotalWaves ? "FINAL WAVE  ·  The siege ogre approaches. Hold the gate!" : "WAVE " + Wave + "  ·  Enemies are approaching from the northern portal.");
            audioSource.PlayOneShot(waveSound);
        }

        public void SelectSite(int index)
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || index < 0 || index >= sites.Count || GameOver) return;
            selectedSite = index;
            RedrawSelection();
        }

        void RedrawSelection()
        {
            if (selectionRing != null) Destroy(selectionRing);
            if (rangeRing != null) Destroy(rangeRing);
            if (!HasSelectedSite) return;
            Vector3 pos = sites[selectedSite].pad.transform.position + Vector3.up * .12f;
            selectionRing = RealmVisuals.Ring(actors, pos, 1.45f, new Color(1, .8f, .35f), .07f);
            if (SelectedHasTower)
            {
                var tower = sites[selectedSite].tower;
                rangeRing = RealmVisuals.Ring(actors, pos + Vector3.up * .02f, towerDefinitions[tower.kind].range + (tower.level - 1) * .65f, new Color(.45f, .9f, 1f, .4f), .04f);
            }
        }

        public void SelectBuild(int kind)
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || GameOver || IsPaused) return;
            if (!HasSelectedSite) { Tell("Select one of the round stone foundations on the map first."); return; }
            if (SelectedHasTower) { Tell("This foundation already has a tower. Upgrade it or choose another foundation."); return; }
            if (kind < 0 || kind >= towerDefinitions.Length) return;
            var definition = towerDefinitions[kind];
            if (Gold < definition.cost) { Tell("Not enough gold. " + definition.title + " costs " + definition.cost + "."); return; }
            Gold -= definition.cost;
            sites[selectedSite].tower = new Tower { kind = kind, invested = definition.cost, model = RealmVisuals.CreateTower(actors, sites[selectedSite].pad.transform.position, kind, 1) };
            Tell(definition.title + " built. Select another foundation, or upgrade this tower.");
            RedrawSelection();
            audioSource.PlayOneShot(buildSound);
        }

        int UpgradeCost(Tower tower) => Mathf.RoundToInt(towerDefinitions[tower.kind].cost * (.65f + tower.level * .35f));

        public void UpgradeSelected()
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || !SelectedHasTower || GameOver || IsPaused) return;
            var tower = sites[selectedSite].tower;
            if (tower.level >= 3) { Tell("This tower is fully upgraded."); return; }
            int cost = UpgradeCost(tower);
            if (Gold < cost) { Tell("You need " + cost + " gold to upgrade this tower."); return; }
            Gold -= cost;
            tower.invested += cost;
            tower.level++;
            Destroy(tower.model);
            tower.model = RealmVisuals.CreateTower(actors, sites[selectedSite].pad.transform.position, tower.kind, tower.level);
            RedrawSelection();
            Tell("Tower upgraded to level " + tower.level + ". Damage, range, and attack speed increased.");
            audioSource.PlayOneShot(buildSound);
        }

        public void SellSelected()
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || !SelectedHasTower || GameOver || IsPaused) return;
            int returned = SelectedSellValue;
            Gold += returned;
            Destroy(sites[selectedSite].tower.model);
            sites[selectedSite].tower = null;
            RedrawSelection();
            Tell("Tower sold. " + returned + " gold returned.");
        }

        public void DismissSelection() { selectedSite = -1; RedrawSelection(); }

        public void SetSelectedTargetMode(TowerTargetMode mode)
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || GameOver || !SelectedHasTower ||
                mode != TowerTargetMode.FarthestAlongRoad && mode != TowerTargetMode.Tankiest) return;
            sites[selectedSite].tower.targetMode = mode;
            Tell(mode == TowerTargetMode.Tankiest ? "TARGET: TANKIEST  ·  Highest maximum health adjusted for armor. Ties go to the farthest along the road." :
                "TARGET: FARTHEST ALONG ROAD  ·  Focus the enemy closest to reaching the gate.");
        }

        public void CastNova()
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || GameOver || IsPaused || HeroHealth <= 0) return;
            if (SkillCooldown > 0) { Tell("Nature Nova will be ready in " + Mathf.CeilToInt(SkillCooldown) + " seconds."); return; }
            SkillCooldown = SkillMaxCooldown;
            foreach (var enemy in enemies.ToArray())
                if (!enemy.dead && (!enemy.neutral || enemy.awake) && FlatDistance(hero.transform.position, enemy.model.transform.position) <= 6)
                { enemy.slowTimer = 3; Damage(enemy, 90 + HeroLevel * 12 + damageBonus, true); }
            var ring = RealmVisuals.Ring(actors, hero.transform.position + Vector3.up * .4f, 1, new Color(.3f, 1f, .65f), .16f);
            effects.Add(new Effect { model = ring, remaining = .65f, duration = .65f, expansion = 6, scale = Vector3.one });
            Tell("NATURE NOVA  ·  Nearby enemies struck and slowed.");
            audioSource.PlayOneShot(novaSound);
        }

        public void CraftEquipment()
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || GameOver || IsPaused) return;
            int cost = CraftCost;
            if (Shards < cost) { Tell("Collect " + cost + " shards from waves and equipment to forge a relic."); return; }
            Shards -= cost;
            bool first = !legendary;
            legendary = true;
            damageBonus += first ? 45 : 15;
            armorBonus += first ? 60 : 25;
            EquippedItems++;
            if (HeroHealth > 0) HeroHealth = HeroMaxHealth;
            Tell(first ? "LEGENDARY RELIC FORGED  ·  Sunforged Bow  +45 attack  /  +60 health" : "RELIC REFORGED  ·  +15 attack  /  +25 health");
            Burst(hero.transform.position + Vector3.up, 2, new Color(1, .75f, .15f), .7f);
            audioSource.PlayOneShot(lootSound);
        }

        public void TogglePause()
        {
            if (InMainMenu || SettingsOpen || LexiconOpen || GameOver) return;
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0 : Speed;
        }
        public void ToggleSpeed() { if (InMainMenu || SettingsOpen || LexiconOpen) return; Speed = Speed == 1 ? 2 : 1; if (!IsPaused && !GameOver) Time.timeScale = Speed; }

        void FinishGame(bool victory)
        {
            Victory = victory;
            GameOver = true;
            WaveRunning = false;
            Message = victory ? "THE GATE STANDS. You have defeated all " + TotalWaves + " waves." : "THE GATE HAS FALLEN. Rebuild your defenses and try again.";
            Time.timeScale = 0;
        }

        public void RestartGame()
        {
            if (ActiveLevel != null) DefenseLevelCatalog.SelectLevel(ActiveLevel);
            StartSelectedLevel();
        }

        public void StartSelectedLevel()
        {
            startOnNextLoad = true;
            Time.timeScale = 1;
            if (SceneManager.GetActiveScene().buildIndex >= 0) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            InMainMenu = true;
            SettingsOpen = false;
            LexiconOpen = false;
            lexicon.Hide();
            Time.timeScale = 0;
            hud.SetVisible(false);
            menu.ShowMainMenu();
        }

        public void OpenSettings()
        {
            if (LexiconOpen) return;
            SettingsOpen = true;
            Time.timeScale = 0;
            menu.ShowSettings(!InMainMenu);
        }

        public void CloseSettings()
        {
            SettingsOpen = false;
            if (InMainMenu) menu.ShowMainMenu();
            else menu.Hide();
            Time.timeScale = InMainMenu || IsPaused || GameOver ? 0 : Speed;
        }

        public void OpenLexicon()
        {
            if (SettingsOpen || LexiconOpen) return;
            LexiconOpen = true;
            Time.timeScale = 0;
            hud.SetVisible(false);
            menu.Hide();
            lexicon.Show();
        }

        public void CloseLexicon()
        {
            LexiconOpen = false;
            lexicon.Hide();
            hud.SetVisible(!InMainMenu);
            if (InMainMenu) menu.ShowMainMenu();
            Time.timeScale = InMainMenu || IsPaused || GameOver ? 0 : Speed;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Tell(string message) { Message = message; messageTimer = 6f; }
        static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        static Vector3 ClampToMap(Vector3 value) => new Vector3(Mathf.Clamp(value.x, -20, 20), .2f, Mathf.Clamp(value.z, -10, 11));
        static void Face(Transform model, Vector3 direction, float dt)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .001f) model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(direction), Mathf.Min(1, dt * 12));
        }

        void MakeHealthBar(Transform owner, float height, out Transform bar, out Transform fill)
        {
            bar = new GameObject("Health").transform; bar.SetParent(owner, false); bar.localPosition = Vector3.up * height;
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Health background"; back.transform.SetParent(bar, false); back.transform.localScale = new Vector3(1.15f, .12f, .045f);
            Destroy(back.GetComponent<Collider>()); back.GetComponent<Renderer>().sharedMaterial = RealmVisuals.Material(new Color(.05f,.09f,.07f));
            var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
            front.name = "Health remaining"; front.transform.SetParent(bar, false); front.transform.localPosition = new Vector3(0, 0, -.03f); front.transform.localScale = new Vector3(1.1f, .085f, .04f);
            Destroy(front.GetComponent<Collider>()); front.GetComponent<Renderer>().sharedMaterial = RealmVisuals.Material(new Color(.4f,1f,.3f), true);
            fill = front.transform;
        }

        void UpdateHealthBar(Transform bar, Transform fill, float ratio)
        {
            if (view != null) bar.rotation = view.transform.rotation;
            ratio = Mathf.Clamp01(ratio);
            fill.localScale = new Vector3(1.1f * ratio, .085f, .04f);
            fill.localPosition = new Vector3(-.55f * (1 - ratio), 0, -.03f);
        }

        void Burst(Vector3 location, float size, Color color, float duration)
        {
            var model = RealmVisuals.Orb(actors, location, size * .35f, color);
            effects.Add(new Effect { model = model, remaining = duration, duration = duration, expansion = 2, scale = model.transform.localScale });
        }

        void TickEffects(float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; effect.remaining -= dt;
                if (effect.remaining <= 0) { Destroy(effect.model); effects.RemoveAt(i); continue; }
                effect.model.transform.localScale = effect.scale * Mathf.Lerp(1, effect.expansion, 1 - effect.remaining / effect.duration);
            }
        }

        static AudioClip Tone(float frequency, float duration)
        {
            const int rate = 22050;
            var clip = AudioClip.Create("Forest chime " + frequency, Mathf.RoundToInt(rate * duration), 1, rate, false);
            var data = new float[clip.samples];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Sin(Mathf.PI * i / data.Length) * Mathf.Exp(-t * 7);
                data[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2) * .35f + Mathf.Sin(t * frequency * 3 * Mathf.PI) * .12f) * envelope;
            }
            clip.SetData(data, 0); return clip;
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            if (menu != null) Destroy(menu.gameObject);
            if (lexicon != null) Destroy(lexicon.gameObject);
            foreach (var clip in new[] { buildSound, lootSound, novaSound, waveSound }) if (clip != null) Destroy(clip);
        }
    }
}
