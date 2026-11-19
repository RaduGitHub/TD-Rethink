using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestWatch
{
    public sealed class EnemyLexiconView : MonoBehaviour
    {
        HeroDefenseGame game;
        GameObject documentObject;
        PanelSettings runtimePanel;
        ThemeStyleSheet fallbackTheme;
        VisualElement root, shell, lockedDetail, revealedDetail, statsGrid;
        Label discoveryCount, creatureName, role, emblem, description, encounters, trait, historyTitle, historyStats, historyCount, tactics, introduction;
        Button close;
        ScrollView creatureList, detailScroll;
        readonly Dictionary<string, Button> entries = new Dictionary<string, Button>();
        string selectedId;

        public bool IsOpen { get; private set; }

        public void Initialize(HeroDefenseGame owner)
        {
            game = owner;
            documentObject = new GameObject("Enemy Lexicon Document");
            documentObject.transform.SetParent(transform, false);
            var document = documentObject.AddComponent<UIDocument>();
            runtimePanel = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanel.name = "Enemy Lexicon Panel";
            runtimePanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanel.referenceResolution = new Vector2Int(1920, 1080);
            runtimePanel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimePanel.match = .5f;
            runtimePanel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ForestWatch/LexiconTheme");
            if (runtimePanel.themeStyleSheet == null)
            {
                fallbackTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                fallbackTheme.name = "Enemy Lexicon Fallback Theme";
                runtimePanel.themeStyleSheet = fallbackTheme;
            }
            document.panelSettings = runtimePanel;
            document.sortingOrder = 250;
            root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.AddToClassList("lx-document-root");
            root.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var layout = Resources.Load<VisualTreeAsset>("ForestWatch/Lexicon");
            if (layout == null) { Debug.LogError("Enemy lexicon layout is missing."); return; }
            layout.CloneTree(root);
            shell = root.Q("lexiconShell");
            discoveryCount = root.Q<Label>("discoveryCount");
            creatureList = root.Q<ScrollView>("creatureList");
            detailScroll = root.Q<ScrollView>("detailScroll");
            creatureList.contentContainer.AddToClassList("lx-list-content");
            detailScroll.contentContainer.AddToClassList("lx-detail-content");
            lockedDetail = root.Q("lockedDetail");
            revealedDetail = root.Q("revealedDetail");
            statsGrid = root.Q("statsGrid");
            creatureName = root.Q<Label>("creatureName");
            role = root.Q<Label>("role");
            emblem = root.Q<Label>("emblem");
            description = root.Q<Label>("description");
            encounters = root.Q<Label>("encounters");
            trait = root.Q<Label>("trait");
            introduction = root.Q<Label>("introduction");
            historyTitle = root.Q<Label>("historyTitle");
            historyStats = root.Q<Label>("historyStats");
            historyCount = root.Q<Label>("historyCount");
            tactics = root.Q<Label>("tactics");
            close = root.Q<Button>("closeLexicon");
            close.focusable = false;
            close.tabIndex = -1;
            close.clicked += () => game.CloseLexicon();
            Hide();
        }

        public void Show()
        {
            if (shell == null) return;
            IsOpen = true;
            close.text = game.InMainMenu ? "BACK TO MENU" : "BACK TO BATTLE";
            discoveryCount.text = EnemyLexicon.DiscoveredCount + " / " + EnemyCatalog.All.Length + " CREATURES DISCOVERED";
            RebuildEntries();
            shell.RemoveFromClassList("lx-hidden");
        }

        public void Hide()
        {
            IsOpen = false;
            if (shell != null) shell.AddToClassList("lx-hidden");
            root?.focusController?.focusedElement?.Blur();
        }

        void RebuildEntries()
        {
            creatureList.Clear();
            entries.Clear();
            EnemyDefinition selected = null, firstDiscovered = null;
            int index = 0;
            foreach (var definition in EnemyCatalog.All)
            {
                if (definition == null) continue;
                bool discovered = EnemyLexicon.IsDiscovered(definition.id);
                if (discovered && firstDiscovered == null) firstDiscovered = definition;
                if (definition.id == selectedId) selected = definition;
                var captured = definition;
                var button = new Button(() => Select(captured));
                button.name = "lexicon-entry-" + definition.id;
                button.focusable = false;
                button.tabIndex = -1;
                button.AddToClassList("lx-entry");
                button.EnableInClassList("lx-entry-locked", !discovered);
                AddLabel(button, (++index).ToString("00") + "  /  " + (discovered ? definition.role.ToUpperInvariant() : "UNDISCOVERED"), "lx-entry-kicker");
                AddLabel(button, discovered ? definition.displayName : "Unknown creature", "lx-entry-name");
                creatureList.Add(button);
                entries[definition.id] = button;
            }
            if (selected == null) selected = firstDiscovered;
            if (selected == null && EnemyCatalog.All.Length > 0) selected = EnemyCatalog.All[0];
            Select(selected);
        }

        void Select(EnemyDefinition definition)
        {
            selectedId = definition != null ? definition.id : null;
            foreach (var entry in entries) entry.Value.EnableInClassList("lx-entry-selected", entry.Key == selectedId);
            bool discovered = definition != null && EnemyLexicon.IsDiscovered(definition.id);
            lockedDetail.EnableInClassList("lx-hidden", discovered);
            revealedDetail.EnableInClassList("lx-hidden", !discovered);
            detailScroll.scrollOffset = Vector2.zero;
            if (!discovered) return;
            var record = EnemyLexicon.GetRecord(definition.id);
            creatureName.text = definition.displayName;
            role.text = definition.role.ToUpperInvariant();
            emblem.text = string.IsNullOrEmpty(definition.displayName) ? "!" : definition.displayName.Substring(0, 1).ToUpperInvariant();
            foreach (string colorClass in new[] { "lx-emblem-red", "lx-emblem-stone", "lx-emblem-violet", "lx-emblem-green", "lx-emblem-boss", "lx-emblem-wild" })
                emblem.RemoveFromClassList(colorClass);
            if (definition.boss) emblem.AddToClassList("lx-emblem-boss");
            else if (definition.neutral) emblem.AddToClassList("lx-emblem-wild");
            else if (definition.kind == 5) emblem.AddToClassList("lx-emblem-red");
            else if (definition.armor > 0) emblem.AddToClassList("lx-emblem-stone");
            else if (definition.healPerPulse > 0) emblem.AddToClassList("lx-emblem-green");
            else if (definition.attackRange > 2.5f) emblem.AddToClassList("lx-emblem-violet");
            description.text = definition.description;
            encounters.text = record.encounterCount + " ENCOUNTERED  /  " + record.killCount + " DEFEATED";
            introduction.text = (definition.neutral ? "OPTIONAL WILD ENCOUNTER" : definition.boss ? "FINAL-WAVE ENCOUNTER" : "APPEARS FROM WAVE " + definition.firstWave) + "  ·  " + definition.experience + " BASE EXPERIENCE";
            statsGrid.Clear();
            AddStat("BASE HEALTH", Number(definition.baseHealth) + " HP");
            AddStat("EFFECTIVE TOUGHNESS", Number(definition.baseHealth / Mathf.Max(.01f, 1 - definition.armor)) + " EHP");
            AddStat("DAMAGE TO HERO", Number(definition.heroDamage));
            AddStat("ATTACK INTERVAL", Number(definition.attackInterval) + " s");
            AddStat("MOVEMENT SPEED", Number(definition.moveSpeed) + " units/s");
            AddStat("ATTACK RANGE", Number(definition.attackRange) + " units");
            AddStat("ARMOR", Number(definition.armor * 100) + "% reduction");
            AddStat("BASE GOLD REWARD", definition.goldReward.ToString());
            AddStat("GATE DAMAGE", definition.gateDamage.ToString());
            trait.text = BuildTraitText(definition);
            historyTitle.text = definition.neutral ? "LAST ENCOUNTER  /  WILDERNESS" : "LAST ENCOUNTER  /  WAVE " + record.lastWave;
            historyStats.text = Number(record.lastHealth) + " HP    ·    " + Number(record.lastDamage) + " HERO DAMAGE    ·    " + Number(record.lastSpeed) + " SPEED";
            historyCount.text = "Highest health encountered: " + Number(record.highestHealth) + " HP. These values include that encounter's wave and level modifiers.";
            tactics.text = definition.tactics;
        }

        static string BuildTraitText(EnemyDefinition definition)
        {
            var lines = new List<string>();
            if (definition.healPerPulse > 0)
                lines.Add("MENDING PULSE  ·  Restores " + Number(definition.healPerPulse) + " HP to nearby allies within " + Number(definition.healRadius) + " units every " + Number(definition.healInterval) + " seconds.");
            if (definition.attackRange > 2.5f)
                lines.Add("RANGED ATTACK  ·  Can damage the ranger from " + Number(definition.attackRange) + " units away. Close the gap or let your towers handle it.");
            if (definition.armor > 0)
                lines.Add("ARMORED  ·  Reduces incoming attack damage by " + Number(definition.armor * 100) + "%. Effective toughness accounts for this reduction.");
            if (definition.boss)
                lines.Add("SIEGE THREAT  ·  Costs " + definition.gateDamage + " gate integrity if it reaches the sanctuary.");
            if (definition.neutral)
                lines.Add("WILD GUARDIAN  ·  Remains at its camp until challenged. A defeated ranger or retreat lets it recover.");
            if (lines.Count == 0) lines.Add("MELEE ATTACK  ·  Strikes the ranger when within " + Number(definition.attackRange) + " units.");
            return string.Join("\n", lines);
        }

        void AddStat(string caption, string value)
        {
            var stat = new VisualElement();
            stat.AddToClassList("lx-stat");
            AddLabel(stat, caption, "lx-stat-caption");
            AddLabel(stat, value, "lx-stat-value");
            statsGrid.Add(stat);
        }

        static void AddLabel(VisualElement parent, string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
        }

        static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        void OnDestroy()
        {
            if (documentObject != null) Destroy(documentObject);
            if (runtimePanel != null) Destroy(runtimePanel);
            if (fallbackTheme != null) Destroy(fallbackTheme);
        }
    }
}
