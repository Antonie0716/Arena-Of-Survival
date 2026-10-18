using UnityEngine;

/// <summary>
/// Chave: fica oculta até todos os inimigos morrerem. Ao ser tocada pelo jogador, é coletada.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyItem : MonoBehaviour
{
    public Transform visual;
    public SpriteRenderer glow;
    public float bobAmplitude = 0.12f;
    public float bobSpeed = 3f;

    bool collected;
    Vector3 visualBase;
    Color glowBase = Color.white;

    void Awake()
    {
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(transform.childCount - 1);
        if (visual != null) visualBase = visual.localPosition;
        if (glow != null) glowBase = glow.color;
    }

    public void Spawn()
    {
        collected = false;
        gameObject.SetActive(true);
    }

    void Update()
    {
        float t = Time.time;
        if (visual != null)
            visual.localPosition = visualBase + Vector3.up * Mathf.Sin(t * bobSpeed) * bobAmplitude;
        if (glow != null)
        {
            Color c = glowBase;
            c.a = 0.45f + 0.25f * Mathf.Sin(t * 4f);
            glow.color = c;
            glow.transform.localScale = Vector3.one * (1.6f + 0.15f * Mathf.Sin(t * 4f));
        }
    }

    void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    void OnTriggerStay2D(Collider2D other) { TryCollect(other); }

    void TryCollect(Collider2D other)
    {
        if (collected || other == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        collected = true;
        gm.OnKeyCollected();
        gameObject.SetActive(false);
    }
}
