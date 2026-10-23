using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Manager for KALKI: NEON DHARMA.
/// Manages round state, player/Maya death conditions, and victory/defeat logic.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        GameOver,
        Victory
    }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("UI Panels (Optional)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Restart Settings")]
    [Tooltip("Allow pressing 'R' to reload the match.")]
    [SerializeField] private bool allowRestartKey = true;

    public GameState State => currentState;
    public bool IsGameOver => currentState != GameState.Playing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        currentState = GameState.Playing;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (allowRestartKey && Input.GetKeyDown(KeyCode.R))
        {
            RestartBattle();
        }
    }

    /// <summary>
    /// Invoked by Health.cs when any character's health drops to 0.
    /// </summary>
    public void OnCharacterDeath(GameObject deadCharacter)
    {
        if (currentState != GameState.Playing) return;

        if (deadCharacter.CompareTag("Player"))
        {
            TriggerGameOver();
        }
        else if (deadCharacter.CompareTag("MAYA") || deadCharacter.name.ToLower().Contains("maya") || deadCharacter.CompareTag("Enemy"))
        {
            TriggerVictory();
        }
        else
        {
            Debug.Log($"[GameManager] Entity '{deadCharacter.name}' perished.");
        }
    }

    public void TriggerGameOver()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.GameOver;
        Debug.Log("[GameManager] KALKI HAS FALLEN — GAME OVER! Press 'R' to try again.");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void TriggerVictory()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.Victory;
        Debug.Log("[GameManager] MAYA DEFEATED — VICTORY! DHARMA HAS BEEN RESTORED! Press 'R' to replay.");

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }

    public void RestartBattle()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
