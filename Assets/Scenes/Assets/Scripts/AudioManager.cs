using System.Collections.Generic;
using UnityEngine;

public enum Sfx
{
    Attack, EnemyHit, EnemyDeath, PlayerHurt, KeyAppear, KeyCollect,
    Locked, GateOpen, Victory, Defeat, Click
}

/// <summary>
/// Efeitos sonoros sintetizados em tempo de execução (ondas simples estilo 8 bits).
/// Não depende de arquivos de áudio. Se algo falhar, o jogo continua normalmente sem som.
/// Uso: AudioManager.Play(Sfx.Attack);
/// </summary>
public class AudioManager : MonoBehaviour
{
    const int SampleRate = 44100;
    public static float MasterVolume = 0.55f;

    static AudioManager instance;
    AudioSource source;
    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

    enum Wave { Sine, Square, Triangle, Noise }

    public static void Play(Sfx sfx)
    {
        if (!Application.isPlaying) return;
        if (instance == null) Create();
        if (instance == null || instance.source == null) return;

        AudioClip clip;
        if (instance.clips.TryGetValue(sfx, out clip) && clip != null)
            instance.source.PlayOneShot(clip, MasterVolume);
    }

    static void Create()
    {
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioManager>();
        instance.source = go.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0f;
        instance.source.ignoreListenerPause = true;
        try
        {
            instance.BuildClips();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Arena of Survival: sons desativados (" + e.Message + ")");
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void BuildClips()
    {
        clips[Sfx.Attack] = Make("sfx_attack", Mix(Tone(1400, 300, 0.12f, Wave.Noise, 0.22f), Tone(700, 220, 0.10f, Wave.Triangle, 0.30f)));
        clips[Sfx.EnemyHit] = Make("sfx_enemy_hit", Mix(Tone(340, 160, 0.09f, Wave.Square, 0.22f), Tone(900, 200, 0.06f, Wave.Noise, 0.15f)));
        clips[Sfx.EnemyDeath] = Make("sfx_enemy_death", Mix(Tone(420, 50, 0.38f, Wave.Square, 0.20f), Tone(800, 80, 0.32f, Wave.Noise, 0.16f)));
        clips[Sfx.PlayerHurt] = Make("sfx_player_hurt", Mix(Tone(230, 80, 0.26f, Wave.Square, 0.28f), Tone(400, 100, 0.20f, Wave.Noise, 0.14f)));
        clips[Sfx.KeyAppear] = Make("sfx_key_appear", Seq(
            Tone(523.25f, 523.25f, 0.08f, Wave.Triangle, 0.35f),
            Tone(659.25f, 659.25f, 0.08f, Wave.Triangle, 0.35f),
            Tone(783.99f, 783.99f, 0.08f, Wave.Triangle, 0.35f),
            Tone(1046.5f, 1046.5f, 0.22f, Wave.Triangle, 0.35f)));
        clips[Sfx.KeyCollect] = Make("sfx_key_collect", Seq(
            Tone(880f, 880f, 0.07f, Wave.Square, 0.16f),
            Tone(1174.66f, 1174.66f, 0.07f, Wave.Square, 0.16f),
            Tone(1567.98f, 1567.98f, 0.18f, Wave.Square, 0.16f)));
        clips[Sfx.Locked] = Make("sfx_locked", Seq(
            Tone(150, 150, 0.08f, Wave.Square, 0.22f),
            Silence(0.04f),
            Tone(120, 120, 0.11f, Wave.Square, 0.22f)));
        clips[Sfx.GateOpen] = Make("sfx_gate_open", Mix(Tone(90, 50, 0.8f, Wave.Noise, 0.20f), Tone(110, 65, 0.8f, Wave.Square, 0.10f)));
        clips[Sfx.Victory] = Make("sfx_victory", Seq(
            Tone(523.25f, 523.25f, 0.13f, Wave.Square, 0.17f),
            Tone(659.25f, 659.25f, 0.13f, Wave.Square, 0.17f),
            Tone(783.99f, 783.99f, 0.13f, Wave.Square, 0.17f),
            Tone(1046.5f, 1046.5f, 0.45f, Wave.Square, 0.17f)));
        clips[Sfx.Defeat] = Make("sfx_defeat", Seq(
            Tone(392f, 392f, 0.22f, Wave.Triangle, 0.35f),
            Tone(349.23f, 349.23f, 0.22f, Wave.Triangle, 0.35f),
            Tone(311.13f, 311.13f, 0.22f, Wave.Triangle, 0.35f),
            Tone(261.63f, 240f, 0.6f, Wave.Triangle, 0.35f)));
        clips[Sfx.Click] = Make("sfx_click", Tone(700, 500, 0.05f, Wave.Square, 0.13f));
    }

    static AudioClip Make(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float[] Tone(float f0, float f1, float duration, Wave wave, float volume)
    {
        int n = Mathf.Max(1, (int)(SampleRate * duration));
        float[] data = new float[n];
        System.Random rng = new System.Random(n);
        double phase = 0;
        float noise = 0f;
        int attack = Mathf.Max(1, (int)(SampleRate * 0.004f));

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            phase += f / SampleRate;
            if (phase >= 1.0)
            {
                phase -= 1.0;
                noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            float p = (float)phase;
            float s;
            switch (wave)
            {
                case Wave.Sine: s = Mathf.Sin(2f * Mathf.PI * p); break;
                case Wave.Square: s = p < 0.5f ? 1f : -1f; break;
                case Wave.Triangle: s = 1f - 4f * Mathf.Abs(p - 0.5f); break;
                default: s = noise; break;
            }
            float env = (i < attack ? (float)i / attack : 1f) * (1f - t) * (1f - t);
            data[i] = s * volume * env;
        }
        return data;
    }

    static float[] Silence(float duration)
    {
        return new float[Mathf.Max(1, (int)(SampleRate * duration))];
    }

    static float[] Seq(params float[][] parts)
    {
        int total = 0;
        foreach (float[] p in parts) total += p.Length;
        float[] result = new float[total];
        int offset = 0;
        foreach (float[] p in parts)
        {
            System.Array.Copy(p, 0, result, offset, p.Length);
            offset += p.Length;
        }
        return result;
    }

    static float[] Mix(params float[][] parts)
    {
        int length = 0;
        foreach (float[] p in parts) length = Mathf.Max(length, p.Length);
        float[] result = new float[length];
        foreach (float[] p in parts)
            for (int i = 0; i < p.Length; i++) result[i] += p[i];
        for (int i = 0; i < length; i++) result[i] = Mathf.Clamp(result[i], -1f, 1f);
        return result;
    }
}
