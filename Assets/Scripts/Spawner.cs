using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

[System.Serializable]
public class NightConfig
{
    public int nightNumber = 1;
    public float nightLength = 225f;
    
    [Header("Normal enemy spawn")]
    public float initialDelayTime = 20f;
    public float spawnInterval = 18f;
    public int initialEnemyCount = 1;
    public int maxEnemyCount = 4;
    public float difficultyIncreaseInterval = 75f;
    public float minSpawnInterval = 2.5f;
    
    [Header("Strong enemy spawn")]
    public float strongSpawnStartTime = 180f;
    public float strongSpawnInterval = 120f;
    public int maxStrongSpawns = 2;
    
    [Header("Range enemy spawn")]
    public float rangeSpawnStartTime = 300f;
    public float rangeSpawnInterval = 120f;
    public int maxRangeSpawns = 0;

    [Header("Exploded enemy spawn")]
    public float explodedSpawnStartTime = 300f;
    public float explodedSpawnInterval = 120f;
    public int maxExplodedSpawns = 0;
}

public class Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject strongEnemyPrefab;
    [SerializeField] private GameObject rangeEnemyPrefab;
    [SerializeField] private GameObject explodedEnemyPrefab;
    [SerializeField] private GameObject helicopterPrefab;

    [Header("Spawn area")]
    [SerializeField] private float spawnRadius = 20f;

    [Header("Nights Configuration")]
    [SerializeField] private int totalNights = 5;
    [SerializeField] private int startingNightIndex = 1;
    private NightConfig[] nightConfigs;

    [Header("Events")]
    public UnityEvent onNightStart;
    public UnityEvent onNightComplete;

    private NightConfig currentNightConfig;
    private int currentEnemyCount;
    private float currentSpawnInterval;
    private float currentNightLength;
    
    private int spawnedStrongCount = 0;
    private float lastStrongSpawnTime = 0f;
    
    private int spawnedRangeCount = 0;
    private float lastRangeSpawnTime = 0f;

    private int spawnedExplodedCount = 0;
    private float lastExplodedSpawnTime = 0f;

    private float elapsedTime = 0f;
    private float lastSpawnTime = 0f;
    private float lastDifficultyIncreaseTime = 0f;
    private bool hasStartedSpawning = false;
    private bool helicopterSpawnedNight3 = false;

    [Header("Transición entre noches")]
    [SerializeField] private FadeIn fadeController;
    [Tooltip("Nombres de las escenas intermedias entre cada noche. Index = noche-1. Vacío = siguiente escena en build settings.")]
    [SerializeField] private string[] interSceneNames;
    [Tooltip("Tags usados para identificar enemigos en escena.")]
    [SerializeField] private string[] enemyTags = new string[] { "Enemy" };
    [Tooltip("Tiempo máximo en segundos a esperar a que desaparezcan los enemigos. 0 = espera ilimitada.")]
    [SerializeField] private float waitForEnemiesTimeout = 30f;

    private bool nightRunning = false;
    private bool nightCompleted = false;
    private const string PREFS_KEY_CURRENT_NIGHT = "Spawner_CurrentNightIndex";
    private int currentNightIndex = 0;

    private void OnEnable()
    {
        if (nightConfigs == null)
        {
            InitializeNightConfigs();
        }
    }

    private void InitializeNightConfigs()
    {
        nightConfigs = new NightConfig[totalNights];

        nightConfigs[0] = new NightConfig
        {
            nightNumber = 1,
            nightLength = 225f,
            initialDelayTime = 20f,
            spawnInterval = 18f,
            initialEnemyCount = 1,
            maxEnemyCount = 4,
            difficultyIncreaseInterval = 75f,
            minSpawnInterval = 2.5f,
            strongSpawnStartTime = 180f,
            strongSpawnInterval = 120f,
            maxStrongSpawns = 2,
            rangeSpawnStartTime = 300f,
            rangeSpawnInterval = 120f,
            maxRangeSpawns = 0
        };

        nightConfigs[1] = new NightConfig
        {
            nightNumber = 2,
            nightLength = 300f,
            initialDelayTime = 15f,
            spawnInterval = 20f,
            initialEnemyCount = 2,
            maxEnemyCount = 3,
            difficultyIncreaseInterval = 65f,
            minSpawnInterval = 12f,
            strongSpawnStartTime = 90f,
            strongSpawnInterval = 75f,
            maxStrongSpawns = 2,
            rangeSpawnStartTime = 20f,
            rangeSpawnInterval = 80f,
            maxRangeSpawns = -1
        };

        nightConfigs[2] = new NightConfig
        {
            nightNumber = 3,
            nightLength = 330f,
            initialDelayTime = 10f,
            spawnInterval = 18f,
            initialEnemyCount = 2,
            maxEnemyCount = 7,
            difficultyIncreaseInterval = 90f,
            minSpawnInterval = 10f,
            strongSpawnStartTime = 60f,
            strongSpawnInterval = 50f,
            maxStrongSpawns = 4,
            rangeSpawnStartTime = 18f,
            rangeSpawnInterval = 30f,
            maxRangeSpawns = -1,
            explodedSpawnStartTime = 22f,
            explodedSpawnInterval = 35f,
            maxExplodedSpawns = -1
        };

        for (int i = 3; i < totalNights; i++)
        {
            nightConfigs[i] = new NightConfig { nightNumber = i + 1 };
        }
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey(PREFS_KEY_CURRENT_NIGHT))
        {
            int stored = PlayerPrefs.GetInt(PREFS_KEY_CURRENT_NIGHT);
            if (stored >= 1 && stored <= totalNights)
            {
                startingNightIndex = stored;
            }
        }

        StartNight(startingNightIndex);
    }

    private void Update()
    {
        if (!nightRunning || nightCompleted) return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= currentNightLength)
        {
            CompleteNight();
            return;
        }

        if (!hasStartedSpawning && elapsedTime >= currentNightConfig.initialDelayTime)
        {
            hasStartedSpawning = true;
            lastDifficultyIncreaseTime = elapsedTime;
            lastSpawnTime = elapsedTime - currentSpawnInterval;
        }

        if (hasStartedSpawning)
        {
            if (elapsedTime - lastSpawnTime >= currentSpawnInterval)
            {
                SpawnWave();
                lastSpawnTime = elapsedTime;
            }

            if (elapsedTime - lastDifficultyIncreaseTime >= currentNightConfig.difficultyIncreaseInterval)
            {
                IncreaseDifficultyProgressive();
                lastDifficultyIncreaseTime = elapsedTime;
            }
        }

        if (elapsedTime >= currentNightConfig.strongSpawnStartTime && 
            spawnedStrongCount < currentNightConfig.maxStrongSpawns && 
            elapsedTime - lastStrongSpawnTime >= currentNightConfig.strongSpawnInterval)
        {
            SpawnStrongEnemy();
            spawnedStrongCount++;
            lastStrongSpawnTime = elapsedTime;
        }

        if (currentNightConfig.nightNumber == 3 && !helicopterSpawnedNight3 && elapsedTime >= 130f)
        {
            SpawnHelicopter();
            helicopterSpawnedNight3 = true;
        }

        if (currentNightConfig.maxRangeSpawns > 0 &&
            elapsedTime >= currentNightConfig.rangeSpawnStartTime && 
            spawnedRangeCount < currentNightConfig.maxRangeSpawns && 
            elapsedTime - lastRangeSpawnTime >= currentNightConfig.rangeSpawnInterval)
        {
            SpawnRangeEnemy();
            spawnedRangeCount++;
            lastRangeSpawnTime = elapsedTime;
        }
        else if (currentNightConfig.maxRangeSpawns < 0 &&
                 elapsedTime >= currentNightConfig.rangeSpawnStartTime && 
                 elapsedTime - lastRangeSpawnTime >= currentNightConfig.rangeSpawnInterval)
        {
            SpawnRangeEnemy();
            lastRangeSpawnTime = elapsedTime;
        }

        if (currentNightConfig.maxExplodedSpawns > 0 &&
            elapsedTime >= currentNightConfig.explodedSpawnStartTime &&
            spawnedExplodedCount < currentNightConfig.maxExplodedSpawns &&
            elapsedTime - lastExplodedSpawnTime >= currentNightConfig.explodedSpawnInterval)
        {
            SpawnExplodedEnemy();
            spawnedExplodedCount++;
            lastExplodedSpawnTime = elapsedTime;
        }
        else if (currentNightConfig.maxExplodedSpawns < 0 &&
                 elapsedTime >= currentNightConfig.explodedSpawnStartTime &&
                 elapsedTime - lastExplodedSpawnTime >= currentNightConfig.explodedSpawnInterval)
        {
            SpawnExplodedEnemy();
            lastExplodedSpawnTime = elapsedTime;
        }
    }

    private void SpawnWave()
    {
        for (int i = 0; i < currentEnemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        if (!nightRunning || nightCompleted) return;

        float angle;
        if (Random.value > 0.5f)
            angle = Random.Range(10f, 80f);    
        else
            angle = Random.Range(190f, 260f);

        float rad = angle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

        Vector3 spawnPos = transform.position + dir * spawnRadius;
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    private void SpawnStrongEnemy()
    {
        if (!nightRunning || nightCompleted) return;

        if (strongEnemyPrefab == null)
        {
            Debug.LogWarning("Strong enemy prefab no asignado.");
            return;
        }

        float angle = Random.Range(45f, 315f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        Vector3 spawnPos = transform.position + dir * (spawnRadius * 0.9f);

        GameObject inst = Instantiate(strongEnemyPrefab, spawnPos, Quaternion.identity);
        inst.transform.localScale *= 1.25f;
    }

    private void SpawnRangeEnemy()
    {
        if (!nightRunning || nightCompleted) return;

        if (rangeEnemyPrefab == null)
        {
            Debug.LogWarning("Range enemy prefab no asignado.");
            return;
        }

        float angle;
        if (Random.value > 0.5f)
            angle = Random.Range(10f, 80f);
        else
            angle = Random.Range(190f, 260f);

        float rad = angle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        Vector3 spawnPos = transform.position + dir * spawnRadius;

        Instantiate(rangeEnemyPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[Spawner] Enemigo Range spawneado a los {elapsedTime:F1}s");
    }

    private void SpawnExplodedEnemy()
    {
        if (!nightRunning || nightCompleted) return;

        if (explodedEnemyPrefab == null)
        {
            Debug.LogWarning("Exploded enemy prefab no asignado.");
            return;
        }

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        Vector3 spawnPos = transform.position + dir * (spawnRadius * 0.8f);

        Instantiate(explodedEnemyPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[Spawner] Enemigo Exploded spawneado a los {elapsedTime:F1}s");
    }

    private void IncreaseDifficultyProgressive()
    {
        currentEnemyCount = Mathf.Min(currentEnemyCount + 1, currentNightConfig.maxEnemyCount);
        currentSpawnInterval = Mathf.Max(currentSpawnInterval * 0.85f, currentNightConfig.minSpawnInterval);
    }

    private void CompleteNight()
    {
        nightCompleted = true;
        nightRunning = false;
        hasStartedSpawning = false;
        Debug.Log($"[Spawner] Noche {currentNightIndex} completada a los {elapsedTime:F1}s");
        StartCoroutine(WaitForEnemiesThenTransition());
    }

    private IEnumerator WaitForEnemiesThenTransition()
    {
        float startTime = Time.time;

        while (true)
        {
            bool anyEnemy = false;
            if (enemyTags != null && enemyTags.Length > 0)
            {
                foreach (var tag in enemyTags)
                {
                    if (string.IsNullOrEmpty(tag))
                        continue;

                    if (GameObject.FindGameObjectsWithTag(tag).Length > 0)
                    {
                        anyEnemy = true;
                        break;
                    }
                }
            }

            if (!anyEnemy)
                break;

            if (waitForEnemiesTimeout > 0f && Time.time - startTime >= waitForEnemiesTimeout)
            {
                Debug.Log("[Spawner] Timeout esperando a que desaparezcan los enemigos.");
                break;
            }

            yield return null;
        }

        onNightComplete?.Invoke();

        if (fadeController == null)
            fadeController = FindObjectOfType<FadeIn>();

        int interIndex = currentNightIndex - 1;

        if (interSceneNames != null && interIndex >= 0 && interIndex < interSceneNames.Length && !string.IsNullOrEmpty(interSceneNames[interIndex]))
        {
            fadeController?.StartFadeOut(interSceneNames[interIndex]);
        }
        else
        {
            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (fadeController != null && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                fadeController.StartFadeOut(nextBuildIndex);
            }
        }

        if (currentNightIndex < totalNights)
        {
            currentNightIndex++;
            Debug.Log($"[Spawner] Se incrementó currentNightIndex a {currentNightIndex}");
        }
        else
        {
            currentNightIndex = totalNights;
        }

        PlayerPrefs.SetInt(PREFS_KEY_CURRENT_NIGHT, currentNightIndex);
        PlayerPrefs.Save();
    }

    public void StartNight(int nightIndex = 1)
    {
        currentNightIndex = nightIndex;
        
        if (nightIndex > 0 && nightIndex <= nightConfigs.Length)
        {
            currentNightConfig = nightConfigs[nightIndex - 1];
        }
        else
        {
            Debug.LogError($"Noche {nightIndex} no configurada.");
            return;
        }

        ResetNightState();
        nightRunning = true;
        onNightStart?.Invoke();
        Debug.Log($"[Spawner] Noche {currentNightIndex} iniciada - Duración: {currentNightLength}s");
    }

    public void StartNextNight()
    {
        if (currentNightIndex < totalNights)
            StartNight(currentNightIndex + 1);
        else
            Debug.Log("No quedan más noches configuradas.");
    }

    public void ResetNightState()
    {
        currentNightLength = currentNightConfig.nightLength;
        currentEnemyCount = currentNightConfig.initialEnemyCount;
        currentSpawnInterval = currentNightConfig.spawnInterval;
        
        elapsedTime = 0f;
        lastSpawnTime = -currentSpawnInterval;
        lastDifficultyIncreaseTime = 0f;
        lastStrongSpawnTime = -currentNightConfig.strongSpawnInterval;
        lastRangeSpawnTime = -currentNightConfig.rangeSpawnInterval;
        
        spawnedStrongCount = 0;
        spawnedRangeCount = 0;
        hasStartedSpawning = false;
        nightCompleted = false;
        helicopterSpawnedNight3 = false;
    }

    public bool IsNightRunning() => nightRunning && !nightCompleted;
    public bool IsNightCompleted() => nightCompleted;

    private void SpawnHelicopter()
    {
        if (helicopterPrefab == null)
        {
            Debug.LogWarning("[Spawner] Prefab del helicóptero no asignado.");
            return;
        }

        Vector3 spawnPos = new Vector3(0f, 30f, 0f);
        Instantiate(helicopterPrefab, spawnPos, Quaternion.identity);
        Debug.Log("[Spawner] Helicóptero spawneado en noche 3 a los 130 segundos.");
    }
}
