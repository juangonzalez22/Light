using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class InstructionScenes : MonoBehaviour
{
    [Header("UI")]
    public GameObject blackoutPanel;

    [Header("Settings")]
    public float fadeInTime = 2f;
    public float waitTime = 3f;
    public float fadeOutTime = 2f;
    public string nextScene;

    private bool canContinue = false;
    private bool transitioning = false;

    void Start()
    {
        // Empieza completamente negro
        blackoutPanel.SetActive(true);

        // Negro -> Transparente
        LeanTween.alpha(blackoutPanel.GetComponent<RectTransform>(), 0f, fadeInTime)
            .setOnComplete(() =>
            {
                StartCoroutine(WaitForInput());
            });
    }

    IEnumerator WaitForInput()
    {
        yield return new WaitForSeconds(waitTime);
        canContinue = true;
    }

    void Update()
    {
        if (!canContinue || transitioning)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            transitioning = true;

            // Transparente -> Negro
            LeanTween.alpha(blackoutPanel.GetComponent<RectTransform>(), 1f, fadeOutTime)
                .setOnComplete(() =>
                {
                    SceneManager.LoadScene(nextScene);
                });
        }
    }
}