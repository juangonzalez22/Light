using UnityEngine;

public class EnemyRanged : BaseEnemy
{
    [Header("Ataque a distancia")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 1f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileHitDistance = 0.2f;

    [Header("Audio de disparo")]
    [SerializeField] private AudioClip shootSound;

    [Header("Rango de ataque")]
    [SerializeField] private float attackRange = 8f;

    private float attackTimer;

    protected override void Awake()
    {
        base.Awake();
        if (firePoint == null)
            firePoint = transform;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;
        if (target == null) return;

        float distanceToTarget = GetDistanceToTarget();

        if (distanceToTarget > attackRange)
        {
            reachedTarget = false;
            animator.SetBool("IsThrow", false);

            MoveTowardTarget();
        }
        else
        {
            if (!reachedTarget)
            {
                reachedTarget = true;
                animator.SetBool("IsThrow", true);
            }

            attackTimer += Time.deltaTime;

            if (attackTimer >= shootInterval)
            {
                attackTimer = 0f;
                Shoot();
            }
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
}
