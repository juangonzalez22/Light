using UnityEngine;

/// <summary>
/// Maneja la energía del faro. Se va agotando con el tiempo mientras está
/// encendido; al llegar a 0, queda apagado (IsOn = false) hasta que se le
/// sume energía de nuevo.
/// </summary>
public class LighthouseEnergy : MonoBehaviour
{
    [Header("Energía")]
    public float maxEnergy = 100f;

    [Tooltip("Cuánta energía se pierde por segundo mientras el faro está encendido.")]
    public float drainPerSecond = 2f;

    // Campo serializado para el Inspector
    [SerializeField]
    private float currentEnergy;

    // Propiedades públicas que mantienen la compatibilidad con el resto de tu código
    public float CurrentEnergy => currentEnergy;

    /// <summary> True mientras quede energía. Cuando llega a 0, queda en false. </summary>
    public bool IsOn => currentEnergy > 0f;

    /// <summary> Valor de 0 a 1, listo para conectar directo a un Slider de UI. </summary>
    public float EnergyNormalized => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;

    void Awake()
    {
        // Si quieres que empiece lleno por defecto, puedes dejarlo así.
        // Si prefieres que el Inspector dicte el valor inicial, puedes comentar esta línea.
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        // 1. Primero procesamos los inputs (así funcionan incluso si el faro está apagado)
        if (Input.GetKeyDown(KeyCode.K))
        {
            AddEnergy(3f);
        }

        // 2. Después, solo si está encendido, drenamos la energía
        if (!IsOn) return;

        currentEnergy -= drainPerSecond * Time.deltaTime;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
    }

    /// <summary>
    /// Para más adelante: cuando tengan el generador de manivela en el piso de abajo,
    /// este método es el que le suma energía de vuelta al faro.
    /// </summary>
    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
    }
}