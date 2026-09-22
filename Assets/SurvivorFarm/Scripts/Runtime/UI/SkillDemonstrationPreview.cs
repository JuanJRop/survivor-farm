using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Reusable animated hero, target and combat-effect preview for skill UI.</summary>
    [DisallowMultipleComponent]
    public sealed class SkillDemonstrationPreview : MonoBehaviour
    {
        private Image avatar;
        private Image enemy;
        private Image effect;
        private Text caption;
        private PlayerAnimationLibrary avatarLibrary;
        private PlayerAnimationLibrary enemyLibrary;
        private SkillFxLibrary.Clip effectClip;
        private string skillId;
        private float elapsed;
        private float nextFrame;

        /// <summary>Builds the preview in this RectTransform and selects its first skill.</summary>
        public void Configure(string selectedSkillId)
        {
            EnsureView();
            SetSkill(selectedSkillId);
        }

        /// <summary>Changes the lazily loaded combat FX clip shown over the animated actors.</summary>
        public void SetSkill(string selectedSkillId)
        {
            if (skillId == selectedSkillId && avatar != null) return;
            skillId = selectedSkillId;
            elapsed = 0f;
            nextFrame = 0f;
            avatarLibrary = avatarLibrary != null ? avatarLibrary : Resources.Load<PlayerAnimationLibrary>("JoshAnimationLibrary");
            enemyLibrary = enemyLibrary != null ? enemyLibrary : Resources.Load<PlayerAnimationLibrary>("SproutSlimeAnimations");
            effectClip = string.IsNullOrEmpty(skillId) ? null : SkillFxLibrary.ForSkill(skillId);
            if (caption != null)
            {
                SkillDefinition definition = SkillTreeCatalog.Find(skillId);
                caption.text = definition != null ? "COMBATE  ·  " + BranchName(definition.Branch) : "COMBATE";
            }
            Render(true);
        }

        private void EnsureView()
        {
            if (avatar != null) return;
            Image background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = JourneyMenuStyle.Ink;
            background.raycastTarget = false;

            caption = MakeLabel("Vista de combate", new Vector2(0f, .84f), new Vector2(1f, .98f), 11);
            caption.alignment = TextAnchor.MiddleCenter;
            caption.color = new Color32(205, 183, 133, 255);

            avatar = MakeImage("Héroe en combate", new Vector2(.10f, .17f), new Vector2(.40f, .84f));
            effect = MakeImage("Efecto de la habilidad", new Vector2(.29f, .11f), new Vector2(.71f, .91f));
            enemy = MakeImage("Enemigo alcanzado", new Vector2(.64f, .20f), new Vector2(.90f, .81f));

            // A quiet gold strike line frames the contact point between attacker and target.
            RectTransform slash = AnchoredRect("Impacto", new Vector2(.44f, .43f), new Vector2(.60f, .45f));
            Image slashImage = slash.gameObject.AddComponent<Image>();
            slashImage.color = new Color32(225, 191, 122, 126);
            slashImage.raycastTarget = false;

            Text heroLabel = MakeLabel("Héroe", new Vector2(.06f, .015f), new Vector2(.42f, .14f), 9);
            heroLabel.text = "GUERRERO";
            heroLabel.alignment = TextAnchor.MiddleCenter;
            heroLabel.color = new Color32(149, 170, 168, 255);
            Text enemyLabel = MakeLabel("Objetivo", new Vector2(.61f, .015f), new Vector2(.93f, .14f), 9);
            enemyLabel.text = "OBJETIVO";
            enemyLabel.alignment = TextAnchor.MiddleCenter;
            enemyLabel.color = new Color32(149, 170, 168, 255);
        }

        private Image MakeImage(string objectName, Vector2 min, Vector2 max)
        {
            RectTransform rect = AnchoredRect(objectName, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Text MakeLabel(string objectName, Vector2 min, Vector2 max, int size)
        {
            RectTransform rect = AnchoredRect(objectName, min, max);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = JourneyMenuStyle.TitleFont;
            text.fontSize = size;
            text.color = JourneyMenuStyle.Paper;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private RectTransform AnchoredRect(string objectName, Vector2 min, Vector2 max)
        {
            RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
            return rect;
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < nextFrame) return;
            nextFrame = elapsed + .075f;
            Render(false);
        }

        private void Render(bool force)
        {
            float phase = elapsed % 1.18f;
            bool striking = phase < .48f;
            if (avatarLibrary != null && avatar != null && (force || elapsed >= nextFrame - .08f))
            {
                PlayerAnimationLibrary.Clip heroClip = avatarLibrary.Find(AvatarClipName());
                if (heroClip == null) heroClip = avatarLibrary.Find("Sword");
                if (heroClip == null) heroClip = avatarLibrary.Find("Idle");
                if (heroClip != null)
                {
                    float local = striking ? phase : phase - .48f;
                    int frame = FrameIndex(heroClip, local, .70f);
                    avatar.sprite = avatarLibrary.Frame(heroClip, 2, frame);
                }
            }

            bool targetHit = phase >= .36f && phase < .80f;
            if (enemyLibrary != null && enemy != null && (force || elapsed >= nextFrame - .08f))
            {
                PlayerAnimationLibrary.Clip targetClip = enemyLibrary.Find(targetHit ? "Damage" : "Idle");
                if (targetClip == null) targetClip = enemyLibrary.Find("Idle");
                if (targetClip != null)
                {
                    float local = targetHit ? phase - .36f : phase;
                    int frame = FrameIndex(targetClip, local, .44f);
                    enemy.sprite = enemyLibrary.Frame(targetClip, 0, frame);
                }
            }

            bool effectActive = effectClip != null && phase <= effectClip.Duration;
            if (effect != null)
            {
                effect.enabled = effectActive;
                if (effectActive) effect.sprite = effectClip.At(phase / Mathf.Max(.01f, effectClip.Duration));
            }
        }

        private string AvatarClipName()
        {
            SkillDefinition definition = SkillTreeCatalog.Find(skillId);
            if (definition == null) return "Sword";
            switch (definition.Branch)
            {
                case SkillBranch.Magia:
                    if (skillId != null && skillId.IndexOf("ice", System.StringComparison.OrdinalIgnoreCase) >= 0) return "Magic/Magic Water";
                    if (skillId != null && skillId.IndexOf("void", System.StringComparison.OrdinalIgnoreCase) >= 0) return "Magic/Magic Poison";
                    return "Magic/Magic Fire";
                case SkillBranch.Supervivencia: return "Magic/Healer";
                case SkillBranch.Movilidad: return "Run";
                case SkillBranch.Caos: return "Magic/Magic Poison";
                default: return "Sword";
            }
        }

        private static int FrameIndex(PlayerAnimationLibrary.Clip clip, float local, float fallbackDuration)
        {
            if (clip.Loop) return Mathf.FloorToInt(Mathf.Max(0f, local) * clip.FramesPerSecond) % clip.Frames;
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, local) / fallbackDuration * clip.Frames), 0, clip.Frames - 1);
        }

        private static string BranchName(SkillBranch branch)
        {
            switch (branch)
            {
                case SkillBranch.Fuerza: return "FUERZA";
                case SkillBranch.Magia: return "MAGIA";
                case SkillBranch.Supervivencia: return "SUPERVIVENCIA";
                case SkillBranch.Movilidad: return "MOVILIDAD";
                case SkillBranch.Caos: return "CAOS";
                default: return branch.ToString().ToUpperInvariant();
            }
        }
    }
}
