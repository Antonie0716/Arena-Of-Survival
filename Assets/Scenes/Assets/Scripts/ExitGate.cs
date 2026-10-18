using UnityEngine;

/// <summary>
/// Portão de saída. Possui um colisor sólido (bloqueia a passagem) e um colisor trigger
/// à frente, que detecta a aproximação do jogador.
/// Sem chave: mostra "PORTÃO TRANCADO!". Com chave: abre e dispara a vitória.
/// </summary>
public class ExitGate : MonoBehaviour
{
    public SpriteRenderer gateRenderer;
    public Sprite closedSprite;
    public Sprite openSprite;
    public Collider2D blockingCollider;
    public SpriteRenderer unlockGlow;
    public float lockedMessageCooldown = 2.5f;

    bool opened;
    bool unlockable;
    float nextMessageTime;
    Color glowBase = Color.white;

    public bool IsOpen => opened;

    void Awake()
    {
        if (gateRenderer == null) gateRenderer = GetComponent<SpriteRenderer>();
        if (gateRenderer != null && closedSprite != null) gateRenderer.sprite = closedSprite;
        if (unlockGlow != null)
        {
            glowBase = unlockGlow.color;
            unlockGlow.enabled = false;
        }
    }

    void Update()
    {
        if (unlockable && !opened && unlockGlow != null)
        {
            Color c = glowBase;
            c.a = 0.35f + 0.3f * Mathf.Sin(Time.time * 5f);
            unlockGlow.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { HandlePlayer(other); }
    void OnTriggerStay2D(Collider2D other) { HandlePlayer(other); }

    void HandlePlayer(Collider2D other)
    {
        if (opened || other == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        if (gm.HasKey)
        {
            gm.OnGateReached();
        }
        else if (Time.time >= nextMessageTime)
        {
            nextMessageTime = Time.time + lockedMessageCooldown;
            AudioManager.Play(Sfx.Locked);
            gm.ShowMessage("PORTÃO TRANCADO! ENCONTRE A CHAVE.", 2.5f);
        }
    }

    /// <summary>Chamado quando o jogador coleta a chave: o portão passa a brilhar.</summary>
    public void SetUnlockable()
    {
        unlockable = true;
        if (unlockGlow != null) unlockGlow.enabled = true;
    }

    public void Open()
    {
        opened = true;
        if (gateRenderer != null && openSprite != null) gateRenderer.sprite = openSprite;
        if (blockingCollider != null) blockingCollider.enabled = false;
        if (unlockGlow != null) unlockGlow.enabled = false;
    }
}
