# Tiny RPG Character assets

Imported from the user-provided archives, retaining their original PNG pixels and baked shadows:

- `Tiny RPG Character Asset Pack 01 v2.0 -Free Soldier&Orc.zip`: Soldier and Orc.
- `Tiny RPG Character Asset Pack 02 v1.01-Free Demon_A&Blood Monster_A (1).zip`: Demon_A and Blood Monster_A.

The Demo, v1.02 and v1.03b Soldier/Orc archives contain earlier versions of the same two characters. They are intentionally not imported as duplicate enemies.

Only the five runtime animation sheets per character are included. Each sheet has one horizontal row of 100 × 100 cells: 6 idle frames, 8 walk frames, 4 hurt frames, 4 death frames, and 6/6/7/8 attack frames respectively. These cells include generous transparent padding; the actual pixel characters retain the game's native 16 pixels per world unit. Use point filtering, no mipmaps, no compression, and a center pivot.

The original artwork provides a side view. The enemy animator mirrors it horizontally and keeps the last facing direction during vertical movement. No extra directions are synthesized.

Rebuild the four resource libraries with **Tools → Survivor Farm → Build Tiny RPG Enemy Assets**.
