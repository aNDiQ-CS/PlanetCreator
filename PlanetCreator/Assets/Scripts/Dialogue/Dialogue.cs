using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class Dialogue : MonoBehaviour
{
    [Serializable]
    public struct DialogueEntry
    {
        [Tooltip("Название реплики для удобства в Inspector (не влияет на игру)")]
        public string label;
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

    private Canvas m_canvas;
    private Coroutine typingRoutine;
    private Coroutine delayRoutine;
    private int currentIndex;
    private bool isDialogueActive;
    private bool isEntryComplete;

    private void Awake()
    {
        m_canvas = GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        // World Space Canvas требует Event Camera для обработки кликов.
        // Без неё кнопки не работают.
        EnsureEventCamera();

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

    // ─────────────────── Camera fix ───────────────────

    private void EnsureEventCamera()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        // World Space Canvas (renderMode == 2) без камеры не принимает ввод
        if (m_canvas != null
            && m_canvas.renderMode == RenderMode.WorldSpace
            && m_canvas.worldCamera == null)
        {
            m_canvas.worldCamera = worldCamera;
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
    ///
    /// Первое нажатие во время печати — показать текст целиком.
    /// Нажатие после завершения реплики — следующая реплика.
    /// После последней реплики — завершение диалога → переход в мини-игру.
    /// </summary>
    public void ShowNextEntry()
    {
        if (!isDialogueActive) return;

        // Текст ещё печатается — показать сразу целиком
        if (!isEntryComplete)
        {
            SkipTyping();
            return;
        }

        // Следующая реплика
        currentIndex++;

        if (currentIndex < m_textDialogue.Length)
        {
            m_animatorDialogue.SetBool("endButton", false);
            ShowCurrentEntry();
        }
        else
        {
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

        if (audioSource != null)
        {
            if (entry.audioClip != null)
            {
                audioSource.clip = entry.audioClip;
                audioSource.Play();
            }
            else
            {
                // У реплики нет аудио — останавливаем предыдущий клип
                audioSource.Stop();
            }
        }

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

        if (currentIndex >= 0 && currentIndex < m_textDialogue.Length)
            displayText.text = m_textDialogue[currentIndex].text;

        OnEntryComplete();
    }

    private void OnEntryComplete()
    {
        isEntryComplete = true;
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