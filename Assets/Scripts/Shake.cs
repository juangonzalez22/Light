using System.Collections;
using UnityEngine;

public class Shake : MonoBehaviour
{
    // Método público que llamaremos desde LighthouseHealth
    public void TriggerShake(float duration, float intensity)
    {
        // Detenemos cualquier shake anterior para evitar comportamientos extraños si recibe daño muy rápido
        StopAllCoroutines(); 
        StartCoroutine(Shaking(duration, intensity));
    }

    private IEnumerator Shaking(float duration, float intensity)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = startPosition + (Random.insideUnitSphere * intensity);
            yield return null;
        }

        transform.position = startPosition;
    }
}