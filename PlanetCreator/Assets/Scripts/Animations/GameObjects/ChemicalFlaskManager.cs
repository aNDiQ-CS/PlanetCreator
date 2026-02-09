using UnityEngine;
using Planets;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ChemicalFlaskManager : MonoBehaviour
{
    public static ChemicalFlaskManager Instance { get; private set; }

    [System.Serializable]
    public class FlaskPrefab
    {
        public ChemicalType type;
        public GameObject prefab;
        [HideInInspector] public bool isActive = true;
    }

    [Header("Flask Settings")]
    public List<FlaskPrefab> flaskPrefabs = new List<FlaskPrefab>();
    public Transform spawnPoint;
    public float spawnHeight = 5f;
    public int maxFlasksPerType = 1;
    public float respawnDelay = 1.5f;
    public Vector3 spawnRotation = Vector3.zero;

    [Header("Spawn Settings")]
    public float horizontalSpacing = 1.5f;
    public bool spawnInCircle = true;
    public float circleRadius = 2f;

    private Dictionary<ChemicalType, Queue<GameObject>> activeFlasks = new Dictionary<ChemicalType, Queue<GameObject>>();
    private Dictionary<ChemicalType, float> lastSpawnTime = new Dictionary<ChemicalType, float>();
    private List<Vector3> spawnPositions = new List<Vector3>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        foreach (var flaskPrefab in flaskPrefabs)
        {
            if (!activeFlasks.ContainsKey(flaskPrefab.type))
            {
                activeFlasks[flaskPrefab.type] = new Queue<GameObject>();
                lastSpawnTime[flaskPrefab.type] = 0f;
            }
        }

        CalculateSpawnPositions();
        SpawnAllFlasks();
    }

    void CalculateSpawnPositions()
    {
        spawnPositions.Clear();

        if (spawnInCircle)
        {
            int count = flaskPrefabs.Count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector3 position = spawnPoint.position +
                    new Vector3(Mathf.Cos(angle) * circleRadius, spawnHeight, Mathf.Sin(angle) * circleRadius);
                spawnPositions.Add(position);
            }
        }
        else
        {
            for (int i = 0; i < flaskPrefabs.Count; i++)
            {
                Vector3 position = spawnPoint.position +
                    new Vector3((i - flaskPrefabs.Count / 2f) * horizontalSpacing, spawnHeight, 0);
                spawnPositions.Add(position);
            }
        }
    }

    public void SpawnAllFlasks()
    {
        for (int i = 0; i < flaskPrefabs.Count; i++)
        {
            var flaskPrefab = flaskPrefabs[i];

            Vector3 spawnPosition = (i < spawnPositions.Count) ?
                spawnPositions[i] :
                spawnPoint.position + Vector3.up * spawnHeight;

            Quaternion rotation = Quaternion.Euler(spawnRotation);
            GameObject newFlask = Instantiate(flaskPrefab.prefab, spawnPosition, rotation);

            activeFlasks[flaskPrefab.type].Enqueue(newFlask);

            Wobble wobbleScript = newFlask.GetComponent<Wobble>();
            if (wobbleScript != null)
            {
                wobbleScript.chemicalType = flaskPrefab.type;
                wobbleScript.SetFillAmount(Random.Range(0.6f, 0.9f));
            }

            Rigidbody rb = newFlask.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                Vector3 randomForce = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0f, 1f),
                    Random.Range(-0.5f, 0.5f)
                );
                rb.AddForce(randomForce, ForceMode.Impulse);

                Vector3 randomTorque = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(-5f, 5f),
                    Random.Range(-5f, 5f)
                );
                rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }
    }

    public void RequestNewFlask(ChemicalType type)
    {
        if (Time.time - lastSpawnTime[type] < respawnDelay)
        {
            Invoke(nameof(SpawnFlaskDelayed), respawnDelay);
            return;
        }

        SpawnFlask(type);
    }

    void SpawnFlaskDelayed()
    {
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
        var flaskPrefab = flaskPrefabs.Find(fp => fp.type == type);
        if (flaskPrefab == null || flaskPrefab.prefab == null) return;
        if (activeFlasks[type].Count >= maxFlasksPerType) return;

        Vector3 spawnPosition = FindSpawnPosition(type);

        Quaternion rotation = Quaternion.Euler(spawnRotation);
        GameObject newFlask = Instantiate(flaskPrefab.prefab, spawnPosition, rotation);

        activeFlasks[type].Enqueue(newFlask);
        lastSpawnTime[type] = Time.time;

        Wobble wobbleScript = newFlask.GetComponent<Wobble>();
        if (wobbleScript != null)
        {
            wobbleScript.chemicalType = type;
            wobbleScript.SetFillAmount(Random.Range(0.6f, 0.9f));
        }

        Rigidbody rb = newFlask.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 randomForce = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.5f, 1.5f),
                Random.Range(-0.3f, 0.3f)
            );
            rb.AddForce(randomForce, ForceMode.Impulse);
        }
    }

    Vector3 FindSpawnPosition(ChemicalType type)
    {
        int typeIndex = flaskPrefabs.FindIndex(fp => fp.type == type);

        if (typeIndex >= 0 && typeIndex < spawnPositions.Count)
        {
            return spawnPositions[typeIndex];
        }

        return spawnPoint.position + Vector3.up * spawnHeight +
               new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
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
                    {
                        newQueue.Enqueue(item);
                    }
                }
                activeFlasks[type] = newQueue;
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
                {
                    Destroy(flask);
                }
            }
        }

        foreach (var type in lastSpawnTime.Keys.ToList())
        {
            lastSpawnTime[type] = 0f;
        }
    }

    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnPoint.position + Vector3.up * spawnHeight, Vector3.one * 0.3f);
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * spawnHeight);

            if (Application.isPlaying && spawnPositions.Count > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < spawnPositions.Count; i++)
                {
                    Gizmos.DrawSphere(spawnPositions[i], 0.2f);

#if UNITY_EDITOR
                    if (i < flaskPrefabs.Count)
                    {
                        UnityEditor.Handles.Label(spawnPositions[i] + Vector3.up * 0.3f,
                            flaskPrefabs[i].type.ToString());
                    }
#endif
                }
            }
            else if (flaskPrefabs.Count > 0)
            {
                Gizmos.color = Color.blue;

                if (spawnInCircle)
                {
                    int count = flaskPrefabs.Count;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = i * Mathf.PI * 2f / count;
                        Vector3 position = spawnPoint.position +
                            new Vector3(Mathf.Cos(angle) * circleRadius, spawnHeight, Mathf.Sin(angle) * circleRadius);
                        Gizmos.DrawSphere(position, 0.15f);
                    }
                }
                else
                {
                    for (int i = 0; i < flaskPrefabs.Count; i++)
                    {
                        Vector3 position = spawnPoint.position +
                            new Vector3((i - flaskPrefabs.Count / 2f) * horizontalSpacing, spawnHeight, 0);
                        Gizmos.DrawSphere(position, 0.15f);
                    }
                }
            }
        }
    }

    [ContextMenu("Пересоздать все колбы")]
    void RecreateAllFlasks()
    {
        ClearAllFlasks();
        SpawnAllFlasks();
    }

    [ContextMenu("Показать статистику")]
    void ShowStats()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[ChemicalFlaskManager] Статистика:");
        sb.AppendLine($"Всего типов: {flaskPrefabs.Count}");

        foreach (var kvp in activeFlasks)
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value.Count} активных колб");
        }

        Debug.Log(sb.ToString());
    }
}