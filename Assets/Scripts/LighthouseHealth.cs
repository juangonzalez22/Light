using UnityEngine;
using UnityEngine.UI;

public class LighthouseHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    [Header("Efectos Visuales")]
    [SerializeField] private Shake objectToShake;
    [SerializeField] private float damageShakeDuration = 0.2f;
    [SerializeField] private float damageShakeIntensity = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private float minPitch = 0.9f; // Tono mínimo (más grave)
    [SerializeField] private float maxPitch = 1.1f; // Tono máximo (más agudo)

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    [SerializeField] private float currentHealth;
    [SerializeField] private bool isAlive = true;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsAlive => isAlive;

    private void Awake()
    {
        currentHealth = maxHealth;
        isAlive = true;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (showDebugMessages)
        {
            Debug.Log($"LighthouseHealth iniciada: {currentHealth}/{maxHealth}");
        }
    }

    public void TakeDamage(float amount)
    {
        if (!isAlive)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateSlider();

        // --- EFECTOS VISUALES Y SONOROS ---
        
        if (objectToShake != null)
        {
            objectToShake.TriggerShake(damageShakeDuration, damageShakeIntensity);
        }

        if (audioSource != null && damageSound != null)
        {
            // Cambiamos el pitch aleatoriamente entre los valores definidos
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(damageSound); 
        }

        if (showDebugMessages)
        {
            Debug.Log(
                $"Lighthouse recibió {amount:F1} de daño. Vida actual: {currentHealth:F1}/{maxHealth}"
            );
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (!isAlive)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        UpdateSlider();

        if (showDebugMessages)
        {
            Debug.Log(
                $"Lighthouse curado {amount:F1}. Vida actual: {currentHealth:F1}/{maxHealth}"
            );
        }
    }

    private void UpdateSlider()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    private void Die()
    {
        if (!isAlive)
            return;

        isAlive = false;
        currentHealth = 0f;

        UpdateSlider();

        if (showDebugMessages)
        {
            Debug.Log("GAME OVER");
        }
    }
}