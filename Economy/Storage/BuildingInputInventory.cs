using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Входной" инвентарь (буфер сырья) для производственного здания.
/// Теперь он также отвечает за создание "Запросов" на доставку.
/// </summary>
[RequireComponent(typeof(BuildingIdentity))] // Нужен для получения координат
public class BuildingInputInventory : MonoBehaviour
{
    [Tooltip("Список требуемого сырья и его вместимость (настраивается в Инспекторе)")]
    public List<StorageData> requiredResources;
    
    [Header("Логистика Запросов")]
    [Tooltip("Приоритет доставки (1-5). Тележки выберут того, у кого '5'")]
    [Range(1, 5)] public int priority = 3;
    
    [Tooltip("Создать 'Запрос', когда склад опустеет до этого % (0.0 - 1.0)")]
    [Range(0f, 1f)] public float requestThresholdPercent = 0.25f; // 25%

    [Tooltip("Снять 'Запрос', когда склад заполнится до этого % (0.0 - 1.0)")]
    [Range(0f, 1f)] public float fulfillThresholdPercent = 0.8f; // 80%

    // --- Состояние запросов ---
    // Нам нужен словарь, т.к. у нас может быть несколько "входных" ресурсов
    // (например, Железо и Уголь)
    private Dictionary<ResourceType, ResourceRequest> _activeRequests = new Dictionary<ResourceType, ResourceRequest>();
    
    private BuildingIdentity _identity;
    private LogisticsManager _logistics;
    public bool IsRequesting { get; private set; } = false;

    private void Start()
    {
        _identity = GetComponent<BuildingIdentity>();
        _logistics = LogisticsManager.Instance;
        
        if (_logistics == null)
        {
            Debug.LogError($"[InputInv] {gameObject.name} не нашел LogisticsManager.Instance!");
        }
    }

    private void Update()
    {
        if (_logistics == null) return;
        
        // Проверяем КАЖДЫЙ слот сырья
        foreach (var slot in requiredResources)
        {
            bool isRequestActive = _activeRequests.ContainsKey(slot.resourceType);
            float fillRatio = slot.currentAmount / slot.maxAmount;

            // 1. ЛОГИКА СОЗДАНИЯ ЗАПРОСА
            if (!isRequestActive && fillRatio <= requestThresholdPercent)
            {
                CreateRequest(slot);
            }
            // 2. ЛОГИКА ОТМЕНЫ ЗАПРОСА
            else if (isRequestActive && fillRatio >= fulfillThresholdPercent)
            {
                FulfillRequest(slot);
            }
        }
    }

    private void CreateRequest(StorageData slot)
    {
        // Создаем новый "бланк заказа"
        var newRequest = new ResourceRequest(
            this, 
            slot.resourceType, 
            priority,
            _identity.rootGridPosition // Тележка поедет к "корню" нашего здания
        );
        
        // "Вешаем" на доску
        _logistics.CreateRequest(newRequest);
        _activeRequests[slot.resourceType] = newRequest; // Запоминаем, что мы его создали
        UpdateIsRequesting();
    }

    private void FulfillRequest(StorageData slot)
    {
        if (_activeRequests.TryGetValue(slot.resourceType, out ResourceRequest request))
        {
            // "Снимаем" с доски
            _logistics.FulfillRequest(request);
            _activeRequests.Remove(slot.resourceType); // Забываем о нем
            UpdateIsRequesting();
        }
    }


    /// <summary>
    /// Проверяет, достаточно ли сырья для ОДНОГО цикла производства.
    /// (Этот код из 1.0, без изменений)
    /// </summary>
    public bool HasEnoughResources()
    {
        // TODO: Заменить '1f' на 'costPerCycle' из ResourceProducer
        const float costPerCycle = 1f; 

        foreach (var slot in requiredResources)
        {
            if (slot.currentAmount < costPerCycle)
            {
                return false; 
            }
        }
        return true;
    }

    /// <summary>
    /// "Съедает" ресурсы за ОДИН цикл производства.
    /// (Этот код из 1.0, без изменений)
    /// </summary>
    public void ConsumeResources()
    {
        // TODO: Заменить '1f' на 'costPerCycle' из ResourceProducer
        const float costPerCycle = 1f;

        foreach (var slot in requiredResources)
        {
            slot.currentAmount -= costPerCycle;
        }
    }

    /// <summary>
    /// Добавляет сырье, привезенное тележкой.
    /// (Этот код из 1.0, но теперь он также вызывает 'FulfillRequest' через Update)
    /// </summary>
    public float AddResource(ResourceType type, float amount)
    {
        foreach (var slot in requiredResources)
        {
            if (slot.resourceType == type)
            {
                float spaceAvailable = slot.maxAmount - slot.currentAmount;
                if (spaceAvailable <= 0) return 0;

                float amountToAdd = Mathf.Min(amount, spaceAvailable);
                slot.currentAmount += amountToAdd;

                // (Update() в следующем кадре сам увидит, что склад полон, и отменит запрос)

                return amountToAdd;
            }
        }
        return 0; // Этот завод не принимает такой тип ресурса
    }
    private void UpdateIsRequesting()
    {
        // "IsRequesting = true" ЕСЛИ есть ХОТЯ БЫ ОДИН активный запрос
        IsRequesting = _activeRequests.Count > 0;
    }
}