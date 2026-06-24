using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float spawnInterval = 2f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0f;
        dir.Normalize();

        Vector3 spawnPos = transform.position + dir * spawnRadius;

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}