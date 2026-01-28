using UnityEngine;

/// <summary>
/// Скрипт для сбора вытекшей жидкости из Wobble (колбы)
/// Используется для создания сосудов, которые собирают жидкость при вытекании
/// </summary>
public class LiquidReceiver : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Wobble sourceFlask; // Источник жидкости (колба)
    [SerializeField] private Renderer receiverRenderer; // Рендерер этого сосуда
    private Material receiverMaterial; // Отдельный экземпляр материала для этого приемника
    
    [Header("Параметры наполнения")]
    [SerializeField] private float fillSpeed = 0.2f; // Скорость заполнения (0.1-0.5)
    [SerializeField] private float maxFillLevel = 0.9f; // Максимальный уровень
    [SerializeField] private float currentFillLevel = 0f; // Текущий уровень
    
    [Header("Эффекты")]
    [SerializeField] private ParticleSystem splashParticles; // Частицы при попадании жидкости
    [SerializeField] private AudioSource splashSound; // Звук при попадании
    
    [Header("Проверка попадания")]
    [SerializeField] private bool useRaycast = true; // Использовать raycast для точности
    [SerializeField] private float raycastDistance = 1f; // Расстояние raycast
    
    private Collider receiverCollider;
    private bool isReceivingLiquid = false;

    void Start()
    {
        // Инициализация компонентов
        receiverCollider = GetComponent<Collider>();
        
        if (receiverCollider != null)
            receiverCollider.isTrigger = true;
        
        if (receiverRenderer == null)
            receiverRenderer = GetComponent<Renderer>();
        
        // ВАЖНО: Создаем отдельный экземпляр материала для этого приемника
        // Это нужно, чтобы _Fill параметр был независим от других сосудов
        if (receiverRenderer != null)
        {
            receiverMaterial = receiverRenderer.material;
            Debug.Log($"[LiquidReceiver] Material instance ID: {receiverMaterial.GetInstanceID()} (объект: {gameObject.name})");
        }
        
        // Если используем эффекты, выключаем их
        if (splashParticles != null)
            splashParticles.Stop();
        
        if (splashSound != null)
            splashSound.Stop();
    }

    void OnTriggerStay(Collider other)
    {
        // Проверяем, есть ли у контакта скрипт Wobble (это источник жидкости)
        if (sourceFlask == null)
            return;
        
        // Проверяем, вытекает ли жидкость из колбы
        if (!sourceFlask.IsPouring())
        {
            isReceivingLiquid = false;
            return;
        }
        
        // Если в сосуде уже максимум, не наполняем
        if (currentFillLevel >= maxFillLevel)
        {
            return;
        }
        
        // Проверяем попадание через raycast для точности
        bool shouldReceive = true;
        
        if (useRaycast)
        {
            shouldReceive = CheckPourRaycast(other);
        }
        
        if (shouldReceive)
        {
            ReceiveLiquid();
        }
    }

    void OnTriggerExit(Collider other)
    {
        isReceivingLiquid = false;
    }

    /// <summary>
    /// Проверяет, попадает ли жидкость в приемник используя raycast
    /// </summary>
    bool CheckPourRaycast(Collider other)
    {
        // Берем позицию горлышка колбы (примерно)
        Vector3 pourOrigin = sourceFlask.transform.position + sourceFlask.transform.up * 0.5f;
        Vector3 pourDirection = -sourceFlask.transform.up;
        
        // Проверяем raycast в направлении вытекания
        Ray ray = new Ray(pourOrigin, pourDirection);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            // Если raycast попал в этот коллайдер
            return hit.collider.gameObject == gameObject;
        }
        
        return false;
    }

    /// <summary>
    /// Наполняет сосуд жидкостью
    /// </summary>
    void ReceiveLiquid()
    {
        isReceivingLiquid = true;
        
        // Наполняем сосуд
        currentFillLevel += fillSpeed * Time.deltaTime;
        currentFillLevel = Mathf.Clamp(currentFillLevel, 0f, maxFillLevel);
        
        // Обновляем визуализацию
        UpdateFillVisual();
        
        // Проигрываем эффекты первый раз когда начинается наполнение
        if (currentFillLevel > 0.05f && !splashParticles.isPlaying)
        {
            PlaySplashEffect();
        }
    }

    /// <summary>
    /// Обновляет визуализацию уровня жидкости в шейдере
    /// </summary>
    void UpdateFillVisual()
    {
        if (receiverMaterial != null)
        {
            // Передаем уровень заполнения в ОТДЕЛЬНЫЙ материал этого сосуда
            receiverMaterial.SetFloat("_Fill", currentFillLevel);
        }
    }

    /// <summary>
    /// Воспроизводит эффекты при попадании жидкости
    /// </summary>
    void PlaySplashEffect()
    {
        // Частицы
        if (splashParticles != null)
        {
            splashParticles.transform.position = transform.position + Vector3.up * 0.3f;
            splashParticles.Play();
        }
        
        // Звук
        if (splashSound != null && !splashSound.isPlaying)
        {
            splashSound.volume = 0.3f;
            splashSound.Play();
        }
    }

    /// <summary>
    /// Опустошает сосуд (например, для повторного использования)
    /// </summary>
    public void EmptyReceiver()
    {
        currentFillLevel = 0f;
        UpdateFillVisual();
    }

    /// <summary>
    /// Получает текущий уровень заполнения
    /// </summary>
    public float GetFillLevel()
    {
        return currentFillLevel;
    }

    /// <summary>
    /// Проверяет, полный ли сосуд
    /// </summary>
    public bool IsFull()
    {
        return currentFillLevel >= maxFillLevel;
    }

    /// <summary>
    /// Проверяет, получает ли сосуд жидкость в данный момент
    /// </summary>
    public bool IsReceivingLiquid()
    {
        return isReceivingLiquid;
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        // Показываем границы триггера
        Gizmos.color = Color.cyan;
        if (receiverCollider is BoxCollider)
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
        
        // Показываем направление raycast при проверке
        if (Application.isPlaying && sourceFlask != null)
        {
            Vector3 pourOrigin = sourceFlask.transform.position + sourceFlask.transform.up * 0.5f;
            Vector3 pourDirection = -sourceFlask.transform.up * raycastDistance;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pourOrigin, pourDirection);
            
            // Показываем текущий уровень заполнения
            Gizmos.color = Color.blue;
            Vector3 fillIndicator = transform.position + Vector3.up * (currentFillLevel * 0.3f);
            Gizmos.DrawWireSphere(fillIndicator, 0.02f);
        }
    }
}
