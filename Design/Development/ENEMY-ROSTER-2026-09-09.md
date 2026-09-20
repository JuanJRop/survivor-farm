# Animated Enemy Roster

Two additional enemies use the original Farm RPG Tiny Asset Pack atlases. No replacement sprites or Main scene regeneration are required.

| Enemy | Health | Damage | Range | Windup | Cooldown | Coin drop |
| --- | --- | --- | --- | --- | --- | --- |
| Goblin lancero | 4 | 1 | 0.95 | 0.55 s | 1.5 s | 3 |
| Goblin arquero | 3 | 1 | 4.8 | 0.7 s | 2.1 s | 4 |

Outdoor day/night multipliers still apply. Existing first-day camp and home-protection rules remain in effect.

## Behavior

- Spear goblins chase, commit to a directional strike and miss when the player leaves its reach or moves behind it.
- Archers approach distant targets, hold firing distance, and retreat below 2.2 world units. A cornered archer can still shoot.
- Arrows aim at the position indicated at the start of the windup, not a homing target. They travel at 5.5 units/second and expire after 1.6 seconds.
- Movement uses Unity 2D collision sweeps and axis sliding. This is local obstacle handling, not global pathfinding through maze-like terrain.
- Shots require line of sight. Swept arrow collision stops at walls, absorbs arrows at the home boundary, and damages the player at most once.
- Both types animate idle, walk, attack, damage and death in three directions, with mirrored side views. The death pose remains briefly before recycling.

## Pools

- Outdoor slots rotate through spear goblin, archer and existing slime, keeping the existing population cap and day/night limits.
- Dungeon pools adopt their scene-authored instances instead of duplicating them. The first two slots of each six introduce the goblins; the other slots retain existing enemies, including golems.
- Encounter entry prewarms enough instances for all spawn points. Repeated encounters reuse them.
- Each owner prewarms two arrow slots per enemy. Shots never instantiate additional arrows when the pool is full.
- Spawn resets health, windup, cooldown, knockback, facing, animation and colliders. Death, despawn, player death and owner disable invalidate active arrows.
- Unique campaign guardians and bosses remain unchanged.

## Assets And Checks

`Tools > Survivor Farm > Build Enemy Animation Assets` rebuilds the three Resources libraries through Unity's AssetDatabase. They reference the existing goblin and arrow textures without copying or generating bitmap art.

`EnemyRosterTests` covers both AI roles, attack timing and dodging, obstacle collision, projectile damage and cancellation, safe-zone protection, original animation frames and bounded pool reuse.

`EnemyRosterChecks` extends the existing isolated Main-scene integration runner with original-atlas alpha checks and screenshots of live attacks, arrows and death. Existing campaign, dialogue, village and storefront checks still run afterward.

Restart Play mode in Unity to initialize the new roster. No saved inventory or campaign schema changed.
