using UnityEngine;
using Planets;
using System.Collections.Generic;

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

    [Header("Container Detection")]
    public float raycastDistance = 2f;
    public float detectionRadius = 0.2f;
    public LayerMask containerLayerMask = -1;

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
    private Vector3 lastPourDirection;
    private bool materialInitialized = false;

    void Start()
    {
        parentObject = this.transform.parent.gameObject;
        rend = GetComponent<Renderer>();
        wobblingMaterial = new Material(rend.sharedMaterial);
        rend.material = wobblingMaterial;
        materialInitialized = true;
        UpdateChemicalAppearance();
        if (neckPoint != null)
        {
            neckLocalPosition = transform.InverseTransformPoint(neckPoint.position);
        }
        else
        {
            neckLocalPosition = Vector3.up * 0.5f;
        }
        SetFillAmount(currentFillAmount);
        if (pourParticles != null)
        {
            pourParticles.Stop();
            var main = pourParticles.main;
            main.startColor = chemicalColor;
        }
        if (pourSound != null)
            pourSound.Stop();
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

    void CheckPouringCondition()
    {
        if (currentFillAmount <= minFillLevel)
        {
            StopPouring();
            return;
        }

        float tiltAngle = Vector3.Angle(Vector3.up, transform.up);
        Vector3 neckWorldPos = transform.TransformPoint(neckLocalPosition);
        Vector3 neckDirection = (neckWorldPos - transform.position).normalized;
        float neckAngle = Vector3.Angle(neckDirection, -Vector3.up);
        float liquidWorldHeight = CalculateLiquidWorldHeight();
        float neckWorldHeight = neckWorldPos.y;

        bool shouldPour = tiltAngle > pourThreshold &&
                         liquidWorldHeight > neckWorldHeight &&
                         neckAngle < 70f;

        if (shouldPour && !isPouring)
        {
            StartPouring();
        }
        else if (!shouldPour && isPouring)
        {
            StopPouring();
        }

        lastPourAngle = tiltAngle;
    }

    void StartPouring()
    {
        isPouring = true;
        CalculatePourDirection();
        FindTargetContainer();

        if (pourParticles != null)
        {
            pourParticles.transform.position = transform.TransformPoint(neckLocalPosition);
            pourParticles.Play();
        }

        if (pourSound != null && !pourSound.isPlaying)
        {
            pourSound.Play();
        }

        wobbleAmountToAddX += MaxWobble * 0.5f;
        wobbleAmountToAddZ += MaxWobble * 0.5f;
    }

    void CalculatePourDirection()
    {
        Vector3 neckWorldPos = transform.TransformPoint(neckLocalPosition);
        Vector3 baseDirection = -transform.up;
        Vector3 tiltDirection = -transform.forward * 0.3f;
        lastPourDirection = (baseDirection + tiltDirection).normalized;
    }

    void FindTargetContainer()
    {
        if (neckPoint == null) return;
        Vector3 neckWorldPos = transform.TransformPoint(neckLocalPosition);

        RaycastHit hit;
        if (Physics.Raycast(neckWorldPos, lastPourDirection, out hit, raycastDistance, containerLayerMask))
        {
            MixingContainer container = hit.collider.GetComponent<MixingContainer>();
            if (container != null)
            {
                targetContainer = container;
                return;
            }
        }

        Collider[] colliders = Physics.OverlapSphere(neckWorldPos, detectionRadius, containerLayerMask);
        foreach (Collider col in colliders)
        {
            MixingContainer container = col.GetComponent<MixingContainer>();
            if (container != null)
            {
                targetContainer = container;
                return;
            }
        }

        if (Physics.Raycast(neckWorldPos, Vector3.down, out hit, raycastDistance, containerLayerMask))
        {
            MixingContainer container = hit.collider.GetComponent<MixingContainer>();
            if (container != null)
            {
                targetContainer = container;
                return;
            }
        }

        targetContainer = null;
    }

    void StopPouring()
    {
        isPouring = false;

        if (pourParticles != null)
        {
            pourParticles.Stop();
        }

        if (pourSound != null)
        {
            pourSound.Stop();
        }

        targetContainer = null;
    }

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
            var velocityModule = pourParticles.velocityOverLifetime;
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.World;

            float particleSpeed = 2f * pourIntensity * viscosityFactor;
            velocityModule.x = lastPourDirection.x * particleSpeed;
            velocityModule.y = lastPourDirection.y * particleSpeed * 3f;
            velocityModule.z = lastPourDirection.z * particleSpeed;

            var emission = pourParticles.emission;
            emission.rateOverTime = 50f * pourIntensity * viscosityFactor;
        }

        if (pourSound != null)
        {
            pourSound.volume = 0.3f * pourIntensity * viscosityFactor;
            pourSound.pitch = 0.8f + 0.4f * pourIntensity;
        }
    }

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
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            wobblingMaterial.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        if (ChemicalFlaskManager.Instance != null)
        {
            ChemicalFlaskManager.Instance.OnFlaskDestroyed(parentObject, chemicalType);
        }
        yield return new WaitForSeconds(0.1f);
        Destroy(parentObject);
    }

    void UpdateChemicalAppearance()
    {
        if (!materialInitialized) return;
        SetShaderColors(wobblingMaterial, chemicalColor);
        if (isRadioactive)
        {
            wobblingMaterial.SetFloat("_Emission", 0.3f);
            wobblingMaterial.EnableKeyword("_EMISSION");
        }
        else  wobblingMaterial.DisableKeyword("_EMISSION");
    }

    void SetShaderColors(Material material, Color color)
    {
        material.SetColor("_SideColor", color);
        material.SetColor("_TopColor", color);
        material.SetColor("_Color", color);
    }

    float CalculateLiquidWorldHeight()
    {
        float fillNormalized = (currentFillAmount - minFillLevel) / (maxFillLevel - minFillLevel);
        Bounds bounds = rend.bounds;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;
        return minY + (fillNormalized * (maxY - minY));
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

    public void SetFillAmount(float amount)
    {
        currentFillAmount = Mathf.Clamp(amount, minFillLevel, maxFillLevel);
        UpdateFillInShader();
    }

    public float GetFillAmount()
    {
        return currentFillAmount;
    }

    public bool IsPouring()
    {
        return isPouring;
    }

    public ChemicalType GetChemicalType()
    {
        return chemicalType;
    }

    public Color GetChemicalColor()
    {
        return chemicalColor;
    }

    public void UpdateChemicalProperties(ChemicalType type, Color color, float density, float viscosity)
    {
        chemicalType = type;
        chemicalColor = color;
        chemicalDensity = density;
        chemicalViscosity = viscosity;
        UpdateChemicalAppearance();
    }

    void OnDrawGizmosSelected()
    {
        if (neckPoint != null)
        {
            Vector3 neckWorldPos = transform.TransformPoint(neckLocalPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(neckPoint.position, 0.01f);
            Gizmos.DrawWireSphere(neckPoint.position, 0.02f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(neckWorldPos, neckWorldPos + lastPourDirection * raycastDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(neckWorldPos, detectionRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(neckWorldPos, neckWorldPos + Vector3.down * raycastDistance);

            if (Application.isPlaying)
            {
                float liquidHeight = CalculateLiquidWorldHeight();
                Vector3 liquidPos = new Vector3(transform.position.x, liquidHeight, transform.position.z);
                Gizmos.color = chemicalColor;
                Gizmos.DrawWireSphere(liquidPos, 0.05f);
                Gizmos.DrawLine(liquidPos, neckWorldPos);
            }
        }
    }
}