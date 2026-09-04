using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerCharacterAnimator : MonoBehaviour
    {
        private const string PreMadeRoot = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Character and Portrait/Character/Pre-made";

        [SerializeField] private string preferredCharacter = "George";
        [SerializeField] private string fallbackCharacter = "Josh";
        [SerializeField] private float idleFrameRate = 5f;
        [SerializeField] private float moveFrameRate = 9f;
        [SerializeField] private float actionFrameRate = 12f;

        private readonly Dictionary<string, Sprite[]> clips = new Dictionary<string, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D body;
        private float frameTimer;
        private int frameIndex;
        private string currentClip = "Idle";
        private string actionClip;
        private float actionEndsAt;
        private Vector2 lastFacing = Vector2.down;

        public void Configure(SpriteRenderer renderer, string characterName, string fallbackName)
        {
            spriteRenderer = renderer;
            preferredCharacter = characterName;
            fallbackCharacter = fallbackName;
            body = GetComponent<Rigidbody2D>();
            LoadCharacterClips();
            PlayClip("Idle", true);
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            if (velocity.sqrMagnitude > 0.02f)
            {
                lastFacing = velocity.normalized;
            }

            if (!string.IsNullOrEmpty(actionClip) && Time.time < actionEndsAt)
            {
                PlayClip(actionClip, false);
                AdvanceFrame(actionFrameRate);
                return;
            }

            actionClip = null;
            string nextClip = velocity.magnitude > 3.9f ? "Run" : velocity.magnitude > 0.08f ? "Walk" : "Idle";
            PlayClip(nextClip, false);
            AdvanceFrame(nextClip == "Idle" ? idleFrameRate : moveFrameRate);
            ApplyFacing();
        }

        public void PlayToolAction(FarmTool tool)
        {
            string clip = GetClipForTool(tool);
            PlayActionClip(clip);
        }

        public void PlayNamedAction(string clip)
        {
            PlayActionClip(clip);
        }

        private void PlayActionClip(string clip)
        {
            if (!clips.ContainsKey(clip))
            {
                return;
            }

            actionClip = clip;
            actionEndsAt = Time.time + Mathf.Max(0.35f, clips[clip].Length / actionFrameRate);
            PlayClip(clip, true);
            ApplyFacing();
        }

        private void LoadCharacterClips()
        {
            clips.Clear();
            string character = CharacterHasClip(preferredCharacter, "Idle") ? preferredCharacter : fallbackCharacter;

            LoadClip(character, "Idle", "Idle");
            LoadClip(character, "Walk", "Walk");
            LoadClip(character, "Run", "Run");
            LoadClip(character, "Axe", "Axe");
            LoadClip(character, "Pickaxe", "Pickaxe");
            LoadClip(character, "Hoe", "Hoe");
            LoadClip(character, "Shovel", "Shovel");
            LoadClip(character, "Watering", "Watering");
            LoadClip(character, "Sword", "Sword");
            LoadClip(character, "Bow and Arrow", "Bow");
            LoadClip(character, "Throwing items", "Plant");
            LoadClipAtPath(character, "Pick Up Itens/pick up", "PickUp");
        }

        private void LoadClip(string character, string fileName, string clipName)
        {
#if UNITY_EDITOR
            string path = $"{PreMadeRoot}/{character}/{fileName}.png";
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i] as Sprite;
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((left, right) => GetSpriteIndex(left.name).CompareTo(GetSpriteIndex(right.name)));
            if (sprites.Count > 0)
            {
                clips[clipName] = BuildStableFrameStrip(sprites, clipName);
            }
#endif
        }

        private void LoadClipAtPath(string character, string relativeFileName, string clipName)
        {
#if UNITY_EDITOR
            string path = $"{PreMadeRoot}/{character}/{relativeFileName}.png";
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i] as Sprite;
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((left, right) => GetSpriteIndex(left.name).CompareTo(GetSpriteIndex(right.name)));
            if (sprites.Count > 0)
            {
                clips[clipName] = BuildStableFrameStrip(sprites, clipName);
            }
#endif
        }

        private static bool CharacterHasClip(string character, string fileName)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAllAssetsAtPath($"{PreMadeRoot}/{character}/{fileName}.png").Length > 0;
#else
            return false;
#endif
        }

        private void PlayClip(string clipName, bool restart)
        {
            if (!clips.ContainsKey(clipName))
            {
                return;
            }

            if (restart || currentClip != clipName)
            {
                currentClip = clipName;
                frameIndex = 0;
                frameTimer = 0f;
            }

            Sprite[] frames = clips[currentClip];
            if (frames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            }
        }

        private void AdvanceFrame(float frameRate)
        {
            if (!clips.ContainsKey(currentClip))
            {
                return;
            }

            Sprite[] frames = clips[currentClip];
            if (frames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float secondsPerFrame = 1f / Mathf.Max(1f, frameRate);
            while (frameTimer >= secondsPerFrame)
            {
                frameTimer -= secondsPerFrame;
                frameIndex = (frameIndex + 1) % frames.Length;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[frameIndex];
            }
        }

        private void ApplyFacing()
        {
            if (spriteRenderer != null && Mathf.Abs(lastFacing.x) > 0.05f)
            {
                spriteRenderer.flipX = lastFacing.x < 0f;
            }
        }

        private static string GetClipForTool(FarmTool tool)
        {
            switch (tool)
            {
                case FarmTool.Axe:
                    return "Axe";
                case FarmTool.Pickaxe:
                    return "Pickaxe";
                case FarmTool.Hoe:
                    return "Hoe";
                case FarmTool.Shovel:
                    return "Shovel";
                case FarmTool.WateringCan:
                    return "Watering";
                case FarmTool.Sword:
                    return "Sword";
                case FarmTool.Bow:
                    return "Bow";
                default:
                    return "Idle";
            }
        }

        private static int GetSpriteIndex(string spriteName)
        {
            int underscore = spriteName.LastIndexOf('_');
            if (underscore < 0 || underscore >= spriteName.Length - 1)
            {
                return 0;
            }

            int index;
            return int.TryParse(spriteName.Substring(underscore + 1), out index) ? index : 0;
        }

        private static Sprite[] BuildStableFrameStrip(List<Sprite> sprites, string clipName)
        {
            int frameCount = clipName == "Idle" ? 3 : 6;
            int count = Mathf.Min(frameCount, sprites.Count);
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = sprites[i];
            }

            return frames;
        }
    }
}
