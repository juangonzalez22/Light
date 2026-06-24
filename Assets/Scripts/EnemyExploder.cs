using UnityEngine;

public class EnemyExploder : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float stopDistance = 0.25f;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 80f;
    [SerializeField] private float currentHealth;

    [Header("Daño recibido por la DamageZone")]
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 10f;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private float maxDamageDistance = 10f;

    [Header("Explosión")]
    [SerializeField] private LighthouseHealth lighthouseHealth;
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private float explosionDamage = 50f;

    [Header("Audio Principal")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 2.0f;

    [Header("Audio Bips")]
    [SerializeField] private AudioSource beepAudioSource;
    [SerializeField] private AudioClip beepSound;
    [SerializeField] private float beepStartPitch = 0.8f;
    [SerializeField] private float beepEndPitch = 1.8f;
    [SerializeField] private float beepStartInterval = 1f;
    [SerializeField] private float beepEndInterval = 0.15f;

    [Header("Emisión")]
    [SerializeField] private float startEmissionIntensity = 7f;
    [SerializeField] private float endEmissionIntensity = 10f;

    [Header("Estado")]
    [SerializeField] private bool reachedTarget;
    [SerializeField] private bool insideDamageZone;
    [SerializeField] private float damageTimer;
    [SerializeField] private float countdownTimer;
    [SerializeField] private float beepTimer;

    private bool isDead = false;
    private bool isExploding = false;

    private LightPivotController lightPivot;
    private MeshRenderer meshRenderer;
    private Collider cachedCollider;
    private Renderer cachedRenderer;

    private Material runtimeMaterial;
    private Color baseEmissionColor;

    private bool CanTakeDamage =>
        lightPivot == null ||
        (!lightPivot.IsRecharging && !lightPivot.IsCameraTransitioning);

    private void Awake()
    {
        currentHealth = maxHealth;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (beepAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                beepAudioSource = sources[1];
            }
        }

        meshRenderer = GetComponent<MeshRenderer>();
        cachedCollider = GetComponent<Collider>();
        cachedRenderer = GetComponent<Renderer>();

        if (cachedRenderer != null)
        {
            runtimeMaterial = cachedRenderer.material;

            if (runtimeMaterial != null && runtimeMaterial.HasProperty("_EmissionColor"))
            {
                baseEmissionColor = runtimeMaterial.GetColor("_EmissionColor");
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.SetColor("_EmissionColor", baseEmissionColor * startEmissionIntensity);
            }
        }

        lightPivot = FindObjectOfType<LightPivotController>();

        if (lighthouseHealth == null)
        {
            lighthouseHealth = FindObjectOfType<LighthouseHealth>();
        }

        if (lighthouseHealth != null)
        {
            target = lighthouseHealth.transform;
        }
        else
        {
            Debug.LogWarning(
                "EnemyExploder: no se encontró ningún objeto con LighthouseHealth en la escena."
            );
        }
    }

    private void Update()
    {
        if (isDead)
            return;

        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        if (target == null)
            return;

        if (!reachedTarget)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

            if (
                Vector3.Distance(
                    transform.position,
                    target.position
                ) <= stopDistance
            )
            {
                reachedTarget = true;
                StartExplosionCountdown();

                Debug.Log("EnemyExploder llegó al objetivo.");
            }
        }
        else
        {
            HandleExplosionCountdown();
        }

        if (insideDamageZone && CanTakeDamage)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageInterval)
            {
                damageTimer = 0f;

                float distance =
                    Vector3.Distance(
                        transform.position,
                        target.position
                    );

                float factor =
                    1f - Mathf.Clamp01(
                        distance / maxDamageDistance
                    );

                float damage =
                    Mathf.Lerp(
                        minDamage,
                        maxDamage,
                        factor
                    );

                TakeDamage(
                    damage,
                    factor
                );
            }
        }
    }

    private void StartExplosionCountdown()
    {
        if (isExploding)
            return;

        isExploding = true;
        countdownTimer = 0f;
        beepTimer = 0f;
    }

    private void HandleExplosionCountdown()
    {
        if (!isExploding)
            return;

        countdownTimer += Time.deltaTime;
        beepTimer += Time.deltaTime;

        float progress =
            Mathf.Clamp01(
                countdownTimer / countdownDuration
            );

        if (
            runtimeMaterial != null &&
            runtimeMaterial.HasProperty("_EmissionColor")
        )
        {
            float emissionIntensity =
                Mathf.Lerp(
                    startEmissionIntensity,
                    endEmissionIntensity,
                    progress
                );

            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor(
                "_EmissionColor",
                baseEmissionColor * emissionIntensity
            );
        }

        float currentInterval =
            Mathf.Lerp(
                beepStartInterval,
                beepEndInterval,
                progress
            );

        if (
            beepSound != null &&
            beepAudioSource != null &&
            beepTimer >= currentInterval &&
            countdownTimer < countdownDuration
        )
        {
            beepTimer = 0f;

            beepAudioSource.pitch =
                Mathf.Lerp(
                    beepStartPitch,
                    beepEndPitch,
                    progress
                );

            beepAudioSource.PlayOneShot(beepSound);
        }

        if (countdownTimer >= countdownDuration)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (isDead)
            return;

        isDead = true;
        isExploding = false;

        Debug.Log("EnemyExploder explotó.");

        if (
            lighthouseHealth != null &&
            lighthouseHealth.IsAlive
        )
        {
            lighthouseHealth.TakeDamage(
                explosionDamage,
                DamageType.Explosive
            );
        }

        if (
            runtimeMaterial != null &&
            runtimeMaterial.HasProperty("_EmissionColor")
        )
        {
            runtimeMaterial.SetColor(
                "_EmissionColor",
                baseEmissionColor * startEmissionIntensity
            );
        }

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        if (
            audioSource != null &&
            explosionSound != null
        )
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(explosionSound);
            Destroy(gameObject, explosionSound.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (
            lighthouseHealth != null &&
            !lighthouseHealth.IsAlive
        )
            return;

        if (other.CompareTag("DamageZone"))
        {
            insideDamageZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead)
            return;

        if (
            lighthouseHealth != null &&
            !lighthouseHealth.IsAlive
        )
            return;

        if (other.CompareTag("DamageZone"))
        {
            insideDamageZone = false;
            damageTimer = 0f;
        }
    }

    public void ForceExitDamageZone()
    {
        insideDamageZone = false;
        damageTimer = 0f;
    }

    public void TakeDamage(
        float amount,
        float distanceFactor = 0.5f
    )
    {
        if (isDead)
            return;

        if (!CanTakeDamage)
            return;

        if (
            lighthouseHealth != null &&
            !lighthouseHealth.IsAlive
        )
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            if (
                audioSource != null &&
                hitSound != null
            )
            {
                audioSource.pitch =
                    Mathf.Lerp(
                        minPitch,
                        maxPitch,
                        distanceFactor
                    );

                audioSource.PlayOneShot(hitSound);
            }
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isExploding = false;

        Debug.Log("EnemyExploder destruido antes de explotar.");

        if (
            runtimeMaterial != null &&
            runtimeMaterial.HasProperty("_EmissionColor")
        )
        {
            runtimeMaterial.SetColor(
                "_EmissionColor",
                baseEmissionColor * startEmissionIntensity
            );
        }

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        if (
            audioSource != null &&
            deathSound != null
        )
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathSound);
            Destroy(gameObject, deathSound.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}