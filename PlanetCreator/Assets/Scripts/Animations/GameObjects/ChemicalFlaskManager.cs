using UnityEngine;
using Planets;
using Infrastructure;
using System;
using System.Collections;
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
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Settings")]
    public int maxFlasksPerType = 1;
    public float respawnDelay = 1.5f;
    public Vector3 spawnRotation = Vector3.zero;

    [Header("Pour Animation")]
    [Tooltip("Точка над контейнером, куда подлетает колба")]
    [SerializeField] private Transform m_pourTarget;
    [SerializeField] private float m_flyDuration = 0.8f;
    [SerializeField] private float m_pourAngleZ = -110f;
    [SerializeField] private float m_tiltDuration = 0.4f;
    [SerializeField] private float m_pourDuration = 1.2f;
    [SerializeField] private float m_fadeOutDuration = 0.6f;

    [Header("Mixing Container")]
    [SerializeField] private MixingContainer m_mixingContainer;

    [Header("Win Condition")]
    [SerializeField] private int m_requiredFlasks = 3;

    public event Action MiniGameCompleted;

    private int m_flasksPoured;
    private bool m_isGameActive;
    private bool m_isAnimating;

    private Dictionary<ChemicalType, Queue<GameObject>> activeFlasks = new();

    void Awake()
    {
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
        m_isAnimating = false;

        ClearAllFlasks();
        SpawnAllFlasks();
    }

    public void StopGame()
    {
        m_isGameActive = false;
        m_isAnimating = false;
        StopAllCoroutines();
        ClearAllFlasks();
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        m_flasksPoured = 0;
        m_isAnimating = false;

        ClearAllFlasks();

        if (m_mixingContainer != null)
            m_mixingContainer.ClearContainer();

        if (m_isGameActive)
            SpawnAllFlasks();
    }

    // ─────────────────── Click handling ───────────────────

    public void OnFlaskClicked(GameObject flaskObject, ChemicalType type)
    {
        if (!m_isGameActive || m_isAnimating)
            return;

        Wobble wobble = flaskObject.GetComponentInChildren<Wobble>();
        if (wobble == null) return;

        StartCoroutine(PourAnimationSequence(flaskObject, wobble, type));
    }

    // ─────────────────── Pour animation ───────────────────

    private IEnumerator PourAnimationSequence(GameObject flask, Wobble wobble, ChemicalType type)
    {
        m_isAnimating = true;

        // Полностью и немедленно убираем из физики
        DestroyAllPhysics(flask);

        // Ждём один физический кадр чтобы движок обработал удаление
        yield return new WaitForFixedUpdate();

        Transform flaskTransform = flask.transform;
        Quaternion startRot = flaskTransform.rotation;

        Vector3 pourPos = m_pourTarget != null
            ? m_pourTarget.position
            : flaskTransform.position + Vector3.up * 2f;

        // 1. Подлёт к контейнеру
        yield return AnimateMovement(flaskTransform, flaskTransform.position, pourPos,
                                      startRot, startRot, m_flyDuration);

        // 2. Наклон для выливания
        Quaternion pourRot = Quaternion.Euler(0, 0, m_pourAngleZ);
        yield return AnimateMovement(flaskTransform, pourPos, pourPos,
                                      startRot, pourRot, m_tiltDuration);

        // 3. Выливание
        if (m_mixingContainer != null)
        {
            Color chemColor = wobble.GetChemicalColor();
            float density = wobble.chemicalDensity;
            float pourVolume = wobble.GetFillAmount() * 2f;

            float startFill = wobble.GetFillAmount();
            float elapsed = 0f;
            bool addedToContainer = false;

            while (elapsed < m_pourDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / m_pourDuration);
                wobble.SetFillAmount(Mathf.Lerp(startFill, wobble.minFillLevel, t));

                if (!addedToContainer && t > 0.1f)
                {
                    m_mixingContainer.AddChemical(type, chemColor, density, pourVolume);
                    addedToContainer = true;
                }

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(m_pourDuration);
        }

        // 4. Fade-out и уменьшение — колба исчезает прямо у контейнера
        yield return FadeOutAndShrink(flask, m_fadeOutDuration);

        // 5. Удаляем
        RemoveFlaskFromTracking(flask, type);
        Destroy(flask);

        m_flasksPoured++;
        Debug.Log($"[ChemicalFlaskManager] Вылито колб: {m_flasksPoured}/{m_requiredFlasks}");

        if (m_flasksPoured >= m_requiredFlasks)
        {
            m_isGameActive = false;
            m_isAnimating = false;

            // Плавно удаляем все оставшиеся колбы
            yield return FadeOutAllRemainingFlasks();

            Debug.Log("[ChemicalFlaskManager] Мини-игра завершена!");
            MiniGameCompleted?.Invoke();
            yield break;
        }

        // Респавн
        yield return new WaitForSeconds(respawnDelay);
        SpawnFlask(type);

        m_isAnimating = false;
    }

    // ─────────────────── Physics removal ───────────────────

    /// <summary>
    /// Полностью и немедленно убирает объект из физического движка.
    /// DestroyImmediate гарантирует что Rigidbody исчезнет в этом же кадре,
    /// а не в конце (как обычный Destroy).
    /// </summary>
    private void DestroyAllPhysics(GameObject obj)
    {
        // Сначала отключаем все коллайдеры
        foreach (var col in obj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        // Уничтожаем Rigidbody немедленно — не ждём конца кадра
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>(true))
            DestroyImmediate(rb);
    }

    // ─────────────────── Fade out ───────────────────

    private IEnumerator FadeOutAndShrink(GameObject obj, float duration)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Vector3 startScale = obj.transform.localScale;

        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].material.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / duration));

            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = startColors[i];
                renderers[i].material.color = new Color(c.r, c.g, c.b, 1f - t);
            }

            yield return null;
        }
    }

    /// <summary>
    /// Плавно удаляет все оставшиеся колбы при завершении мини-игры.
    /// </summary>
    private IEnumerator FadeOutAllRemainingFlasks()
    {
        List<GameObject> remaining = new List<GameObject>();
        foreach (var kvp in activeFlasks)
        {
            foreach (var flask in kvp.Value)
            {
                if (flask != null)
                {
                    DestroyAllPhysics(flask);
                    remaining.Add(flask);
                }
            }
        }

        if (remaining.Count == 0)
            yield break;

        // Fade-out всех одновременно
        float elapsed = 0f;
        float duration = m_fadeOutDuration;

        // Собираем данные рендереров
        var rendererData = new List<(Renderer[] renderers, Color[] colors, Vector3 scale)>();
        foreach (var flask in remaining)
        {
            var renderers = flask.GetComponentsInChildren<Renderer>();
            var colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                colors[i] = renderers[i].material.color;
            rendererData.Add((renderers, colors, flask.transform.localScale));
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / duration));

            for (int f = 0; f < remaining.Count; f++)
            {
                if (remaining[f] == null) continue;

                var (renderers, colors, scale) = rendererData[f];
                remaining[f].transform.localScale = Vector3.Lerp(scale, scale * 0.2f, t);

                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Color c = colors[i];
                    renderers[i].material.color = new Color(c.r, c.g, c.b, 1f - t);
                }
            }

            yield return null;
        }

        // Уничтожаем
        foreach (var flask in remaining)
        {
            if (flask != null)
                Destroy(flask);
        }

        // Очищаем трекинг
        foreach (var kvp in activeFlasks)
            kvp.Value.Clear();
    }

    // ─────────────────── Movement animation ───────────────────

    private IEnumerator AnimateMovement(Transform tr, Vector3 fromPos, Vector3 toPos,
                                         Quaternion fromRot, Quaternion toRot, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / duration));
            tr.position = Vector3.Lerp(fromPos, toPos, t);
            tr.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }

        tr.position = toPos;
        tr.rotation = toRot;
    }

    // ─────────────────── Spawn ───────────────────

    private void InitializeFlaskTracking()
    {
        activeFlasks.Clear();
        foreach (var fp in flaskPrefabs)
        {
            if (!activeFlasks.ContainsKey(fp.type))
                activeFlasks[fp.type] = new Queue<GameObject>();
        }
    }

    public void SpawnAllFlasks()
    {
        for (int i = 0; i < flaskPrefabs.Count; i++)
        {
            Vector3 position = GetSpawnPosition(i);
            Quaternion rotation = Quaternion.Euler(spawnRotation);

            GameObject newFlask = Instantiate(flaskPrefabs[i].prefab, position, rotation);
            activeFlasks[flaskPrefabs[i].type].Enqueue(newFlask);
            SetupFlask(newFlask, flaskPrefabs[i].type);
        }
    }

    private void SetupFlask(GameObject flask, ChemicalType type)
    {
        Wobble wobble = flask.GetComponentInChildren<Wobble>();
        if (wobble != null)
        {
            wobble.chemicalType = type;
            wobble.SetFillAmount(UnityEngine.Random.Range(0.6f, 0.9f));
        }

        FlaskClickHandler handler = flask.GetComponent<FlaskClickHandler>();
        if (handler == null)
            handler = flask.AddComponent<FlaskClickHandler>();
        handler.Initialize(type, this);
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (index < spawnPoints.Count && spawnPoints[index] != null)
            return spawnPoints[index].position;
        return transform.position + Vector3.right * (index - flaskPrefabs.Count / 2f) * 1.5f;
    }

    void SpawnFlask(ChemicalType type)
    {
        if (!m_isGameActive) return;

        int idx = flaskPrefabs.FindIndex(fp => fp.type == type);
        if (idx < 0) return;
        if (flaskPrefabs[idx].prefab == null) return;
        if (activeFlasks[type].Count >= maxFlasksPerType) return;

        Vector3 position = GetSpawnPosition(idx);
        GameObject newFlask = Instantiate(flaskPrefabs[idx].prefab, position, Quaternion.Euler(spawnRotation));
        activeFlasks[type].Enqueue(newFlask);
        SetupFlask(newFlask, type);
    }

    private void RemoveFlaskFromTracking(GameObject flask, ChemicalType type)
    {
        if (!activeFlasks.ContainsKey(type)) return;
        var q = activeFlasks[type];
        var newQ = new Queue<GameObject>();
        foreach (var item in q)
            if (item != flask) newQ.Enqueue(item);
        activeFlasks[type] = newQ;
    }

    public void ClearAllFlasks()
    {
        foreach (var kvp in activeFlasks)
        {
            while (kvp.Value.Count > 0)
            {
                GameObject flask = kvp.Value.Dequeue();
                if (flask != null) Destroy(flask);
            }
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
        if (m_pourTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(m_pourTarget.position, 0.3f);
        }
    }
}
