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
    [SerializeField] private Camera worldCamera;

    [Header("Delay")]
    [SerializeField][Range(0f, 15f)] private float m_startDelay = 5f;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueEntry[] m_textDialogue;

    public event Action DialogueFinished;

    private Canvas m_canvas;
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

    private void EnsureEventCamera()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (m_canvas != null
            && m_canvas.renderMode == RenderMode.WorldSpace
            && m_canvas.worldCamera == null)
        {
            m_canvas.worldCamera = worldCamera;
        }
    }

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

    public void ShowNextEntry()
    {
        if (!isDialogueActive) return;

        if (!isEntryComplete)
        {
            OnEntryComplete();
            return;
        }

        currentIndex++;

        if (currentIndex < m_textDialogue.Length)
        {
            ShowCurrentEntry();
        }
        else
        {
            EndDialogue();
        }
        UpdateNavButtonsVisibility();
    }

    public void ShowPreviousEntry()
    {
        if (!isDialogueActive) return;
        if (currentIndex <= 0) return;

        currentIndex--;

        var entry = m_textDialogue[currentIndex];
        if (displayText != null)
            displayText.text = entry.text;

        // Останавливаем любое проигрываемое аудио
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        isEntryComplete = true;
        UpdateNavButtonsVisibility();
    }

    public void EndDialogue()
    {
        CleanUp();
        DialogueFinished?.Invoke();
    }

    private void ShowCurrentEntry()
    {
        if (!isDialogueActive) return;
        if (currentIndex < 0 || currentIndex >= m_textDialogue.Length) return;

        isEntryComplete = false;
        var entry = m_textDialogue[currentIndex];

        if (displayText != null)
            displayText.text = entry.text;

        if (audioSource != null)
        {
            if (entry.audioClip != null)
            {
                audioSource.clip = entry.audioClip;
                audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }

        OnEntryComplete();
    }

    private void OnEntryComplete()
    {
        isEntryComplete = true;
    }

    private void UpdateNavButtonsVisibility()
    {
        if(currentIndex >= m_textDialogue.Length)
        {
            m_animatorDialogue.SetBool("nazadButton", false);
        }
        else
        {
            bool showBackButton = currentIndex > 0;
            m_animatorDialogue.SetBool("nazadButton", showBackButton);
        }
    }

    public void StartAnimation()
    {
        m_animatorDialogue.SetBool("dialogue", true);
        m_animatorDialogue.SetBool("endButton", true);
    }

    private void CleanUp()
    {
        isDialogueActive = false;
        isEntryComplete = false;

        StopAllActiveCoroutines();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        m_animatorDialogue.SetBool("nazadButton", false);
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
    }
}