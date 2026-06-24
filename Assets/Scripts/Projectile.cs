using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float hitDistance = 0.2f;
    [SerializeField] private float lifeTime = 10f;

    private LighthouseHealth target;
    private float damage;
    private float lifeTimer = 0f;

    public void Initialize(
        LighthouseHealth lighthouse,
        float projectileDamage,
        float projectileSpeed,
        float projectileHitDistance = 0.2f)
    {
        target = lighthouse;
        damage = projectileDamage;
        speed = projectileSpeed;
        hitDistance = projectileHitDistance;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.transform.position,
            speed * Time.deltaTime
        );

        transform.LookAt(target.transform);

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance <= hitDistance)
        {
            target.TakeDamage(damage, DamageType.Ranged);
            Destroy(gameObject);
        }
    }
}