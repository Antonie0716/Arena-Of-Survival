using UnityEngine;

/// <summary>
/// Controla a movimentação do jogador.
/// O jogador se movimenta em quatro direções usando WASD.
/// Não existe movimento diagonal.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;

    [Header("Visual")]
    public SpriteRenderer body;
    public Sprite spriteDown;
    public Sprite spriteUp;
    public Sprite spriteSide;

    public Vector2 Facing
    {
        get;
        private set;
    }

    public bool IsMoving
    {
        get
        {
            if (moveInput != Vector2.zero)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool IsStopped
    {
        get
        {
            return stopped;
        }
    }

    Rigidbody2D rb;

    Vector2 moveInput;

    bool horizontalPriority = true;
    bool stopped = false;

    Vector2 knockbackVelocity;
    float knockbackTimer = 0f;

    Vector3 bodyBasePosition;

    void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        if (body == null)
        {
            body =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (body != null)
        {
            bodyBasePosition =
                body.transform.localPosition;
        }

        // O jogador começa olhando para baixo.
        Facing =
            Vector2.down;

        UpdateFacingSprite();
    }

    void Update()
    {
        GameManager gm =
            GameManager.Instance;

        // Verifica se o jogador deve ficar parado.
        if (stopped)
        {
            moveInput =
                Vector2.zero;

            return;
        }

        if (gm != null)
        {
            if (!gm.IsPlaying)
            {
                moveInput =
                    Vector2.zero;

                return;
            }
        }

        UpdateMovementPriority();

        float horizontal =
            GetHorizontalInput();

        float vertical =
            GetVerticalInput();

        // Evita movimento diagonal.
        if (horizontal != 0f && vertical != 0f)
        {
            if (horizontalPriority)
            {
                vertical = 0f;
            }
            else
            {
                horizontal = 0f;
            }
        }

        moveInput =
            new Vector2(
                horizontal,
                vertical
            );

        // Atualiza a direção do jogador.
        if (moveInput != Vector2.zero)
        {
            Facing =
                moveInput;

            UpdateFacingSprite();
        }

        UpdateWalkAnimation();
    }

    void FixedUpdate()
    {
        // Se o jogador estiver parado,
        // a velocidade fica zerada.
        if (stopped)
        {
            Physics2DCompat.SetVelocity(
                rb,
                Vector2.zero
            );

            return;
        }

        // Movimento de knockback.
        if (knockbackTimer > 0f)
        {
            knockbackTimer =
                knockbackTimer -
                Time.fixedDeltaTime;

            Physics2DCompat.SetVelocity(
                rb,
                knockbackVelocity
            );

            return;
        }

        // Movimento normal.
        Vector2 velocity =
            moveInput * moveSpeed;

        Physics2DCompat.SetVelocity(
            rb,
            velocity
        );
    }

    /// <summary>
    /// Define qual eixo possui prioridade
    /// dependendo da última tecla pressionada.
    /// </summary>
    void UpdateMovementPriority()
    {
        if (GameInput.LeftPressed || GameInput.RightPressed)
        {
            horizontalPriority =
                true;
        }

        if (GameInput.UpPressed || GameInput.DownPressed)
        {
            horizontalPriority =
                false;
        }
    }

    /// <summary>
    /// Calcula o movimento horizontal.
    /// Retorna -1, 0 ou 1.
    /// </summary>
    float GetHorizontalInput()
    {
        float horizontal =
            0f;

        if (GameInput.RightHeld)
        {
            horizontal =
                horizontal + 1f;
        }

        if (GameInput.LeftHeld)
        {
            horizontal =
                horizontal - 1f;
        }

        return horizontal;
    }

    /// <summary>
    /// Calcula o movimento vertical.
    /// Retorna -1, 0 ou 1.
    /// </summary>
    float GetVerticalInput()
    {
        float vertical =
            0f;

        if (GameInput.UpHeld)
        {
            vertical =
                vertical + 1f;
        }

        if (GameInput.DownHeld)
        {
            vertical =
                vertical - 1f;
        }

        return vertical;
    }

    /// <summary>
    /// Atualiza o sprite de acordo
    /// com a direção do jogador.
    /// </summary>
    void UpdateFacingSprite()
    {
        if (body == null)
        {
            return;
        }

        // Jogador olhando para cima.
        if (Facing.y > 0.1f)
        {
            if (spriteUp != null)
            {
                body.sprite =
                    spriteUp;
            }

            body.flipX =
                false;
        }

        // Jogador olhando para baixo.
        else if (Facing.y < -0.1f)
        {
            if (spriteDown != null)
            {
                body.sprite =
                    spriteDown;
            }

            body.flipX =
                false;
        }

        // Jogador olhando para os lados.
        else
        {
            if (spriteSide != null)
            {
                body.sprite =
                    spriteSide;
            }

            if (Facing.x < 0f)
            {
                body.flipX =
                    true;
            }
            else
            {
                body.flipX =
                    false;
            }
        }
    }

    /// <summary>
    /// Faz uma pequena animação
    /// enquanto o jogador está andando.
    /// </summary>
    void UpdateWalkAnimation()
    {
        if (body == null)
        {
            return;
        }

        float bob =
            0f;

        if (IsMoving)
        {
            bob =
                Mathf.Abs(
                    Mathf.Sin(
                        Time.time * 14f
                    )
                ) * 0.07f;
        }
        else
        {
            bob =
                0f;
        }

        body.transform.localPosition =
            bodyBasePosition +
            Vector3.up * bob;
    }

    /// <summary>
    /// Aplica um empurrão no jogador
    /// quando ele recebe dano.
    /// </summary>
    public void ApplyKnockback(
        Vector2 direction,
        float force,
        float duration
    )
    {
        if (stopped)
        {
            return;
        }

        // Se não existir uma direção válida,
        // usa a direção oposta à que o jogador está olhando.
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction =
                -Facing;
        }

        knockbackVelocity =
            direction.normalized *
            force;

        knockbackTimer =
            duration;
    }

    /// <summary>
    /// Interrompe o movimento do jogador
    /// quando a partida termina.
    /// </summary>
    public void Stop()
    {
        stopped =
            true;

        moveInput =
            Vector2.zero;

        knockbackTimer =
            0f;

        if (rb != null)
        {
            Physics2DCompat.SetVelocity(
                rb,
                Vector2.zero
            );

            rb.constraints =
                RigidbodyConstraints2D.FreezeAll;
        }

        if (body != null)
        {
            body.transform.localPosition =
                bodyBasePosition;
        }
    }
}