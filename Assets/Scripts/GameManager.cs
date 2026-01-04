using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public EmotionController emotion;
    public LogicController logic;

    public GameObject emotionArrow;
    public GameObject logicArrow;

    public Transform emotionSpawn;
    public Transform logicSpawn;

    public int maxDeathsBeforeRestart = 3;
    public int totalDeaths;

    public int score = 0;
    public int emotionSwapCost = 10;

    [SerializeField] private bool emotionActive = true;

    private bool emotionInPortal;
    private bool logicInPortal;

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
        totalDeaths++;

        if (totalDeaths >= maxDeathsBeforeRestart)
        {
            RestartLevel();
            return;
        }

        if (who == "Emotion") RespawnEmotion();
        else if (who == "Logic") RespawnLogic();
        else RespawnBothToSpawns();
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

        totalDeaths = 0;
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

    private void OnLevelCompleted()
    {
    }
}
