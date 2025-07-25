using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class GameManager : MonoBehaviour
{
    
    public static GameManager Instance { get; private set; }
    public float remainingTime; // Example timer, adjust as needed
    public float totalTime = 60f; // Total time for the level


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
    
    // UI elements we'll find dynamically
    private GameObject mainMenuUI;
    private GameObject pauseMenuUI;
    private GameObject gameOverUI;
    private TextMeshProUGUI timerText;
    private void OnEnable()
    {
    GameEvents.OnPlayerShot += AddTime;
    GameEvents.OnEnemyKilled += AddTime;
    }

    private void OnDisable()
    {
    GameEvents.OnPlayerShot -= AddTime;
    GameEvents.OnEnemyKilled -= AddTime;
    }

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
        // Initialize timer text if available
        if (scene.buildIndex != 0)
        {
            remainingTime = totalTime; // Reset timer for new level
            UpdateTimerDisplay();
        }
    }

    void FindUIElements()
    {
        // Find UI elements by tag (you need to tag them in your scenes)
        mainMenuUI = GameObject.FindGameObjectWithTag("MainMenuUI");
        pauseMenuUI = GameObject.FindGameObjectWithTag("PauseMenuUI");
        gameOverUI = GameObject.FindGameObjectWithTag("GameOverUI");
       GameObject timerObj = GameObject.FindGameObjectWithTag("TimerText");
        if(timerObj != null)
        {
            timerText = timerObj.GetComponentInChildren<TextMeshProUGUI>();
            
            // Log for debugging
            if(timerText == null)
            {
                Debug.LogError("TimerText object found but has no Text component!");
            }
            else
            {
                Debug.Log("TimerText found and initialized!");
            }
        }
        else
        {
            Debug.LogError("TimerText object not found! Make sure it has 'TimerText' tag.");
        }
        
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
        if (currentState == GameState.Playing && !isLoadingScene)
        {
            // Update timer if in playing state
           remainingTime -= Time.deltaTime;
            UpdateTimerDisplay();
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                GameOver();
            }
           
        }
    }
    void UpdateTimerDisplay()
{
    if(timerText != null)
    {
        // Update the timer text display
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        if (minutes < 0) minutes = 0; // Prevent negative minutes
        if (seconds < 0) seconds = 0; // Prevent negative seconds
       timerText.text = $"{minutes:00}:{seconds:00}";
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
    public void AddTime(float timeToAdd)
    {
        remainingTime += timeToAdd; // Prevent negative time
        UpdateTimerDisplay();
    }

    public void StartGame()
    {
        if (isLoadingScene) return;
        isLoadingScene = true;
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