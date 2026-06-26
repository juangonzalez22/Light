using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeforeLevels : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private GameObject continueHint;

    [Header("Story")]
    [TextArea(2, 6)]
    [SerializeField] private List<string> storyLines = new();

    [Header("Next Scene")]
    [SerializeField] private string nextScene;

    [Header("Typewriter")]
    [SerializeField] private float characterDelay = 0.03f;
    [SerializeField] private float hintDelay = 8f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private int currentLine;
    private float timer;

    private bool isTyping;
    private Coroutine typingRoutine;

    private void Start()
    {
        if (storyLines.Count == 0)
        {
            Debug.LogError("No story lines assigned.");
            return;
        }

        StartTyping();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!isTyping && timer >= hintDelay)
            continueHint.SetActive(true);

        if (Input.GetMouseButtonDown(0))
        {
            PlayClick();

            if (isTyping)
            {
                FinishTyping();
            }
            else
            {
                NextLine();
            }
        }
    }

    private void StartTyping()
    {
        timer = 0f;
        continueHint.SetActive(false);

        typingRoutine = StartCoroutine(TypeLine(storyLines[currentLine]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        storyText.text = "";

        foreach (char c in line)
        {
            storyText.text += c;
            yield return new WaitForSeconds(characterDelay);
        }

        isTyping = false;
    }

    private void FinishTyping()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        storyText.text = storyLines[currentLine];
        isTyping = false;
    }

    private void NextLine()
    {
        currentLine++;

        if (currentLine >= storyLines.Count)
        {
            SceneManager.LoadScene(nextScene);
            return;
        }

        StartTyping();
    }

    private void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
}