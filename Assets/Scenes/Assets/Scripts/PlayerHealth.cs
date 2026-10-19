using System.Collections;
using UnityEngine;

/// <summary>
/// Controla as vidas do jogador.
/// O jogador começa com 3 vidas e fica
/// temporariamente invulnerável após receber dano.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;

    public float invulnerabilityTime = 1f;

    public float knockbackForce = 7f;

    public SpriteRenderer body;

    public int CurrentLives
    {
        get;
        private set;
    }

    public bool IsInvulnerable
    {
        get;
        private set;
    }

    public bool IsDead
    {
        get;
        private set;
    }

    PlayerController controller;

    void Awake()
    {
        // O jogador começa com a quantidade máxima de vidas.
        CurrentLives = maxLives;

        IsInvulnerable = false;
        IsDead = false;

        controller =
            GetComponent<PlayerController>();

        if (body == null)
        {
            body =
                GetComponentInChildren<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Aplica dano ao jogador.
    /// Retorna true se o dano foi aplicado
    /// e false caso o jogador não possa receber dano.
    /// </summary>
    public bool TakeDamage(
        int amount,
        Vector2 sourcePosition
    )
    {
        GameManager gm =
            GameManager.Instance;

        // Jogador morto não recebe mais dano.
        if (IsDead)
        {
            return false;
        }

        // Não recebe dano durante a invulnerabilidade.
        if (IsInvulnerable)
        {
            return false;
        }

        // Não permite valores de dano inválidos.
        if (amount <= 0)
        {
            return false;
        }

        // Só recebe dano enquanto a partida estiver acontecendo.
        if (gm != null)
        {
            if (!gm.IsPlaying)
            {
                return false;
            }
        }

        // Subtrai o dano das vidas.
        CurrentLives =
            CurrentLives - amount;

        // Impede que o número de vidas fique negativo.
        if (CurrentLives < 0)
        {
            CurrentLives = 0;
        }

        AudioManager.Play(
            Sfx.PlayerHurt
        );

        // Atualiza as vidas mostradas na interface.
        if (gm != null)
        {
            gm.OnPlayerLivesChanged(
                CurrentLives,
                maxLives
            );
        }

        // Verifica se o jogador morreu.
        if (CurrentLives <= 0)
        {
            IsDead = true;

            if (body != null)
            {
                body.color =
                    new Color(
                        1f,
                        0.35f,
                        0.35f,
                        1f
                    );
            }

            if (gm != null)
            {
                gm.OnPlayerDied();
            }

            return true;
        }

        // Empurra o jogador para longe do inimigo.
        if (controller != null)
        {
            Vector2 direction =
                (Vector2)transform.position -
                sourcePosition;

            controller.ApplyKnockback(
                direction,
                knockbackForce,
                0.15f
            );
        }

        // Inicia o período de invulnerabilidade.
        StartCoroutine(
            InvulnerabilityRoutine()
        );

        return true;
    }

    /// <summary>
    /// Controla o período de invulnerabilidade
    /// e o efeito visual piscando.
    /// </summary>
    IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;

        float time = 0f;

        // Continua enquanto o tempo atual
        // for menor que o tempo de invulnerabilidade.
        while (time < invulnerabilityTime)
        {
            if (body != null)
            {
                Color currentColor;

                // No início do dano,
                // o jogador fica vermelho.
                if (time < 0.15f)
                {
                    currentColor =
                        new Color(
                            1f,
                            0.3f,
                            0.3f,
                            1f
                        );
                }
                else
                {
                    currentColor =
                        Color.white;
                }

                // Calcula se o jogador deve
                // aparecer ou ficar transparente.
                int flash =
                    Mathf.FloorToInt(
                        time * 12f
                    ) % 2;

                if (flash == 0)
                {
                    currentColor.a = 1f;
                }
                else
                {
                    currentColor.a = 0.35f;
                }

                body.color =
                    currentColor;
            }

            // Atualiza o tempo.
            time =
                time + Time.deltaTime;

            yield return null;
        }

        // Volta a cor normal após a invulnerabilidade.
        if (body != null)
        {
            if (!IsDead)
            {
                body.color =
                    Color.white;
            }
        }

        IsInvulnerable = false;
    }
}