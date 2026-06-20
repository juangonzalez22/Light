using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el faro:
/// - Yaw (giro horizontal): libre, con el movimiento del mouse en X. Es la única
///   rotación que tiene la cámara/pivot, no hay inclinación vertical (pitch).
/// - Rango: controlado con la rueda del mouse, entre minRange y maxRange.
/// El TargetMarker no usa física ni Raycast: se calcula su posición directamente
/// como un punto en el suelo, a "currentRange" metros de la base del faro,
/// en la dirección hacia donde apunta el pivot (osea, orbita alrededor de la base).
/// Este script va sobre el objeto LightPivot.
/// </summary>
public class LightPivotController : MonoBehaviour
{
    [Header("Rotación horizontal (yaw)")]
    public float mouseSensitivity = 25f;

    [Header("Rango de la luz (rueda del mouse)")]
    public float minRange = 3f;
    public float maxRange = 20f;
    public float startRange = 10f;
    public float scrollSensitivity = 0.02f; // el scroll del nuevo Input System viene en valores grandes (~120 por "click" de rueda)

    [Header("Línea visual del rayo (se ve en el juego, no solo en el editor)")]
    public LineRenderer beamLine;

    [Header("Referencias")]
    [Tooltip("El marcador que va a orbitar en el piso, representando el alcance de la luz.")]
    public Transform targetMarker;
    [Tooltip("La base del faro (normalmente el objeto padre 'Lighthouse'). Se usa su X y Z, ignorando su altura Y.")]
    public Transform lighthouseBase;
    [Tooltip("Altura fija a la que se mantiene el marcador sobre el piso, para evitar z-fighting.")]
    public float markerGroundHeight = 0.05f;

    [Header("Brillo según distancia (cerca = más concentrado = más brillante)")]
    [Tooltip("El Spotlight que acompaña al marcador (hijo del TargetMarker).")]
    public Light markerSpotlight;
    public float minLightIntensity = 1f; // al rango máximo (lejos)
    public float maxLightIntensity = 8f; // al rango mínimo (cerca)

    [Tooltip("El Renderer del marcador, el que tiene el material con emisión.")]
    public Renderer markerRenderer;
    public Color emissionBaseColor = Color.white;
    public float minEmissionIntensity = 1f; // al rango máximo (lejos)
    public float maxEmissionIntensity = 6f; // al rango mínimo (cerca)

    [Header("Tamaño del punto de luz según la distancia")]
    [Tooltip("Diámetro del marcador cuando está al rango mínimo (cerca).")]
    public float minMarkerDiameter = 1f;
    [Tooltip("Diámetro del marcador cuando está al rango máximo (lejos).")]
    public float maxMarkerDiameter = 4f;
    [Tooltip("Qué tan achatado se mantiene el marcador en Y (su 'grosor' como disco).")]
    public float markerFlatHeight = 0.1f;

    [Header("Energía")]
    [Tooltip("Si está apagado (IsOn = false), se congela la rotación/rango y se ocultan los visuales.")]
    public LighthouseEnergy energy;

    private float yaw;
    private float fixedPitch; // la inclinación que el usuario dejó puesta a mano en el editor, no se toca con el mouse
    private MaterialPropertyBlock propBlock; // para tocar la emisión sin crear una instancia nueva de material
    private MaterialPropertyBlock beamPropBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    /// <summary> Rango actual, expuesto para que otros scripts (ej. el de daño) lo lean más adelante. </summary>
    public float CurrentRange { get; private set; }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.localEulerAngles.y;
        fixedPitch = transform.localEulerAngles.x; // se respeta tal cual quedó en el Inspector
        CurrentRange = startRange;

        // Tomamos el color de emisión original del material para no perder el amarillo/blanco/lo que ya tuviera.
        if (markerRenderer != null && markerRenderer.sharedMaterial != null && markerRenderer.sharedMaterial.HasProperty(EmissionColorID))
        {
            emissionBaseColor = markerRenderer.sharedMaterial.GetColor(EmissionColorID);
        }

        if (beamLine != null && beamLine.sharedMaterial != null && beamLine.sharedMaterial.HasProperty(EmissionColorID))
        {
            // Si quieres que la línea conserve su propio color de emisión original,
            // usamos una segunda base interna sin tocar el resto del script.
            beamPropBlock = new MaterialPropertyBlock();
        }
    }

    void Update()
    {
        bool isOn = energy == null || energy.IsOn; // si no hay script de energía asignado, asumimos que siempre está encendido

        if (isOn)
        {
            HandleRotation();
            HandleRange();
        }

        UpdateVisuals(isOn);
    }

    private void HandleRotation()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();
        float mouseX = delta.x * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;

        // Solo cambia el yaw. La inclinación (X) se queda fija en lo que el usuario dejó puesto en el editor.
        transform.localRotation = Quaternion.Euler(fixedPitch, yaw, 0f);
    }

    private void HandleRange()
    {
        float scrollY = Mouse.current.scroll.ReadValue().y;

        CurrentRange += scrollY * scrollSensitivity;
        CurrentRange = Mathf.Clamp(CurrentRange, minRange, maxRange);
    }

    private void UpdateVisuals(bool isOn)
    {
        // Apaga o prende los visuales según si queda energía. Si está apagado,
        // no calculamos nada más: el marcador y la línea simplemente desaparecen.
        if (targetMarker != null) targetMarker.gameObject.SetActive(isOn);
        if (beamLine != null) beamLine.enabled = isOn;

        if (!isOn || targetMarker == null) return;

        Vector3 basePos = lighthouseBase != null
            ? lighthouseBase.position
            : new Vector3(transform.position.x, 0f, transform.position.z);
        basePos.y = 0f; // ignoramos la altura del faro, nos interesa solo su posición en el piso

        // Dirección horizontal hacia la que apunta el pivot (sin componente vertical, ya que no hay pitch)
        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        Vector3 targetPos = basePos + direction * CurrentRange;
        targetPos.y = markerGroundHeight;

        targetMarker.position = targetPos;

        // Mientras más lejos apunta (mayor rango), más grande/ancho se ve el punto de luz
        float t = Mathf.InverseLerp(minRange, maxRange, CurrentRange);
        float diameter = Mathf.Lerp(minMarkerDiameter, maxMarkerDiameter, t);
        targetMarker.localScale = new Vector3(diameter, markerFlatHeight, diameter);

        // Mientras más cerca (t bajo), más intenso. Mientras más lejos (t alto), más tenue.
        if (markerSpotlight != null)
        {
            markerSpotlight.intensity = Mathf.Lerp(maxLightIntensity, minLightIntensity, t);
        }

        // Emisión del marcador: conserva el color original del material y solo cambia la intensidad.
        if (markerRenderer != null)
        {
            float emissionIntensity = Mathf.Lerp(maxEmissionIntensity, minEmissionIntensity, t);

            propBlock ??= new MaterialPropertyBlock();
            markerRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorID, emissionBaseColor * emissionIntensity);
            markerRenderer.SetPropertyBlock(propBlock);
        }

        // Emisión de la línea: usa el mismo principio, pero aplicado al LineRenderer.
        if (beamLine != null)
        {
            float emissionIntensity = Mathf.Lerp(maxEmissionIntensity, minEmissionIntensity, t);

            beamPropBlock ??= new MaterialPropertyBlock();
            beamLine.GetPropertyBlock(beamPropBlock);

            Color beamBaseColor = Color.white;
            if (beamLine.sharedMaterial != null && beamLine.sharedMaterial.HasProperty(EmissionColorID))
            {
                beamBaseColor = beamLine.sharedMaterial.GetColor(EmissionColorID);
            }

            beamPropBlock.SetColor(EmissionColorID, beamBaseColor * emissionIntensity);
            beamLine.SetPropertyBlock(beamPropBlock);

            beamLine.SetPosition(0, basePos);
            beamLine.SetPosition(1, targetPos);
        }
    }

    void OnDrawGizmos()
    {
        if (targetMarker == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(transform.position.x, 0f, transform.position.z), targetMarker.position);
    }
}