using UnityEngine;

/// <summary>
/// Movimentação do jogador em quatro direções (WASD) usando Rigidbody2D.
/// A tecla pressionada mais recentemente tem prioridade, então não há movimento diagonal.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;

    [Header("Visual (direção do último movimento)")]
    public SpriteRenderer body;
    public Sprite spriteDown;
    public Sprite spriteUp;
    public Sprite spriteSide; // desenhado olhando para a direita; esquerda usa flipX

    public Vector2 Facing { get; private set; } = Vector2.down;
    public bool IsMoving => moveInput != Vector2.zero;
    public bool IsStopped => stopped;

    Rigidbody2D rb;
    Vector2 moveInput;
    bool horizontalPriority = true;
    bool stopped;

    Vector2 knockbackVelocity;
    float knockbackTimer;

    Vector3 bodyBasePosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body != null) bodyBasePosition = body.transform.localPosition;
        UpdateFacingSprite();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (stopped || (gm != null && !gm.IsPlaying))
        {
            moveInput = Vector2.zero;
            return;
        }

        // A tecla apertada por último define o eixo prioritário.
        if (GameInput.LeftPressed || GameInput.RightPressed) horizontalPriority = true;
        if (GameInput.UpPressed || GameInput.DownPressed) horizontalPriority = false;

        float h = 0f, v = 0f;
        if (GameInput.RightHeld) h += 1f;
        if (GameInput.LeftHeld) h -= 1f;
        if (GameInput.UpHeld) v += 1f;
        if (GameInput.DownHeld) v -= 1f;

        if (h != 0f && v != 0f)
        {
            if (horizontalPriority) v = 0f; else h = 0f;
        }

        moveInput = new Vector2(h, v);
        if (moveInput != Vector2.zero)
        {
            Facing = moveInput;
            UpdateFacingSprite();
        }

        // Pequena animação de passos.
        if (body != null)
        {
            float bob = IsMoving ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.07f : 0f;
            body.transform.localPosition = bodyBasePosition + Vector3.up * bob;
        }
    }

    void FixedUpdate()
    {
        if (stopped)
        {
            Physics2DCompat.SetVelocity(rb, Vector2.zero);
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            Physics2DCompat.SetVelocity(rb, knockbackVelocity);
            return;
        }

        Physics2DCompat.SetVelocity(rb, moveInput * moveSpeed);
    }

    void UpdateFacingSprite()
    {
        if (body == null) return;

        if (Facing.y > 0.1f)
        {
            if (spriteUp != null) body.sprite = spriteUp;
            body.flipX = false;
        }
        else if (Facing.y < -0.1f)
        {
            if (spriteDown != null) body.sprite = spriteDown;
            body.flipX = false;
        }
        else
        {
            if (spriteSide != null) body.sprite = spriteSide;
            body.flipX = Facing.x < 0f;
        }
    }

    /// <summary>Empurrão rápido ao receber dano (o jogador continua controlável logo depois).</summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (stopped) return;
        if (direction.sqrMagnitude < 0.0001f) direction = -Facing;
        knockbackVelocity = direction.normalized * force;
        knockbackTimer = duration;
    }

    /// <summary>Interrompe definitivamente o movimento (vitória ou derrota).</summary>
    public void Stop()
    {
        stopped = true;
        moveInput = Vector2.zero;
        knockbackTimer = 0f;
        if (rb != null)
        {
            Physics2DCompat.SetVelocity(rb, Vector2.zero);
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (body != null) body.transform.localPosition = bodyBasePosition;
    }
}
