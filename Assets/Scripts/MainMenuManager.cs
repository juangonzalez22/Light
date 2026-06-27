using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject blackoutPanel;

    [Header("Audio Settings")]
    public AudioSource bgmSource;       // Música de fondo
    public AudioSource sfxSource;       // Efectos de sonido
    public AudioClip transitionSound;   // Sonido de transición

    private void Start()
    {
        // Al iniciar, el panel pasa de negro a transparente
        LeanTween.alpha(blackoutPanel.GetComponent<RectTransform>(), 0f, 1f).setOnComplete(() =>
        {
            blackoutPanel.SetActive(false);
        });
    }

    /// <summary>
    /// Llamar desde el botón "Play".
    /// </summary>
    public void GoToScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    /// <summary>
    /// Llamar desde el botón "Reset your game".
    /// Borra todos los PlayerPrefs y recarga la escena actual.
    /// </summary>
    public void ResetGame()
    {
        StartCoroutine(TransitionRoutine(SceneManager.GetActiveScene().name, true));
    }

    /// <summary>
    /// Maneja la transición entre escenas.
    /// </summary>
    private IEnumerator TransitionRoutine(string sceneName, bool resetGame = false)
    {
        // Activamos el panel negro
        blackoutPanel.SetActive(true);

        // Duración de la transición
        float duration = (transitionSound != null) ? transitionSound.length : 1f;

        // Reproducimos el sonido
        if (sfxSource != null && transitionSound != null)
        {
            sfxSource.PlayOneShot(transitionSound);
        }

        // Fade a negro
        LeanTween.alpha(blackoutPanel.GetComponent<RectTransform>(), 1f, duration);

        // Fade de la música
        if (bgmSource != null)
        {
            LeanTween.value(gameObject, bgmSource.volume, 0f, duration)
                .setOnUpdate((float volume) =>
                {
                    bgmSource.volume = volume;
                });
        }

        // Esperamos a que termine la transición
        yield return new WaitForSeconds(duration);

        // Si es un reinicio, borramos toda la partida
        if (resetGame)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("Todos los PlayerPrefs han sido eliminados.");
        }

        // Cargamos la escena
        SceneManager.LoadScene(sceneName);
    }
}