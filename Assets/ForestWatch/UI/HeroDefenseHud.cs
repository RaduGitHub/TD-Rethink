using UnityEngine;
using UnityEngine.UIElements;

namespace ForestWatch
{
    public sealed class HeroDefenseHud : MonoBehaviour
    {
        private HeroDefenseGame game;
        private UIDocument document;
        private PanelSettings runtimePanel;
        private ThemeStyleSheet fallbackTheme;
        private VisualElement root;
        private Label gold, lives, wave, toast, heroLevel, heroHealth, equipment, shards;
        private Label selectedTitle, selectedDescription, selectionHint, encounter, endTitle, endDescription;
        private Label arrowCost, frostCost, cannonCost;
        private ProgressBar healthBar, experienceBar;
        private Button pause, speed, startWave, nova, craft, arrow, frost, cannon, upgrade, sell;
        private Button targetFarthest, targetTankiest;
        private VisualElement buildChoices, towerActions, endScreen;

        public void Initialize(HeroDefenseGame owner)
        {
            game = owner;
            document = GetComponent<UIDocument>();
            if (document == null) document = gameObject.AddComponent<UIDocument>();
            runtimePanel = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanel.name = "Forest Watch Runtime Panel";
            runtimePanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanel.referenceResolution = new Vector2Int(1920, 1080);
            runtimePanel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimePanel.match = 0.5f;
            runtimePanel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ForestWatch/DefenseTheme");
            if (runtimePanel.themeStyleSheet == null)
            {
                fallbackTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                fallbackTheme.name = "Forest Watch Runtime Theme";
                runtimePanel.themeStyleSheet = fallbackTheme;
            }
            document.panelSettings = runtimePanel;
            document.sortingOrder = 100;
            root = document.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.AddToClassList("document-root");
            root.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var layout = Resources.Load<VisualTreeAsset>("ForestWatch/DefenseHud");
            if (layout == null)
            {
                Debug.LogError("Forest Watch HUD layout is missing from Resources/ForestWatch/DefenseHud.");
                return;
            }
            layout.CloneTree(root);
            gold = root.Q<Label>("gold"); lives = root.Q<Label>("lives"); wave = root.Q<Label>("wave");
            toast = root.Q<Label>("toast"); heroLevel = root.Q<Label>("heroLevel");
            heroHealth = root.Q<Label>("heroHealth"); equipment = root.Q<Label>("equipment");
            shards = root.Q<Label>("shards"); healthBar = root.Q<ProgressBar>("healthBar");
            experienceBar = root.Q<ProgressBar>("experienceBar");
            selectedTitle = root.Q<Label>("selectedTitle"); selectedDescription = root.Q<Label>("selectedDescription");
            selectionHint = root.Q<Label>("selectionHint"); encounter = root.Q<Label>("encounter");
            buildChoices = root.Q("buildChoices"); towerActions = root.Q("towerActions");
            endScreen = root.Q("endScreen"); endTitle = root.Q<Label>("endTitle");
            endDescription = root.Q<Label>("endDescription");

            pause = Bind("pause", () => game.TogglePause());
            speed = Bind("speed", () => game.ToggleSpeed());
            Bind("mainMenu", () => game.ReturnToMainMenu());
            Bind("settings", () => game.OpenSettings());
            Bind("lexicon", () => game.OpenLexicon());
            Bind("restart", () => game.RestartGame());
            Bind("endRestart", () => game.RestartGame());
            Bind("endMenu", () => game.ReturnToMainMenu());
            Bind("dismissSelection", () => game.DismissSelection());
            startWave = Bind("startWave", () => game.StartWave());
            nova = Bind("nova", () => game.CastNova());
            craft = Bind("craft", () => game.CraftEquipment());
            arrow = Bind("arrow", () => game.SelectBuild(0));
            frost = Bind("frost", () => game.SelectBuild(1));
            cannon = Bind("cannon", () => game.SelectBuild(2));
            arrowCost = arrow.Q<Label>(className: "tower-cost");
            frostCost = frost.Q<Label>(className: "tower-cost");
            cannonCost = cannon.Q<Label>(className: "tower-cost");
            upgrade = Bind("upgrade", () => game.UpgradeSelected());
            sell = Bind("sell", () => game.SellSelected());
            targetFarthest = Bind("targetFarthest", () => game.SetSelectedTargetMode(TowerTargetMode.FarthestAlongRoad));
            targetTankiest = Bind("targetTankiest", () => game.SetSelectedTargetMode(TowerTargetMode.Tankiest));
            targetFarthest.tooltip = "Focus the in-range enemy that has traveled farthest along the road.";
            targetTankiest.tooltip = "Focus the highest maximum HP divided by (1 − armor). Ties favor road progress.";
            Refresh();
        }

        private Button Bind(string name, System.Action action)
        {
            var button = root.Q<Button>(name);
            // Gameplay owns Space and Tab; a clicked button must not also submit on those keys.
            button.focusable = false;
            button.tabIndex = -1;
            button.clicked += action;
            return button;
        }

        private void LateUpdate()
        {
            if (game != null && gold != null) Refresh();
        }

        public bool IsPointerOverUi()
        {
            if (root == null || root.panel == null) return false;
            Vector2 position = RuntimePanelUtils.ScreenToPanel(root.panel,
                new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            for (VisualElement hit = root.panel.Pick(position); hit != null; hit = hit.parent)
                if (hit.ClassListContains("ui-blocker")) return true;
            return false;
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.EnableInClassList("hidden", !visible);
        }

        public void Refresh()
        {
            if (game == null || gold == null) return;
            gold.text = game.Gold.ToString();
            lives.text = game.Lives.ToString();
            wave.text = game.Wave + " / " + game.TotalWaves;
            root.Q<Label>("levelName").text = game.LevelName.ToUpperInvariant();
            toast.text = game.IsPaused ? "PAUSED  ·  Take a moment to plan your defense" : game.Message;
            toast.EnableInClassList("hidden", string.IsNullOrEmpty(toast.text));
            heroLevel.text = "RANGER  /  LEVEL " + game.HeroLevel;
            heroHealth.text = Mathf.CeilToInt(game.HeroHealth) + " / " + Mathf.CeilToInt(game.HeroMaxHealth) + " HP";
            healthBar.value = game.HeroMaxHealth > 0 ? game.HeroHealth / game.HeroMaxHealth * 100 : 0;
            experienceBar.value = game.HeroExperienceRequired > 0 ? game.HeroExperience / game.HeroExperienceRequired * 100 : 0;
            equipment.text = string.IsNullOrEmpty(game.EquipmentSummary) ? "No equipment · defeat enemies to find loot" : game.EquipmentSummary;
            shards.text = game.Shards + " SHARDS  ·  " + game.EquippedItems + " EQUIPPED";
            craft.text = "FORGE EQUIPMENT  /  " + game.CraftCost + " SHARDS";
            craft.SetEnabled(game.Shards >= game.CraftCost && !game.GameOver && !game.IsPaused);
            pause.text = game.IsPaused ? "RESUME" : "PAUSE";
            speed.text = game.Speed + "× SPEED";
            selectedTitle.text = game.HasSelectedSite ? game.SelectedTitle : "BUILD YOUR DEFENSE";
            selectedDescription.text = game.HasSelectedSite ? game.SelectedDescription : "Select a stone foundation beside the path to build a tower.";
            selectionHint.text = game.SelectedHasTower ? "IMPROVE THIS TOWER" : "CHOOSE A TOWER";
            buildChoices.EnableInClassList("hidden", game.SelectedHasTower);
            towerActions.EnableInClassList("hidden", !game.SelectedHasTower);
            int arrowGold = game.GetTowerCost(0);
            int frostGold = game.GetTowerCost(1);
            int cannonGold = game.GetTowerCost(2);
            arrowCost.text = arrowGold + " GOLD";
            frostCost.text = frostGold + " GOLD";
            cannonCost.text = cannonGold + " GOLD";
            arrow.SetEnabled(game.HasSelectedSite && !game.SelectedHasTower && game.Gold >= arrowGold && !game.GameOver && !game.IsPaused);
            frost.SetEnabled(game.HasSelectedSite && !game.SelectedHasTower && game.Gold >= frostGold && !game.GameOver && !game.IsPaused);
            cannon.SetEnabled(game.HasSelectedSite && !game.SelectedHasTower && game.Gold >= cannonGold && !game.GameOver && !game.IsPaused);
            upgrade.text = game.SelectedTowerLevel >= 3 ? "FULLY UPGRADED" : "UPGRADE  /  " + game.SelectedUpgradeCost + " GOLD";
            upgrade.SetEnabled(game.SelectedHasTower && game.SelectedTowerLevel < 3 && game.Gold >= game.SelectedUpgradeCost && !game.GameOver && !game.IsPaused);
            sell.text = "SELL  /  +" + game.SelectedSellValue + " GOLD";
            sell.SetEnabled(game.SelectedHasTower && !game.GameOver && !game.IsPaused);
            targetFarthest.SetEnabled(game.SelectedHasTower && !game.GameOver);
            targetTankiest.SetEnabled(game.SelectedHasTower && !game.GameOver);
            targetFarthest.EnableInClassList("target-selected", game.SelectedHasTower && game.SelectedTargetMode == TowerTargetMode.FarthestAlongRoad);
            targetTankiest.EnableInClassList("target-selected", game.SelectedHasTower && game.SelectedTargetMode == TowerTargetMode.Tankiest);
            if (game.WaveRunning)
            {
                encounter.text = game.EnemiesAlive + " ENEMIES ON THE FIELD";
                startWave.text = "WAVE " + game.Wave + " IN PROGRESS";
            }
            else
            {
                encounter.text = game.Wave == 0 ? "THE FOREST NEEDS A WARDEN" : "REBUILD. REFORGE. PREPARE.";
                startWave.text = "START WAVE " + Mathf.Min(game.Wave + 1, game.TotalWaves) + "  [R]";
            }
            startWave.SetEnabled(!game.WaveRunning && !game.GameOver && !game.IsPaused);
            nova.text = game.HeroHealth <= 0 ? "RANGER RECOVERING" : game.SkillCooldown > 0 ? "NATURE NOVA  /  " + Mathf.CeilToInt(game.SkillCooldown) + "s" : "NATURE NOVA  [Q]";
            nova.SetEnabled(game.HeroHealth > 0 && game.SkillCooldown <= 0 && !game.GameOver && !game.IsPaused);
            endScreen.EnableInClassList("hidden", !game.GameOver);
            endTitle.text = game.Victory ? "THE FOREST STANDS" : "THE GATE HAS FALLEN";
            endDescription.text = game.Victory ? "Every wave repelled. Your watch has become a legend." : "Rally your hero, rethink your towers, and begin a new defense.";
        }

        private void OnDestroy()
        {
            if (runtimePanel != null) Destroy(runtimePanel);
            if (fallbackTheme != null) Destroy(fallbackTheme);
        }
    }
}
