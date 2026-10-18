using System.Collections;
using UnityEngine;

/// <summary>
/// Vida do inimigo (2 pontos). Pisca em vermelho ao ser atingido e desaparece ao morrer.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    public float knockbackForce = 6f;
    public SpriteRenderer body;

    public bool IsDead { get; private set; }
    public int CurrentHealth { get; private set; }

    EnemyController controller;
    Color baseColor = Color.white;
    Coroutine flashRoutine;

    void Awake()
    {
        CurrentHealth = maxHealth;
        controller = GetComponent<EnemyController>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body != null) baseColor = body.color;
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth -= amount;
        if (controller != null) controller.ApplyKnockback(hitDirection, knockbackForce, 0.12f);

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        AudioManager.Play(Sfx.EnemyHit);
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        if (body == null) yield break;
        body.color = new Color(1f, 0.2f, 0.2f, 1f);
        body.transform.localScale = Vector3.one * 1.2f;
        yield return new WaitForSeconds(0.08f);
        body.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(0.1f);
        body.color = baseColor;
        flashRoutine = null;
    }

    void Die()
    {
        IsDead = true;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        AudioManager.Play(Sfx.EnemyDeath);

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (controller != null) controller.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Physics2DCompat.SetVelocity(rb, Vector2.zero);
            rb.simulated = false;
        }

        if (GameManager.Instance != null) GameManager.Instance.OnEnemyDefeated();
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        if (body != null)
        {
            Vector3 startScale = body.transform.localScale;
            float t = 0f, duration = 0.35f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                body.color = new Color(1f, 0.25f, 0.25f, 1f - k);
                body.transform.localScale = startScale * (1f + 0.4f * k);
                yield return null;
            }
        }
        Destroy(gameObject);
    }
}
