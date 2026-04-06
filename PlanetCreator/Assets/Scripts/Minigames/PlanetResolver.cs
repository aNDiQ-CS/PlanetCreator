using System;
using System.Collections;
using System.Collections.Generic;
using Infrastructure;
using Planets;
using UnityEngine;

/// <summary>
/// Определяет итоговый тип планеты по результатам всех мини-игр.
/// Реализует IMiniGame, чтобы быть шагом в LevelSequence.
/// При StartGame() — определяет тип, спавнит префаб, завершается.
///
/// Таблица соответствий:
/// 1. Пустынная  — Близко, любой размер/масса, хим: силикаты+металлы+лёгкие/летучие или силикаты×2 или металлы×2
/// 2. Газовый Гигант — Далеко, большая масса, хим: лёгкие×2+силикаты/металлы или силикаты+летучие+лёгкие и т.д.
/// 3. Ледяной Гигант — Далеко, любая масса, хим: силикаты+металлы+летучие или металлы+летучие×2 или силикаты+летучие×2
/// 4. Лава — Близко, большая масса, хим: содержит радиоактивные
/// 5. Океан — только через миграцию ледяного гиганта
/// 6. Земля — Среднее, большая масса, хим: силикаты+металлы+летучие
/// 7. Каменистая — любая зона, малая масса, хим: однородные смеси (силикаты×3, металлы×3, лёгкие×2+сил/мет и т.д.)
/// 8. Протопланета — если ничего не совпало
/// </summary>
public class PlanetResolver : MonoBehaviour, IMiniGame
{
    [Serializable]
    public class PlanetPrefabEntry
    {
        public string planetName;
        public GameObject prefab;
    }

    [Header("Planet Prefabs")]
    [SerializeField] private PlanetPrefabEntry m_desert;
    [SerializeField] private PlanetPrefabEntry m_gasGiant;
    [SerializeField] private PlanetPrefabEntry m_iceGiant;
    [SerializeField] private PlanetPrefabEntry m_lava;
    [SerializeField] private PlanetPrefabEntry m_ocean;
    [SerializeField] private PlanetPrefabEntry m_earth;
    [SerializeField] private PlanetPrefabEntry m_rocky;
    [SerializeField] private PlanetPrefabEntry m_protoplanet;

    [Header("Spawn")]
    [SerializeField] private Transform m_spawnPoint;
    [SerializeField] private float m_spawnScale = 1f;
    [SerializeField] private float m_appearDuration = 1.5f;

    [Header("Satellites & Rings")]
    [SerializeField] private GameObject m_satellitePrefab;
    [SerializeField] private GameObject m_ringsPrefab;
    [SerializeField] private float m_orbitRadius = 3f;

    [Header("Chemical Data")]
    [SerializeField] private MixingContainer m_mixingContainer;

    [Header("Timing")]
    [SerializeField] private float m_completeDelay = 3f;

    public event Action MiniGameCompleted;

    private GameObject m_spawnedPlanet;

    public void StartGame()
    {
        var data = PlanetBuildData.Instance;
        if (data == null)
        {
            Debug.LogError("[PlanetResolver] PlanetBuildData not found!");
            MiniGameCompleted?.Invoke();
            return;
        }

        // Получаем химический состав
        Dictionary<ChemicalType, float> chemicals = null;
        if (m_mixingContainer != null)
            chemicals = m_mixingContainer.GetChemicalComposition();

        List<ChemicalType> mixture = GetSortedChemicals(chemicals);

        // Определяем тип
        string planetType = ResolvePlanetType(data, mixture);
        Debug.Log($"[PlanetResolver] Итог: {planetType}");

        // Спавним
        GameObject prefab = GetPrefab(planetType);
        if (prefab != null)
            StartCoroutine(SpawnPlanet(prefab));

        StartCoroutine(CompleteAfterDelay());
    }

    public void StopGame()
    {
        StopAllCoroutines();
    }

    // ─────────────────── Resolution ───────────────────

    private string ResolvePlanetType(PlanetBuildData data, List<ChemicalType> mix)
    {
        bool hasRadioactive = mix.Contains(ChemicalType.Radioactive);
        bool hasSilicates = mix.Contains(ChemicalType.Silicates);
        bool hasMetals = mix.Contains(ChemicalType.Metal);
        bool hasVolatiles = mix.Contains(ChemicalType.Volatiles);
        bool hasLight = mix.Contains(ChemicalType.Light);

        int silCount = CountType(mix, ChemicalType.Silicates);
        int metCount = CountType(mix, ChemicalType.Metal);
        int volCount = CountType(mix, ChemicalType.Volatiles);
        int lightCount = CountType(mix, ChemicalType.Light);
        int radCount = CountType(mix, ChemicalType.Radioactive);

        // 4. Лава — Близко + большая масса + радиоактивные
        if (data.remoteness == Remoteness.Near && data.mass == Mass.Heavy && hasRadioactive)
            return "Lava";

        // 6. Земля — Среднее + большая масса + силикаты+металлы+летучие
        if (data.remoteness == Remoteness.Medium && data.mass == Mass.Heavy
            && hasSilicates && hasMetals && hasVolatiles)
            return "Earth";

        // 2. Газовый Гигант — Далеко + большая масса + лёгкие газы
        if (data.remoteness == Remoteness.Far && data.mass == Mass.Heavy && hasLight)
            return "GasGiant";

        // 3. Ледяной Гигант — Далеко + летучие вещества
        if (data.remoteness == Remoteness.Far && hasVolatiles
            && (hasSilicates || hasMetals))
            return "IceGiant";

        // 1. Пустынная — Близко + силикаты или металлы
        if (data.remoteness == Remoteness.Near && (hasSilicates || hasMetals) && !hasRadioactive)
            return "Desert";

        // 7. Каменистая — малая масса + однородные смеси
        if (data.mass == Mass.Light)
            return "Rocky";

        // 5. Океан — через миграцию ледяного гиганта (проверяется отдельно)
        // Если был ледяной гигант + миграция → океан
        if (data.migration == Migration.Yes && data.remoteness == Remoteness.Far && hasVolatiles)
            return "Ocean";

        // 8. Протопланета — если ничего не совпало
        return "Protoplanet";
    }

    private int CountType(List<ChemicalType> mix, ChemicalType type)
    {
        int count = 0;
        foreach (var t in mix)
            if (t == type) count++;
        return count;
    }

    private List<ChemicalType> GetSortedChemicals(Dictionary<ChemicalType, float> chemicals)
    {
        var result = new List<ChemicalType>();
        if (chemicals == null) return result;

        // Сортируем по объёму (от большего к меньшему)
        var sorted = new List<KeyValuePair<ChemicalType, float>>(chemicals);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        foreach (var kvp in sorted)
            result.Add(kvp.Key);

        return result;
    }

    private GameObject GetPrefab(string planetType)
    {
        return planetType switch
        {
            "Desert" => m_desert?.prefab,
            "GasGiant" => m_gasGiant?.prefab,
            "IceGiant" => m_iceGiant?.prefab,
            "Lava" => m_lava?.prefab,
            "Ocean" => m_ocean?.prefab,
            "Earth" => m_earth?.prefab,
            "Rocky" => m_rocky?.prefab,
            "Protoplanet" => m_protoplanet?.prefab,
            _ => m_protoplanet?.prefab
        };
    }

    // ─────────────────── Spawn ───────────────────

    private IEnumerator SpawnPlanet(GameObject prefab)
    {
        Vector3 pos = m_spawnPoint != null ? m_spawnPoint.position : transform.position;

        m_spawnedPlanet = Instantiate(prefab, pos, Quaternion.identity);
        Transform tr = m_spawnedPlanet.transform;

        Vector3 targetScale = Vector3.one * m_spawnScale;
        tr.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < m_appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / m_appearDuration));
            tr.localScale = targetScale * t;
            yield return null;
        }

        tr.localScale = targetScale;

        // Спавним спутники или кольца по результатам мини-игры
        var data = PlanetBuildData.Instance;
        if (data != null)
            SpawnSatellitesOrRings(data.satellites, tr);
    }

    private void SpawnSatellitesOrRings(SatellitesOrRings choice, Transform planetTransform)
    {
        switch (choice)
        {
            case SatellitesOrRings.OneSatellite:
                SpawnSatellites(1, planetTransform);
                break;

            case SatellitesOrRings.ThreeSatellites:
                SpawnSatellites(3, planetTransform);
                break;

            case SatellitesOrRings.Rings:
                SpawnRings(planetTransform);
                break;

            case SatellitesOrRings.None:
            default:
                break;
        }
    }

    private void SpawnSatellites(int count, Transform planetTransform)
    {
        if (m_satellitePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject satellite = Instantiate(m_satellitePrefab, planetTransform);

            float angle = i * (360f / count) * Mathf.Deg2Rad;
            satellite.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * m_orbitRadius,
                0f,
                Mathf.Sin(angle) * m_orbitRadius);

            if (satellite.GetComponent<OrbitRotation>() == null)
                satellite.AddComponent<OrbitRotation>();

            // Анимация появления
            StartCoroutine(AnimateScaleIn(satellite.transform, 0.5f, i * 0.2f));
        }
    }

    private void SpawnRings(Transform planetTransform)
    {
        if (m_ringsPrefab == null) return;

        GameObject rings = Instantiate(m_ringsPrefab, planetTransform);
        rings.transform.localPosition = Vector3.zero;

        StartCoroutine(AnimateScaleIn(rings.transform, 0.8f, 0f));
    }

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

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
    }
}