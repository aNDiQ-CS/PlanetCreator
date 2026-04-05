using System;
using System.Collections;
using Infrastructure;
using Planets;
using UnityEngine;
using UnityEngine.UI;

public class MassMiniGame : MonoBehaviour, IMiniGame
{
    [Header("UI")]
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_lightButton;
    [SerializeField] private Button m_heavyButton;

    [Header("Planet")]
    [SerializeField] private Renderer m_planetRenderer;
    [SerializeField] private Color m_lightColor = Color.gray;
    [SerializeField] private Color m_heavyColor = new Color(0.7f, 0.7f, 0.8f, 1f);
    [SerializeField] private float m_animDuration = 1f;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 1.5f;

    public event Action MiniGameCompleted;
    private bool m_isActive;
    private Material m_material;

    private void Awake()
    {
        m_lightButton.onClick.AddListener(OnLightPressed);
        m_heavyButton.onClick.AddListener(OnHeavyPressed);
    }

    public void StartGame()
    {
        m_isActive = true;

        if (m_planetRenderer != null)
            m_material = m_planetRenderer.material;

        m_panel.SetActive(true);
    }

    public void StopGame()
    {
        m_isActive = false;
        m_panel.SetActive(false);
    }

    private void OnLightPressed()
    {
        if (!m_isActive) return;
        Confirm(Mass.Light, m_lightColor);
    }

    private void OnHeavyPressed()
    {
        if (!m_isActive) return;
        Confirm(Mass.Heavy, m_heavyColor);
    }

    private void Confirm(Mass mass, Color targetColor)
    {
        m_isActive = false;

        var data = PlanetBuildData.Instance;
        if (data != null)
            data.mass = mass;

        m_panel.SetActive(false);
        StartCoroutine(AnimateAndComplete(targetColor));
    }

    private IEnumerator AnimateAndComplete(Color targetColor)
    {
        if (m_material != null)
        {
            Color startColor = m_material.color;
            float elapsed = 0f;

            while (elapsed < m_animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.InOut(Mathf.Clamp01(elapsed / m_animDuration));
                m_material.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            m_material.color = targetColor;
        }

        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }
}