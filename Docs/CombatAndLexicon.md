# Tower targeting and enemy lexicon

Select a built tower's stone foundation. Its panel now has two targeting buttons:

- **Farthest Along Road**: attacks the eligible enemy that has traveled the greatest distance along the route. This uses the actual lengths of the road segments, so it works on custom levels with uneven waypoint spacing.
- **Tankiest**: attacks the eligible enemy with the highest **maximum health ÷ (1 − armor reduction)**. For example, a Stoneguard with 260 HP and 35% armor has 400 effective HP. Wounding it does not change its maximum toughness. Ties favor the enemy farther along the road, then the closer target.

Each tower chooses independently. You can change modes during combat or while paused. Upgrading keeps the mode. Selling and rebuilding creates a new tower with Farthest Along Road selected. Dead enemies, neutral wild creatures, and enemies outside the tower's range are excluded from tower targeting.

## Enemy roles

These are base statistics before wave and level modifiers:

| Creature | Role | Health | Speed (units/s) | Hero damage per hit | Armor |
|---|---|---:|---:|---:|---:|
| Goblin Raider | Balanced melee | 48 | 1.85 | 9 | 0% |
| Bone Runner | Fast runner | 65 | 2.3 | 9 | 0% |
| Iron Brute | Armored bruiser | 150 | 1.3 | 16 | 20% |
| Bloodblade | Glass cannon | 32 | 3.5 | 28 | 0% |
| Stoneguard | Heavy tank | 260 | 0.95 | 12 | 35% |
| Hexcaster | Ranged attacker | 55 | 1.5 | 20 | 0% |
| Moss Mender | Support healer | 80 | 1.35 | 6 | 0% |
| Siege Ogre | Final-wave boss | 1,400 | 1.3 | 32 | 10% |
| Ancient Guardian | Optional wild encounter | 420 | 1.3 | 18 | 10% |

Bloodblades first join wave two, Iron Brutes and Stoneguards wave three, Hexcasters wave four, and Moss Menders wave five. The ogre remains the final-wave boss on every level.

- Bloodblades are fragile but move quickly and strike the ranger every 0.8 seconds. Frost and fast attacks help keep them away.
- Armor reduces damage from towers, the ranger, and Nature Nova. Tanks are slow enough for overlapping upgraded towers to concentrate fire.
- Hexcasters stop to fire visible projectiles when the ranger is within 5.5 units. Projectiles deal damage on impact and disappear if the ranger is defeated.
- Moss Menders restore 12 HP to other living enemies within 3.5 units every three seconds. They cannot revive corpses, overheal, or heal themselves. A green pulse shows a successful heal.

Road enemies' maximum health scales with the wave and the level's health multiplier. Movement uses the level's speed multiplier. Raiders/runners and glass cannons/casters also gain hero damage in later waves. The lexicon shows the actual last encountered values separately from the base profile.

## Open the lexicon

Use **Enemy Lexicon** from the main menu, **Lexicon [B]** in battle, or press **B** during a run. **Escape**, **B**, or the back button closes it. Opening it pauses combat; closing preserves an existing pause, game-over state, and the selected speed.

A creature unlocks when it appears during a live expedition. Menu scenery does not unlock entries. Undiscovered species hide their name, role, and statistics. Discovered entries show base stats, effective toughness, attack behavior, tactical notes, encounters, kills, and the last encounter's scaled health/damage/speed.

Discoveries are saved between runs and application launches. They are separate from the currently selected level and from the run's equipment and gold.
