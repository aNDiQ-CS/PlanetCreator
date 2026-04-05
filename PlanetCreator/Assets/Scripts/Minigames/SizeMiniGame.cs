using System;
using System.Collections;
using Infrastructure;
using Planets;
using UnityEngine;
using UnityEngine.UI;

public class SizeMiniGame : MonoBehaviour, IMiniGame
{
    [Header("UI")]
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_smallButton;
    [SerializeField] private Button m_bigButton;

    [Header("Planet")]
    [SerializeField] private Transform m_planetTransform;
    [SerializeField] private float m_smallScale = 0.5f;
    [SerializeField] private float m_bigScale = 1.5f;
    [SerializeField] private float m_animDuration = 1f;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 1.5f;

    public event Action MiniGameCompleted;
    private bool m_isActive;

    private void Awake()
    {
        m_smallButton.onClick.AddListener(OnSmallPressed);
        m_bigButton.onClick.AddListener(OnBigPressed);
    }

    public void StartGame()
    {
        m_isActive = true;
        m_panel.SetActive(true);
    }

    public void StopGame()
    {
        m_isActive = false;
        m_panel.SetActive(false);
    }

    private void OnSmallPressed()
    {
        if (!m_isActive) return;
        Confirm(Size.Small, m_smallScale);
    }

    private void OnBigPressed()
    {
        if (!m_isActive) return;
        Confirm(Size.Big, m_bigScale);
    }

    private void Confirm(Size size, float targetScale)
    {
        m_isActive = false;

        var data = PlanetBuildData.Instance;
        if (data != null)
            data.size = size;

        m_panel.SetActive(false);
        StartCoroutine(AnimateAndComplete(targetScale));
    }

    private IEnumerator AnimateAndComplete(float targetScale)
    {
        float startScale = m_planetTransform.localScale.x;
        float elapsed = 0f;

        while (elapsed < m_animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / m_animDuration));
            float scale = Mathf.Lerp(startScale, targetScale, t);
            m_planetTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        m_planetTransform.localScale = Vector3.one * targetScale;

        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }
}