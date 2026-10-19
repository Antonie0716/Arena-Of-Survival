using UnityEngine;

/// <summary>
/// Controla a chave do jogo.
/// A chave fica escondida até todos os inimigos morrerem.
/// Quando o jogador encosta nela, a chave é coletada.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyItem : MonoBehaviour
{
    public Transform visual;
    public SpriteRenderer glow;

    public float bobAmplitude = 0.12f;
    public float bobSpeed = 3f;

    bool collected = false;

    Vector3 visualBase;
    Vector3 glowBaseScale;

    Color glowBase = Color.white;

    void Awake()
    {
        // Garante que o colisor funcione como Trigger.
        Collider2D collider =
            GetComponent<Collider2D>();

        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Se o objeto visual não foi definido,
        // tenta encontrar um filho automaticamente.
        if (visual == null)
        {
            if (transform.childCount > 0)
            {
                visual =
                    transform.GetChild(
                        transform.childCount - 1
                    );
            }
        }

        // Guarda a posição inicial da chave.
        if (visual != null)
        {
            visualBase =
                visual.localPosition;
        }

        // Guarda a cor e a escala inicial do brilho.
        if (glow != null)
        {
            glowBase =
                glow.color;

            glowBaseScale =
                glow.transform.localScale;
        }
    }

    /// <summary>
    /// Faz a chave aparecer no cenário.
    /// </summary>
    public void Spawn()
    {
        collected = false;

        gameObject.SetActive(true);
    }

    void Update()
    {
        float time =
            Time.time;

        // Faz a chave subir e descer.
        if (visual != null)
        {
            float movimento =
                Mathf.Sin(
                    time * bobSpeed
                ) * bobAmplitude;

            visual.localPosition =
                visualBase +
                Vector3.up * movimento;
        }

        // Faz o brilho da chave pulsar.
        if (glow != null)
        {
            Color novaCor =
                glowBase;

            novaCor.a =
                0.45f +
                0.25f *
                Mathf.Sin(
                    time * 4f
                );

            glow.color =
                novaCor;

            float escala =
                1f +
                0.15f *
                Mathf.Sin(
                    time * 4f
                );

            glow.transform.localScale =
                glowBaseScale * escala;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    /// <summary>
    /// Verifica se o objeto que encostou
    /// na chave é o jogador.
    /// </summary>
    void TryCollect(Collider2D other)
    {
        // Não permite coletar duas vezes.
        if (collected)
        {
            return;
        }

        // Verifica se existe um objeto.
        if (other == null)
        {
            return;
        }

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        // Verifica se quem encostou é o jogador.
        if (player == null)
        {
            return;
        }

        GameManager gm =
            GameManager.Instance;

        // Verifica se existe GameManager.
        if (gm == null)
        {
            return;
        }

        // Só permite coletar durante a partida.
        if (!gm.IsPlaying)
        {
            return;
        }

        // Marca a chave como coletada.
        collected = true;

        // Informa o GameManager.
        gm.OnKeyCollected();

        // Esconde a chave.
        gameObject.SetActive(false);
    }
}