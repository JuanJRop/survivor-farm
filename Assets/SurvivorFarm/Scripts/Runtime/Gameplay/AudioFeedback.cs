using System;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public enum CombatSound
    {
        Swing, ThirdSwing, Impact, HeavyImpact, Hurt, Death, Break,
        Drop, Pickup, Mine, Chop, Craft, Finisher, Bow, Charge, ChargeReady, ChargedSwing, ChargedImpact,
        ChickenDeath, RabbitDeath, WalkStep, RunStep
    }

    /// <summary>Small reusable voice pool. Cosmetic randomness never changes gameplay RNG.</summary>
    [DisallowMultipleComponent]
    public sealed class AudioFeedback : MonoBehaviour
    {
        [Tooltip("Optional recorded clips, indexed by CombatSound; empty entries use the original synthesized bank.")]
        [SerializeField] private AudioClip[] clipOverrides;
        private const int VoiceCount = 8;
        private readonly AudioSource[] voices = new AudioSource[VoiceCount];
        private readonly float[] lastPlayed = new float[Enum.GetValues(typeof(CombatSound)).Length];
        private readonly System.Random variation = new System.Random(5729);
        private static AudioClip[] bank;
        private static AudioFeedback listener;
        private int voice;
        private AudioListener audioListener;
        private float nextListenerCheck;
        public CombatSound LastSound { get; private set; }
        public float LastPitch { get; private set; }
        public int PlayedCount { get; private set; }
        public int VoiceCapacity => VoiceCount;

        private void Awake()
        {
            listener = this;
            if (bank == null)
            {
                bank = new AudioClip[lastPlayed.Length];
                for (int i = 0; i < bank.Length; i++) bank[i] = Compose((CombatSound)i);
            }
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10;
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0;
                voices[i].priority = 80;
            }
        }

        public static void PlayAt(CombatSound sound, Vector3 position, float volume = 1f)
        { if (listener != null) listener.Play(sound, position, volume); }

        public void Play(CombatSound sound, Vector3 position, float volume = 1f)
        {
            int id = (int)sound;
            if (!GameFeelFeedback.Enabled || !isActiveAndEnabled || bank == null || id < 0 || id >= bank.Length) return;
            // Area attacks and loot piles should not multiply loudness by the collider count.
            if (Time.unscaledTime - lastPlayed[id] < .028f) return;
            lastPlayed[id] = Time.unscaledTime;
            float distance = Vector2.Distance(transform.position, position);
            if (distance > 22f) return;
            var source = voices[voice++ % VoiceCount];
            source.Stop();
            source.clip = clipOverrides != null && id < clipOverrides.Length && clipOverrides[id] != null ? clipOverrides[id] : bank[id];
            source.pitch = LastPitch = 1f + (float)(variation.NextDouble() * .12 - .06);
            source.volume = Mathf.Clamp01(volume) * .36f * (float)(.91 + variation.NextDouble() * .09)
                * Mathf.Clamp01(1f - Mathf.Max(0, distance - 7f) / 18f);
            LastSound = sound; PlayedCount++;
            if (audioListener == null && Time.unscaledTime >= nextListenerCheck)
            {
                audioListener = FindFirstObjectByType<AudioListener>();
                nextListenerCheck = Time.unscaledTime + 1;
            }
            // Data-only test scenes and disabled audio scenes do not need an engine warning.
            if (audioListener != null && audioListener.isActiveAndEnabled) source.Play();
        }

        public static AudioClip Clip(CombatSound sound) => bank != null ? bank[(int)sound] : null;

        private static AudioClip Compose(CombatSound sound)
        {
            const int rate = 22050;
            bool swing = sound == CombatSound.Swing || sound == CombatSound.ThirdSwing || sound == CombatSound.Bow || sound==CombatSound.ChargedSwing;
            bool reward = sound == CombatSound.Pickup || sound == CombatSound.Craft;
            bool strong = sound == CombatSound.HeavyImpact || sound == CombatSound.ThirdSwing || sound == CombatSound.Finisher || sound==CombatSound.ChargedImpact || sound==CombatSound.ChargedSwing;
            bool charge=sound==CombatSound.Charge||sound==CombatSound.ChargeReady;
            bool step=sound==CombatSound.WalkStep||sound==CombatSound.RunStep;
            bool animal=sound==CombatSound.ChickenDeath||sound==CombatSound.RabbitDeath;
            float duration = step?.085f:animal?.23f:charge?.32f:sound==CombatSound.ChargedImpact?.34f:sound == CombatSound.Finisher ? .38f : sound == CombatSound.Death ? .26f :
                sound == CombatSound.Break ? .25f : reward ? .17f : strong ? .2f : .12f;
            float frequency = sound == CombatSound.Mine ? 720 : sound == CombatSound.Chop ? 205 :
                sound == CombatSound.Drop ? 360 : sound == CombatSound.Hurt ? 110 : strong ? 85 : 155;
            var noise = new System.Random(610 + (int)sound);
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            float low = 0, phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, progress = t / duration;
                float white = (float)(noise.NextDouble() * 2 - 1);
                low += (white - low) * (sound == CombatSound.Mine ? .65f : .2f);
                float attack = Mathf.Min(1, t * 900);
                float tail = Mathf.Pow(1 - progress, reward ? 1.6f : 2.4f);
                phase += 2 * Mathf.PI * frequency * (1f - progress * .45f) / rate;
                float sample;
                if(step)sample=low*.65f+Mathf.Sin(2*Mathf.PI*(sound==CombatSound.RunStep?95:130)*t)*.22f;
                else if(animal)
                {
                    float hz=sound==CombatSound.ChickenDeath?680:1150;
                    float envelope=sound==CombatSound.ChickenDeath?Mathf.Pow(Mathf.Abs(Mathf.Sin(t*43)),2):1;
                    sample=(Mathf.Sin(2*Mathf.PI*(hz*t-hz*.7f*t*t))*.5f+low*.13f)*envelope;
                }
                else if(charge)
                {
                    float hz=sound==CombatSound.ChargeReady?740:260;
                    sample=Mathf.Sin(2*Mathf.PI*(hz*t+260*t*t))*.35f+Mathf.Sin(2*Mathf.PI*hz*1.5f*t)*.15f;
                }
                else if (reward)
                {
                    float hz = t < .06f ? 660 : sound == CombatSound.Craft ? 990 : 880;
                    sample = Mathf.Sin(2 * Mathf.PI * hz * t) * .4f + Mathf.Sin(2 * Mathf.PI * hz * 2 * t) * .07f;
                }
                else if (swing)
                    sample = low * Mathf.Sin(progress * Mathf.PI) * 1.7f + Mathf.Sin(phase) * .07f;
                else
                {
                    sample = Mathf.Sin(phase) * .48f + low * (sound == CombatSound.Break ? .9f : .55f);
                    if (sound == CombatSound.Mine || sound == CombatSound.Drop)
                        sample += Mathf.Sin(phase * 2.71f) * .2f;
                    if (sound == CombatSound.Death || sound == CombatSound.Hurt)
                        sample += Mathf.Sin(phase * .51f) * .22f;
                    if (sound == CombatSound.Finisher)
                        sample += Mathf.Sin(2 * Mathf.PI * 440 * t) * Mathf.Exp(-t * 14) * .16f;
                    if(sound==CombatSound.ChargedImpact)sample+=Mathf.Sin(2*Mathf.PI*62*t)*.24f;
                }
                samples[i] = Mathf.Clamp(sample * attack * tail, -.85f, .85f);
            }
            var clip = AudioClip.Create("Farm · " + sound, samples.Length, 1, rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy() { if (listener == this) listener = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBank()
        {
            if (bank != null) foreach (var clip in bank) if (clip != null) Destroy(clip);
            bank = null; listener = null;
        }
    }
}
