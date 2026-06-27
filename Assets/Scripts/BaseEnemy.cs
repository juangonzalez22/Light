using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] protected Transform target;
    [SerializeField] protected float speed = 2f;
    [SerializeField] protected float stopDistance = 0.2f;

    [Header("Vida")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;

    [Header("Daño recibido por la DamageZone")]
    [SerializeField] protected float minDamage = 1f;
    [SerializeField] protected float maxDamage = 10f;
    [SerializeField] protected float damageInterval = 1f;
    [SerializeField] protected float maxDamageDistance = 10f;

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected float minPitch = 0.8f;
    [SerializeField] protected float maxPitch = 2.0f;

    [Header("Sonido de pasos")]
    [SerializeField] protected AudioClip footstepSound;
    [SerializeField] protected float footstepInterval = 0.5f;
    [SerializeField] protected float footstepMinPitch = 0.85f;
    [SerializeField] protected float footstepMaxPitch = 1.15f;
    [SerializeField] protected float footstepMinVolume = 0.05f;
    [SerializeField] protected float footstepMaxVolume = 1f;

    [Header("Estado")]
    [SerializeField] protected bool reachedTarget;
    [SerializeField] protected bool insideDamageZone;
    [SerializeField] protected float damageTimer;

    protected bool isDead = false;
    protected float footstepTimer = 0f;
    protected LightPivotController lightPivot;
    protected LighthouseHealth lighthouseHealth;
    protected MeshRenderer meshRenderer;
    protected Collider cachedCollider;
    protected Animator animator;

    protected bool CanTakeDamage => lightPivot == null || (!lightPivot.IsRecharging && !lightPivot.IsCameraTransitioning);

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
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
            Debug.LogWarning(GetType().Name + ": no se encontró ningún objeto con LighthouseHealth en la escena.");
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive) return;
        if (target == null) return;

        UpdateRotation();
        UpdateDamageZone();
    }

    protected void UpdateRotation()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    protected void MoveTowardTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
        HandleFootsteps();
    }

    protected float GetDistanceToTarget()
    {
        return Vector3.Distance(transform.position, target.position);
    }

    protected void HandleFootsteps()
    {
        if (audioSource == null || footstepSound == null || target == null) return;

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            footstepTimer = 0f;
            float distance = GetDistanceToTarget();
            float t = 1f - Mathf.Clamp01(distance / maxDamageDistance);
            float volume = Mathf.Lerp(footstepMinVolume, footstepMaxVolume, t);

            audioSource.pitch = Random.Range(footstepMinPitch, footstepMaxPitch);
            audioSource.PlayOneShot(footstepSound, volume);
        }
    }

    protected void UpdateDamageZone()
    {
        if (insideDamageZone && CanTakeDamage)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageInterval)
            {
                damageTimer = 0f;
                float distance = GetDistanceToTarget();
                float factor = 1f - Mathf.Clamp01(distance / maxDamageDistance);
                float damage = Mathf.Lerp(minDamage, maxDamage, factor);
                TakeDamage(damage, factor);
            }
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

    public virtual void TakeDamage(float amount, float distanceFactor = 0.5f)
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
            PlayHitSound(distanceFactor);
        }
    }

    protected void PlayHitSound(float distanceFactor)
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, distanceFactor);
            audioSource.PlayOneShot(hitSound);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        PlayDeathSound();
    }

    protected void PlayDeathSound()
    {
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
