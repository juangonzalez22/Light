using UnityEngine;
using UnityEngine.UI;

public class LighthouseEnergy : MonoBehaviour
{
    [Header("Energía")]
    public float maxEnergy = 100f;

    [Tooltip("Cuánta energía se pierde por segundo mientras el faro está encendido.")]
    public float drainPerSecond = 2f;

    [Header("UI")]
    [SerializeField] private Slider energySlider;

    [Header("Estado")]
    [Tooltip("Mientras esto esté activo, no se drena energía.")]
    [SerializeField] private bool drainPaused;

    [SerializeField] private float currentEnergy;

    public float CurrentEnergy => currentEnergy;
    public bool IsOn => currentEnergy > 0f;
    public bool IsDrainPaused => drainPaused;
    public float EnergyNormalized => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;

    private void Awake()
    {
        currentEnergy = maxEnergy;

        if (energySlider != null)
        {
            energySlider.minValue = 0f;
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            AddEnergy(3f);
        }

        if (!IsOn || drainPaused)
            return;

        currentEnergy -= drainPerSecond * Time.deltaTime;
        currentEnergy = Mathf.Max(currentEnergy, 0f);

        UpdateSlider();
    }

    public void SetDrainPaused(bool paused)
    {
        drainPaused = paused;
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        UpdateSlider();
    }

    private void UpdateSlider()
    {
        if (energySlider != null)
            energySlider.value = currentEnergy;
    }
}