using System;
using System.Collections;
using Infrastructure;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Мини-игра: объект падает на позицию, появляется кнопка,
/// по нажатию играет анимация и мини-игра завершается.
/// </summary>
public class DropAndAnimateMiniGame : MonoBehaviour, IMiniGame
{
    [Header("Object")]
    [SerializeField] private Transform m_targetObject;
    [SerializeField] private Transform m_dropPosition;
    [SerializeField] private float m_dropDuration = 1f;

    [Header("Animation")]
    [SerializeField] private Animator m_animator;
    [SerializeField] private string m_triggerName = "Play";

    [Header("UI")]
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_playButton;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 1f;

    public event Action MiniGameCompleted;

    private bool m_isActive;
    private Vector3 m_initialObjectPosition;
    private Quaternion m_initialObjectRotation;
    private bool m_initialSaved;

    private void Awake()
    {
        if (m_playButton != null)
            m_playButton.onClick.AddListener(OnPlayPressed);

        // Сохраняем начальную позицию объекта
        if (m_targetObject != null)
        {
            m_initialObjectPosition = m_targetObject.position;
            m_initialObjectRotation = m_targetObject.rotation;
            m_initialSaved = true;
        }
    }

    public void StartGame()
    {
        m_isActive = true;

        // Восстанавливаем объект в начальную позицию (для повторного запуска после StepBack)
        if (m_initialSaved && m_targetObject != null)
        {
            m_targetObject.position = m_initialObjectPosition;
            m_targetObject.rotation = m_initialObjectRotation;
        }

        if (m_panel != null)
            m_panel.SetActive(false);

        // Убеждаемся что кнопка активна
        if (m_playButton != null)
            m_playButton.enabled = true;

        StartCoroutine(DropThenShowButton());
    }

    public void StopGame()
    {
        m_isActive = false;
        StopAllCoroutines();

        if (m_panel != null)
            m_panel.SetActive(false);

        // НЕ вызываем MiniGameCompleted — StopGame используется при cleanup/undo
    }

    /// <summary>
    /// Завершает мини-игру и уведомляет LevelFlowState о завершении.
    /// Вызывается из DropButtonActivation когда игра завершена штатно.
    /// </summary>
    public void CompleteGame()
    {
        m_isActive = false;
        StopAllCoroutines();

        if (m_panel != null)
            m_panel.SetActive(false);

        MiniGameCompleted?.Invoke();
    }

    private IEnumerator DropThenShowButton()
    {
        if (m_targetObject != null && m_dropPosition != null)
        {
            Vector3 startPos = m_targetObject.position;
            Vector3 endPos = m_dropPosition.position;
            float elapsed = 0f;

            while (elapsed < m_dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.InOut(Mathf.Clamp01(elapsed / m_dropDuration));
                m_targetObject.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            m_targetObject.position = endPos;
        }

        if (m_panel != null)
            m_panel.SetActive(true);
    }

    private void OnPlayPressed()
    {
        if (!m_isActive) return;
        m_isActive = false;

        if (m_panel != null)
            m_panel.SetActive(false);

        if (m_animator != null)
            m_animator.SetTrigger(m_triggerName);

        StartCoroutine(WaitForAnimation());
    }

    private IEnumerator WaitForAnimation()
    {
        yield return null;

        if (m_animator != null)
        {
            AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
            float animLength = stateInfo.length;
            yield return new WaitForSeconds(animLength);
        }

        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }
}
