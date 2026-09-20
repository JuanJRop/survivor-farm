using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>One atlas tree, one ground anchor and one solid trunk. Legacy canopy/trunk
    /// renderers remain inactive so regrowth cannot bring back the overlapping artwork.</summary>
    public static class TreePresentation
    {
        public static void Configure(TreeResource tree, int tier)
        {
            if (!PortfolioSession.Active || tree == null) return;
            var child = tree.transform.Find("Árbol del pack");
            if (child == null)
            {
                foreach (var old in tree.GetComponentsInChildren<SpriteRenderer>(true))
                    if (old.transform != tree.transform) old.gameObject.SetActive(false); else old.enabled = false;
                child = new GameObject("Árbol del pack").transform;
                child.SetParent(tree.transform, false); child.gameObject.AddComponent<SpriteRenderer>();
            }
            var art = child.GetComponent<SpriteRenderer>();
            string atlas = tier == 2 ? "LivingBirch" : "LivingMaple";
            int row = tier == 3 ? 96 : 48;
            var frames = new[] { 0, 64, 96 }.Select(x => HouseSprites.Slice(atlas, x, row, 32, 48)).ToArray();
            art.sprite = frames[0];
            float rootScale = Mathf.Max(.01f, Mathf.Abs(tree.transform.lossyScale.x));
            child.localScale = Vector3.one * ((tier == 1 ? 1.8f : 2.1f) / 2 / rootScale);
            child.localPosition = Vector3.down * .08f / rootScale;
            var animation = child.GetComponent<EnvironmentSpriteAnimation>() ?? child.gameObject.AddComponent<EnvironmentSpriteAnimation>();
            animation.Visual = art; animation.Frames = frames; animation.FramesPerSecond = 2.3f;
            foreach (var depth in tree.GetComponentsInChildren<WorldSpriteDepth>(true)) depth.enabled = false;
            var sort = tree.GetComponent<WorldSpriteDepth>() ?? tree.gameObject.AddComponent<WorldSpriteDepth>();
            sort.enabled = true; sort.Visual = art; sort.GroundOffset = .08f;
            art.color = Color.white;
            TreeOcclusionFader.Ensure(tree.gameObject).enabled = true;
            foreach (var collider in tree.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
            var trunk = tree.GetComponent<CircleCollider2D>();
            if(trunk==null)trunk=tree.gameObject.AddComponent<CircleCollider2D>();
            trunk.enabled = !tree.IsHarvested; trunk.isTrigger = false;
            trunk.radius = .42f / rootScale; trunk.offset = new Vector2(0, .18f / rootScale);
            tree.Configure(art, null, tree.Definition.HarvestAmount, tree.Definition.CoinReward);
            // Configure refreshes cached colliders; keep obsolete colliders disabled.
            foreach (var collider in tree.GetComponentsInChildren<Collider2D>(true))
                if (collider != trunk) collider.enabled = false;
        }
    }
}
