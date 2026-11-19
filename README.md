# TD-Rethink — Forest Watch

Forest Watch is a Unity tower defense prototype where you build and upgrade towers, command a ranger, defend against enemy waves, and collect equipment. It includes three editable maps, an enemy lexicon, and saved audio and display settings.

Clone this repository, add its root folder in Unity Hub, and open it with Unity **6000.6.7f1**. Let Unity import the assets and restore the packages listed in `Packages/manifest.json`.

Open `Assets/ForestWatch/Scenes/ForestWatch.unity`, then press **Play**. Alternatively, choose **Tools → Forest Watch → Open Playable Prototype**.

Create a Windows player with **Tools → Forest Watch → Build Windows Playable**. The local output is `Builds/ForestWatch/ForestWatch.exe`; keep its neighboring files and `ForestWatch_Data` folder together when sharing it. Generated builds, Unity caches, local settings, and test output are excluded from this source repository.

The game opens at its main menu. Choose **Start Game** to play the selected map, **Choose Level** to pick another, or **Quit Game** to close the player. **Settings** is at the top right and saves master volume, fullscreen, and vertical sync. The in-game **Menu** button returns to the main menu; starting again begins a fresh run. Settings pause the current run and preserve its pause/speed state when closed.

## Create your own levels

See [the step-by-step level guide](Docs/CreatingLevels.md). In Unity, select a level under **Assets → ForestWatch → Resources → ForestWatch → Levels**, click **Duplicate as a New Level** in its Inspector, then **Preview Level in Scene**. Edit the route and foundation markers in Scene view, preview again, and press Play. Valid assets in this folder appear in **Choose Level** automatically.

Included maps: **Forest Crossing** (green woodland, ten waves), **Amber Hollow** (autumn switchbacks, eight waves), and **Frostbound Pass** (frozen woodland, eight waves). The Inspector exposes each map's starting resources, enemy strength/speed, colors, and scenery seed. Rebuild the Windows player after adding or editing levels.

## Play

- Click a round stone foundation, then choose **Arrow Watch (75 gold)**, **Frost Spire (100)**, or **Cannon Keep (125)**.
- Click a tower's foundation to see its range, upgrade it up to level three, or sell it for 70% of the invested gold.
- Select a built tower, then choose **Farthest Along Road** or **Tankiest**. Each tower keeps its own choice through upgrades. Tankiest compares maximum health adjusted for armor; modes can also be changed while paused.
- Move your ranger with **WASD / arrow keys** or **right-click** a destination. The ranger attacks nearby enemies automatically. Left-click an enemy to command an engagement.
- **Q** casts Nature Nova: damage and slow nearby enemies, with a twelve-second cooldown.
- **R** starts the next wave. Waves wait for your command, so you can build and recover between them.
- **Space** pauses. **Tab** switches 1× / 2× speed. **Scroll** zooms. **Escape** clears the tower selection or toggles pause.
- **B** opens the persistent enemy lexicon and pauses combat. It is also available from the main menu and HUD. Entries unlock as creatures appear in live expeditions.
- Walk near glowing loot chests to equip gear automatically and gain crafting shards. Forge a legendary relic for **five shards**, then reforge it for **seven**. **F** also forges.
- Challenge the wild guardian at the small camp for extra gold and gear. This encounter is optional.
- Survive your chosen map's waves and the siege ogre. Enemies that reach the castle reduce your gate's integrity. A fallen ranger returns after five seconds.

## First-pass scope

Three editable maps, a main menu and level chooser, saved audio/display settings, three tower types, hero leveling, equipment drops, relic crafting, an optional wild encounter, pause/speed/zoom, victory/defeat, and restart. Runs start fresh; campaign progress is not saved yet.

Map layout, controls, tower statistics, waves, damage formulas, loot rates, and crafting recipes can be adjusted as the prototype develops.

See [combat and lexicon details](Docs/CombatAndLexicon.md) for targeting rules and the nine enemy roles, including glass cannons, armored tanks, ranged casters, and support healers.

## Iteration

- `HeroDefenseGame.cs`: game loop and inspector-editable starting gold, lives, wave count, hero speed/damage, enemy scaling, and tower definitions.
- `RealmVisuals.cs`: procedural map, models, colors, camera, and lighting.
- `DefenseHud.uxml` / `DefenseHud.uss` and `HeroDefenseHud.cs`: interface and interaction.
- `HeroDefenseSceneBuilder.cs`: creates the editor scene, saves generated map assets, and builds the standalone player.
- `DefenseLevel.cs` and `DefenseLevelCatalog.cs`: level data and automatic map discovery.
- `DefenseLevelEditor.cs`: level duplication, validation, and Scene-view layout editing.
- `ForestWatchMenu.cs` and `ForestWatchSettings.cs`: menu navigation and saved settings.

The previous setup scene is preserved as `Assets/ForestWatch/Scenes/PreviousSetup.unity`.
