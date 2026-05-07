using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    
    public static GameManager Instance { get; private set; }
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,

    }

    private GameState currentState = GameState.MainMenu;
    private int currentLevel = 1;
    private bool isLoadingScene = false;

    [Header("Music")]
    [Tooltip("Played when the player clicks the Start button.")]
    [SerializeField] private AudioClip gameplayMusic;

    // UI elements we'll find dynamically
    private GameObject mainMenuUI;
    private GameObject pauseMenuUI;
    private GameObject gameOverUI;



    void Awake()
    {
        // Singleton implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene load events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Clean up event subscription
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset loading flag
        isLoadingScene = false;

        // Find UI elements in the new scene
        FindUIElements();

        // Set initial state based on scene
        if (scene.buildIndex == 0) // Main menu scene
        {
            SetGameState(GameState.MainMenu);
        }
        else // Game level scene
        {
            SetGameState(GameState.Playing);
        }
    }

    void FindUIElements()
    {
        // Find UI elements by tag (you need to tag them in your scenes)
        mainMenuUI = GameObject.FindGameObjectWithTag("MainMenuUI");
        pauseMenuUI = GameObject.FindGameObjectWithTag("PauseMenuUI");
        gameOverUI = GameObject.FindGameObjectWithTag("GameOverUI");
        // Set up button listeners
        SetupButtonListeners();
    }

    void SetupButtonListeners()
    {
        // Main Menu
        if (mainMenuUI != null)
        {
            Button startButton = mainMenuUI.transform.Find("StartButton").GetComponent<Button>();
            Button quitButton = mainMenuUI.transform.Find("QuitButton").GetComponent<Button>();
            
            if (startButton) startButton.onClick.AddListener(StartGame);
            if (quitButton) quitButton.onClick.AddListener(QuitGame);
        }
        
        // Pause Menu
        if (pauseMenuUI != null)
        {
            Button resumeButton = pauseMenuUI.transform.Find("ResumeButton").GetComponent<Button>();
            Button restartButton = pauseMenuUI.transform.Find("RestartButton").GetComponent<Button>();
            Button mainMenuButton = pauseMenuUI.transform.Find("MainMenuButton").GetComponent<Button>();
            
            if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton) restartButton.onClick.AddListener(RestartLevel);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
        
        // Game Over
        if (gameOverUI != null)
        {
            Button gameOverRestartButton = gameOverUI.transform.Find("RestartButton").GetComponent<Button>();
            Button gameOverMainMenuButton = gameOverUI.transform.Find("MainMenuButton").GetComponent<Button>();
            
            if (gameOverRestartButton) gameOverRestartButton.onClick.AddListener(RestartLevel);
            if (gameOverMainMenuButton) gameOverMainMenuButton.onClick.AddListener(LoadMainMenu);
        }
        
    }

    void Update()
    {


        // Handle pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    public void SetGameState(GameState newState)
    {
        currentState = newState;
        if (currentState == GameState.Playing)
        {
            Time.timeScale = 1f; // Game continues
        }
        else
        {
            Time.timeScale = 0f; // Game paused or stopped
        }

        // Update UI visibility
        SetUIVisibility();

        // Handle state-specific logic
        UpdateCursorState();
    }

    void SetUIVisibility()
    {
        if (mainMenuUI) mainMenuUI.SetActive(currentState == GameState.MainMenu);
        if (pauseMenuUI) pauseMenuUI.SetActive(currentState == GameState.Paused);
        if (gameOverUI) gameOverUI.SetActive(currentState == GameState.GameOver);
       
    }

    void UpdateCursorState()
    {
        switch (currentState)
        {
            case GameState.MainMenu:
            case GameState.Paused:
            case GameState.GameOver:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
                
            case GameState.Playing:
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }

    public void StartGame()
    {
        if (isLoadingScene) return;
        isLoadingScene = true;

        if (gameplayMusic != null && SoundManager.Instance != null)
            SoundManager.Instance.PlayMusic(gameplayMusic);

        SceneManager.LoadScene(currentLevel);
    }

    public void PauseGame()
    {
        SetGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        SetGameState(GameState.Playing);
    }

    public void RestartLevel()
    {
        if (isLoadingScene) return;
        isLoadingScene = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GameOver()
    {
        SetGameState(GameState.GameOver);
    }
    public void LoadMainMenu()
    {
        if (isLoadingScene) return;
        isLoadingScene = true;
        currentLevel = 1;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}