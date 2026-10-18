using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, Won, Lost }

/// <summary>
/// Controla o fluxo da partida: contagem de inimigos, chave, portão, pausa, vitória e derrota.
/// Reiniciar recarrega a cena, restaurando vidas, inimigos, chave e portão.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referências")]
    public UIManager ui;
    public PlayerController player;
    public PlayerHealth playerHealth;
    public KeyItem key;
    public ExitGate gate;

    [Header("Regras")]
    public int totalEnemies = 3;
    public string mainMenuScene = "MainMenu";

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;
    public int EnemiesDefeated { get; private set; }
    public bool HasKey { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (key != null) key.gameObject.SetActive(false);

        if (ui != null)
        {
            ui.SetEnemies(0, totalEnemies);
            if (playerHealth != null) ui.SetLives(playerHealth.CurrentLives, playerHealth.maxLives);
            ui.SetHasKey(false);
            ui.ShowMessage("DERROTE OS INIMIGOS!", 3f);
            ui.QueueMessage("ESPAÇO para atacar  •  ESC para pausar", 3f);
        }
    }

    void Update()
    {
        if (GameInput.PausePressed)
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        if (ui != null) ui.ShowPause(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        if (ui != null) ui.ShowPause(false);
    }

    public void OnEnemyDefeated()
    {
        if (State == GameState.Won || State == GameState.Lost) return;

        EnemiesDefeated = Mathf.Min(EnemiesDefeated + 1, totalEnemies);
        if (ui != null) ui.SetEnemies(EnemiesDefeated, totalEnemies);

        if (EnemiesDefeated >= totalEnemies)
        {
            AllEnemiesDefeated();
        }
        else if (ui != null)
        {
            int left = totalEnemies - EnemiesDefeated;
            ui.ShowMessage(left == 1 ? "INIMIGO DERROTADO! FALTA 1!" : "INIMIGO DERROTADO! FALTAM " + left + "!", 2f);
        }
    }

    void AllEnemiesDefeated()
    {
        if (key != null) key.Spawn();
        AudioManager.Play(Sfx.KeyAppear);
        if (ui != null)
        {
            ui.ShowMessage("TODOS OS INIMIGOS DERROTADOS! ENCONTRE A CHAVE!", 4f);
            ui.QueueMessage("ENCONTRE A CHAVE!", 3f);
        }
    }

    public void OnKeyCollected()
    {
        if (HasKey) return;
        HasKey = true;
        AudioManager.Play(Sfx.KeyCollect);
        if (gate != null) gate.SetUnlockable();
        if (ui != null)
        {
            ui.SetHasKey(true);
            ui.ShowMessage("CHAVE COLETADA!", 2.5f);
            ui.QueueMessage("ESCAPE PELO PORTÃO!", 4f);
        }
    }

    public void OnPlayerLivesChanged(int current, int max)
    {
        if (ui != null) ui.SetLives(current, max);
    }

    public void OnPlayerDied()
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        State = GameState.Lost;
        if (player != null) player.Stop();
        StartCoroutine(EndRoutine(false));
    }

    public void OnGateReached()
    {
        if (State != GameState.Playing || !HasKey) return;
        State = GameState.Won;
        if (player != null) player.Stop();
        if (gate != null) gate.Open();
        AudioManager.Play(Sfx.GateOpen);
        if (ui != null) ui.ShowMessage("PORTÃO DESBLOQUEADO!", 1f);
        StartCoroutine(EndRoutine(true));
    }

    IEnumerator EndRoutine(bool victory)
    {
        yield return new WaitForSecondsRealtime(victory ? 0.9f : 0.8f);
        AudioManager.Play(victory ? Sfx.Victory : Sfx.Defeat);
        Time.timeScale = 0f;
        if (ui != null)
        {
            if (victory) ui.ShowVictory();
            else ui.ShowGameOver();
        }
    }

    public void ShowMessage(string text, float duration)
    {
        if (ui != null) ui.ShowMessage(text, duration);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
