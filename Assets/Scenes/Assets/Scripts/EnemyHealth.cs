using System.Collections;
using UnityEngine;

/// <summary>
/// Controla a vida do inimigo.
/// O inimigo recebe dano, sofre knockback,
/// pisca ao ser atingido e desaparece ao morrer.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;

    public float knockbackForce = 6f;

    public SpriteRenderer body;

    public bool IsDead
    {
        get;
        private set;
    }

    public int CurrentHealth
    {
        get;
        private set;
    }

    EnemyController controller;

    Color baseColor = Color.white;

    Coroutine flashRoutine;

    void Awake()
    {
        // A vida atual começa com a vida máxima.
        CurrentHealth = maxHealth;

        controller =
            GetComponent<EnemyController>();

        if (body == null)
        {
            body =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (body != null)
        {
            baseColor =
                body.color;
        }
    }

    /// <summary>
    /// Recebe o dano causado ao inimigo.
    /// </summary>
    public void TakeDamage(
        int amount,
        Vector2 hitDirection
    )
    {
        // Não recebe dano se já estiver morto.
        if (IsDead)
        {
            return;
        }

        // Também não recebe valores de dano inválidos.
        if (amount <= 0)
        {
            return;
        }

        // Subtrai o dano da vida atual.
        CurrentHealth =
            CurrentHealth - amount;

        // Aplica o empurrão do ataque.
        if (controller != null)
        {
            controller.ApplyKnockback(
                hitDirection,
                knockbackForce,
                0.12f
            );
        }

        // Verifica se a vida chegou a zero.
        if (CurrentHealth <= 0)
        {
            Die();

            return;
        }

        // Som de inimigo atingido.
        AudioManager.Play(
            Sfx.EnemyHit
        );

        // Para a animação anterior,
        // caso ela ainda esteja acontecendo.
        if (flashRoutine != null)
        {
            StopCoroutine(
                flashRoutine
            );
        }

        // Começa a animação de dano.
        flashRoutine =
            StartCoroutine(
                FlashRoutine()
            );
    }

    /// <summary>
    /// Faz o inimigo piscar quando recebe dano.
    /// </summary>
    IEnumerator FlashRoutine()
    {
        if (body == null)
        {
            yield break;
        }

        body.color =
            new Color(
                1f,
                0.2f,
                0.2f,
                1f
            );

        body.transform.localScale =
            Vector3.one * 1.2f;

        yield return new WaitForSeconds(
            0.08f
        );

        body.transform.localScale =
            Vector3.one;

        yield return new WaitForSeconds(
            0.1f
        );

        body.color =
            baseColor;

        flashRoutine =
            null;
    }

    /// <summary>
    /// Executa as ações quando o inimigo morre.
    /// </summary>
    void Die()
    {
        IsDead = true;

        // Para a animação de dano,
        // caso ela ainda esteja acontecendo.
        if (flashRoutine != null)
        {
            StopCoroutine(
                flashRoutine
            );
        }

        AudioManager.Play(
            Sfx.EnemyDeath
        );

        // Guarda todos os colisores em um array.
        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>();

        // Percorre o array e desativa cada colisor.
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled =
                false;
        }

        // Para o comportamento do inimigo.
        if (controller != null)
        {
            controller.enabled =
                false;
        }

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            Physics2DCompat.SetVelocity(
                rb,
                Vector2.zero
            );

            rb.simulated =
                false;
        }

        // Informa ao GameManager
        // que um inimigo foi derrotado.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyDefeated();
        }

        // Inicia a animação de desaparecimento.
        StartCoroutine(
            DeathRoutine()
        );
    }

    /// <summary>
    /// Faz a animação do inimigo desaparecendo.
    /// </summary>
    IEnumerator DeathRoutine()
    {
        if (body != null)
        {
            Vector3 startScale =
                body.transform.localScale;

            float t = 0f;

            float duration =
                0.35f;

            // Continua enquanto o tempo da animação
            // ainda não chegou à duração definida.
            while (t < duration)
            {
                t =
                    t + Time.deltaTime;

                float k =
                    Mathf.Clamp01(
                        t / duration
                    );

                body.color =
                    new Color(
                        1f,
                        0.25f,
                        0.25f,
                        1f - k
                    );

                body.transform.localScale =
                    startScale *
                    (1f + 0.4f * k);

                yield return null;
            }
        }

        // Remove o inimigo da cena.
        Destroy(
            gameObject
        );
    }
}