using System;
using System.Collections;
using Infrastructure;
using Planets;
using UnityEngine;
using UnityEngine.UI;

public class RemotenessMiniGame : MonoBehaviour, IMiniGame
{
    [Header("UI")]
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_nearButton;
    [SerializeField] private Button m_mediumButton;
    [SerializeField] private Button m_farButton;

    [Header("Planet")]
    [SerializeField] private Transform m_planetTransform;
    [SerializeField] private float m_moveDuration = 1.5f;

    [Header("Zone Positions")]
    [SerializeField] private Transform m_nearPosition;
    [SerializeField] private Transform m_mediumPosition;
    [SerializeField] private Transform m_farPosition;

    [Header("Scene Objects")]
    [SerializeField] private GameObject m_sunObject;
    [SerializeField] private GameObject m_iceLineObject;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 1.5f;

    public event Action MiniGameCompleted;
    private bool m_isActive;

    private void Awake()
    {
        m_nearButton.onClick.AddListener(OnNearPressed);
        m_mediumButton.onClick.AddListener(OnMediumPressed);
        m_farButton.onClick.AddListener(OnFarPressed);
    }

    public void StartGame()
    {
        m_isActive = true;

        if (m_sunObject != null)
            m_sunObject.SetActive(true);
        if (m_iceLineObject != null)
            m_iceLineObject.SetActive(true);

        m_panel.SetActive(true);
    }

    public void StopGame()
    {
        m_isActive = false;
        m_panel.SetActive(false);
    }

    private void OnNearPressed()
    {
        if (!m_isActive) return;
        Confirm(Remoteness.Near, m_nearPosition.position);
    }

    private void OnMediumPressed()
    {
        if (!m_isActive) return;
        Confirm(Remoteness.Medium, m_mediumPosition.position);
    }

    private void OnFarPressed()
    {
        if (!m_isActive) return;
        Confirm(Remoteness.Far, m_farPosition.position);
    }

    private void Confirm(Remoteness value, Vector3 targetPos)
    {
        m_isActive = false;

        var data = PlanetBuildData.Instance;
        if (data != null)
            data.remoteness = value;

        m_panel.SetActive(false);
        StartCoroutine(AnimateAndComplete(targetPos));
    }

    private IEnumerator AnimateAndComplete(Vector3 targetPos)
    {
        Vector3 startPos = m_planetTransform.position;
        float elapsed = 0f;

        while (elapsed < m_moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / m_moveDuration));
            m_planetTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        m_planetTransform.position = targetPos;

        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }
}