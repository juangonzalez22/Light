using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRadius = 20f;

    private float initialDelayTime = 20f;
    private float currentSpawnInterval = 18f;
    private float difficultyIncreaseInterval = 75f;
    private int currentEnemyCount = 1;
    private int maxEnemyCount = 4;
    private float minSpawnInterval = 2.5f;

    private float elapsedTime = 0f;
    private float lastSpawnTime = 0f;
    private float lastDifficultyIncreaseTime = 0f;
    private bool hasStartedSpawning = false;

    private void Start()
    {
        lastSpawnTime = -currentSpawnInterval;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (!hasStartedSpawning && elapsedTime >= initialDelayTime)
        {
            hasStartedSpawning = true;
            lastDifficultyIncreaseTime = elapsedTime;
        }

        if (hasStartedSpawning)
        {
            if (elapsedTime - lastSpawnTime >= currentSpawnInterval)
            {
                SpawnWave();
                lastSpawnTime = elapsedTime;
            }

            if (elapsedTime - lastDifficultyIncreaseTime >= difficultyIncreaseInterval)
            {
                IncreaseDifficultyProgressive();
                lastDifficultyIncreaseTime = elapsedTime;
            }
        }
    }

    private void SpawnWave()
    {
        for (int i = 0; i < currentEnemyCount; i++)
        {
            SpawnEnemy();
        }
        Debug.Log($"[Spawner] Wave: {currentEnemyCount} enemigos a los {elapsedTime:F1}s");
    }

    public void SpawnEnemy() 
    {
        float angle = Random.Range(45f, 315f) * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        Vector3 spawnPos = transform.position + dir * spawnRadius;
        Debug.Log(angle);
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    private void IncreaseDifficultyProgressive()
    {
        currentEnemyCount = Mathf.Min(currentEnemyCount + 1, maxEnemyCount);
        currentSpawnInterval = Mathf.Max(currentSpawnInterval * 0.85f, minSpawnInterval);

        Debug.Log($"[Spawner] Dificultad +: {currentEnemyCount} enemigos cada {currentSpawnInterval:F1}s");
    }
}