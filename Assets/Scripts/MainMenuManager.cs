using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject blackoutPanel;

    [Header("Audio Settings")]
    public AudioSource bgmSource;       // El AudioSource que reproduce la música de fondo
    public AudioSource sfxSource;       // El AudioSource para los efectos de sonido
    public AudioClip transitionSound;   // El archivo de audio de la transición

    void Start()
    {
        // Al iniciar, el panel pasa de negro a transparente (alpha 0)
        LeanTween.alpha(blackoutPanel.GetComponent<RectTransform>(), 0f, 1f).setOnComplete(() =>
        {
            blackoutPanel.SetActive(false);
        });
    }

    /// <summary>
    /// Este es el método que debes llamar desde tu botón de "Jugar" o "Start"
    /// </summary>
    public void GoToScene(string sceneName)
    {
        // Iniciamos la corrutina que maneja toda la secuencia
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. Reactivamos el panel para poder verlo oscurecerse
        blackoutPanel.SetActive(true);

        // 2. Calculamos la duración basados en lo que dura el sonido de transición
        // Si no hay sonido asignado, por defecto durará 1 segundo.
        float duration = (transitionSound != null) ? transitionSound.length : 1f;

        // 3. Reproducimos el sonido de transición
        if (sfxSource != null && transitionSound != null)
        {
            sfxSource.PlayOneShot(transitionSound);
        }

        // 4. Oscurecemos el panel gradualmente (llevamos el alpha a 1) en el tiempo que dura el sonido
        LeanTween.alpha(blackoutPanel.GetComponent<RectTransform>(), 1f, duration);

        // 5. Bajamos el volumen de la música de fondo a 0 suavemente usando LeanTween.value
        if (bgmSource != null)
        {
            LeanTween.value(gameObject, bgmSource.volume, 0f, duration).setOnUpdate((float vol) =>
            {
                bgmSource.volume = vol;
            });
        }

        // 6. Esperamos a que termine el tiempo de la transición (lo que dura el sonido)
        yield return new WaitForSeconds(duration);

        // 7. Finalmente, cargamos la nueva escena
        SceneManager.LoadScene(sceneName);
    }
}