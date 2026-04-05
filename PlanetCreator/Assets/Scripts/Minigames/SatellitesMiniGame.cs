using System;
using System.Collections;
using System.Collections.Generic;
using Infrastructure;
using Planets;
using UnityEngine;
using UnityEngine.UI;

public class SatellitesMiniGame : MonoBehaviour, IMiniGame
{
    [Header("UI")]
    [SerializeField] private GameObject m_panel;
    [SerializeField] private Button m_oneSatelliteButton;
    [SerializeField] private Button m_threeSatellitesButton;
    [SerializeField] private Button m_ringsButton;
    [SerializeField] private Button m_noneButton;

    [Header("Planet")]
    [SerializeField] private Transform m_planetTransform;

    [Header("Satellite Settings")]
    [SerializeField] private GameObject m_satellitePrefab;
    [SerializeField] private float m_orbitRadius = 3f;
    [SerializeField] private float m_spawnAnimDuration = 0.5f;

    [Header("Rings Settings")]
    [SerializeField] private GameObject m_ringsPrefab;
    [SerializeField] private float m_ringsFadeDuration = 0.8f;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 1.5f;

    public event Action MiniGameCompleted;

    private bool m_isActive;
    private List<GameObject> m_spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        m_oneSatelliteButton.onClick.AddListener(OnOneSatellitePressed);
        m_threeSatellitesButton.onClick.AddListener(OnThreeSatellitesPressed);
        m_ringsButton.onClick.AddListener(OnRingsPressed);
        m_noneButton.onClick.AddListener(OnNonePressed);
    }

    public void StartGame()
    {
        m_isActive = true;
        ClearAll();
        m_panel.SetActive(true);
    }

    public void StopGame()
    {
        m_isActive = false;
        m_panel.SetActive(false);
        ClearAll();
    }

    private void OnOneSatellitePressed()
    {
        if (!m_isActive) return;
        ClearAll();
        SpawnSatellites(1);
        Confirm(SatellitesOrRings.OneSatellite);
    }

    private void OnThreeSatellitesPressed()
    {
        if (!m_isActive) return;
        ClearAll();
        SpawnSatellites(3);
        Confirm(SatellitesOrRings.ThreeSatellites);
    }

    private void OnRingsPressed()
    {
        if (!m_isActive) return;
        ClearAll();
        SpawnRings();
        Confirm(SatellitesOrRings.Rings);
    }

    private void OnNonePressed()
    {
        if (!m_isActive) return;
        ClearAll();
        Confirm(SatellitesOrRings.None);
    }

    private void Confirm(SatellitesOrRings value)
    {
        m_isActive = false;

        var data = PlanetBuildData.Instance;
        if (data != null)
            data.satellites = value;

        m_panel.SetActive(false);
        StartCoroutine(CompleteAfterDelay());
    }

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }

    // ─────────────────── Spawn ───────────────────

    private void SpawnSatellites(int count)
    {
        if (m_satellitePrefab == null || m_planetTransform == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject satellite = Instantiate(m_satellitePrefab, m_planetTransform);

            float angle = i * (360f / count) * Mathf.Deg2Rad;
            satellite.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * m_orbitRadius,
                0f,
                Mathf.Sin(angle) * m_orbitRadius);

            if (satellite.GetComponent<OrbitRotation>() == null)
                satellite.AddComponent<OrbitRotation>();

            // Анимация появления: масштаб 0 → 1 с ease-in-out
            StartCoroutine(AnimateScaleIn(satellite.transform, m_spawnAnimDuration, i * 0.15f));

            m_spawnedObjects.Add(satellite);
        }
    }

    private void SpawnRings()
    {
        if (m_ringsPrefab == null || m_planetTransform == null) return;

        GameObject rings = Instantiate(m_ringsPrefab, m_planetTransform);
        rings.transform.localPosition = Vector3.zero;

        // Анимация появления: прозрачность 0 → 1 с ease-in-out
        StartCoroutine(AnimateFadeIn(rings));

        m_spawnedObjects.Add(rings);
    }

    // ─────────────────── Animations ───────────────────

    private IEnumerator AnimateScaleIn(Transform tr, float duration, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector3 targetScale = tr.localScale;
        tr.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / duration));
            tr.localScale = targetScale * t;
            yield return null;
        }

        tr.localScale = targetScale;
    }

    private IEnumerator AnimateFadeIn(GameObject obj)
    {
        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend == null) yield break;

        Material mat = rend.material;
        Color color = mat.color;
        color.a = 0f;
        mat.color = color;

        float elapsed = 0f;

        while (elapsed < m_ringsFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / m_ringsFadeDuration));
            color.a = t;
            mat.color = color;
            yield return null;
        }

        color.a = 1f;
        mat.color = color;
    }

    private void ClearAll()
    {
        StopAllCoroutines();

        foreach (var obj in m_spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        m_spawnedObjects.Clear();
    }
}