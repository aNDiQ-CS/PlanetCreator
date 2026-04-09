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
/// 1. Пустынная  — Близко, любой размер/масса, хим: силикаты+металлы
/// 2. Газовый Гигант — Далеко, большая масса, хим: лёгкие газы
/// 3. Ледяной Гигант — Далеко, летучие вещества (без миграции)
/// 4. Лава — Близко, большая масса, хим: радиоактивные
/// 5. Океан — миграция ледяного гиганта (Far + Volatiles + Migration.Yes)
/// 6. Земля — Среднее, большая масса, хим: силикаты+металлы+летучие
/// 7. Каменистая — малая масса
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
    private List<GameObject> m_spawnedExtras = new List<GameObject>();

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

        Debug.Log($"[PlanetResolver] Параметры: Size={data.size}, Mass={data.mass}, " +
                  $"Remoteness={data.remoteness}, Migration={data.migration}, " +
                  $"Satellites={data.satellites}");
        Debug.Log($"[PlanetResolver] Итог: {planetType}");

        // Спавним планету — завершение мини-игры произойдёт внутри корутины после спавна
        GameObject prefab = GetPrefab(planetType);
        if (prefab != null)
            StartCoroutine(SpawnPlanetAndComplete(prefab, data.satellites));
        else
        {
            Debug.LogError($"[PlanetResolver] Префаб для '{planetType}' не назначен!");
            MiniGameCompleted?.Invoke();
        }
    }

    public void StopGame()
    {
        StopAllCoroutines();

        // Очищаем заспавненные объекты при StopGame (для undo)
        if (m_spawnedPlanet != null)
        {
            Destroy(m_spawnedPlanet);
            m_spawnedPlanet = null;
        }

        foreach (var obj in m_spawnedExtras)
        {
            if (obj != null)
                Destroy(obj);
        }
        m_spawnedExtras.Clear();
    }

    // ─────────────────── Resolution ───────────────────

    private string ResolvePlanetType(PlanetBuildData data, List<ChemicalType> mix)
    {
        bool hasRadioactive = mix.Contains(ChemicalType.Radioactive);
        bool hasSilicates = mix.Contains(ChemicalType.Silicates);
        bool hasMetals = mix.Contains(ChemicalType.Metal);
        bool hasVolatiles = mix.Contains(ChemicalType.Volatiles);
        bool hasLight = mix.Contains(ChemicalType.Light);

        // 4. Лава — Близко + большая масса + радиоактивные
        if (data.remoteness == Remoteness.Near && data.mass == Mass.Heavy && hasRadioactive)
            return "Lava";

        // 5. Океан — миграция ледяного гиганта (проверяем ПЕРЕД ледяным гигантом!)
        if (data.remoteness == Remoteness.Far && hasVolatiles && data.migration == Migration.Yes
            && (hasSilicates || hasMetals))
            return "Ocean";

        // 6. Земля — Среднее + большая масса + силикаты+металлы+летучие
        if (data.remoteness == Remoteness.Medium && data.mass == Mass.Heavy
            && hasSilicates && hasMetals && hasVolatiles)
            return "Earth";

        // 2. Газовый Гигант — Далеко + большая масса + лёгкие газы
        if (data.remoteness == Remoteness.Far && data.mass == Mass.Heavy && hasLight)
            return "GasGiant";

        // 3. Ледяной Гигант — Далеко + летучие вещества (без миграции, т.к. проверили выше)
        if (data.remoteness == Remoteness.Far && hasVolatiles
            && (hasSilicates || hasMetals))
            return "IceGiant";

        // 1. Пустынная — Близко + силикаты или металлы
        if (data.remoteness == Remoteness.Near && (hasSilicates || hasMetals) && !hasRadioactive)
            return "Desert";

        // 7. Каменистая — малая масса
        if (data.mass == Mass.Light)
            return "Rocky";

        // 8. Протопланета — если ничего не совпало
        return "Protoplanet";
    }

    private List<ChemicalType> GetSortedChemicals(Dictionary<ChemicalType, float> chemicals)
    {
        var result = new List<ChemicalType>();
        if (chemicals == null) return result;

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

    private IEnumerator SpawnPlanetAndComplete(GameObject prefab, SatellitesOrRings satelliteChoice)
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

        // Спавним спутники или кольца
        // Передаём выбор напрямую вместо повторного чтения из PlanetBuildData
        Debug.Log($"[PlanetResolver] Спавним дополнения: {satelliteChoice}");
        SpawnSatellitesOrRings(satelliteChoice, tr);

        // Ждём перед завершением, чтобы игрок увидел результат
        yield return new WaitForSeconds(m_completeDelay);
        MiniGameCompleted?.Invoke();
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
                Debug.Log("[PlanetResolver] Без спутников и колец.");
                break;
        }
    }

    private void SpawnSatellites(int count, Transform planetTransform)
    {
        if (m_satellitePrefab == null)
        {
            Debug.LogError("[PlanetResolver] m_satellitePrefab не назначен в инспекторе!");
            return;
        }

        Debug.Log($"[PlanetResolver] Спавним {count} спутник(ов)");

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

            StartCoroutine(AnimateScaleIn(satellite.transform, 0.5f, i * 0.2f));

            m_spawnedExtras.Add(satellite);
        }
    }

    private void SpawnRings(Transform planetTransform)
    {
        if (m_ringsPrefab == null)
        {
            Debug.LogError("[PlanetResolver] m_ringsPrefab не назначен в инспекторе!");
            return;
        }

        Debug.Log("[PlanetResolver] Спавним кольца");

        GameObject rings = Instantiate(m_ringsPrefab, planetTransform);
        rings.transform.localPosition = Vector3.zero;

        StartCoroutine(AnimateScaleIn(rings.transform, 0.8f, 0f));

        m_spawnedExtras.Add(rings);
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
}
