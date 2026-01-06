using System.Collections;
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

    [SerializeField] private bool emotionActive = true;

    public int maxLives = 3;
    [SerializeField] private int lives;
    public Image[] heartImages;

    public float deathCooldown = 0.25f;

    public int emotionSpecialCost = 10;
    public int logicSpecialCost = 10;

    public int initialEmotionPoints = 100;
    public int initialLogicPoints = 100;

    [SerializeField] private int emotionPoints;
    [SerializeField] private int logicPoints;

    [SerializeField] private int emotionSpentThisLevel;
    [SerializeField] private int logicSpentThisLevel;

    private int levelStartEmotionPoints;
    private int levelStartLogicPoints;

    private bool emotionInPortal;
    private bool logicInPortal;

    private bool deathLocked;
    private Coroutine deathUnlockRoutine;

    private bool restartingScene;
    private bool pointsInitialized;

    public int EmotionPoints => emotionPoints;
    public int LogicPoints => logicPoints;
    public int EmotionSpentThisLevel => emotionSpentThisLevel;
    public int LogicSpentThisLevel => logicSpentThisLevel;
    public int Lives => lives;

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
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (I == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (!pointsInitialized)
        {
            emotionPoints = Mathf.Max(0, initialEmotionPoints);
            logicPoints = Mathf.Max(0, initialLogicPoints);
            pointsInitialized = true;
        }

        levelStartEmotionPoints = emotionPoints;
        levelStartLogicPoints = logicPoints;

        emotionSpentThisLevel = 0;
        logicSpentThisLevel = 0;

        lives = Mathf.Max(1, maxLives);

        RebindSceneRefs();
        EnsureHeartsBound();
        RefreshHearts();

        emotionActive = true;
        SetActiveCharacter(emotionActive);
        RespawnBothToSpawns();
    }

    private void Update()
    {
        if (restartingScene) return;

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
        Time.timeScale = 1f;

        if (deathUnlockRoutine != null)
        {
            StopCoroutine(deathUnlockRoutine);
            deathUnlockRoutine = null;
        }

        deathLocked = false;

        RebindSceneRefs();
        EnsureHeartsBound();

        emotionInPortal = false;
        logicInPortal = false;

        if (restartingScene)
        {
            emotionPoints = Mathf.Max(0, levelStartEmotionPoints);
            logicPoints = Mathf.Max(0, levelStartLogicPoints);

            emotionSpentThisLevel = 0;
            logicSpentThisLevel = 0;

            lives = Mathf.Max(1, maxLives);

            emotionActive = true;
            SetActiveCharacter(emotionActive);
            RespawnBothToSpawns();

            restartingScene = false;
        }
        else
        {
            levelStartEmotionPoints = emotionPoints;
            levelStartLogicPoints = logicPoints;

            emotionSpentThisLevel = 0;
            logicSpentThisLevel = 0;

            lives = Mathf.Max(1, maxLives);

            SetActiveCharacter(emotionActive);
        }

        RefreshHearts();
    }

    private void RebindSceneRefs()
    {
        emotion = FindFirstObjectByType<EmotionController>();
        logic = FindFirstObjectByType<LogicController>();

        var f = GameObject.Find("SpawnPointE");
        if (f != null) emotionSpawn = f.transform;

        var l = GameObject.Find("SpawnPointL");
        if (l != null) logicSpawn = l.transform;

        if (emotion != null)
        {
            var glow = emotion.GetComponentInChildren<ActiveArrowGlow>(true);
            if (glow != null) emotionArrow = glow.gameObject;
        }

        if (logic != null)
        {
            var glow = logic.GetComponentInChildren<ActiveArrowGlow>(true);
            if (glow != null) logicArrow = glow.gameObject;
        }
    }

    private void EnsureHeartsBound()
    {
        bool need = heartImages == null || heartImages.Length != 3;
        if (!need)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) { need = true; break; }
            }
        }

        if (!need) return;

        var h1 = GameObject.Find("Heart1")?.GetComponent<Image>();
        var h2 = GameObject.Find("Heart2")?.GetComponent<Image>();
        var h3 = GameObject.Find("Heart3")?.GetComponent<Image>();

        if (h1 != null && h2 != null && h3 != null)
        {
            heartImages = new Image[3] { h1, h2, h3 };
            return;
        }

        var all = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Image[] found = new Image[3];

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;

            string n = all[i].gameObject.name;
            if (n == "Heart1") found[0] = all[i];
            else if (n == "Heart2") found[1] = all[i];
            else if (n == "Heart3") found[2] = all[i];
        }

        if (found[0] != null && found[1] != null && found[2] != null)
            heartImages = found;
    }

    public void SetActiveCharacter(bool isEmotionActive)
    {
        if (emotion == null || logic == null) return;

        emotionActive = isEmotionActive;

        emotion.SetControllable(emotionActive);
        logic.SetControllable(!emotionActive);

        SetArrow(emotionArrow, emotionActive);
        SetArrow(logicArrow, !emotionActive);
    }

    private void SetArrow(GameObject arrow, bool active)
    {
        if (arrow == null) return;

        arrow.SetActive(active);

        var glow = arrow.GetComponent<ActiveArrowGlow>();
        if (glow != null) glow.SetActive(active);
    }

    public bool TrySpendEmotionSpecial()
    {
        return TrySpendPoints(ref emotionPoints, ref emotionSpentThisLevel, emotionSpecialCost);
    }

    public bool TrySpendLogicSpecial()
    {
        return TrySpendPoints(ref logicPoints, ref logicSpentThisLevel, logicSpecialCost);
    }

    private bool TrySpendPoints(ref int points, ref int spent, int cost)
    {
        if (cost <= 0) return true;
        if (points < cost) return false;

        points -= cost;
        spent += cost;

        if (points <= 0)
        {
            RestartLevelToSnapshot();
            return false;
        }

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
        if (restartingScene) return;
        if (deathLocked) return;

        deathLocked = true;
        if (deathUnlockRoutine != null) StopCoroutine(deathUnlockRoutine);
        deathUnlockRoutine = StartCoroutine(UnlockDeathAfterCooldown());

        lives = Mathf.Max(0, lives - 1);

        EnsureHeartsBound();
        RefreshHearts();

        if (lives <= 0)
        {
            RestartLevelToSnapshot();
            return;
        }

        if (who == "Emotion") RespawnEmotion();
        else if (who == "Logic") RespawnLogic();
        else RespawnBothToSpawns();
    }

    private IEnumerator UnlockDeathAfterCooldown()
    {
        float t = Mathf.Max(0.01f, deathCooldown);
        yield return new WaitForSeconds(t);
        deathLocked = false;
    }

    private void RestartLevelToSnapshot()
    {
        restartingScene = true;
        deathLocked = false;

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(buildIndex);
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
        int current = SceneManager.GetActiveScene().buildIndex;

        restartingScene = false;
        deathLocked = false;

        SceneManager.LoadScene(current + 1);
    }
}
