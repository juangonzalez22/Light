using UnityEngine;
using UnityEngine.InputSystem;

public class LightPivotController : MonoBehaviour
{
    [Header("Rotación horizontal (yaw)")]
    public float mouseSensitivity = 25f;

    [Header("Rango de la luz (rueda del mouse)")]
    public float minRange = 3f;
    public float maxRange = 20f;
    public float startRange = 10f;
    public float scrollSensitivity = 0.02f;

    [Header("Cámara")]
    [Tooltip("Objeto que se moverá entre ambas posiciones. Si tu Camera está dentro de un rig, asigna el rig.")]
    public Transform cameraRig;
    [Tooltip("Punto de referencia para la vista normal del faro.")]
    public Transform normalCameraPoint;
    [Tooltip("Punto de referencia para la vista de recarga.")]
    public Transform rechargeCameraPoint;
    [Tooltip("Duración del movimiento de cámara.")]
    public float cameraTransitionDuration = 1f;

    [Header("Línea visual del rayo")]
    public LineRenderer beamLine;

    [Header("Referencias")]
    public Transform targetMarker;
    public Transform lighthouseBase;
    public float markerGroundHeight = 0.05f;

    [Header("Brillo según distancia")]
    public Light markerSpotlight;
    public float minLightIntensity = 1f;
    public float maxLightIntensity = 8f;

    public Renderer markerRenderer;
    public Color emissionBaseColor = Color.white;
    public float minEmissionIntensity = 1f;
    public float maxEmissionIntensity = 6f;

    [Header("Tamaño del punto de luz según la distancia")]
    public float minMarkerDiameter = 1f;
    public float maxMarkerDiameter = 4f;
    public float markerFlatHeight = 0.1f;

    [Header("Energía")]
    public LighthouseEnergy energy;

    [Header("Vida")]
    public LighthouseHealth lighthouseHealth;

    [Header("Recarga")]
    [SerializeField] private bool isRecharging;

    private bool isCameraTransitioning;
    private bool targetRechargeState;
    private float yaw;
    private float fixedPitch;
    private MaterialPropertyBlock propBlock;
    private MaterialPropertyBlock beamPropBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    public float CurrentRange { get; private set; }
    public bool IsRecharging => isRecharging;
    public bool IsCameraTransitioning => isCameraTransitioning;

    private void Start()
    {
        if (cameraRig == null && Camera.main != null)
        {
            cameraRig = Camera.main.transform.parent != null ? Camera.main.transform.parent : Camera.main.transform;
        }

        yaw = transform.localEulerAngles.y;
        fixedPitch = transform.localEulerAngles.x;
        CurrentRange = startRange;

        if (markerRenderer != null && markerRenderer.sharedMaterial != null && markerRenderer.sharedMaterial.HasProperty(EmissionColorID))
        {
            emissionBaseColor = markerRenderer.sharedMaterial.GetColor(EmissionColorID);
        }

        if (beamLine != null)
        {
            beamPropBlock = new MaterialPropertyBlock();
        }

        if (cameraRig != null)
        {
            if (isRecharging && rechargeCameraPoint != null)
            {
                cameraRig.SetPositionAndRotation(rechargeCameraPoint.position, rechargeCameraPoint.rotation);
            }
            else if (normalCameraPoint != null)
            {
                cameraRig.SetPositionAndRotation(normalCameraPoint.position, normalCameraPoint.rotation);
            }
        }

        if (isRecharging)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetRechargeState(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            RefreshVisuals();
        }
    }

    private void Update()
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
        {
            UpdateVisuals(false);
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryToggleRechargeMode();
        }

        bool canOperate = !isRecharging && !isCameraTransitioning && (energy == null || energy.IsOn);

        if (canOperate)
        {
            HandleRotation();
            HandleRange();
        }

        UpdateVisuals(canOperate);
    }

    public void TryToggleRechargeMode()
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        if (isCameraTransitioning)
            return;

        if (isRecharging)
            ExitRechargeMode();
        else
            EnterRechargeMode();
    }

    public void EnterRechargeMode()
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isCameraTransitioning || isRecharging)
            return;

        BeginCameraTransition(true);
    }

    public void ExitRechargeMode()
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (isCameraTransitioning || !isRecharging)
            return;

        BeginCameraTransition(false);
    }

    private void BeginCameraTransition(bool toRechargeMode)
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        if (cameraRig == null || normalCameraPoint == null || rechargeCameraPoint == null)
            return;

        isCameraTransitioning = true;
        targetRechargeState = toRechargeMode;
        isRecharging = true;

        if (energy != null)
        {
            energy.SetDrainPaused(true);
        }

        UpdateVisuals(false);

        Transform targetPoint = toRechargeMode ? rechargeCameraPoint : normalCameraPoint;

        LeanTween.cancel(cameraRig.gameObject);

        LeanTween.move(cameraRig.gameObject, targetPoint.position, cameraTransitionDuration)
            .setEase(LeanTweenType.easeInOutQuad);

        LeanTween.rotate(cameraRig.gameObject, targetPoint.eulerAngles, cameraTransitionDuration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(OnCameraTransitionComplete);
    }

    private void OnCameraTransitionComplete()
    {
        isCameraTransitioning = false;
        isRecharging = targetRechargeState;

        if (lighthouseHealth == null || lighthouseHealth.IsAlive)
        {
            if (!isRecharging && energy != null)
            {
                energy.SetDrainPaused(false);
            }
        }

        RefreshVisuals();
    }

    private void SetRechargeState(bool active)
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
            return;

        isRecharging = active;

        if (energy != null)
        {
            energy.SetDrainPaused(active);
        }

        RefreshVisuals();
    }

    private void HandleRotation()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();
        float mouseX = delta.x * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        transform.localRotation = Quaternion.Euler(fixedPitch, yaw, 0f);
    }

    private void HandleRange()
    {
        float scrollY = Mouse.current.scroll.ReadValue().y;
        CurrentRange += scrollY * scrollSensitivity;
        CurrentRange = Mathf.Clamp(CurrentRange, minRange, maxRange);
    }

    private void RefreshVisuals()
    {
        bool canShow = lighthouseHealth == null || lighthouseHealth.IsAlive;
        canShow = canShow && !isRecharging && !isCameraTransitioning && (energy == null || energy.IsOn);

        UpdateVisuals(canShow);
    }

    private void UpdateVisuals(bool shouldShow)
    {
        if (targetMarker != null) targetMarker.gameObject.SetActive(shouldShow);
        if (beamLine != null) beamLine.enabled = shouldShow;

        if (!shouldShow || targetMarker == null)
            return;

        Vector3 basePos = lighthouseBase != null
            ? lighthouseBase.position
            : new Vector3(transform.position.x, 0f, transform.position.z);
        basePos.y = 0f;

        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        Vector3 targetPos = basePos + direction * CurrentRange;
        targetPos.y = markerGroundHeight;

        targetMarker.position = targetPos;

        float t = Mathf.InverseLerp(minRange, maxRange, CurrentRange);
        float diameter = Mathf.Lerp(minMarkerDiameter, maxMarkerDiameter, t);
        targetMarker.localScale = new Vector3(diameter, markerFlatHeight, diameter);

        if (markerSpotlight != null)
        {
            markerSpotlight.intensity = Mathf.Lerp(maxLightIntensity, minLightIntensity, t);
        }

        if (markerRenderer != null)
        {
            float emissionIntensity = Mathf.Lerp(maxEmissionIntensity, minEmissionIntensity, t);

            propBlock ??= new MaterialPropertyBlock();
            markerRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorID, emissionBaseColor * emissionIntensity);
            markerRenderer.SetPropertyBlock(propBlock);
        }

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

    private void OnDrawGizmos()
    {
        if (targetMarker == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(transform.position.x, 0f, transform.position.z), targetMarker.position);
    }
}