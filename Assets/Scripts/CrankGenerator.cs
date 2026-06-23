using UnityEngine;
using UnityEngine.InputSystem;

public class CrankGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public Camera playerCamera;
    public Collider knobCollider;
    public LightPivotController lightPivot;
    public LighthouseEnergy energy;
    public LighthouseHealth lighthouseHealth;

    [Header("Giro")]
    [Tooltip("1 o -1. Si gira al revés de lo que quieres, cambia este valor.")]
    public int allowedTurnSign = 1;

    [Tooltip("Grados necesarios para dar una media vuelta.")]
    public float degreesPerHalfTurn = 180f;

    [Tooltip("Cuánta energía da cada media vuelta.")]
    public float energyPerHalfTurn = 5f;

    [Tooltip("Si está activo, el objeto gira visualmente junto con el mouse.")]
    public bool rotatePivotVisual = true;

    [Header("Sonidos")]
    [Tooltip("El componente que reproducirá el sonido.")]
    public AudioSource audioSource;
    [Tooltip("El clip de sonido que sonará en cada media vuelta de recarga.")]
    public AudioClip crankSound;

    private bool isHolding;
    private float previousMouseAngle;
    private float accumulatedAcceptedDegrees;
    private Vector2 screenCenter;

    private void Update()
    {
        if (lighthouseHealth != null && !lighthouseHealth.IsAlive)
        {
            if (isHolding)
                Release();

            return;
        }

        if (playerCamera == null || lightPivot == null || Mouse.current == null)
            return;

        bool canUseCrank = lightPivot.IsRecharging && !lightPivot.IsCameraTransitioning;

        if (!canUseCrank)
        {
            if (isHolding)
                Release();

            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryGrab();
        }

        if (isHolding && Mouse.current.leftButton.isPressed)
        {
            UpdateCrankRotation();
        }

        if (isHolding && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Release();
        }
    }

    private void TryGrab()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (knobCollider == null || hit.collider != knobCollider)
            return;

        isHolding = true;
        accumulatedAcceptedDegrees = 0f;

        screenCenter = playerCamera.WorldToScreenPoint(transform.position);
        previousMouseAngle = GetMouseAngle();

        Debug.Log("Knob agarrado");
    }

    private void UpdateCrankRotation()
    {
        float currentMouseAngle = GetMouseAngle();
        float deltaAngle = Mathf.DeltaAngle(previousMouseAngle, currentMouseAngle);
        previousMouseAngle = currentMouseAngle;

        if (Mathf.Approximately(deltaAngle, 0f))
            return;

        if (deltaAngle * allowedTurnSign <= 0f)
            return;

        float acceptedDelta = deltaAngle * allowedTurnSign;

        if (rotatePivotVisual)
        {
            transform.Rotate(0f, 0f, acceptedDelta * allowedTurnSign, Space.Self);
        }

        accumulatedAcceptedDegrees += acceptedDelta;

        while (accumulatedAcceptedDegrees >= degreesPerHalfTurn)
        {
            accumulatedAcceptedDegrees -= degreesPerHalfTurn;

            if (energy != null)
            {
                energy.AddEnergy(energyPerHalfTurn);
            }
            
            // Reproducimos el sonido cada vez que se completa una vuelta/media vuelta válida
            PlayCrankSound();

            Debug.Log("Media vuelta completa -> energía añadida");
        }
    }

    private void Release()
    {
        if (!isHolding)
            return;

        isHolding = false;
        Debug.Log("Knob soltado");
    }

    private float GetMouseAngle()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 direction = mousePos - screenCenter;

        if (direction.sqrMagnitude < 0.001f)
            return previousMouseAngle;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void PlayCrankSound()
    {
        if (audioSource != null && crankSound != null)
        {
            // PlayOneShot permite que el sonido se superponga si giras muy rápido
            audioSource.PlayOneShot(crankSound);
        }
    }
}