using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu References")]
    public Button startButton;
    public Button quitButton;

    [Header("Pause Menu References")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Game Over References")]
    public Button gameOverRestartButton;
    public Button gameOverMainMenuButton;

  
    void Start()
    {
        // Main Menu
        if (startButton) startButton.onClick.AddListener(GameManager.Instance.StartGame);
        if (quitButton) quitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        
        // Pause Menu
        if (resumeButton) resumeButton.onClick.AddListener(GameManager.Instance.ResumeGame);
        if (restartButton) restartButton.onClick.AddListener(GameManager.Instance.RestartLevel);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(GameManager.Instance.LoadMainMenu);
        
        // Game Over
        if (gameOverRestartButton) gameOverRestartButton.onClick.AddListener(GameManager.Instance.RestartLevel);
        if (gameOverMainMenuButton) gameOverMainMenuButton.onClick.AddListener(GameManager.Instance.LoadMainMenu);
        
    
    }
}