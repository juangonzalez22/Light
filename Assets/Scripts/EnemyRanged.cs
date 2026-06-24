using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float attackRange = 8f;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Daño recibido por la DamageZone")]
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 10f;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private float maxDamageDistance = 10f;

    [Header("Ataque a distancia")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 1f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileHitDistance = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 2.0f;

    [Header("Sonido de pasos")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.5f;
    [SerializeField] private float footstepMinPitch = 0.85f;
    [SerializeField] private float footstepMaxPitch = 1.15f;
    [SerializeField] private float footstepMinVolume = 0.05f;
    [SerializeField] private float footstepMaxVolume = 1f;

    [Header("Estado")]
    [SerializeField] private bool reachedTarget;
    [SerializeField] private bool insideDamageZone;
    [SerializeField] private float damageTimer;
    [SerializeField] private float attackTimer;

    private bool isDead = false;
    private float footstepTimer = 0f;
    private LightPivotController lightPivot;
    private LighthouseHealth lighthouseHealth;
    private MeshRenderer meshRenderer;
    private Collider cachedCollider;

    private bool CanTakeDamage =>
        lightPivot == null || (!lightPivot.IsRecharging && !lightPivot.IsCameraTransitioning);

    private void Awake()
    {
        currentHealth = maxHealth;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        meshRenderer = GetComponent<MeshRenderer>();
        cachedCollider = GetComponent<Collider>();

        lightPivot = FindObjectOfType<LightPivotController>();
        lighthouseHealth = FindObjectOfType<LighthouseHealth>();

        if (lighthouseHealth != null)
        {
            target = lighthouseHealth.transform;
        }
        else
        {
            Debug.LogWarning("EnemyRanged: no se encontró ningún objeto con LighthouseHealth en la escena.");
        }

        if (firePoint == null)
            firePoint = transform;
    }

    private void Update()
    {
        if (isDead) return;

        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        if (target == null)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > attackRange)
        {
            reachedTarget = false;

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

            HandleFootsteps();
        }
        else
        {
            if (!reachedTarget)
            {
                reachedTarget = true;
                Debug.Log("EnemyRanged entró en rango de disparo.");
            }

            attackTimer += Time.deltaTime;

            if (attackTimer >= shootInterval)
            {
                attackTimer = 0f;
                Shoot();
            }
        }

        if (insideDamageZone && CanTakeDamage)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageInterval)
            {
                damageTimer = 0f;

                float distance = Vector3.Distance(transform.position, target.position);
                float factor = 1f - Mathf.Clamp01(distance / maxDamageDistance);
                float damage = Mathf.Lerp(minDamage, maxDamage, factor);

                TakeDamage(damage, factor);
            }
        }
    }

    private void HandleFootsteps()
    {
        if (audioSource == null || footstepSound == null) return;

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            footstepTimer = 0f;

            float distance = target != null ? Vector3.Distance(transform.position, target.position) : 0f;
            float t = 1f - Mathf.Clamp01(distance / maxDamageDistance);
            float volume = Mathf.Lerp(footstepMinVolume, footstepMaxVolume, t);

            audioSource.pitch = Random.Range(footstepMinPitch, footstepMaxPitch);
            audioSource.PlayOneShot(footstepSound, volume);
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("EnemyRanged: no hay Projectile asignado.");
            return;
        }

        if (lighthouseHealth == null)
            return;

        Projectile projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        projectile.Initialize(
            lighthouseHealth,
            projectileDamage,
            projectileSpeed,
            projectileHitDistance
        );

        if (audioSource != null && shootSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(shootSound);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive) return;

        if (other.CompareTag("DamageZone"))
            insideDamageZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead) return;
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive) return;

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

    public void TakeDamage(float amount, float distanceFactor = 0.5f)
    {
        if (isDead) return;
        if (!CanTakeDamage) return;
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            if (audioSource != null && hitSound != null)
            {
                audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, distanceFactor);
                audioSource.PlayOneShot(hitSound);
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("EnemyRanged destruido.");

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