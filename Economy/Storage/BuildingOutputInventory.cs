using UnityEngine;

public class BuildingOutputInventory : MonoBehaviour
{
    [Tooltip("Какой ресурс производим и его вместимость (настраивается в Инспекторе)")]
    public StorageData outputResource;

    // События для ResourceProducer, чтобы он мог "встать на паузу"
    public event System.Action OnFull;
    public event System.Action OnSpaceAvailable;
    
    private bool _wasFull = false; // Помощник для событий

    /// <summary>
    /// Проверяет, есть ли место (вызывается из ResourceProducer).
    /// </summary>
    public bool HasSpace()
    {
        return outputResource.currentAmount < outputResource.maxAmount;
    }

    /// <summary>
    /// Добавляет готовую продукцию (вызывается из ResourceProducer).
    /// </summary>
    public void AddResource(float amount)
    {
        outputResource.currentAmount += amount;
        
        if (outputResource.currentAmount >= outputResource.maxAmount)
        {
            outputResource.currentAmount = outputResource.maxAmount;
            
            if (!_wasFull)
            {
                _wasFull = true;
                OnFull?.Invoke(); // Сообщаем: "Я ПОЛОН!"
            }
        }
    }

    /// <summary>
    /// Забирает продукцию (вызывается тележкой CartAgent).
    /// </summary>
    /// <returns>Сколько РЕАЛЬНО удалось забрать.</returns>
    public float TakeResource(float amountToTake)
    {
        float amountTaken = Mathf.Min(amountToTake, outputResource.currentAmount);
        
        outputResource.currentAmount -= amountTaken;
        
        if (_wasFull && outputResource.currentAmount < outputResource.maxAmount)
        {
            _wasFull = false;
            OnSpaceAvailable?.Invoke(); // Сообщаем: "ЕСТЬ МЕСТО!"
        }
        
        return amountTaken;
    }

    /// <summary>
    /// Используется тележкой, чтобы решить, стоит ли ехать.
    /// </summary>
    public bool HasAtLeastOneUnit()
    {
        return outputResource.currentAmount >= 1f;
    }

    /// <summary>
    /// Используется тележкой (старый метод от BuildingInventory).
    /// </summary>
    public float TakeAllResources()
    {
        return TakeResource(float.MaxValue);
    }
    
    /// <summary>
    /// Используется тележкой (старый метод от BuildingInventory).
    /// </summary>
    public ResourceType GetResourceType()
    {
        return outputResource.resourceType;
    }
}