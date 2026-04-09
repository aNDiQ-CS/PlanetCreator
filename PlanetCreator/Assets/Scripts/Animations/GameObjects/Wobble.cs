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
    private Vector3 neckLocalPosition;
    private bool isEmpty = false;

    private GameObject parentObject;
    private bool materialInitialized = false;

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

        UpdateFillInShader();
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
    }

    // ─────────────────── Public API ───────────────────

    public void SetFillAmount(float amount)
    {
        currentFillAmount = Mathf.Clamp(amount, minFillLevel, maxFillLevel);
        UpdateFillInShader();
    }

    public float GetFillAmount() => currentFillAmount;
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