using UnityEngine;

/// <summary>
/// Controla o comportamento do inimigo.
/// O inimigo patrulha perto do ponto inicial,
/// detecta o jogador, persegue e causa dano ao encostar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Perseguição")]
    public float moveSpeed = 2.6f;
    public float detectionRadius = 6.5f;
    public float loseInterestRadius = 11f;

    [Header("Patrulha")]
    public float wanderSpeed = 1.1f;
    public float wanderRadius = 2.5f;

    [Header("Dano")]
    public int contactDamage = 1;
    public float pauseAfterHit = 0.6f;

    [Header("Referências")]
    public SpriteRenderer body;
    public Transform target;

    Rigidbody2D rb;

    Vector2 home;

    bool chasing;

    float pauseTimer;
    float knockTimer;

    Vector2 knockVelocity;

    float wanderTimer;
    Vector2 wanderDir;

    Vector2 lastPos;

    float stuckTimer;
    float detourTimer;

    Vector2 detourDir;

    Vector3 bodyBase;

    float animOffset;

    // Retorna se o inimigo está perseguindo o jogador.
    public bool IsChasing
    {
        get
        {
            return chasing;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (body == null)
        {
            body = GetComponentInChildren<SpriteRenderer>();
        }

        if (body != null)
        {
            bodyBase = body.transform.localPosition;
        }

        animOffset = Random.value * 10f;
    }

    void Start()
    {
        home = rb.position;

        lastPos = home;

        // Procura o jogador caso ele ainda não tenha sido definido.
        if (target == null)
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.player != null)
                {
                    target = GameManager.Instance.player.transform;
                }
            }
        }
    }

    void Update()
    {
        if (body == null)
        {
            return;
        }

        float speed =
            Physics2DCompat.GetVelocity(rb).magnitude;

        float bob;

        // Se estiver andando, a animação é mais rápida.
        if (speed > 0.1f)
        {
            bob =
                Mathf.Abs(
                    Mathf.Sin(
                        (Time.time + animOffset) * 10f
                    )
                ) * 0.08f;
        }
        else
        {
            bob =
                Mathf.Sin(
                    (Time.time + animOffset) * 3f
                ) * 0.03f;
        }

        body.transform.localPosition =
            bodyBase + Vector3.up * bob;
    }

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;

        // Para o inimigo caso o jogo não esteja acontecendo.
        if (gm != null)
        {
            if (!gm.IsPlaying)
            {
                Physics2DCompat.SetVelocity(
                    rb,
                    Vector2.zero
                );

                return;
            }
        }

        float dt = Time.fixedDeltaTime;

        Vector2 pos = rb.position;

        // Knockback recebido ao ser atacado.
        if (knockTimer > 0f)
        {
            knockTimer = knockTimer - dt;

            Physics2DCompat.SetVelocity(
                rb,
                knockVelocity
            );

            lastPos = pos;

            return;
        }

        // Pequena pausa depois de atacar o jogador.
        if (pauseTimer > 0f)
        {
            pauseTimer = pauseTimer - dt;

            Physics2DCompat.SetVelocity(
                rb,
                Vector2.zero
            );

            lastPos = pos;

            return;
        }

        // Verifica a distância do jogador.
        if (target != null)
        {
            float distance =
                Vector2.Distance(
                    pos,
                    target.position
                );

            // Começa a perseguir.
            if (!chasing && distance <= detectionRadius)
            {
                chasing = true;
            }
            else
            {
                // Para de perseguir caso o jogador fique muito longe.
                if (chasing && distance > loseInterestRadius)
                {
                    chasing = false;
                }
            }
        }

        Vector2 velocity;

        // =========================================
        // PERSEGUIÇÃO
        // =========================================

        if (chasing && target != null)
        {
            Vector2 toTarget =
                (Vector2)target.position - pos;

            Vector2 dir;

            // Evita normalizar uma direção praticamente zerada.
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                dir = toTarget.normalized;
            }
            else
            {
                dir = Vector2.zero;
            }

            // Caso tenha ficado preso,
            // tenta fazer um pequeno desvio.
            if (detourTimer > 0f)
            {
                detourTimer = detourTimer - dt;

                dir =
                    (dir * 0.35f + detourDir).normalized;
            }

            velocity = dir * moveSpeed;

            // Descobre quanto o inimigo conseguiu se mover.
            float moved =
                (pos - lastPos).magnitude;

            // Se quase não se moveu,
            // aumenta o tempo considerado preso.
            if (moved < moveSpeed * dt * 0.3f)
            {
                stuckTimer = stuckTimer + dt;
            }
            else
            {
                stuckTimer = 0f;
            }

            // Se ficou preso por tempo suficiente,
            // escolhe uma direção lateral.
            if (stuckTimer > 0.4f && detourTimer <= 0f)
            {
                stuckTimer = 0f;

                detourTimer = 0.8f;

                Vector2 perpendicular =
                    new Vector2(
                        -dir.y,
                        dir.x
                    );

                // Escolhe aleatoriamente
                // para qual lado fará o desvio.
                if (Random.value < 0.5f)
                {
                    detourDir = perpendicular;
                }
                else
                {
                    detourDir = -perpendicular;
                }
            }
        }

        // =========================================
        // PATRULHA
        // =========================================

        else
        {
            wanderTimer =
                wanderTimer - dt;

            // Escolhe uma nova direção de tempos em tempos.
            if (wanderTimer <= 0f)
            {
                wanderTimer =
                    Random.Range(
                        1f,
                        2.5f
                    );

                Vector2 toHome =
                    home - pos;

                // Se estiver longe do ponto inicial,
                // volta em direção a ele.
                if (toHome.magnitude > wanderRadius)
                {
                    wanderDir =
                        toHome.normalized;
                }
                else
                {
                    // Às vezes fica parado.
                    if (Random.value < 0.35f)
                    {
                        wanderDir =
                            Vector2.zero;
                    }
                    else
                    {
                        // Senão escolhe uma direção aleatória.
                        wanderDir =
                            Random.insideUnitCircle.normalized;
                    }
                }
            }

            velocity =
                wanderDir * wanderSpeed;

            stuckTimer = 0f;
        }

        lastPos = pos;

        Physics2DCompat.SetVelocity(
            rb,
            velocity
        );
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(
            collision.collider
        );
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(
            collision.collider
        );
    }

    /// <summary>
    /// Tenta causar dano ao jogador.
    /// </summary>
    void TryDamage(Collider2D other)
    {
        if (!enabled)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        GameManager gm =
            GameManager.Instance;

        if (gm != null)
        {
            if (!gm.IsPlaying)
            {
                return;
            }
        }

        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null)
        {
            return;
        }

        chasing = true;

        bool recebeuDano =
            player.TakeDamage(
                contactDamage,
                rb.position
            );

        if (recebeuDano)
        {
            pauseTimer =
                pauseAfterHit;
        }
    }

    /// <summary>
    /// Empurra o inimigo após ele receber um ataque.
    /// </summary>
    public void ApplyKnockback(
        Vector2 direction,
        float force,
        float duration
    )
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        knockVelocity =
            direction.normalized * force;

        knockTimer =
            duration;

        chasing =
            true;
    }

    /// <summary>
    /// Mostra no editor o raio de detecção do inimigo.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}