using UnityEngine;

public class Enemy : BaseEnemy
{
    [Header("Ataque al faro")]
    [SerializeField] private LighthouseHealth lighthouseHealthRef;
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackInterval = 1f;

    [Header("GameObjects")]
    [SerializeField] private GameObject mesh;

    [Header("Dissolve")]
    [SerializeField] private float dissolveDuration = 2f;

    private Material _dissolveMaterial;
    private float _dissolveTimer = 0f;
    private bool _isDissolving = false;
    private SkinnedMeshRenderer skin;
    private float attackTimer;

    protected override void Awake()
    {
        skin = mesh.GetComponent<SkinnedMeshRenderer>();
        base.Awake();
        _dissolveMaterial = skin.material;
        if (lighthouseHealthRef == null)
            lighthouseHealthRef = lighthouseHealth;
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
    }

    protected override void Update()
    {
        if (_isDissolving)
        {
            _dissolveTimer += Time.deltaTime;
            float amount = Mathf.Clamp01(_dissolveTimer / dissolveDuration);
            _dissolveMaterial.SetFloat("_DissolveAmount", amount);

            if (amount >= 1f)
                Destroy(gameObject);

            return;
        }

        base.Update();

        if (isDead) return;
        if (target == null) return;

        if (!reachedTarget)
        {
            MoveTowardTarget();

            if (GetDistanceToTarget() <= stopDistance)
            {
                reachedTarget = true;
                if (animator != null)
                    animator.SetBool("IsAttack", true);
            }
        }
        else
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;

                if (lighthouseHealthRef != null)
                {
                    lighthouseHealthRef.TakeDamage(attackDamage, DamageType.Melee);
                }
            }
        }
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        GetComponent<Collider>().enabled = false;

        if (audioSource != null && deathSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathSound);
        }

        _isDissolving = true;
    }
}
