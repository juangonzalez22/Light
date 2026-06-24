using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float stopDistance = 0.2f;

    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Daño recibido por la DamageZone")]
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 10f;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private float maxDamageDistance = 10f;

    [Header("Ataque al faro")]
    [SerializeField] private LighthouseHealth lighthouseHealth;
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackInterval = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
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

    [SerializeField] private GameObject mesh;

    private SkinnedMeshRenderer skin;

    private bool isDead = false;
    private float footstepTimer = 0f;
    private LightPivotController lightPivot;

    private bool CanTakeDamage => lightPivot == null || (!lightPivot.IsRecharging && !lightPivot.IsCameraTransitioning);
    private Animator animator;
    private void Awake()
    {
        skin = mesh.GetComponent<SkinnedMeshRenderer>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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
            Debug.LogWarning("Enemy: no se encontró ningún objeto con LighthouseHealth en la escena.");
        }
    }

    private void Start()
    {
        CreateEnemyVariant();
    }

    private void CreateEnemyVariant()
    {
        for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
        {
            skin.SetBlendShapeWeight(i, 0f);
        }

        if (skin == null) return;
        skin.SetBlendShapeWeight(1, Random.Range(0f, 100f));
        skin.SetBlendShapeWeight(2, Random.Range(0f, 100f));
        skin.SetBlendShapeWeight(3, Random.Range(0f, 100f));
        skin.SetBlendShapeWeight(4, Random.Range(0f, 100f));
        skin.SetBlendShapeWeight(5, Random.Range(0f, 100f));

        float randomNumber = Random.Range(0f, 1f);
        if (randomNumber < 0f)
        {
            skin.SetBlendShapeWeight(6, Random.Range(0f, 100f));
        }
        else
        {
            skin.SetBlendShapeWeight(0, Random.Range(0f, 100f));
        }

        Debug.Log("Características"+ "BlendShape 0: " + skin.GetBlendShapeWeight(0)
            + "BlendShape 1: " + skin.GetBlendShapeWeight(1)+
            "BlendShape 2: " + skin.GetBlendShapeWeight(2)+
            "BlendShape 3: " + skin.GetBlendShapeWeight(3)+
            "BlendShape 4: " + skin.GetBlendShapeWeight(4)+
            "BlendShape 5: " + skin.GetBlendShapeWeight(5)+
            "BlendShape 6: " + skin.GetBlendShapeWeight(6));
    }

    private void Update()
    {
        if (isDead) return;

        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        if (target == null)
            return;
        Vector3 direction = target.position - transform.position;
        direction.y = 0f; 
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        if (!reachedTarget)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, target.position) <= stopDistance)
            {
                reachedTarget = true;
                if (animator != null)
                    animator.SetBool("IsAttack", true);
                Debug.Log("Enemy llegó al objetivo.");
            }

            HandleFootsteps();
        }
        else
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;

                if (lighthouseHealth != null)
                {
                    lighthouseHealth.TakeDamage(attackDamage, DamageType.Melee);
                }
                else
                {
                    Debug.LogWarning("Enemy: no hay referencia a LighthouseHealth.");
                }
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
        if (audioSource == null || footstepSound == null || target == null) return;

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            footstepTimer = 0f;

            float distance = Vector3.Distance(transform.position, target.position);

            float t = 1f - Mathf.Clamp01(distance / maxDamageDistance);
            float volume = Mathf.Lerp(footstepMinVolume, footstepMaxVolume, t);

            audioSource.pitch = Random.Range(footstepMinPitch, footstepMaxPitch);
            audioSource.PlayOneShot(footstepSound, volume);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive) return;

        if (other.CompareTag("DamageZone"))
            insideDamageZone = true;
        Debug.Log("Enemigo siendo atacado");
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

        Debug.Log("Enemy destruido.");

        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

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