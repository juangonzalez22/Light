using UnityEngine;

public class EnemyExploder : BaseEnemy
{
    [Header("Explosión")]
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private float explosionDamage = 50f;

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

    private float countdownTimer;
    private float beepTimer;
    private bool isExploding = false;
    private Renderer cachedRenderer;
    private Material runtimeMaterial;
    private Color baseEmissionColor;

    protected override void Awake()
    {
        base.Awake();

        if (beepAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                beepAudioSource = sources[1];
            }
        }

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
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (target == null) return;

        if (!reachedTarget)
        {
            MoveTowardTarget();

            if (GetDistanceToTarget() <= stopDistance)
            {
                reachedTarget = true;
                StartExplosionCountdown();
            }
        }
        else
        {
            HandleExplosionCountdown();
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

        float progress = Mathf.Clamp01(countdownTimer / countdownDuration);

        if (runtimeMaterial != null && runtimeMaterial.HasProperty("_EmissionColor"))
        {
            float emissionIntensity = Mathf.Lerp(startEmissionIntensity, endEmissionIntensity, progress);
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor("_EmissionColor", baseEmissionColor * emissionIntensity);
        }

        float currentInterval = Mathf.Lerp(beepStartInterval, beepEndInterval, progress);

        if (beepSound != null && beepAudioSource != null && beepTimer >= currentInterval && countdownTimer < countdownDuration)
        {
            beepTimer = 0f;
            beepAudioSource.pitch = Mathf.Lerp(beepStartPitch, beepEndPitch, progress);
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

        if (lighthouseHealth != null && lighthouseHealth.IsAlive)
        {
            lighthouseHealth.TakeDamage(explosionDamage, DamageType.Explosive);
        }

        if (runtimeMaterial != null && runtimeMaterial.HasProperty("_EmissionColor"))
        {
            runtimeMaterial.SetColor("_EmissionColor", baseEmissionColor * startEmissionIntensity);
        }

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        if (audioSource != null && deathSound != null)
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
