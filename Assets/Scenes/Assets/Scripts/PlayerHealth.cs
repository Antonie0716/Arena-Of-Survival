using System.Collections;
using UnityEngine;

/// <summary>
/// Sistema de vidas: 3 vidas, 1 s de invulnerabilidade (piscando) após cada dano.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;
    public float invulnerabilityTime = 1f;
    public float knockbackForce = 7f;
    public SpriteRenderer body;

    public int CurrentLives { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public bool IsDead { get; private set; }

    PlayerController controller;

    void Awake()
    {
        CurrentLives = maxLives;
        controller = GetComponent<PlayerController>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>Aplica dano. Retorna true se o dano foi realmente aplicado.</summary>
    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        GameManager gm = GameManager.Instance;
        if (IsDead || IsInvulnerable || amount <= 0) return false;
        if (gm != null && !gm.IsPlaying) return false;

        CurrentLives = Mathf.Max(0, CurrentLives - amount);
        AudioManager.Play(Sfx.PlayerHurt);
        if (gm != null) gm.OnPlayerLivesChanged(CurrentLives, maxLives);

        if (CurrentLives <= 0)
        {
            IsDead = true;
            if (body != null) body.color = new Color(1f, 0.35f, 0.35f, 1f);
            if (gm != null) gm.OnPlayerDied();
            return true;
        }

        if (controller != null)
            controller.ApplyKnockback((Vector2)transform.position - sourcePosition, knockbackForce, 0.15f);

        StartCoroutine(InvulnerabilityRoutine());
        return true;
    }

    IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;
        float t = 0f;
        while (t < invulnerabilityTime)
        {
            if (body != null)
            {
                Color c = t < 0.15f ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
                c.a = (Mathf.FloorToInt(t * 12f) % 2 == 0) ? 1f : 0.35f;
                body.color = c;
            }
            t += Time.deltaTime;
            yield return null;
        }
        if (body != null && !IsDead) body.color = Color.white;
        IsInvulnerable = false;
    }
}
