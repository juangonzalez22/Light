using UnityEngine;

public class Helicopter : MonoBehaviour
{
    private Vector3 center = Vector3.zero;
    private float wanderRadius = 6f;
    private float moveLerp = 2.5f;

    private float minHeight = 20f;
    private float maxHeight = 30f;
    private float heightNoiseSpeed = 0.3f;

    private float turnSmooth = 2f;

    public AudioClip helicopterSound;
    private AudioSource audioSource;

    public GameObject bengala;
    public float bengalaDelayInicial = 5f;
    public float bengalaSpawnInterval = 40f;
    private float timeSinceBengalaSpawn = 0f;
    private bool firstBengalaLaunched = false;

    private float seed;

    private void Awake()
    {
        seed = Random.Range(0f, 1000f);
        timeSinceBengalaSpawn = 0f;
        firstBengalaLaunched = false;
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && helicopterSound != null)
        {
            audioSource.clip = helicopterSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void Update()
    {
        float t = Time.time + seed;

        float nx = Mathf.PerlinNoise(t * 0.22f, seed) * 2f - 1f;
        float nz = Mathf.PerlinNoise(seed, t * 0.24f) * 2f - 1f;

        Vector3 targetOffset = new Vector3(nx, 0f, nz) * wanderRadius;
        Vector3 targetPos = center + targetOffset;

        float heightNoise = Mathf.PerlinNoise(t * heightNoiseSpeed, seed + 42.7f);
        targetPos.y = Mathf.Lerp(minHeight, maxHeight, heightNoise);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveLerp);

        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSmooth);
        }

        timeSinceBengalaSpawn += Time.deltaTime;

        if (!firstBengalaLaunched)
        {
            if (timeSinceBengalaSpawn >= bengalaDelayInicial && bengala != null)
            {
                GameObject bengalaInstance = Instantiate(bengala, transform.position, Quaternion.identity);
                bengalaInstance.transform.parent = null;
                firstBengalaLaunched = true;
                timeSinceBengalaSpawn = 0f;
            }
        }
        else
        {
            if (timeSinceBengalaSpawn >= bengalaSpawnInterval && bengala != null)
            {
                GameObject bengalaInstance = Instantiate(bengala, transform.position, Quaternion.identity);
                bengalaInstance.transform.parent = null;
                timeSinceBengalaSpawn = 0f;
            }
        }
    }
}
