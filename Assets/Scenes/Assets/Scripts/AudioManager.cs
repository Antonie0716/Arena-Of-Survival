using UnityEngine;

public enum Sfx
{
    Attack,
    EnemyHit,
    EnemyDeath,
    PlayerHurt,
    KeyAppear,
    KeyCollect,
    Locked,
    GateOpen,
    Victory,
    Defeat,
    Click
}

/// <summary>
/// Gerencia os efeitos sonoros do jogo.
/// Os sons são criados por código em estilo 8 bits.
/// Não utiliza arquivos externos de áudio.
/// </summary>
public class AudioManager : MonoBehaviour
{
    const int SampleRate = 44100;
    const int TotalSons = 11;

    public static float MasterVolume = 0.55f;

    static AudioManager instance;

    AudioSource source;

    // Array que guarda todos os efeitos sonoros.
    AudioClip[] clips = new AudioClip[TotalSons];

    enum Wave
    {
        Sine,
        Square,
        Triangle,
        Noise
    }

    /// <summary>
    /// Toca o efeito sonoro solicitado.
    /// </summary>
    public static void Play(Sfx sfx)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (instance == null)
        {
            Create();
        }

        if (instance == null || instance.source == null)
        {
            return;
        }

        int indice = (int)sfx;

        if (indice >= 0 && indice < instance.clips.Length)
        {
            AudioClip clip = instance.clips[indice];

            if (clip != null)
            {
                instance.source.PlayOneShot(clip, MasterVolume);
            }
        }
    }

    /// <summary>
    /// Cria o objeto responsável pelos sons.
    /// </summary>
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
            Debug.LogWarning(
                "Arena of Survival: sons desativados (" +
                e.Message +
                ")"
            );
        }
    }

    /// <summary>
    /// Limpa a referência quando o objeto é destruído.
    /// </summary>
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Cria todos os sons utilizados no jogo.
    /// </summary>
    void BuildClips()
    {
        clips[(int)Sfx.Attack] = Make(
            "sfx_attack",
            Mix(
                Tone(1400, 300, 0.12f, Wave.Noise, 0.22f),
                Tone(700, 220, 0.10f, Wave.Triangle, 0.30f)
            )
        );

        clips[(int)Sfx.EnemyHit] = Make(
            "sfx_enemy_hit",
            Mix(
                Tone(340, 160, 0.09f, Wave.Square, 0.22f),
                Tone(900, 200, 0.06f, Wave.Noise, 0.15f)
            )
        );

        clips[(int)Sfx.EnemyDeath] = Make(
            "sfx_enemy_death",
            Mix(
                Tone(420, 50, 0.38f, Wave.Square, 0.20f),
                Tone(800, 80, 0.32f, Wave.Noise, 0.16f)
            )
        );

        clips[(int)Sfx.PlayerHurt] = Make(
            "sfx_player_hurt",
            Mix(
                Tone(230, 80, 0.26f, Wave.Square, 0.28f),
                Tone(400, 100, 0.20f, Wave.Noise, 0.14f)
            )
        );

        clips[(int)Sfx.KeyAppear] = Make(
            "sfx_key_appear",
            Seq(
                Tone(523.25f, 523.25f, 0.08f, Wave.Triangle, 0.35f),
                Tone(659.25f, 659.25f, 0.08f, Wave.Triangle, 0.35f),
                Tone(783.99f, 783.99f, 0.08f, Wave.Triangle, 0.35f),
                Tone(1046.5f, 1046.5f, 0.22f, Wave.Triangle, 0.35f)
            )
        );

        clips[(int)Sfx.KeyCollect] = Make(
            "sfx_key_collect",
            Seq(
                Tone(880f, 880f, 0.07f, Wave.Square, 0.16f),
                Tone(1174.66f, 1174.66f, 0.07f, Wave.Square, 0.16f),
                Tone(1567.98f, 1567.98f, 0.18f, Wave.Square, 0.16f)
            )
        );

        clips[(int)Sfx.Locked] = Make(
            "sfx_locked",
            Seq(
                Tone(150, 150, 0.08f, Wave.Square, 0.22f),
                Silence(0.04f),
                Tone(120, 120, 0.11f, Wave.Square, 0.22f)
            )
        );

        clips[(int)Sfx.GateOpen] = Make(
            "sfx_gate_open",
            Mix(
                Tone(90, 50, 0.8f, Wave.Noise, 0.20f),
                Tone(110, 65, 0.8f, Wave.Square, 0.10f)
            )
        );

        clips[(int)Sfx.Victory] = Make(
            "sfx_victory",
            Seq(
                Tone(523.25f, 523.25f, 0.13f, Wave.Square, 0.17f),
                Tone(659.25f, 659.25f, 0.13f, Wave.Square, 0.17f),
                Tone(783.99f, 783.99f, 0.13f, Wave.Square, 0.17f),
                Tone(1046.5f, 1046.5f, 0.45f, Wave.Square, 0.17f)
            )
        );

        clips[(int)Sfx.Defeat] = Make(
            "sfx_defeat",
            Seq(
                Tone(392f, 392f, 0.22f, Wave.Triangle, 0.35f),
                Tone(349.23f, 349.23f, 0.22f, Wave.Triangle, 0.35f),
                Tone(311.13f, 311.13f, 0.22f, Wave.Triangle, 0.35f),
                Tone(261.63f, 240f, 0.6f, Wave.Triangle, 0.35f)
            )
        );

        clips[(int)Sfx.Click] = Make(
            "sfx_click",
            Tone(700, 500, 0.05f, Wave.Square, 0.13f)
        );
    }

    /// <summary>
    /// Cria um AudioClip a partir dos dados do som.
    /// </summary>
    static AudioClip Make(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(
            name,
            data.Length,
            1,
            SampleRate,
            false
        );

        clip.SetData(data, 0);

        return clip;
    }

    /// <summary>
    /// Gera uma onda sonora.
    /// </summary>
    static float[] Tone(
        float f0,
        float f1,
        float duration,
        Wave wave,
        float volume
    )
    {
        int n = Mathf.Max(
            1,
            (int)(SampleRate * duration)
        );

        float[] data = new float[n];

        System.Random rng = new System.Random(n);

        double phase = 0;
        float noise = 0f;

        int attack = Mathf.Max(
            1,
            (int)(SampleRate * 0.004f)
        );

        // Percorre todas as posições do array de áudio.
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / n;

            float f = Mathf.Lerp(f0, f1, t);

            phase = phase + f / SampleRate;

            if (phase >= 1.0)
            {
                phase = phase - 1.0;

                noise = (float)(
                    rng.NextDouble() * 2.0 - 1.0
                );
            }

            float p = (float)phase;

            float s;

            // Define o formato da onda.
            if (wave == Wave.Sine)
            {
                s = Mathf.Sin(
                    2f * Mathf.PI * p
                );
            }
            else if (wave == Wave.Square)
            {
                if (p < 0.5f)
                {
                    s = 1f;
                }
                else
                {
                    s = -1f;
                }
            }
            else if (wave == Wave.Triangle)
            {
                s = 1f - 4f * Mathf.Abs(
                    p - 0.5f
                );
            }
            else
            {
                s = noise;
            }

            float inicio;

            if (i < attack)
            {
                inicio = (float)i / attack;
            }
            else
            {
                inicio = 1f;
            }

            float env =
                inicio *
                (1f - t) *
                (1f - t);

            data[i] = s * volume * env;
        }

        return data;
    }

    /// <summary>
    /// Cria um período de silêncio.
    /// </summary>
    static float[] Silence(float duration)
    {
        int tamanho = Mathf.Max(
            1,
            (int)(SampleRate * duration)
        );

        float[] silencio = new float[tamanho];

        return silencio;
    }

    /// <summary>
    /// Junta vários sons em sequência.
    /// </summary>
    static float[] Seq(params float[][] parts)
    {
        int total = 0;

        // Primeiro descobre o tamanho total.
        for (int i = 0; i < parts.Length; i++)
        {
            total = total + parts[i].Length;
        }

        float[] result = new float[total];

        int offset = 0;

        // Depois copia cada som para o array final.
        for (int i = 0; i < parts.Length; i++)
        {
            float[] parte = parts[i];

            System.Array.Copy(
                parte,
                0,
                result,
                offset,
                parte.Length
            );

            offset = offset + parte.Length;
        }

        return result;
    }

    /// <summary>
    /// Mistura vários sons ao mesmo tempo.
    /// </summary>
    static float[] Mix(params float[][] parts)
    {
        int length = 0;

        // Descobre qual parte possui o maior tamanho.
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > length)
            {
                length = parts[i].Length;
            }
        }

        float[] result = new float[length];

        // Soma os valores dos diferentes sons.
        for (int i = 0; i < parts.Length; i++)
        {
            float[] parte = parts[i];

            for (int j = 0; j < parte.Length; j++)
            {
                result[j] =
                    result[j] +
                    parte[j];
            }
        }

        // Mantém os valores dentro do limite permitido.
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Mathf.Clamp(
                result[i],
                -1f,
                1f
            );
        }

        return result;
    }
}