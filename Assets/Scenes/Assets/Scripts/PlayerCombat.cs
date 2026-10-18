using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque corpo a corpo: Espaço golpeia na direção em que o jogador está olhando.
/// Detecção com Physics2D.OverlapCircleAll, alcance limitado e intervalo de 0,5 s.
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
    float nextAttackTime;
    readonly HashSet<EnemyHealth> hitThisSwing = new HashSet<EnemyHealth>();

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (slashEffect != null) slashEffect.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;
        if (controller.IsStopped) return;

        if (GameInput.AttackPressed && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            Attack();
        }
    }

    void Attack()
    {
        Vector2 dir = controller.Facing;
        Vector2 center = (Vector2)transform.position + dir * attackDistance;

        Collider2D[] cols = Physics2D.OverlapCircleAll(center, attackRadius);
        hitThisSwing.Clear();
        foreach (Collider2D col in cols)
        {
            if (col == null || col.isTrigger) continue;
            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || hitThisSwing.Contains(enemy)) continue;
            hitThisSwing.Add(enemy);
            enemy.TakeDamage(damage, dir);
        }

        AudioManager.Play(Sfx.Attack);
        if (slashEffect != null) StartCoroutine(SlashRoutine(dir));
    }

    IEnumerator SlashRoutine(Vector2 dir)
    {
        Transform t = slashEffect.transform;
        t.localPosition = dir * attackDistance;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        t.localRotation = Quaternion.Euler(0f, 0f, angle);
        slashEffect.enabled = true;
        yield return new WaitForSeconds(slashDuration);
        slashEffect.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 dir = (Application.isPlaying && controller != null) ? controller.Facing : Vector2.down;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + dir * attackDistance, attackRadius);
    }
}
