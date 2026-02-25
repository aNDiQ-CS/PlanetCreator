using UnityEngine;
using Planets;
using System.Collections;

public class Wobble : MonoBehaviour
{
    Renderer rend;
    Material wobblingMaterial;
    Vector3 lastPos;
    Vector3 velocity;
    Vector3 lastRot;
    Vector3 angularVelocity;

    [Header("Hierarchy Links")]
    public Transform neckPoint; // Объект "Target"
    private Transform beakerRoot;
    private Rigidbody beakerRb;

    [Header("Wobble Settings")]
    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;

    [Header("Pour Settings")]
    public float pourThreshold = 20f;
    public float pourRate = 0.2f;
    public float minFillLevel = 0.1f;
    public float maxFillLevel = 0.9f;

    [Header("Auto-Tilt & Detection (НАСТРОЙКА ЗОНЫ)")]
    public bool useAutoTilt = true;
    public float tiltSpeed = 5f;
    public float targetTiltAngle = 75f;

    [Space(10)]
    public float detectionRadius = 1.5f; // Увеличь это значение, чтобы расширить зону
    public Vector3 detectionOffset = new Vector3(0, -0.5f, 0); // Смещение зоны (например, ниже горлышка)
    public LayerMask containerLayerMask = -1;

    [Header("Настройки струи")]
    public GameObject streamPrefab; // Префаб с LineRenderer и скриптом Stream
    private Stream currentStream;
    public AudioSource pourSound;

    [Header("Chemical Properties")]
    public ChemicalType chemicalType = ChemicalType.Silicates;
    public Color chemicalColor = Color.gray;

    private float currentFillAmount = 0.8f;
    private bool isPouring = false;
    private Vector3 neckLocalPosition;
    private float lastPourAngle = 0f;
    private bool isEmpty = false;
    private MixingContainer targetContainer;
    private bool materialInitialized = false;

    void Start()
    {
        beakerRoot = transform.parent != null ? transform.parent : transform;
        beakerRb = beakerRoot.GetComponent<Rigidbody>();

        rend = GetComponent<Renderer>();
        wobblingMaterial = new Material(rend.sharedMaterial);
        rend.material = wobblingMaterial;
        materialInitialized = true;

        UpdateChemicalAppearance();
        if (neckPoint != null) neckLocalPosition = transform.InverseTransformPoint(neckPoint.position);

        SetFillAmount(currentFillAmount);
    }

    private void Update()
    {
        if (isEmpty || !materialInitialized) return;

        HandleWobble();
        FindTargetContainer();
        HandleAutoTilt();
        CheckPouringCondition();

        if (isPouring) PourLiquid();
        UpdateFillInShader();

        if (currentFillAmount <= minFillLevel + 0.01f && !isPouring && !isEmpty)
            EmptyAndDestroy();
    }

    void FindTargetContainer()
    {
        // Вычисляем центр сферы поиска: позиция Target + смещение (с учетом вращения колбы)
        Vector3 origin = neckPoint != null ? neckPoint.position : beakerRoot.position;
        Vector3 checkPos = origin + beakerRoot.TransformDirection(detectionOffset);

        // Ищем коллайдеры в зоне
        Collider[] colliders = Physics.OverlapSphere(checkPos, detectionRadius, containerLayerMask);

        targetContainer = null;
        foreach (Collider col in colliders)
        {
            MixingContainer container = col.GetComponent<MixingContainer>();
            if (container != null)
            {
                targetContainer = container;
                break;
            }
        }
    }

    void HandleAutoTilt()
    {
        if (!useAutoTilt || beakerRoot == null) return;

        Quaternion targetRotation;
        if (targetContainer != null)
        {
            Vector3 dirToContainer = (targetContainer.transform.position - beakerRoot.position);
            dirToContainer.y = 0;

            if (dirToContainer.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dirToContainer.normalized);
                targetRotation = lookRot * Quaternion.Euler(targetTiltAngle, 0, 0);
            }
            else
            {
                targetRotation = Quaternion.Euler(targetTiltAngle, 0, 0);
            }

            if (beakerRb != null) beakerRb.angularVelocity = Vector3.zero;
        }
        else
        {
            targetRotation = Quaternion.identity;
        }

        beakerRoot.rotation = Quaternion.Slerp(beakerRoot.rotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    // --- Остальная логика без изменений ---

    void HandleWobble()
    {
        time += Time.deltaTime;
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * Recovery);
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * Recovery);
        pulse = 2 * Mathf.PI * WobbleSpeed;
        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);
        wobblingMaterial.SetFloat("_WobbleX", wobbleAmountX);
        wobblingMaterial.SetFloat("_WobbleZ", wobbleAmountZ);
        velocity = (lastPos - transform.position) / Time.deltaTime;
        angularVelocity = transform.rotation.eulerAngles - lastRot;
        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    private float time = 0.5f;
    private float wobbleAmountX, wobbleAmountZ, wobbleAmountToAddX, wobbleAmountToAddZ, pulse;

    void CheckPouringCondition()
    {
        if (currentFillAmount <= minFillLevel) { StopPouring(); return; }
        float tiltAngle = Vector3.Angle(Vector3.up, beakerRoot.up);
        Vector3 neckWorldPos = neckPoint != null ? neckPoint.position : transform.position;
        float liquidWorldHeight = CalculateLiquidWorldHeight();
        bool shouldPour = (targetContainer != null) && (tiltAngle > pourThreshold) && (liquidWorldHeight > neckWorldPos.y - 0.1f);
        if (shouldPour && !isPouring) StartPouring();
        else if (!shouldPour && isPouring) StopPouring();
        lastPourAngle = tiltAngle;
    }

    void StartPouring()
    {
        isPouring = true;

        if (streamPrefab != null && neckPoint != null)
        {
            GameObject streamObj = Instantiate(streamPrefab, neckPoint.position, Quaternion.identity, neckPoint);
            currentStream = streamObj.GetComponent<Stream>();

            // ПЕРЕДАЕМ ЦВЕТ ЖИДКОСТИ В СТРУЮ
            currentStream.SetColor(chemicalColor);

            currentStream.StartStream();
        }

        if (pourSound != null && !pourSound.isPlaying) pourSound.Play();
    }

    void StopPouring()
    {
        isPouring = false;

        if (currentStream != null)
        {
            currentStream.StopStream();
            currentStream = null;
        }

        if (pourSound != null) pourSound.Stop();
    }

    void PourLiquid()
    {
        float pourAmount = pourRate * Time.deltaTime;
        currentFillAmount -= pourAmount;
        currentFillAmount = Mathf.Max(currentFillAmount, minFillLevel);
        if (targetContainer != null) targetContainer.AddChemical(chemicalType, chemicalColor, 1.0f, pourAmount);
    }

    void UpdateFillInShader() { if (materialInitialized) wobblingMaterial.SetFloat("_Fill", currentFillAmount); }
    public void SetFillAmount(float amount) { currentFillAmount = amount; UpdateFillInShader(); }
    void UpdateChemicalAppearance() { if (materialInitialized) { wobblingMaterial.SetColor("_SideColor", chemicalColor); wobblingMaterial.SetColor("_TopColor", chemicalColor); } }
    float CalculateLiquidWorldHeight() { Bounds bounds = rend.bounds; return Mathf.Lerp(bounds.min.y, bounds.max.y, (currentFillAmount - minFillLevel) / (maxFillLevel - minFillLevel)); }
    void EmptyAndDestroy() { isEmpty = true; StartCoroutine(FadeOutAndDestroy()); }
    IEnumerator FadeOutAndDestroy() { yield return new WaitForSeconds(0.5f); if (ChemicalFlaskManager.Instance != null) ChemicalFlaskManager.Instance.OnFlaskDestroyed(beakerRoot.gameObject, chemicalType); Destroy(beakerRoot.gameObject); }

    // Отрисовка зоны в редакторе
    private void OnDrawGizmos()
    {
        if (beakerRoot == null) beakerRoot = transform.parent != null ? transform.parent : transform;
        Vector3 origin = neckPoint != null ? neckPoint.position : beakerRoot.position;
        Vector3 checkPos = origin + beakerRoot.TransformDirection(detectionOffset);

        Gizmos.color = (targetContainer != null) ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(checkPos, detectionRadius);

        // Линия от горлышка к центру зоны
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, checkPos);
    }
}