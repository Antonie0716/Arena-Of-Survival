using UnityEngine;

/// <summary>
/// Inimigo simples: vagueia perto do ponto inicial, detecta o jogador por distância e o persegue.
/// Causa dano ao encostar e respeita colisões (usa Rigidbody2D dinâmico).
/// Se ficar preso num obstáculo, desvia lateralmente por alguns instantes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Perseguição")]
    public float moveSpeed = 2.6f;          // menor que a do jogador (5)
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

    public bool IsChasing => chasing;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body != null) bodyBase = body.transform.localPosition;
        animOffset = Random.value * 10f;
    }

    void Start()
    {
        home = rb.position;
        lastPos = home;
        if (target == null && GameManager.Instance != null && GameManager.Instance.player != null)
            target = GameManager.Instance.player.transform;
    }

    void Update()
    {
        if (body == null) return;
        float speed = Physics2DCompat.GetVelocity(rb).magnitude;
        float bob = speed > 0.1f
            ? Mathf.Abs(Mathf.Sin((Time.time + animOffset) * 10f)) * 0.08f
            : Mathf.Sin((Time.time + animOffset) * 3f) * 0.03f;
        body.transform.localPosition = bodyBase + Vector3.up * bob;
    }

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying)
        {
            Physics2DCompat.SetVelocity(rb, Vector2.zero);
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector2 pos = rb.position;

        if (knockTimer > 0f)
        {
            knockTimer -= dt;
            Physics2DCompat.SetVelocity(rb, knockVelocity);
            lastPos = pos;
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= dt;
            Physics2DCompat.SetVelocity(rb, Vector2.zero);
            lastPos = pos;
            return;
        }

        if (target != null)
        {
            float d = Vector2.Distance(pos, target.position);
            if (!chasing && d <= detectionRadius) chasing = true;
            else if (chasing && d > loseInterestRadius) chasing = false;
        }

        Vector2 velocity;
        if (chasing && target != null)
        {
            Vector2 toTarget = (Vector2)target.position - pos;
            Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;

            if (detourTimer > 0f)
            {
                detourTimer -= dt;
                dir = (dir * 0.35f + detourDir).normalized;
            }

            velocity = dir * moveSpeed;

            // Detecção simples de "preso" em obstáculos.
            float moved = (pos - lastPos).magnitude;
            if (moved < moveSpeed * dt * 0.3f) stuckTimer += dt; else stuckTimer = 0f;
            if (stuckTimer > 0.4f && detourTimer <= 0f)
            {
                stuckTimer = 0f;
                detourTimer = 0.8f;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                detourDir = Random.value < 0.5f ? perp : -perp;
            }
        }
        else
        {
            wanderTimer -= dt;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(1f, 2.5f);
                Vector2 toHome = home - pos;
                if (toHome.magnitude > wanderRadius) wanderDir = toHome.normalized;
                else if (Random.value < 0.35f) wanderDir = Vector2.zero;
                else wanderDir = Random.insideUnitCircle.normalized;
            }
            velocity = wanderDir * wanderSpeed;
            stuckTimer = 0f;
        }

        lastPos = pos;
        Physics2DCompat.SetVelocity(rb, velocity);
    }

    void OnCollisionEnter2D(Collision2D collision) { TryDamage(collision.collider); }
    void OnCollisionStay2D(Collision2D collision) { TryDamage(collision.collider); }

    void TryDamage(Collider2D other)
    {
        if (!enabled || other == null) return;
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null) return;

        chasing = true;
        if (player.TakeDamage(contactDamage, rb.position))
            pauseTimer = pauseAfterHit; // dá tempo do jogador se afastar
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        knockVelocity = direction.normalized * force;
        knockTimer = duration;
        chasing = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
