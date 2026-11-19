using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestWatch
{
    public sealed class ForestWatchMenu : MonoBehaviour
    {
        HeroDefenseGame game;
        UIDocument document;
        GameObject documentObject;
        PanelSettings runtimePanel;
        ThemeStyleSheet fallbackTheme;
        VisualElement root, shell, mainScreen, levelScreen, settingsScreen;
        Label selectedName, selectedStats, levelSelection, volumeValue, settingsCaption;
        Slider volume;
        Toggle fullscreen, vsync;
        ScrollView levelList;
        Button settingsButton;
        readonly Dictionary<DefenseLevel, Button> levelButtons = new Dictionary<DefenseLevel, Button>();

        public bool IsOpen { get; private set; }

        public void Initialize(HeroDefenseGame owner)
        {
            game = owner;
            documentObject = new GameObject("Forest Watch Menu");
            documentObject.transform.SetParent(transform, false);
            document = documentObject.AddComponent<UIDocument>();
            runtimePanel = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanel.name = "Forest Watch Menu Panel";
            runtimePanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanel.referenceResolution = new Vector2Int(1920, 1080);
            runtimePanel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimePanel.match = .5f;
            runtimePanel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ForestWatch/MenuTheme");
            if (runtimePanel.themeStyleSheet == null)
            {
                fallbackTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                fallbackTheme.name = "Forest Watch Fallback Menu Theme";
                runtimePanel.themeStyleSheet = fallbackTheme;
            }
            document.panelSettings = runtimePanel;
            document.sortingOrder = 200;
            root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.AddToClassList("fw-document-root");
            root.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var layout = Resources.Load<VisualTreeAsset>("ForestWatch/Menu");
            if (layout == null) { Debug.LogError("Forest Watch menu layout is missing."); return; }
            layout.CloneTree(root);
            shell = root.Q("menuShell");
            mainScreen = root.Q("mainScreen");
            levelScreen = root.Q("levelScreen");
            settingsScreen = root.Q("settingsScreen");
            selectedName = root.Q<Label>("selectedLevelName");
            selectedStats = root.Q<Label>("selectedLevelStats");
            levelSelection = root.Q<Label>("levelSelection");
            volumeValue = root.Q<Label>("volumeValue");
            settingsCaption = root.Q<Label>("settingsCaption");
            volume = root.Q<Slider>("volume");
            fullscreen = root.Q<Toggle>("fullscreen");
            vsync = root.Q<Toggle>("vsync");
            levelList = root.Q<ScrollView>("levelList");
            levelList.contentContainer.AddToClassList("fw-level-grid");
            Bind("startGame", () => game.StartSelectedLevel());
            Bind("chooseLevel", ShowLevelSelection);
            Bind("openLexicon", () => game.OpenLexicon());
            Bind("quitGame", () => game.QuitGame());
            settingsButton = Bind("openSettings", () => game.OpenSettings());
            Bind("levelsBack", ShowMainMenu);
            Bind("playSelected", () => game.StartSelectedLevel());
            Bind("settingsBack", () => game.CloseSettings());
            Bind("resetSettings", () => { ForestWatchSettings.ResetDefaults(); RefreshSettings(); });
            volume.RegisterValueChangedCallback(evt => {
                ForestWatchSettings.SetMasterVolume(evt.newValue / 100);
                volumeValue.text = Mathf.RoundToInt(evt.newValue) + "%";
            });
            fullscreen.RegisterValueChangedCallback(evt => ForestWatchSettings.SetFullscreen(evt.newValue));
            vsync.RegisterValueChangedCallback(evt => ForestWatchSettings.SetVSync(evt.newValue));
            RefreshSelection();
            Hide();
        }

        Button Bind(string name, System.Action action)
        {
            var button = root.Q<Button>(name);
            button.focusable = false;
            button.tabIndex = -1;
            button.clicked += action;
            return button;
        }

        void OpenPage(VisualElement page)
        {
            if (shell == null) return;
            IsOpen = true;
            shell.RemoveFromClassList("fw-hidden");
            mainScreen.EnableInClassList("fw-hidden", page != mainScreen);
            levelScreen.EnableInClassList("fw-hidden", page != levelScreen);
            settingsScreen.EnableInClassList("fw-hidden", page != settingsScreen);
            settingsButton.EnableInClassList("fw-hidden", page == settingsScreen);
        }

        public void ShowMainMenu()
        {
            RefreshSelection();
            OpenPage(mainScreen);
        }

        public void ShowLevelSelection()
        {
            RebuildLevelCards();
            OpenPage(levelScreen);
        }

        public void ShowSettings(bool returnToGame = false)
        {
            RefreshSettings();
            settingsCaption.text = returnToGame ? "Your expedition is paused while you adjust settings." : "Set the mood for your next expedition.";
            root.Q<Button>("settingsBack").text = returnToGame ? "BACK TO GAME" : "BACK TO MENU";
            OpenPage(settingsScreen);
        }

        public void Hide()
        {
            IsOpen = false;
            if (shell != null) shell.AddToClassList("fw-hidden");
            root?.focusController?.focusedElement?.Blur();
        }

        public void NavigateBack()
        {
            if (settingsScreen != null && !settingsScreen.ClassListContains("fw-hidden")) game.CloseSettings();
            else ShowMainMenu();
        }

        void RefreshSelection()
        {
            if (selectedName == null) return;
            var selected = DefenseLevelCatalog.SelectedLevel;
            selectedName.text = selected != null ? selected.displayName : "No level selected";
            selectedStats.text = selected != null ? selected.waveCount + " WAVES   /   " + selected.startingGold + " STARTING GOLD   /   " + Difficulty(selected) : "Create a level asset to begin.";
            if (levelSelection != null) levelSelection.text = selected != null ? "SELECTED: " + selected.displayName.ToUpperInvariant() : "SELECT A LEVEL";
            foreach (var pair in levelButtons)
                pair.Value.EnableInClassList("fw-level-selected", pair.Key == selected);
            root.Q<Button>("startGame").SetEnabled(selected != null);
            root.Q<Button>("playSelected").SetEnabled(selected != null);
        }

        void RebuildLevelCards()
        {
            levelList.Clear();
            levelButtons.Clear();
            var levels = DefenseLevelCatalog.GetLevels();
            foreach (var level in levels)
            {
                if (level == null) continue;
                var captured = level;
                var button = new Button(() => { DefenseLevelCatalog.SelectLevel(captured); RefreshSelection(); });
                button.name = "level-" + level.levelId;
                button.focusable = false;
                button.tabIndex = -1;
                button.AddToClassList("fw-level-card");
                var route = new LevelRoutePreview(level);
                route.AddToClassList("fw-route-preview");
                button.Add(route);
                AddLabel(button, (levelButtons.Count + 1).ToString("00") + "  /  " + Difficulty(level), "fw-level-eyebrow");
                AddLabel(button, level.displayName, "fw-level-title");
                AddLabel(button, level.description, "fw-level-description");
                AddLabel(button, level.waveCount + " WAVES  ·  " + level.startingGold + " GOLD", "fw-level-stats");
                AddLabel(button, "SELECT EXPEDITION", "fw-level-select-label");
                levelButtons.Add(level, button);
                levelList.Add(button);
            }
            RefreshSelection();
        }

        static string Difficulty(DefenseLevel level)
        {
            return level.enemyHealthMultiplier >= 1.3f ? "CHALLENGING" : level.enemyHealthMultiplier > 1.05f ? "ADVENTUROUS" : "STANDARD";
        }

        static void AddLabel(VisualElement parent, string text, string styleClass)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(styleClass);
            parent.Add(label);
        }

        void RefreshSettings()
        {
            volume.SetValueWithoutNotify(ForestWatchSettings.MasterVolume * 100);
            volumeValue.text = Mathf.RoundToInt(ForestWatchSettings.MasterVolume * 100) + "%";
            fullscreen.SetValueWithoutNotify(ForestWatchSettings.Fullscreen);
            vsync.SetValueWithoutNotify(ForestWatchSettings.VSync);
        }

        void OnDestroy()
        {
            if (documentObject != null) Destroy(documentObject);
            if (runtimePanel != null) Destroy(runtimePanel);
            if (fallbackTheme != null) Destroy(fallbackTheme);
        }

        sealed class LevelRoutePreview : VisualElement
        {
            readonly DefenseLevel level;
            public LevelRoutePreview(DefenseLevel source)
            {
                level = source;
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            Vector2 Point(Vector3 world, float width, float height)
            {
                return new Vector2(14 + (world.x + 22) / 44 * (width - 28), 10 + (12 - world.z) / 24 * (height - 20));
            }

            void Draw(MeshGenerationContext context)
            {
                float width = contentRect.width, height = contentRect.height;
                if (width < 30 || height < 30 || level.route == null || level.route.Length < 2) return;
                var painter = context.painter2D;
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;
                painter.strokeColor = new Color(.7f, .61f, .39f);
                painter.lineWidth = 6;
                painter.BeginPath();
                painter.MoveTo(Point(level.route[0], width, height));
                for (int i = 1; i < level.route.Length; i++) painter.LineTo(Point(level.route[i], width, height));
                painter.Stroke();
                if (level.buildSites != null)
                    foreach (var site in level.buildSites) Circle(painter, Point(site, width, height), 3, new Color(.72f, .77f, .67f));
                Circle(painter, Point(level.route[0], width, height), 5, new Color(.9f, .58f, .35f));
                Circle(painter, Point(level.route[level.route.Length - 1], width, height), 5, new Color(.35f, .85f, .72f));
            }

            static void Circle(Painter2D painter, Vector2 center, float radius, Color color)
            {
                painter.fillColor = color;
                painter.BeginPath();
                painter.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
