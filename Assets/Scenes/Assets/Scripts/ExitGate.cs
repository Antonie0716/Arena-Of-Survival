using UnityEngine;

/// <summary>
/// Controla o portão de saída.
/// Sem a chave, o portão permanece trancado.
/// Com a chave, o jogador pode abrir o portão e vencer o jogo.
/// </summary>
public class ExitGate : MonoBehaviour
{
    public SpriteRenderer gateRenderer;
    public Sprite closedSprite;
    public Sprite openSprite;

    public Collider2D blockingCollider;

    public SpriteRenderer unlockGlow;

    public float lockedMessageCooldown = 2.5f;

    bool opened = false;
    bool unlockable = false;

    float nextMessageTime = 0f;

    Color glowBase = Color.white;

    /// <summary>
    /// Retorna se o portão está aberto.
    /// </summary>
    public bool IsOpen
    {
        get
        {
            return opened;
        }
    }

    void Awake()
    {
        // Procura o SpriteRenderer caso não tenha sido definido.
        if (gateRenderer == null)
        {
            gateRenderer =
                GetComponent<SpriteRenderer>();
        }

        // Coloca o sprite de portão fechado.
        if (gateRenderer != null)
        {
            if (closedSprite != null)
            {
                gateRenderer.sprite =
                    closedSprite;
            }
        }

        // Configura o brilho do portão.
        if (unlockGlow != null)
        {
            glowBase =
                unlockGlow.color;

            unlockGlow.enabled =
                false;
        }
    }

    void Update()
    {
        // Faz o portão brilhar quando ele pode ser aberto.
        if (unlockable)
        {
            if (!opened)
            {
                if (unlockGlow != null)
                {
                    Color novaCor =
                        glowBase;

                    novaCor.a =
                        0.35f +
                        0.3f *
                        Mathf.Sin(
                            Time.time * 5f
                        );

                    unlockGlow.color =
                        novaCor;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayer(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HandlePlayer(other);
    }

    /// <summary>
    /// Verifica o que deve acontecer quando o jogador
    /// chega perto do portão.
    /// </summary>
    void HandlePlayer(Collider2D other)
    {
        // Se o portão já estiver aberto, não faz nada.
        if (opened)
        {
            return;
        }

        // Verifica se existe um objeto entrando no trigger.
        if (other == null)
        {
            return;
        }

        // Verifica se o objeto é o jogador.
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

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

        // Verifica se o jogo está acontecendo.
        if (!gm.IsPlaying)
        {
            return;
        }

        // Se o jogador possui a chave,
        // pode concluir o jogo.
        if (gm.HasKey)
        {
            gm.OnGateReached();
        }
        else
        {
            // Se não possui a chave,
            // mostra a mensagem de portão trancado.
            if (Time.time >= nextMessageTime)
            {
                nextMessageTime =
                    Time.time +
                    lockedMessageCooldown;

                AudioManager.Play(
                    Sfx.Locked
                );

                gm.ShowMessage(
                    "PORTÃO TRANCADO! ENCONTRE A CHAVE.",
                    2.5f
                );
            }
        }
    }

    /// <summary>
    /// Chamado quando o jogador coleta a chave.
    /// O portão passa a brilhar.
    /// </summary>
    public void SetUnlockable()
    {
        unlockable = true;

        if (unlockGlow != null)
        {
            unlockGlow.enabled =
                true;
        }
    }

    /// <summary>
    /// Abre o portão.
    /// </summary>
    public void Open()
    {
        opened = true;

        // Troca o sprite para o portão aberto.
        if (gateRenderer != null)
        {
            if (openSprite != null)
            {
                gateRenderer.sprite =
                    openSprite;
            }
        }

        // Remove a colisão que bloqueava o jogador.
        if (blockingCollider != null)
        {
            blockingCollider.enabled =
                false;
        }

        // Desliga o brilho.
        if (unlockGlow != null)
        {
            unlockGlow.enabled =
                false;
        }
    }
}