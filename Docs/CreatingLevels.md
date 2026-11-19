# Create another Forest Watch level

Levels are editable Unity assets. One shared game scene reads the chosen asset, so you do **not** need to copy scenes or change C# code for each map.

## Start with a working example

1. Stop Play mode in Unity.
2. In the **Project** window, open **Assets → ForestWatch → Resources → ForestWatch → Levels**.
3. Select **01_ForestCrossing**, **02_AmberHollow**, or **03_FrostboundPass**.
4. In the **Inspector**, click **Duplicate as a New Level**. This creates an independent asset with a new unique Level Id. The original level stays available.
5. Rename the asset in the Project window. Set **Display Name** and **Description** in the Inspector. Set **Sort Order** to control its position in the level chooser: lower numbers appear first.
6. Click **Preview Level in Scene**. Keep the asset selected, switch to the **Scene** tab, and use the colored markers to edit its layout.
7. Click **Preview Level in Scene** again after layout changes to rebuild the visible road and scenery.
8. Save with **Ctrl+S**, press **Play**, choose your level in **Choose Level**, and start the game.

Any valid `DefenseLevel` asset inside the **Resources/ForestWatch/Levels** folder is listed automatically. You do not need to register it in code or add another scene to Build Settings. Rebuild the Windows player to include new or edited assets in the standalone game.

You can also right-click that folder and choose **Create → Forest Watch → Level** to start from the default layout. If you duplicate with Unity's ordinary **Ctrl+D** instead of the Inspector button, change **Level Id** to a new unique value, such as `my-river-crossing`.

## Shape the map

The island currently has a fixed size. **X** runs left/right across it, **Z** runs forward/back, and the ground height is **Y = 0.2**. Keep X between **−20 and 20** and Z between **−10 and 10**.

- **Route:** the enemy path in travel order. The first point places the enemy portal; the final point places the sanctuary gate. Blue markers and lines show this route.
- **Build Sites:** one tower foundation for each gold marker. Keep the centers at least **2.2 units from the blue route line** and **2.3 units apart**.
- **Hero Start:** the green marker is the ranger's starting and respawn position.
- **Camp Position:** the orange marker is the optional wild guardian's camp. Leave a clearing about **5 units across** around it and keep it away from the road and foundations.

With **Edit layout handles in Scene view** enabled, click a colored marker, then drag its move gizmo. Markers stay at ground height. The wire rectangle shows the available layout area. The Inspector gives validation errors for missing or overlapping foundations, out-of-bounds markers, or an unusable route.

To add or remove points, expand the **Route** or **Build Sites** list in the Inspector and use its list controls. New route points are visited in list order. Keep consecutive route points at least **0.5 units apart**, avoid sharp U-turns at a single point, and leave room for the portal and the larger sanctuary model at the two endpoints. Changes to the asset are saved; the Scene preview updates when you click **Preview** again.

## Change its appearance and difficulty

| Inspector field | What it changes |
|---|---|
| Starting Gold | Gold available before wave one. |
| Starting Lives | The gate's initial integrity. |
| Wave Count | Number of waves; the last wave includes the siege ogre. |
| Enemy Health Multiplier | `1` is base health; `1.3` means 30% more. |
| Enemy Speed Multiplier | `1` is normal speed; `1.1` means 10% faster. |
| Seed | Repositions trees, rocks, and flowers while leaving your route and foundations intact. |
| Biome | Selects the lighting treatment: Forest, Autumn, or Winter. |
| Grass / Path / Foliage colors | Sets the ground, route, and tree palette. |
| Background Color | Color surrounding the island. |

Try changing one or two things at a time, then play through the result. A long road generally gives towers more time to attack; clustered foundations can be powerful where the route loops close to itself. Test the early waves, the final ogre, hero movement, and the wild camp before treating a new level as finished.

## The three included levels

- **Forest Crossing:** a green forest route, 300 starting gold, 20 lives, ten waves, normal enemy health and speed.
- **Amber Hollow:** a new autumn switchback, 350 starting gold, eight waves, 15% more enemy health and 5% more speed.
- **Frostbound Pass:** a frozen route with long turns, 400 starting gold, eight waves, 30% more enemy health and 10% more speed.

The first version shares tower choices, enemy types, loot, crafting, and wave composition across levels. Those systems can be extended later without changing how level assets are selected.
