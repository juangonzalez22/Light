using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum DamageType
{
    Melee,
    Ranged,
    Explosive
}

public class LighthouseHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    [Header("Shake - Melee")]
    [SerializeField] private Shake objectToShake;
    [SerializeField] private float meleeShakeDuration = 0.2f;
    [SerializeField] private float meleeShakeIntensity = 0.3f;

    [Header("Shake - Ranged")]
    [SerializeField] private float rangedShakeDuration = 0.12f;
    [SerializeField] private float rangedShakeIntensity = 0.15f;

    [Header("Shake - Explosive")]
    [SerializeField] private float explosiveShakeDuration = 0.6f;
    [SerializeField] private float explosiveShakeIntensity = 1.0f;

    [Header("Audio - Melee")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip meleeDamageSound;
    [SerializeField] private float meleeMinPitch = 0.9f;
    [SerializeField] private float meleeMaxPitch = 1.1f;

    [Header("Audio - Ranged")]
    [SerializeField] private AudioClip rangedDamageSound;
    [SerializeField] private float rangedMinPitch = 1.0f;
    [SerializeField] private float rangedMaxPitch = 1.2f;

    [Header("Audio - Explosive")]
    [SerializeField] private AudioClip explosiveDamageSound;
    [SerializeField] private float explosiveMinPitch = 0.95f;
    [SerializeField] private float explosiveMaxPitch = 1.05f;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    [Header("Fade")]
    [SerializeField] private FadeIn fadeController;

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

    public void TakeDamage(float amount, DamageType damageType)
    {
        if (!isAlive)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateSlider();

        ApplyDamageEffects(damageType);

        if (showDebugMessages)
        {
            Debug.Log(
                $"Lighthouse recibió {amount:F1} de daño ({damageType}). Vida actual: {currentHealth:F1}/{maxHealth}"
            );
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void ApplyDamageEffects(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Melee:

                if (objectToShake != null)
                {
                    objectToShake.TriggerShake(
                        meleeShakeDuration,
                        meleeShakeIntensity
                    );
                }

                if (audioSource != null && meleeDamageSound != null)
                {
                    audioSource.pitch = Random.Range(
                        meleeMinPitch,
                        meleeMaxPitch
                    );

                    audioSource.PlayOneShot(meleeDamageSound);
                }

                break;

            case DamageType.Ranged:

                if (objectToShake != null)
                {
                    objectToShake.TriggerShake(
                        rangedShakeDuration,
                        rangedShakeIntensity
                    );
                }

                if (audioSource != null && rangedDamageSound != null)
                {
                    audioSource.pitch = Random.Range(
                        rangedMinPitch,
                        rangedMaxPitch
                    );

                    audioSource.PlayOneShot(rangedDamageSound);
                }

                break;

            case DamageType.Explosive:

                if (objectToShake != null)
                {
                    objectToShake.TriggerShake(
                        explosiveShakeDuration,
                        explosiveShakeIntensity
                    );
                }

                if (audioSource != null && explosiveDamageSound != null)
                {
                    audioSource.pitch = Random.Range(
                        explosiveMinPitch,
                        explosiveMaxPitch
                    );

                    audioSource.PlayOneShot(explosiveDamageSound);
                }

                break;
        }
    }

    public void Heal(float amount)
    {
        if (!isAlive)
            return;

        currentHealth = Mathf.Min(
            currentHealth + amount,
            maxHealth
        );

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

        if (fadeController == null)
            fadeController = FindObjectOfType<FadeIn>();

        if (fadeController != null)
        {
            fadeController.StartFadeOutRestart();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}