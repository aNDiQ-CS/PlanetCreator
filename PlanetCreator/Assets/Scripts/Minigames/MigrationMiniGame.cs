using System;
using System.Collections;
using Infrastructure;
using Planets;
using UnityEngine;
using UnityEngine.UI;

public class MigrationMiniGame : MonoBehaviour, IMiniGame
{
    [Header("UI")]
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_migrateButton;
    [SerializeField] private Button m_declineButton;

    [Header("Effect")]
    [SerializeField] private GameObject m_migrationEffect;
    [SerializeField] private float m_effectFadeDuration = 0.8f;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 1.5f;

    public event Action MiniGameCompleted;
    private bool m_isActive;

    private void Awake()
    {
        m_migrateButton.onClick.AddListener(OnMigratePressed);
        m_declineButton.onClick.AddListener(OnDeclinePressed);
    }

    public void StartGame()
    {
        m_isActive = true;

        if (m_migrationEffect != null)
            m_migrationEffect.SetActive(false);

        m_panel.SetActive(true);
    }

    public void StopGame()
    {
        m_isActive = false;
        m_panel.SetActive(false);
    }

    private void OnMigratePressed()
    {
        if (!m_isActive) return;
        m_isActive = false;

        var data = PlanetBuildData.Instance;
        if (data != null)
            data.migration = Migration.Yes;

        m_panel.SetActive(false);
        StartCoroutine(ShowEffectAndComplete());
    }

    private void OnDeclinePressed()
    {
        if (!m_isActive) return;
        m_isActive = false;

        var data = PlanetBuildData.Instance;
        if (data != null)
            data.migration = Migration.No;

        m_panel.SetActive(false);
        StartCoroutine(CompleteAfterDelay());
    }

    private IEnumerator ShowEffectAndComplete()
    {
        if (m_migrationEffect != null)
        {
            m_migrationEffect.SetActive(true);

            // Плавное появление через scale
            Transform tr = m_migrationEffect.transform;
            Vector3 targetScale = tr.localScale;
            tr.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < m_effectFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.InOut(Mathf.Clamp01(elapsed / m_effectFadeDuration));
                tr.localScale = targetScale * t;
                yield return null;
            }

            tr.localScale = targetScale;
        }

        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }
}
