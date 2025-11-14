using UnityEngine;

/// <summary>
/// Производит ресурсы и складывает их в локальный BuildingInventory.
/// Теперь также учитывает бонус от модулей (если они есть).
/// </summary>
public class ResourceProducer : MonoBehaviour
{
    [Tooltip("Данные о том, ЧТО и СКОЛЬКО производим (БАЗОВАЯ ставка)")]
    public ResourceProductionData productionData;
    
    // --- Ссылки на Input/Output (из 1.0) ---
    private BuildingInputInventory _inputInv;
    private BuildingOutputInventory _outputInv;
    private bool _isPaused = false;
    
    // --- НОВЫЙ КОД (Бонус Модулей) ---
    [Header("Бонусы от Модулей")]
    [Tooltip("Производительность = База * (1.0 + (Кол-во модулей * X))")]
    public float productionPerModule = 0.25f;

    private float _currentModuleBonus = 1.0f; // (Множитель, 1.0 = 100%)
    // --- КОНЕЦ НОВОГО КОДА ---
    [Header("Эффективность")]
    [Tooltip("Множитель производительности. Управляется UI (напр. 0.5-1.5)")]
    private float _efficiencyModifier = 1.0f; // 100% по дефолту
    public bool IsPaused { get; private set; } = false;

    void Awake()
    {
        // (Логика из 1.0)
        _inputInv = GetComponent<BuildingInputInventory>();
        _outputInv = GetComponent<BuildingOutputInventory>();

        if (_inputInv == null)
            Debug.LogError($"На здании {gameObject.name} нет компонента BuildingInputInventory!", this);
        if (_outputInv == null)
            Debug.LogError($"На здании {gameObject.name} нет компонента BuildingOutputInventory!", this);
        
        if (_outputInv != null)
        {
            _outputInv.OnFull += PauseProduction;
            _outputInv.OnSpaceAvailable += ResumeProduction;
        }
    }
    
    private void OnDestroy()
    {
        if (_outputInv != null)
        {
            _outputInv.OnFull -= PauseProduction;
            _outputInv.OnSpaceAvailable -= ResumeProduction;
        }
    }

    void Update()
    {
        // (Логика Паузы и Входа из 1.0)
        if (_isPaused || _inputInv == null || _outputInv == null || productionData == null)
            return;
        if (!_inputInv.HasEnoughResources())
            return;
        if (!_outputInv.HasSpace())
        {
            PauseProduction();
            return;
        }
        
        // (Логика Производства из 1.0)
        _inputInv.ConsumeResources();
            
        // --- ИЗМЕНЕНИЕ: Умножаем на бонус ---
        float finalProduction = productionData.amountPerSecond * _currentModuleBonus * _efficiencyModifier;
        _outputInv.AddResource(finalProduction * Time.deltaTime);
        // --- КОНЕЦ ИЗМЕНЕНИЯ ---
    }

    // --- НОВЫЙ ПУБЛИЧНЫЙ МЕТОД ---

    /// <summary>
    /// Вызывается из ModularBuilding, когда кол-во модулей меняется.
    /// </summary>
    public void UpdateProductionRate(int moduleCount)
    {
        _currentModuleBonus = 1.0f + (moduleCount * productionPerModule);
        Debug.Log($"[Producer] {gameObject.name} обновил бонус. Модулей: {moduleCount}, Множитель: {_currentModuleBonus}x");
    }
    
    // --- КОНЕЦ НОВОГО МЕТОДА ---
    // --- ⬇️ НОВЫЕ ПУБЛИЧНЫЕ МЕТОДЫ (Шаг 3.0) ⬇️ ---
    public void SetEfficiency(float normalizedValue)
    {
        _efficiencyModifier = normalizedValue;
        // (В будущем здесь можно менять 'upkeepCostPerMinute' этого здания)
    }
    public float GetEfficiency() => _efficiencyModifier;
    
    // --- ⬆️ КОНЕЦ НОВЫХ МЕТОДОВ ⬆️ ---
    
    // (Методы PauseProduction / ResumeProduction из 1.0 остаются без изменений)
    private void PauseProduction()
    {
        if (_isPaused) return;
        _isPaused = true;
        Debug.Log($"Производство {gameObject.name} на ПАУЗЕ (склад полон).");
    }

    private void ResumeProduction()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Debug.Log($"Производство {gameObject.name} ВОЗОБНОВЛЕНО (место появилось).");
    }
}