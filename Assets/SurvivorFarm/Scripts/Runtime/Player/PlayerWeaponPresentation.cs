using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Shared tier icons for the HUD and skill tree. Character attacks use their authored animation.</summary>
    public static class PlayerWeaponPresentation
    {
        private static readonly Sprite[] swords = new Sprite[3];

        public static Sprite SwordSprite(int tier)
        {
            int index = Mathf.Clamp(tier, 1, 3) - 1;
            if (swords[index] != null) return swords[index];
            var texture = Resources.Load<Texture2D>("WeaponProgression/SwordTier" + (index + 1));
            if (texture == null) return FarmUiStyle.ItemIcon("Sword");
            texture.filterMode = FilterMode.Point;
            // The supplied RPG sheets place the sword in column 2 of the first 16 px row.
            swords[index] = Sprite.Create(texture, new Rect(32, texture.height - 16, 16, 16), new Vector2(.25f, .25f), 16, 0, SpriteMeshType.FullRect);
            swords[index].name = "Sword tier " + (index + 1); swords[index].hideFlags = HideFlags.DontSave;
            return swords[index];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            for (int i = 0; i < swords.Length; i++) { if (swords[i] != null) Object.Destroy(swords[i]); swords[i] = null; }
        }
    }
}

