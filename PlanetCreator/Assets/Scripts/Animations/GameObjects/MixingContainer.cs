
using UnityEngine;
using Planets;
using System.Collections.Generic;
using System.Text;

public class MixingContainer : MonoBehaviour
{
    [System.Serializable]
    public class ChemicalLayer
    {
        public ChemicalType type;
        public float density;
        public float volume;
        public Color color;
    }

    [Header("Container Settings")]
    public float maxVolume = 10f;
    public float currentVolume = 0f;
    public float fillSpeed = 0.5f;

    [Header("Liquid Object")]
    public GameObject liquidObject;
    public Color liquidColor = Color.black;
    public float minFillLevel = 0.1f;
    public float maxFillLevel = 0.9f;

    [Header("Visual Effects")]
    public ParticleSystem mixingParticles;
    public AudioSource mixingSound;
    public Light mixingLight;

    [Header("Color Mixing")]
    public ColorMixMode colorMixMode = ColorMixMode.Weighted;
    public float colorMixSpeed = 3f;

    [Header("Debug")]
    public bool debugLogContents = true;
    public bool showGizmoInfo = true;

    private List<ChemicalLayer> chemicalLayers = new List<ChemicalLayer>();
    private Renderer liquidRenderer;
    private Material liquidMaterial;
    private float targetFillAmount = 0f;
    private float currentFillAmount = 0f;
    private bool isFilling = false;
    private Color targetColor;
    private bool isColorMixing = false;
    private bool materialInitialized = false;

    public enum ColorMixMode
    {
        Average,
        Weighted,
        Additive,
        LastAdded
    }

    void Start()
    {
        InitializeLiquidMaterial();
        targetColor = liquidColor;

        if (materialInitialized) UpdateLiquidAppearance();
    }

    void InitializeLiquidMaterial()
    {
        if (liquidObject != null)
        {
            liquidRenderer = liquidObject.GetComponent<Renderer>();
            if (liquidRenderer != null)
            {
                liquidMaterial = new Material(liquidRenderer.sharedMaterial);
                liquidRenderer.material = liquidMaterial;

                SetShaderColors(liquidMaterial, liquidColor);
                liquidMaterial.SetFloat("_Fill", currentFillAmount);
                liquidMaterial.SetFloat("_WobbleX", 0);
                liquidMaterial.SetFloat("_WobbleZ", 0);

                Debug.Log($"[MixingContainer] Создан новый материал с ID: {liquidMaterial.GetInstanceID()}");
                materialInitialized = true;
            }
        }
    }

    void Update()
    {
        if (!materialInitialized) return;

        if (isFilling && currentFillAmount < targetFillAmount)
        {
            currentFillAmount = Mathf.MoveTowards(currentFillAmount, targetFillAmount, fillSpeed * Time.deltaTime);
            UpdateLiquidFill();
        }
        else if (isFilling && currentFillAmount >= targetFillAmount)
        {
            isFilling = false;
        }

        if (isColorMixing)
        {
            liquidColor = Color.Lerp(liquidColor, targetColor, colorMixSpeed * Time.deltaTime);
            UpdateLiquidAppearance();

            if (ColorDifference(liquidColor, targetColor) < 0.01f)
            {
                liquidColor = targetColor;
                isColorMixing = false;
            }
        }
    }

    public void AddChemical(ChemicalType type, Color color, float density, float volume)
    {
        if (!materialInitialized) return;

        Debug.Log($"[MixingContainer] Добавление: {type}, объем: {volume}, цвет: {color}");

        if (currentVolume + volume > maxVolume)
        {
            volume = maxVolume - currentVolume;
            if (volume <= 0)
            {
                LogContainerContents("Попытка добавления в полный контейнер");
                return;
            }
        }

        bool merged = false;
        int mergeIndex = -1;

        for (int i = 0; i < chemicalLayers.Count; i++)
        {
            if (chemicalLayers[i].type == type &&
                Mathf.Abs(chemicalLayers[i].density - density) < 0.1f)
            {
                chemicalLayers[i].color = MixColors(chemicalLayers[i].color, color,
                    chemicalLayers[i].volume, volume);
                chemicalLayers[i].volume += volume;
                merged = true;
                mergeIndex = i;
                break;
            }
        }

        if (!merged)
        {
            ChemicalLayer newLayer = new ChemicalLayer
            {
                type = type,
                density = density,
                volume = volume,
                color = color
            };
            chemicalLayers.Add(newLayer);

            Debug.Log($"[MixingContainer] Создан новый слой: {type}");
        }
        else
        {
            Debug.Log($"[MixingContainer] Объединен слой {mergeIndex}: {type}");
        }

        currentVolume += volume;

        CalculateMixedColor();
        isColorMixing = true;

        targetFillAmount = Mathf.Lerp(minFillLevel, maxFillLevel, currentVolume / maxVolume);
        isFilling = true;

        UpdateLiquidAppearance();
        PlayMixingEffects();
        LogContainerContents($"После добавления {type}");
        CheckSpecialCombinations();
    }

    void CalculateMixedColor()
    {
        if (chemicalLayers.Count == 0)
        {
            targetColor = Color.black;
            return;
        }

        switch (colorMixMode)
        {
            case ColorMixMode.Average:
                targetColor = CalculateAverageColor();
                break;

            case ColorMixMode.Weighted:
                targetColor = CalculateWeightedColor();
                break;

            case ColorMixMode.Additive:
                targetColor = CalculateAdditiveColor();
                break;

            case ColorMixMode.LastAdded:
                targetColor = chemicalLayers[chemicalLayers.Count - 1].color;
                break;
        }
    }

    Color CalculateAverageColor()
    {
        Color sumColor = Color.black;

        foreach (var layer in chemicalLayers)
        {
            sumColor += layer.color;
        }

        return sumColor / chemicalLayers.Count;
    }

    Color CalculateWeightedColor()
    {
        Color sumColor = Color.black;
        float totalWeight = 0f;

        foreach (var layer in chemicalLayers)
        {
            float weight = layer.volume;
            sumColor += layer.color * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? sumColor / totalWeight : Color.black;
    }

    Color CalculateAdditiveColor()
    {
        Color additiveColor = Color.black;

        foreach (var layer in chemicalLayers)
        {
            additiveColor.r = Mathf.Clamp01(additiveColor.r + layer.color.r);
            additiveColor.g = Mathf.Clamp01(additiveColor.g + layer.color.g);
            additiveColor.b = Mathf.Clamp01(additiveColor.b + layer.color.b);
            additiveColor.a = Mathf.Clamp01(additiveColor.a + layer.color.a);
        }

        return additiveColor;
    }

    Color MixColors(Color color1, Color color2, float weight1, float weight2)
    {
        float totalWeight = weight1 + weight2;
        if (totalWeight <= 0) return color1;

        return (color1 * weight1 + color2 * weight2) / totalWeight;
    }

    float ColorDifference(Color c1, Color c2)
    {
        return Mathf.Abs(c1.r - c2.r) +
               Mathf.Abs(c1.g - c2.g) +
               Mathf.Abs(c1.b - c2.b);
    }

    void UpdateLiquidAppearance()
    {
        if (liquidMaterial != null)
        {
            SetShaderColors(liquidMaterial, liquidColor);

            if (HasRadioactiveMaterials())
            {
                liquidMaterial.EnableKeyword("_EMISSION");
                liquidMaterial.SetColor("_EmissionColor", Color.green * 0.5f);
            }
            else
            {
                liquidMaterial.DisableKeyword("_EMISSION");
            }
        }
    }

    void SetShaderColors(Material material, Color color)
    {
        material.SetColor("_SideColor", color);
        material.SetColor("_TopColor", color);
        material.SetColor("_Color", color);
    }

    void UpdateLiquidFill()
    {
        if (liquidMaterial != null)
        {
            liquidMaterial.SetFloat("_Fill", currentFillAmount);

            if (isFilling)
            {
                float wobble = Mathf.Sin(Time.time * 3f) * 0.02f;
                liquidMaterial.SetFloat("_WobbleX", wobble);
                liquidMaterial.SetFloat("_WobbleZ", wobble);
            }
            else
            {
                liquidMaterial.SetFloat("_WobbleX", 0);
                liquidMaterial.SetFloat("_WobbleZ", 0);
            }
        }
    }

    bool HasRadioactiveMaterials()
    {
        foreach (var layer in chemicalLayers)
        {
            if (layer.type == ChemicalType.Radioactive)
                return true;
        }
        return false;
    }

    void PlayMixingEffects()
    {
        if (mixingParticles != null)
        {
            mixingParticles.Play();
            var main = mixingParticles.main;
            main.startColor = targetColor;
        }

        if (mixingSound != null && !mixingSound.isPlaying)
        {
            mixingSound.Play();
        }

        if (mixingLight != null)
        {
            mixingLight.enabled = true;
            mixingLight.color = targetColor;
            mixingLight.intensity = 1f + chemicalLayers.Count * 0.2f;
            Invoke(nameof(TurnOffLight), 0.5f);
        }
    }

    void TurnOffLight()
    {
        if (mixingLight != null)
            mixingLight.enabled = false;
    }

    void CheckSpecialCombinations()
    {
        int metalCount = 0;
        int silicateCount = 0;
        int radioactiveCount = 0;
        int volatilesCount = 0;
        int lightCount = 0;

        foreach (var layer in chemicalLayers)
        {
            switch (layer.type)
            {
                case ChemicalType.Metal: metalCount++; break;
                case ChemicalType.Silicates: silicateCount++; break;
                case ChemicalType.Radioactive: radioactiveCount++; break;
                case ChemicalType.Volatiles: volatilesCount++; break;
                case ChemicalType.Light: lightCount++; break;
            }
        }

        if (metalCount > 0 && silicateCount > 0 && chemicalLayers.Count >= 2)
        {
            Debug.Log("[MixingContainer] Обнаружена комбинация: Металл + Силикаты = Планетарное ядро");
        }

        if (radioactiveCount > 0)
        {
            Debug.Log("[MixingContainer] присутствуют радиоактивные материалы");
        }

        if (volatilesCount > 0 && lightCount > 0)
        {
            Debug.Log("[MixingContainer] Обнаружена комбинация: Летучие вещества + Легкие элементы = Атмосфера");
        }
    }

    public void LogContainerContents(string context = "Текущее состояние")
    {
        if (!debugLogContents) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"═══════════════════════════════════════════════");
        sb.AppendLine($"[MixingContainer] {context}");
        sb.AppendLine($"═══════════════════════════════════════════════");
        sb.AppendLine($"Общий объем: {currentVolume:F2} / {maxVolume:F2}");
        sb.AppendLine($"Заполнение: {currentVolume / maxVolume * 100:F1}%");
        sb.AppendLine($"Текущий цвет: R:{liquidColor.r:F2}, G:{liquidColor.g:F2}, B:{liquidColor.b:F2}");
        sb.AppendLine($"Целевой цвет: R:{targetColor.r:F2}, G:{targetColor.g:F2}, B:{targetColor.b:F2}");
        sb.AppendLine($"Количество слоев: {chemicalLayers.Count}");
        sb.AppendLine($"───────────────────────────────────────────────");

        if (chemicalLayers.Count == 0)
        {
            sb.AppendLine("Контейнер пуст");
        }
        else
        {
            sb.AppendLine("Содержимое по слоям:");
            for (int i = 0; i < chemicalLayers.Count; i++)
            {
                var layer = chemicalLayers[i];
                string colorStr = $"(R:{layer.color.r:F2}, G:{layer.color.g:F2}, B:{layer.color.b:F2})";
                string densityStr = $"(ρ:{layer.density:F2})";
                string volumePercent = $"({layer.volume / currentVolume * 100:F1}%)";

                sb.AppendLine($"  [{i}] {GetChemicalName(layer.type)}: {layer.volume:F2} ед. {volumePercent} {densityStr} {colorStr}");
            }
        }

        sb.AppendLine($"═══════════════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    string GetChemicalName(ChemicalType type)
    {
        switch (type)
        {
            case ChemicalType.Silicates: return "Силикаты";
            case ChemicalType.Metal: return "Металл";
            case ChemicalType.Volatiles: return "Летучие вещества";
            case ChemicalType.Radioactive: return "Радиоактивные";
            case ChemicalType.Light: return "Легкие элементы";
            default: return "Неизвестно";
        }
    }

    public void ClearContainer()
    {
        Debug.Log("[MixingContainer] Контейнер очищен");

        chemicalLayers.Clear();
        currentVolume = 0f;
        currentFillAmount = 0f;
        targetFillAmount = 0f;
        liquidColor = Color.black;
        targetColor = Color.black;
        isColorMixing = false;

        if (materialInitialized)
        {
            UpdateLiquidAppearance();
            UpdateLiquidFill();
        }

        LogContainerContents("После очистки");
    }

    public List<ChemicalType> GetChemicalMixture()
    {
        List<ChemicalType> mixture = new List<ChemicalType>();
        foreach (var layer in chemicalLayers)
        {
            mixture.Add(layer.type);
        }
        return mixture;
    }

    public Dictionary<ChemicalType, float> GetChemicalComposition()
    {
        Dictionary<ChemicalType, float> composition = new Dictionary<ChemicalType, float>();

        foreach (var layer in chemicalLayers)
        {
            if (composition.ContainsKey(layer.type))
            {
                composition[layer.type] += layer.volume;
            }
            else
            {
                composition[layer.type] = layer.volume;
            }
        }

        return composition;
    }

    public Color GetCurrentColor()
    {
        return liquidColor;
    }

    public void SetColorMixMode(ColorMixMode mode)
    {
        colorMixMode = mode;
        CalculateMixedColor();
        isColorMixing = true;
    }

    [ContextMenu("Показать содержимое")]
    void ShowContents()
    {
        LogContainerContents("Запрошено вручную");
    }

    [ContextMenu("Очистить контейнер")]
    void ClearContainerContext()
    {
        ClearContainer();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmoInfo) return;

        if (liquidObject != null && liquidRenderer != null)
        {
            Gizmos.color = liquidColor;
            Bounds bounds = liquidRenderer.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            float fillHeight = Mathf.Lerp(bounds.min.y, bounds.max.y, currentFillAmount);
            Vector3 fillPos = new Vector3(bounds.center.x, fillHeight, bounds.center.z);
            Gizmos.DrawSphere(fillPos, 0.05f);
#if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 10;
            style.alignment = TextAnchor.MiddleCenter;

            Vector3 labelPos = transform.position + Vector3.up * 1.5f;
            string info = $"Объем: {currentVolume:F1}/{maxVolume:F1}\n" +
                         $"Слоев: {chemicalLayers.Count}\n" +
                         $"Заполнение: {(currentFillAmount - minFillLevel) / (maxFillLevel - minFillLevel) * 100:F0}%";

            UnityEditor.Handles.Label(labelPos, info, style);
#endif
        }
    }

    void OnGUI()
    {
        if (!debugLogContents) return;

        if (showGizmoInfo && Application.isPlaying)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);

            if (screenPos.z > 0)
            {
                GUI.color = Color.white;
                Rect rect = new Rect(screenPos.x - 100, Screen.height - screenPos.y, 200, 80);

                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.textColor = Color.white;
                boxStyle.fontSize = 10;
                boxStyle.alignment = TextAnchor.MiddleCenter;

                string contentInfo = $"Контейнер: {gameObject.name}\n" +
                                   $"Объем: {currentVolume:F1}/{maxVolume:F1}\n" +
                                   $"Слоев: {chemicalLayers.Count}\n" +
                                   $"Цвет: R:{liquidColor.r:F2} G:{liquidColor.g:F2} B:{liquidColor.b:F2}";

                GUI.Box(rect, contentInfo, boxStyle);
            }
        }
    }
}