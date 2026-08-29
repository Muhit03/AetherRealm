using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The project has no sound files, so every sound effect is built in code as a
/// short tone or burst of noise. Other scripts just call
/// <c>AudioManager.Play(AudioManager.Sound.Hit)</c>.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum Sound
    {
        Swing, Hit, Coin, PlayerHurt, PlayerDown, EnemyHurt, EnemyDown,
        Ability, Dodge, Portal, WaveStart, Buy, UiClick, BossRoar
    }

    public static AudioManager Instance { get; private set; }

    Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
    AudioSource source;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.volume = 0.5f;

        BuildClips();
    }

    void BuildClips()
    {
        // frequency (Hz), length (seconds), whether it is noise instead of a tone
        clips[Sound.Swing]      = MakeSound(0f, 0.15f, true);
        clips[Sound.Hit]        = MakeSound(130f, 0.15f, false);
        clips[Sound.Coin]       = MakeSound(1050f, 0.18f, false);
        clips[Sound.PlayerHurt] = MakeSound(200f, 0.2f, false);
        clips[Sound.PlayerDown] = MakeSound(90f, 0.8f, false);
        clips[Sound.EnemyHurt]  = MakeSound(300f, 0.12f, false);
        clips[Sound.EnemyDown]  = MakeSound(150f, 0.3f, true);
        clips[Sound.Ability]    = MakeSound(500f, 0.4f, false);
        clips[Sound.Dodge]      = MakeSound(0f, 0.18f, true);
        clips[Sound.Portal]     = MakeSound(160f, 0.4f, false);
        clips[Sound.WaveStart]  = MakeSound(440f, 0.5f, false);
        clips[Sound.Buy]        = MakeSound(700f, 0.2f, false);
        clips[Sound.UiClick]    = MakeSound(520f, 0.06f, false);
        clips[Sound.BossRoar]   = MakeSound(70f, 1f, true);
    }

    // Builds a single AudioClip by filling an array of samples with a sine wave
    // (a tone) or random values (noise), fading out towards the end.
    AudioClip MakeSound(float frequency, float length, bool noise)
    {
        // 16 kHz is plenty for short blips and uses less than half the memory
        // of a full-quality clip.
        int sampleRate = 16000;
        int sampleCount = (int)(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float fadeOut = 1f - (time / length);

            float value;
            if (noise)
            {
                value = Random.Range(-1f, 1f);
            }
            else
            {
                value = Mathf.Sin(time * frequency * 2f * Mathf.PI);
            }

            samples[i] = value * fadeOut * 0.6f;
        }

        AudioClip clip = AudioClip.Create("sound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static void Play(Sound sound)
    {
        if (Instance == null)
        {
            return;
        }
        if (Instance.clips.ContainsKey(sound))
        {
            Instance.source.pitch = Random.Range(0.95f, 1.05f);
            Instance.source.PlayOneShot(Instance.clips[sound]);
        }
    }
}
