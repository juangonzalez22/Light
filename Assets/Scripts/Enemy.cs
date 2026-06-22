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

    [Header("Estado")]
    [SerializeField] private bool reachedTarget;
    [SerializeField] private bool insideDamageZone;
    [SerializeField] private float damageTimer;
    [SerializeField] private float attackTimer;

    private void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log($"Enemy creado. Vida: {currentHealth}");
    }

    private void Update()
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

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
                Debug.Log("Enemy llegó al objetivo.");
            }
        }
        else
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;

                if (lighthouseHealth != null)
                {
                    Debug.Log($"Enemy atacó al faro por {attackDamage}.");

                    lighthouseHealth.TakeDamage(attackDamage);
                }
                else
                {
                    Debug.LogWarning("Enemy: no hay referencia a LighthouseHealth.");
                }
            }
        }

        if (insideDamageZone)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageInterval)
            {
                damageTimer = 0f;

                float distance = Vector3.Distance(transform.position, target.position);
                float factor = 1f - Mathf.Clamp01(distance / maxDamageDistance);

                float damage = Mathf.Lerp(minDamage, maxDamage, factor);

                Debug.Log(
                    $"Enemy recibe daño: {damage:F2} | Distancia: {distance:F2} | Vida antes: {currentHealth:F2}"
                );

                TakeDamage(damage);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        Debug.Log($"Trigger Enter: {other.name}");

        if (other.CompareTag("DamageZone"))
        {
            insideDamageZone = true;
            Debug.Log("Entró en DamageZone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        Debug.Log($"Trigger Exit: {other.name}");

        if (other.CompareTag("DamageZone"))
        {
            insideDamageZone = false;
            damageTimer = 0f;
            Debug.Log("Salió de DamageZone");
        }
    }

    public void TakeDamage(float amount)
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        currentHealth -= amount;
        Debug.Log($"Vida actual del enemigo: {currentHealth:F2}");

        if (currentHealth <= 0f)
        {
            Debug.Log("Enemy destruido.");
            Destroy(gameObject);
        }
    }
}