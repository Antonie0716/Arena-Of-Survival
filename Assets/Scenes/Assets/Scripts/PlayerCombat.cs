using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o ataque do jogador.
/// Ao pressionar Espaço, o jogador ataca
/// na direção em que está olhando.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Ataque")]
    public float attackCooldown = 0.5f;

    [Tooltip("Distância do centro do golpe até o jogador.")]
    public float attackDistance = 0.85f;

    [Tooltip("Raio da área atingida pelo golpe.")]
    public float attackRadius = 0.65f;

    public int damage = 1;

    [Header("Visual")]
    public SpriteRenderer slashEffect;

    public float slashDuration = 0.12f;

    PlayerController controller;

    float nextAttackTime = 0f;

    void Awake()
    {
        controller =
            GetComponent<PlayerController>();

        if (slashEffect != null)
        {
            slashEffect.enabled =
                false;
        }
    }

    void Update()
    {
        GameManager gm =
            GameManager.Instance;

        // Não permite atacar se a partida estiver parada.
        if (gm != null)
        {
            if (!gm.IsPlaying)
            {
                return;
            }
        }

        // Não permite atacar se o jogador estiver parado.
        if (controller.IsStopped)
        {
            return;
        }

        // Verifica se o jogador apertou Espaço.
        if (GameInput.AttackPressed)
        {
            // Verifica se o tempo de espera do ataque terminou.
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime =
                    Time.time +
                    attackCooldown;

                Attack();
            }
        }
    }

    /// <summary>
    /// Executa o ataque do jogador.
    /// </summary>
    void Attack()
    {
        Vector2 direction =
            controller.Facing;

        Vector2 center =
            (Vector2)transform.position +
            direction * attackDistance;

        // Retorna todos os colisores
        // encontrados dentro da área do ataque.
        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                center,
                attackRadius
            );

        // Array que guarda os inimigos
        // que já receberam dano neste ataque.
        EnemyHealth[] enemiesHit =
            new EnemyHealth[colliders.Length];

        int enemiesHitCount =
            0;

        // Percorre todos os colisores encontrados.
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D currentCollider =
                colliders[i];

            // Ignora posições vazias.
            if (currentCollider == null)
            {
                continue;
            }

            // Ignora colisores do tipo Trigger.
            if (currentCollider.isTrigger)
            {
                continue;
            }

            EnemyHealth enemy =
                currentCollider
                .GetComponentInParent<EnemyHealth>();

            // Verifica se existe um inimigo.
            if (enemy == null)
            {
                continue;
            }

            // Não ataca inimigos que já morreram.
            if (enemy.IsDead)
            {
                continue;
            }

            // Verifica se esse inimigo já foi atingido
            // durante o mesmo golpe.
            bool alreadyHit =
                WasAlreadyHit(
                    enemy,
                    enemiesHit,
                    enemiesHitCount
                );

            if (alreadyHit)
            {
                continue;
            }

            // Guarda o inimigo no array.
            enemiesHit[enemiesHitCount] =
                enemy;

            enemiesHitCount =
                enemiesHitCount + 1;

            // Aplica o dano.
            enemy.TakeDamage(
                damage,
                direction
            );
        }

        AudioManager.Play(
            Sfx.Attack
        );

        // Mostra o efeito visual do ataque.
        if (slashEffect != null)
        {
            StartCoroutine(
                SlashRoutine(direction)
            );
        }
    }

    /// <summary>
    /// Verifica se um inimigo já foi atingido
    /// durante o ataque atual.
    /// </summary>
    bool WasAlreadyHit(
        EnemyHealth enemy,
        EnemyHealth[] enemiesHit,
        int enemiesHitCount
    )
    {
        // Percorre apenas as posições
        // que já foram preenchidas.
        for (int i = 0; i < enemiesHitCount; i++)
        {
            if (enemiesHit[i] == enemy)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Mostra o efeito visual do golpe.
    /// </summary>
    IEnumerator SlashRoutine(Vector2 direction)
    {
        Transform slashTransform =
            slashEffect.transform;

        slashTransform.localPosition =
            direction * attackDistance;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        slashTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        slashEffect.enabled =
            true;

        yield return new WaitForSeconds(
            slashDuration
        );

        slashEffect.enabled =
            false;
    }

    /// <summary>
    /// Mostra no editor a área atingida pelo ataque.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Vector2 direction;

        if (Application.isPlaying && controller != null)
        {
            direction =
                controller.Facing;
        }
        else
        {
            direction =
                Vector2.down;
        }

        Gizmos.color =
            Color.red;

        Vector2 center =
            (Vector2)transform.position +
            direction * attackDistance;

        Gizmos.DrawWireSphere(
            center,
            attackRadius
        );
    }
}