using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class FadeIn : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float duration = 1f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    IEnumerator Start()
    {
        canvasGroup.alpha = 1f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - t / duration;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void StartFadeOut(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    public void StartFadeOut(int buildIndex)
    {
        StartCoroutine(FadeOutAndLoad(buildIndex));
    }

    public void StartFadeOutRestart()
    {
        StartCoroutine(FadeOutAndRestart());
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = true;

        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOutAndLoad(int buildIndex)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = true;

        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        SceneManager.LoadScene(buildIndex);
    }

    private IEnumerator FadeOutAndRestart()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = true;

        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex);
    }
}
