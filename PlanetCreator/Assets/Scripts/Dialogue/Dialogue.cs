using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class Dialogue : MonoBehaviour
{
    [Serializable]
    public struct DialogueEntry
    {
        [TextArea(2, 5)] public string text;
        public AudioClip audioClip;
    }

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator m_animatorDialogue;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private Camera worldCamera;

    [Header("Delay")]
    [SerializeField][Range(0f, 15f)] private float m_startDelay = 5f;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueEntry[] m_textDialogue;

    /// <summary>
    /// Вызывается когда все реплики проиграны.
    /// DialogState подписывается на это событие для перехода в MiniGameState.
    /// </summary>
    public event Action DialogueFinished;

    private Coroutine typingRoutine;
    private Coroutine delayRoutine;
    private int currentIndex;
    private bool isDialogueActive;
    private bool isEntryComplete;

    private void Reset()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        delayRoutine = StartCoroutine(StartWithDelay());
    }

    private void OnDisable()
    {
        StopAllActiveCoroutines();

        if (isDialogueActive)
            CleanUp();
    }

    private void Update()
    {
        if (isDialogueActive && worldCamera != null)
        {
            Quaternion rotation = worldCamera.transform.rotation;
            transform.LookAt(
                transform.position + rotation * Vector3.forward,
                rotation * Vector3.up);
        }
    }

    // ─────────────────── Lifecycle ───────────────────

    private IEnumerator StartWithDelay()
    {
        yield return new WaitForSeconds(m_startDelay);
        delayRoutine = null;

        StartAnimation();
        StartDialogue();
    }

    public void StartDialogue(int startIndex = 0)
    {
        if (m_textDialogue == null || m_textDialogue.Length == 0)
        {
            Debug.LogWarning("[Dialogue] Нет фраз для диалога!");
            DialogueFinished?.Invoke();
            return;
        }

        StopAllActiveCoroutines();

        currentIndex = Mathf.Clamp(startIndex, 0, m_textDialogue.Length - 1);
        isDialogueActive = true;
        isEntryComplete = false;

        ShowCurrentEntry();
    }

    /// <summary>
    /// Привязать к кнопке "Далее" в Inspector:
    /// Button.OnClick → Dialogue.ShowNextEntry
    /// </summary>
    public void ShowNextEntry()
    {
        if (!isDialogueActive) return;

        // Если текст ещё печатается — показать сразу целиком
        if (!isEntryComplete)
        {
            SkipTyping();
            return;
        }

        // Переход к следующей реплике
        currentIndex++;

        if (currentIndex < m_textDialogue.Length)
        {
            m_animatorDialogue.SetBool("endButton", false);
            ShowCurrentEntry();
        }
        else
        {
            // Все реплики проиграны
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        CleanUp();
        DialogueFinished?.Invoke();
    }

    // ─────────────────── Entry display ───────────────────

    private void ShowCurrentEntry()
    {
        if (!isDialogueActive) return;
        if (currentIndex < 0 || currentIndex >= m_textDialogue.Length) return;

        isEntryComplete = false;
        var entry = m_textDialogue[currentIndex];

        if (displayText != null)
            displayText.text = "";

        // Запускаем аудио
        if (audioSource != null && entry.audioClip != null)
        {
            audioSource.clip = entry.audioClip;
            audioSource.Play();
        }

        // Запускаем печатание текста
        typingRoutine = StartCoroutine(TypeText(entry.text));
    }

    private IEnumerator TypeText(string text)
    {
        foreach (char c in text)
        {
            displayText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingRoutine = null;
        OnEntryComplete();
    }

    private void SkipTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        // Показываем весь текст сразу
        if (currentIndex >= 0 && currentIndex < m_textDialogue.Length)
            displayText.text = m_textDialogue[currentIndex].text;

        OnEntryComplete();
    }

    private void OnEntryComplete()
    {
        isEntryComplete = true;

        // Показываем кнопку "далее"
        m_animatorDialogue.SetBool("endButton", true);
    }

    // ─────────────────── Animation ───────────────────

    public void StartAnimation()
    {
        m_animatorDialogue.SetBool("dialogue", true);
    }

    // ─────────────────── Cleanup ───────────────────

    private void CleanUp()
    {
        isDialogueActive = false;
        isEntryComplete = false;

        StopAllActiveCoroutines();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        m_animatorDialogue.SetBool("dialogue", false);
        m_animatorDialogue.SetBool("endButton", false);
    }

    private void StopAllActiveCoroutines()
    {
        if (delayRoutine != null)
        {
            StopCoroutine(delayRoutine);
            delayRoutine = null;
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }
}