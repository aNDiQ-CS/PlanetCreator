using UnityEngine;
using Planets;

public class Wobble : MonoBehaviour
{
    Renderer rend;
    Material wobblingMaterial;
    Vector3 lastPos;
    Vector3 velocity;
    Vector3 lastRot;
    Vector3 angularVelocity;

    [Header("Wobble Settings")]
    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;

    [Header("Pour Settings")]
    public Transform neckPoint;
    public float pourThreshold = 20f;
    public float pourRate = 0.2f;
    public float minFillLevel = 0.1f;
    public float maxFillLevel = 0.9f;

    [Header("Pour Effects")]
    public ParticleSystem pourParticles;
    public AudioSource pourSound;

    [Header("Chemical Properties")]
    public ChemicalType chemicalType = ChemicalType.Silicates;
    public Color chemicalColor = Color.gray;
    public float chemicalDensity = 1.0f;
    public float chemicalViscosity = 1.0f;
    public bool isFlammable = false;
    public bool isRadioactive = false;

    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float time = 0.5f;

    private float currentFillAmount = 0.8f;
    private bool isPouring = false;
    private Vector3 neckLocalPosition;
    private float lastPourAngle = 0f;
    private bool isEmpty = false;

    private GameObject parentObject;
    private MixingContainer targetContainer;
    private bool materialInitialized = false;
    private bool m_isInsidePourZone = false;

    void Start()
    {
        parentObject = transform.parent != null ? transform.parent.gameObject : gameObject;
        rend = GetComponent<Renderer>();
        wobblingMaterial = new Material(rend.sharedMaterial);
        rend.material = wobblingMaterial;
        materialInitialized = true;
        UpdateChemicalAppearance();

        if (neckPoint != null)
            neckLocalPosition = transform.InverseTransformPoint(neckPoint.position);
        else
            neckLocalPosition = Vector3.up * 0.5f;

        SetFillAmount(currentFillAmount);

        if (pourParticles != null)
        {
            pourParticles.Stop();
            var main = pourParticles.main;
            main.startColor = chemicalColor;
        }

        if (pourSound != null)
            pourSound.Stop();

        // Добавляем PourTriggerRelay на родителя если его нет
        if (parentObject != gameObject)
        {
            if (parentObject.GetComponent<PourTriggerRelay>() == null)
                parentObject.AddComponent<PourTriggerRelay>();
        }
    }

    private void Update()
    {
        if (isEmpty || !materialInitialized) return;

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

        CheckPouringCondition();
        if (isPouring) PourLiquid();
        UpdateFillInShader();
        if (currentFillAmount <= minFillLevel + 0.01f && !isPouring && !isEmpty) EmptyAndDestroy();
    }

    // ─────────────────── Trigger (вызывается из PourTriggerRelay) ───────────────────

    public void OnPourZoneEnter(Collider other)
    {
        MixingContainer container = other.GetComponent<MixingContainer>();
        if (container == null)
            container = other.GetComponentInParent<MixingContainer>();

        if (container != null)
        {
            m_isInsidePourZone = true;
            targetContainer = container;
            Debug.Log($"[Wobble] Вошла в зону контейнера: {other.gameObject.name}");
        }
    }

    public void OnPourZoneExit(Collider other)
    {
        MixingContainer container = other.GetComponent<MixingContainer>();
        if (container == null)
            container = other.GetComponentInParent<MixingContainer>();

        if (container != null)
        {
            m_isInsidePourZone = false;
            targetContainer = null;

            if (isPouring)
                StopPouring();

            Debug.Log($"[Wobble] Вышла из зоны контейнера");
        }
    }

    // ─────────────────── Pour detection ───────────────────

    void CheckPouringCondition()
    {
        if (currentFillAmount <= minFillLevel)
        {
            StopPouring();
            return;
        }

        if (!m_isInsidePourZone)
        {
            if (isPouring)
                StopPouring();
            return;
        }

        float tiltAngle = Vector3.Angle(Vector3.up, transform.up);
        bool shouldPour = tiltAngle > pourThreshold;

        if (shouldPour && !isPouring)
            StartPouring();
        else if (!shouldPour && isPouring)
            StopPouring();

        lastPourAngle = tiltAngle;
    }

    void StartPouring()
    {
        isPouring = true;

        if (pourParticles != null)
        {
            pourParticles.transform.position = transform.TransformPoint(neckLocalPosition);
            pourParticles.Play();
        }

        if (pourSound != null && !pourSound.isPlaying)
            pourSound.Play();

        wobbleAmountToAddX += MaxWobble * 0.5f;
        wobbleAmountToAddZ += MaxWobble * 0.5f;
    }

    void StopPouring()
    {
        isPouring = false;

        if (pourParticles != null)
            pourParticles.Stop();

        if (pourSound != null)
            pourSound.Stop();
    }

    // ─────────────────── Pour logic ───────────────────

    void PourLiquid()
    {
        float viscosityFactor = Mathf.Clamp(1 / chemicalViscosity, 0.1f, 2f);
        float pourIntensity = Mathf.Clamp01((lastPourAngle - pourThreshold) / 90f);
        float pourAmount = pourRate * pourIntensity * viscosityFactor * Time.deltaTime;

        currentFillAmount -= pourAmount;
        currentFillAmount = Mathf.Clamp(currentFillAmount, minFillLevel, maxFillLevel);

        if (targetContainer != null && pourAmount > 0)
        {
            targetContainer.AddChemical(chemicalType, chemicalColor, chemicalDensity, pourAmount);

            if (pourParticles != null)
            {
                var main = pourParticles.main;
                main.startColor = chemicalColor;
            }
        }

        if (pourParticles != null && pourParticles.isPlaying)
        {
            pourParticles.transform.position = transform.TransformPoint(neckLocalPosition);

            var velocityModule = pourParticles.velocityOverLifetime;
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.World;

            float particleSpeed = 2f * pourIntensity * viscosityFactor;
            velocityModule.x = 0f;
            velocityModule.y = -particleSpeed * 3f;
            velocityModule.z = 0f;

            var emission = pourParticles.emission;
            emission.rateOverTime = 50f * pourIntensity * viscosityFactor;
        }

        if (pourSound != null)
        {
            pourSound.volume = 0.3f * pourIntensity * viscosityFactor;
            pourSound.pitch = 0.8f + 0.4f * pourIntensity;
        }
    }

    // ─────────────────── Empty & destroy ───────────────────

    void EmptyAndDestroy()
    {
        isEmpty = true;
        Debug.Log($"[Wobble] Колба опустошена, тип: {chemicalType}");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(FadeOutAndDestroy());
    }

    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        float fadeTime = 1f;
        float elapsed = 0f;
        Color startColor = wobblingMaterial.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Easing.InOut(Mathf.Clamp01(elapsed / fadeTime));
            float alpha = Mathf.Lerp(1f, 0f, t);
            wobblingMaterial.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (ChemicalFlaskManager.Instance != null)
            ChemicalFlaskManager.Instance.OnFlaskDestroyed(parentObject, chemicalType);

        yield return new WaitForSeconds(0.1f);
        Destroy(parentObject);
    }

    // ─────────────────── Visual ───────────────────

    void UpdateChemicalAppearance()
    {
        if (!materialInitialized) return;
        SetShaderColors(wobblingMaterial, chemicalColor);
        if (isRadioactive)
        {
            wobblingMaterial.SetFloat("_Emission", 0.3f);
            wobblingMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            wobblingMaterial.DisableKeyword("_EMISSION");
        }
    }

    void SetShaderColors(Material material, Color color)
    {
        material.SetColor("_SideColor", color);
        material.SetColor("_TopColor", color);
        material.SetColor("_Color", color);
    }

    void UpdateFillInShader()
    {
        if (!materialInitialized) return;
        wobblingMaterial.SetFloat("_Fill", currentFillAmount);
        if (isPouring)
        {
            float pourIntensity = Mathf.Clamp01((lastPourAngle - pourThreshold) / 90f);
            wobbleAmountToAddX += Random.Range(-0.01f, 0.01f) * pourIntensity;
            wobbleAmountToAddZ += Random.Range(-0.01f, 0.01f) * pourIntensity;
        }
    }

    // ─────────────────── Public API ───────────────────

    public void SetFillAmount(float amount)
    {
        currentFillAmount = Mathf.Clamp(amount, minFillLevel, maxFillLevel);
        UpdateFillInShader();
    }

    public float GetFillAmount() => currentFillAmount;
    public bool IsPouring() => isPouring;
    public bool IsInsidePourZone() => m_isInsidePourZone;
    public ChemicalType GetChemicalType() => chemicalType;
    public Color GetChemicalColor() => chemicalColor;

    public void UpdateChemicalProperties(ChemicalType type, Color color, float density, float viscosity)
    {
        chemicalType = type;
        chemicalColor = color;
        chemicalDensity = density;
        chemicalViscosity = viscosity;
        UpdateChemicalAppearance();
    }
}