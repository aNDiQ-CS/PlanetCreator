using UnityEngine;
using Planets;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

public class ChemicalFlaskManager : MonoBehaviour, IMiniGame
{
    public static ChemicalFlaskManager Instance { get; private set; }

    [Serializable]
    public class FlaskPrefab
    {
        public ChemicalType type;
        public GameObject prefab;
    }

    [Header("Flask Prefabs (5 веществ)")]
    public List<FlaskPrefab> flaskPrefabs = new List<FlaskPrefab>();

    [Header("Spawn Points (расставить на сцене)")]
    [Tooltip("По одной позиции для каждого типа колбы. Размер должен совпадать с flaskPrefabs.")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Settings")]
    public int maxFlasksPerType = 1;
    public float respawnDelay = 1.5f;
    public Vector3 spawnRotation = Vector3.zero;

    [Header("Win Condition")]
    [SerializeField] private int m_requiredFlasks = 3;

    public event Action MiniGameCompleted;

    private int m_flasksPoured;
    private bool m_isGameActive;

    private Dictionary<ChemicalType, Queue<GameObject>> activeFlasks = new Dictionary<ChemicalType, Queue<GameObject>>();
    private Dictionary<ChemicalType, float> lastSpawnTime = new Dictionary<ChemicalType, float>();

    void Awake()
    {
        // Регистрируем Instance, но НЕ уничтожаем объект при дубликате —
        // просто перезаписываем ссылку. LevelFlowState управляет активностью.
        Instance = this;
        InitializeFlaskTracking();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────── IMiniGame ───────────────────

    public void StartGame()
    {
        m_flasksPoured = 0;
        m_isGameActive = true;

        ClearAllFlasks();
        SpawnAllFlasks();
    }

    public void StopGame()
    {
        m_isGameActive = false;
        ClearAllFlasks();
    }

    // ─────────────────── Internal ───────────────────

    private void InitializeFlaskTracking()
    {
        activeFlasks.Clear();
        lastSpawnTime.Clear();

        foreach (var flaskPrefab in flaskPrefabs)
        {
            if (!activeFlasks.ContainsKey(flaskPrefab.type))
            {
                activeFlasks[flaskPrefab.type] = new Queue<GameObject>();
                lastSpawnTime[flaskPrefab.type] = 0f;
            }
        }
    }

    public void SpawnAllFlasks()
    {
        for (int i = 0; i < flaskPrefabs.Count; i++)
        {
            var flaskPrefab = flaskPrefabs[i];

            Vector3 position = GetSpawnPosition(i);
            Quaternion rotation = Quaternion.Euler(spawnRotation);

            GameObject newFlask = Instantiate(flaskPrefab.prefab, position, rotation);
            activeFlasks[flaskPrefab.type].Enqueue(newFlask);

            Wobble wobbleScript = newFlask.GetComponent<Wobble>();
            if (wobbleScript != null)
            {
                wobbleScript.chemicalType = flaskPrefab.type;
                wobbleScript.SetFillAmount(UnityEngine.Random.Range(0.6f, 0.9f));
            }

            Rigidbody rb = newFlask.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (index < spawnPoints.Count && spawnPoints[index] != null)
            return spawnPoints[index].position;

        // Fallback если точка не назначена
        Debug.LogWarning($"[ChemicalFlaskManager] SpawnPoint [{index}] не назначен, используем позицию объекта");
        return transform.position + Vector3.right * (index - flaskPrefabs.Count / 2f) * 1.5f;
    }

    public void RequestNewFlask(ChemicalType type)
    {
        if (!m_isGameActive) return;

        if (Time.time - lastSpawnTime[type] < respawnDelay)
        {
            Invoke(nameof(SpawnFlaskDelayed), respawnDelay);
            return;
        }

        SpawnFlask(type);
    }

    void SpawnFlaskDelayed()
    {
        if (!m_isGameActive) return;

        foreach (var flaskPrefab in flaskPrefabs)
        {
            if (activeFlasks[flaskPrefab.type].Count < maxFlasksPerType)
            {
                SpawnFlask(flaskPrefab.type);
                break;
            }
        }
    }

    void SpawnFlask(ChemicalType type)
    {
        if (!m_isGameActive) return;

        int typeIndex = flaskPrefabs.FindIndex(fp => fp.type == type);
        if (typeIndex < 0) return;

        var flaskPrefab = flaskPrefabs[typeIndex];
        if (flaskPrefab.prefab == null) return;
        if (activeFlasks[type].Count >= maxFlasksPerType) return;

        Vector3 position = GetSpawnPosition(typeIndex);
        Quaternion rotation = Quaternion.Euler(spawnRotation);

        GameObject newFlask = Instantiate(flaskPrefab.prefab, position, rotation);
        activeFlasks[type].Enqueue(newFlask);
        lastSpawnTime[type] = Time.time;

        Wobble wobbleScript = newFlask.GetComponent<Wobble>();
        if (wobbleScript != null)
        {
            wobbleScript.chemicalType = type;
            wobbleScript.SetFillAmount(UnityEngine.Random.Range(0.6f, 0.9f));
        }

        Rigidbody rb = newFlask.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public void OnFlaskDestroyed(GameObject flask, ChemicalType type)
    {
        if (activeFlasks.ContainsKey(type))
        {
            var queue = activeFlasks[type];
            if (queue.Contains(flask))
            {
                var newQueue = new Queue<GameObject>();
                foreach (var item in queue)
                {
                    if (item != flask)
                        newQueue.Enqueue(item);
                }
                activeFlasks[type] = newQueue;
            }
        }

        if (m_isGameActive)
        {
            m_flasksPoured++;
            Debug.Log($"[ChemicalFlaskManager] Вылито колб: {m_flasksPoured}/{m_requiredFlasks}");

            if (m_flasksPoured >= m_requiredFlasks)
            {
                m_isGameActive = false;
                Debug.Log("[ChemicalFlaskManager] Мини-игра завершена!");
                MiniGameCompleted?.Invoke();
                return;
            }
        }

        RequestNewFlask(type);
    }

    public void ClearAllFlasks()
    {
        foreach (var kvp in activeFlasks)
        {
            while (kvp.Value.Count > 0)
            {
                GameObject flask = kvp.Value.Dequeue();
                if (flask != null)
                    Destroy(flask);
            }
        }

        foreach (var type in lastSpawnTime.Keys.ToList())
        {
            lastSpawnTime[type] = 0f;
        }
    }

    // ─────────────────── Gizmos ───────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] == null) continue;

            Gizmos.DrawWireSphere(spawnPoints[i].position, 0.2f);

#if UNITY_EDITOR
            string label = i < flaskPrefabs.Count ? flaskPrefabs[i].type.ToString() : $"Point {i}";
            UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 0.3f, label);
#endif
        }
    }
}