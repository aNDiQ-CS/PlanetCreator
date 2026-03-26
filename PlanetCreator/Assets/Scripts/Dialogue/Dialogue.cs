using UnityEngine;
using TMPro;              
using System.Collections;

public class Dialogue : MonoBehaviour
{
    [System.Serializable]
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

    [Header("Dialogue Data")]
    [SerializeField] private DialogueEntry[] m_textDialogue;       

    private Coroutine typingRoutine;
    private Coroutine audioWaitRoutine;
    private int currentIndex;
    private bool isDialogueActive;

    private void Reset()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (isDialogueActive && worldCamera != null)
        {
            Quaternion rotation = worldCamera.transform.rotation;
            transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
        }
    }

    public void StartDialogue(int startIndex = 0)
    {
        if (m_textDialogue == null || m_textDialogue.Length == 0)
        {
            Debug.LogWarning("Нет фраз для диалога!");
            return;
        }

        StopAllCoroutines();
        typingRoutine = null;
        audioWaitRoutine = null;

        currentIndex = Mathf.Clamp(startIndex, 0, m_textDialogue.Length - 1);
        isDialogueActive = true;
        ShowCurrentEntry();
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        StopAllCoroutines();

        m_animatorDialogue.SetBool("dialogue", false);
        m_animatorDialogue.SetBool("endButton", false);
    }

    //public void ShowNextDialogue()
    //{
    //    if (!isDialogueActive) return;

    //    currentIndex++;
    //    if (currentIndex < m_textDialogue.Length)
    //    {
    //        ShowCurrentEntry();
    //    }
    //    else
    //    {
    //        EndDialogue();
    //    }
    //}

    private void ShowCurrentEntry()
    {
        if (!isDialogueActive) return;
        if (currentIndex < 0 || currentIndex >= m_textDialogue.Length) return;

        var entry = m_textDialogue[currentIndex];

        if (displayText != null)
            displayText.text = "";

        if (audioSource != null && entry.audioClip != null)
        {
            audioSource.clip = entry.audioClip;
            audioSource.Play();
        }

        typingRoutine = StartCoroutine(TypeText(entry.text));
        audioWaitRoutine = StartCoroutine(WaitForAudio());
    }

    private IEnumerator TypeText(string text)
    {
        foreach (char c in text)
        {
            displayText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private IEnumerator WaitForAudio()
    {
        if (audioSource != null && audioSource.clip != null && audioSource.isPlaying)
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return null;
        }
        // выезд кнопки далее
        m_animatorDialogue.SetBool("endButton", true);
    }

    public void StartAnimation()
    {
        m_animatorDialogue.SetBool("dialogue", true);
    }
}