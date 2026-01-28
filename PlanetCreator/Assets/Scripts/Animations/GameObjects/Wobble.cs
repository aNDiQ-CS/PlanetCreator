using UnityEngine;

public class Wobble : MonoBehaviour
{
    Renderer rend;
    Material wobblingMaterial; // Отдельный экземпляр материала для этого объекта
    Vector3 lastPos;
    Vector3 velocity;
    Vector3 lastRot;  
    Vector3 angularVelocity;
    
    [Header("Wobble Settings")]
    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;
    
    [Header("Pour Settings")]
    public Transform neckPoint; // Точка горлышка колбы
    public float pourThreshold = 20f; // Минимальный угол для начала вытекания
    public float pourRate = 0.2f; // Скорость вытекания
    public float minFillLevel = 0.1f; // Минимальный уровень заполнения
    public float maxFillLevel = 0.9f; // Максимальный уровень заполнения
    
    [Header("Pour Effects")]
    public ParticleSystem pourParticles;
    public AudioSource pourSound;
    
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float time = 0.5f;
    
    private float currentFillAmount = 0.8f; // Начальный уровень заполнения
    private bool isPouring = false;
    private Vector3 neckLocalPosition; // Локальная позиция горлышка
    private float lastPourAngle = 0f;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        
        // ВАЖНО: Создаем отдельный экземпляр материала для этого объекта
        // Это нужно, чтобы изменения _Fill параметра на одной колбе
        // не влияли на другие колбы, использующие тот же шейдер
        wobblingMaterial = rend.material; // Это автоматически создает копию материала
        Debug.Log($"[Wobble] Material instance ID: {wobblingMaterial.GetInstanceID()} (объект: {gameObject.name})");
        
        // Сохраняем локальную позицию горлышка относительно колбы
        if (neckPoint != null)
        {
            neckLocalPosition = transform.InverseTransformPoint(neckPoint.position);
        }
        else
        {
            // Если горлышко не задано, используем верхнюю точку по умолчанию
            neckLocalPosition = Vector3.up * 0.5f;
        }
        
        // Устанавливаем начальный уровень жидкости
        SetFillAmount(currentFillAmount);
        
        // Выключаем эффекты вытекания, если они есть
        if (pourParticles != null)
            pourParticles.Stop();
            
        if (pourSound != null)
            pourSound.Stop();
    }
    
    private void Update()
    {
        time += Time.deltaTime;
        
        // Обработка колебаний (ваш существующий код)
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
        
        // Проверка условий для вытекания
        CheckPouringCondition();
        
        // Обработка вытекания
        if (isPouring)
        {
            PourLiquid();
        }
        
        // Обновляем уровень жидкости в шейдере
        UpdateFillInShader();
    }
    
    void CheckPouringCondition()
    {
        if (currentFillAmount <= minFillLevel) 
        {
            StopPouring();
            return;
        }
        
        // Рассчитываем угол наклона колбы
        float tiltAngle = Vector3.Angle(Vector3.up, transform.up);
        
        // Определяем, направлено ли горлышко вниз
        Vector3 neckWorldPos = transform.TransformPoint(neckLocalPosition);
        Vector3 neckDirection = (neckWorldPos - transform.position).normalized;
        
        // Угол между направлением горлышка и вертикалью вниз
        float neckAngle = Vector3.Angle(neckDirection, -Vector3.up);
        
        // Уровень жидкости в мировых координатах (примерная высота)
        float liquidWorldHeight = CalculateLiquidWorldHeight();
        
        // Высота горлышка в мировых координатах
        float neckWorldHeight = neckWorldPos.y;
        
        // Вытекание происходит, если:
        // 1. Колба наклонена достаточно сильно
        // 2. Уровень жидкости выше горлышка
        // 3. Горлышко направлено вниз
        bool shouldPour = tiltAngle > pourThreshold && 
                         liquidWorldHeight > neckWorldHeight && 
                         neckAngle < 70f; // Горлышко не смотрит вверх
        
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
        
        // Запускаем эффекты
        if (pourParticles != null)
        {
            pourParticles.transform.position = transform.TransformPoint(neckLocalPosition);
            pourParticles.Play();
        }
        
        if (pourSound != null && !pourSound.isPlaying)
        {
            pourSound.Play();
        }
        
        // Увеличиваем "беспокойство" жидкости при начале вытекания
        wobbleAmountToAddX += MaxWobble * 0.5f;
        wobbleAmountToAddZ += MaxWobble * 0.5f;
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
    }
    
    void PourLiquid()
    {
        // Скорость вытекания зависит от угла наклона
        float pourIntensity = Mathf.Clamp01((lastPourAngle - pourThreshold) / 90f);
        float pourAmount = pourRate * pourIntensity * Time.deltaTime;
        
        // Уменьшаем уровень жидкости
        currentFillAmount -= pourAmount;
        currentFillAmount = Mathf.Clamp(currentFillAmount, minFillLevel, maxFillLevel);
        
        // Обновляем направление частиц
        if (pourParticles != null && pourParticles.isPlaying)
        {
            // Направляем частицы вниз от горлышка
            Vector3 pourDirection = -transform.up * 0.7f + -transform.forward * 0.3f;
            var velocityModule = pourParticles.velocityOverLifetime;
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.World;
            
            // Меняем скорость частиц в зависимости от интенсивности вытекания
            float particleSpeed = 2f * pourIntensity;
            velocityModule.x = pourDirection.x * particleSpeed;
            velocityModule.y = pourDirection.y * particleSpeed * 3f;
            velocityModule.z = pourDirection.z * particleSpeed;
            
            // Меняем rate в зависимости от интенсивности
            var emission = pourParticles.emission;
            emission.rateOverTime = 50f * pourIntensity;
        }
        
        // Меняем громкость звука в зависимости от интенсивности
        if (pourSound != null)
        {
            pourSound.volume = 0.3f * pourIntensity;
        }
    }
    
    float CalculateLiquidWorldHeight()
    {
        // Преобразуем уровень заполнения в мировую высоту
        // Это упрощенный расчет - можно настроить под вашу модель колбы
        float fillNormalized = (currentFillAmount - minFillLevel) / (maxFillLevel - minFillLevel);
        
        // Берем размер рендерера для расчета высоты
        Bounds bounds = rend.bounds;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;
        
        // Высота жидкости = нижняя граница + (уровень заполнения * высота колбы)
        return minY + (fillNormalized * (maxY - minY));
    }
    
    void UpdateFillInShader()
    {
        // Обновляем параметр _Fill в шейдере ОТДЕЛЬНОГО материала
        wobblingMaterial.SetFloat("_Fill", currentFillAmount);
        
        // Увеличиваем колебания при вытекании
        if (isPouring)
        {
            // Добавляем дополнительное "беспокойство" жидкости
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
    
    // Визуализация для отладки
    void OnDrawGizmosSelected()
    {
        if (neckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(neckPoint.position, 0.01f);
            Gizmos.DrawWireSphere(neckPoint.position, 0.02f);
            
            // Рисуем направление горлышка
            Gizmos.color = Color.yellow;
            Vector3 neckWorldPos = transform.TransformPoint(neckLocalPosition);
            Gizmos.DrawLine(neckWorldPos, neckWorldPos - Vector3.up * 0.2f);
            
            // Показываем уровень жидкости
            if (Application.isPlaying)
            {
                float liquidHeight = CalculateLiquidWorldHeight();
                Vector3 liquidPos = new Vector3(transform.position.x, liquidHeight, transform.position.z);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(liquidPos, 0.05f);
                
                // Линия от уровня жидкости до горлышка
                Gizmos.DrawLine(liquidPos, neckWorldPos);
            }
        }
    }
}