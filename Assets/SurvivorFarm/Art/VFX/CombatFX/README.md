# Combat FX 1.1 integration

The supplied `Combat FX 1.1.zip` is authored by Raphael Hatencia / RagnaPixel.
The original artwork, source Aseprite file, alternate weapon/glow sheet, and
license are preserved. Runtime attribution is in the main menu's Options page
and `Resources/CombatFX/CREDITS.txt`.

The runtime atlas is `Resources/CombatFX/Combat-Sheet.png`: 640 × 1856 pixels,
29 rows, 64 × 64 cells. `AnimationData.json` contains the exact frame counts and
per-frame durations read from the source Aseprite tags, including intentional
empty terminal frames. Runtime sequences scale those timings to the existing
combat contact/recovery windows; gameplay damage and hit timing are unchanged.

| In-game use | Authored row |
| --- | --- |
| Alternating normal sword sweeps | 18 |
| Third combo attack | 27 |
| Fully charged sweep | 12 |
| Held sword charge | 26 |
| Standard hit, chopping and construction | 6 |
| Stone/mining impact | 7 |
| Heavy creature hit / defeat | 22 |

Leaf and player impacts tint the authored standard hit sprite. Exact gameplay
warning circles remain radius indicators. Sword trails, charge energy, impact
bursts and hit feedback no longer use generated energy/white-flash shaders or
procedural particle systems. The atlas without a weapon is used because the
character's existing animation already renders the equipped weapon.
