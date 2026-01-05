using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public EmotionController emotion;
    public LogicController logic;

    public GameObject emotionArrow;
    public GameObject logicArrow;

    public Transform emotionSpawn;
    public Transform logicSpawn;

    public int score = 0;
    public int emotionSwapCost = 10;

    [SerializeField] private bool emotionActive = true;

    [Header("Lives")]
    public int maxLives = 3;
    [SerializeField] private int lives;

    [Header("Hearts UI (assign 3 Images in order: Heart1, Heart2, Heart3)")]
    public Image[] heartImages;

    private bool emotionInPortal;
    private bool logicInPortal;

    private bool deathLock;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (I == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        lives = Mathf.Max(1, maxLives);
        RefreshHearts();

        emotionActive = true;
        SetActiveCharacter(emotionActive);
        RespawnBothToSpawns();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            emotionActive = !emotionActive;
            SetActiveCharacter(emotionActive);
        }

        if (emotionInPortal && logicInPortal)
            OnLevelCompleted();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (emotion == null) emotion = FindFirstObjectByType<EmotionController>();
        if (logic == null) logic = FindFirstObjectByType<LogicController>();

        emotionInPortal = false;
        logicInPortal = false;

        deathLock = false;

        lives = Mathf.Max(1, maxLives);
        RefreshHearts();

        SetActiveCharacter(emotionActive);
    }

    public void SetActiveCharacter(bool isEmotionActive)
    {
        if (emotion == null || logic == null) return;

        emotionActive = isEmotionActive;

        emotion.SetControllable(emotionActive);
        logic.SetControllable(!emotionActive);

        if (emotionArrow) emotionArrow.SetActive(emotionActive);
        if (logicArrow) logicArrow.SetActive(!emotionActive);
    }

    public bool TrySpendScore(int amount)
    {
        if (score < amount) return false;
        score -= amount;
        return true;
    }

    public void NotifyEnteredPortal(string who)
    {
        if (who == "Emotion") emotionInPortal = true;
        if (who == "Logic") logicInPortal = true;
    }

    public void NotifyExitedPortal(string who)
    {
        if (who == "Emotion") emotionInPortal = false;
        if (who == "Logic") logicInPortal = false;
    }

    public void RegisterDeathAndRespawn(string who)
    {
        if (deathLock) return;
        deathLock = true;

        lives = Mathf.Max(0, lives - 1);
        RefreshHearts();

        if (lives <= 0)
        {
            RestartLevel();
            return;
        }

        if (who == "Emotion") RespawnEmotion();
        else if (who == "Logic") RespawnLogic();
        else RespawnBothToSpawns();

        deathLock = false;
    }

    public void RespawnEmotion()
    {
        if (emotion == null || emotionSpawn == null) return;
        emotion.TeleportTo(emotionSpawn.position);
    }

    public void RespawnLogic()
    {
        if (logic == null || logicSpawn == null) return;
        logic.TeleportTo(logicSpawn.position);
    }

    public void RespawnBothToSpawns()
    {
        RespawnEmotion();
        RespawnLogic();
    }

    public void RestartLevel()
    {
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        lives = Mathf.Max(1, maxLives);
        deathLock = false;
        emotionInPortal = false;
        logicInPortal = false;
        SceneManager.LoadScene(buildIndex);
    }

    public void SwapCharactersPositions()
    {
        if (emotion == null || logic == null) return;

        Vector3 ePos = emotion.transform.position;
        Vector3 lPos = logic.transform.position;

        emotion.TeleportTo(lPos + Vector3.left * 0.1f);
        logic.TeleportTo(ePos + Vector3.right * 0.1f);
    }

    private void RefreshHearts()
    {
        if (heartImages == null || heartImages.Length == 0) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            heartImages[i].enabled = i < lives;
        }
    }

    private void OnLevelCompleted()
    {
    }
}
