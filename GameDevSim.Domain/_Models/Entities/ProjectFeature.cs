using GameDevSim.Domain._Models.Enums;

namespace GameDevSim.Domain._Models.Entities;

/// <summary>
/// Представляет функциональную фичу игрового проекта, требующую инвестиции различных типов ресурсов для реализации.
/// </summary>
public class ProjectFeature(Dictionary<ResourceType, int> resourcesRequired)
{
    #region Fields & Properties

    /// <summary>
    /// Номер игрового месяца, в котором фича была взята в разработку. 
    /// Равен <see langword="null"/>, если разработка еще не начата.
    /// </summary>
    public int? MonthInited { get; private set; }

    /// <summary>
    /// Требуемое количество ресурсов каждого типа для полного завершения фичи.
    /// </summary>
    public readonly Dictionary<ResourceType, int> ResourcesRequired = resourcesRequired;

    /// <summary>
    /// Плоский список всех произведенных инвестиций в фичу для ведения истории.
    /// </summary>
    public List<(ResourceType Type, InvestedResourceRecord Record)> ResourcesAdded { get; } = [];

    /// <summary>
    /// Возвращает словарь с суммарным количеством уже инвестированных ресурсов по каждому типу.
    /// Включает в себя все требуемые типы ресурсов со значением 0, если инвестиций по ним еще не было.
    /// </summary>
    public Dictionary<ResourceType, int> TotalResourcesAdded
    {
        get
        {
            var aggregated = ResourcesAdded
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Record.Amount));

            // Заполняем нулями требуемые типы ресурсов, которые еще не инвестировались
            foreach (var requiredType in ResourcesRequired.Keys)
            {
                aggregated.TryAdd(requiredType, 0);
            }

            return aggregated;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Переводит фичу в статус разработки и фиксирует стартовый месяц.
    /// </summary>
    /// <param name="monthNumber">Номер текущего игрового месяца.</param>
    public void TakeToDeveloping(int monthNumber) => MonthInited = monthNumber;

    /// <summary>
    /// Инвестирует указанное количество ресурса в разработку фичи.
    /// </summary>
    /// <param name="resourceType">Тип инвестируемого ресурса.</param>
    /// <param name="resource">Запись об инвестиции, содержащая месяц и объем.</param>
    /// <exception cref="InvalidOperationException">Выбрасывается, если фича еще не взята в разработку.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если месяц инвестиции меньше месяца начала разработки, 
    /// если тип ресурса не требуется для этой фичи, или если объем инвестиции превышает лимит.
    /// </exception>
    public void InvestResource(ResourceType resourceType, InvestedResourceRecord resource)
    {
        if (MonthInited == null) 
            throw new InvalidOperationException("Фича не была взята в разработку. Вызовите TakeToDeveloping перед инвестированием.");

        if (resource.MonthNumber < MonthInited)
        {
            throw new ArgumentOutOfRangeException(nameof(resource), 
                $"Невозможно инвестировать ресурсы в месяц [{resource.MonthNumber}] до начала разработки фичи (месяц [{MonthInited}]).");
        }

        // 1. Проверка: нужен ли вообще этот тип ресурса фиче
        if (!ResourcesRequired.TryGetValue(resourceType, out var requiredAmount))
        {
            var allowed = string.Join(", ", ResourcesRequired.Select(kv => $"[{kv.Key}]: {kv.Value}"));
            throw new ArgumentOutOfRangeException(nameof(resourceType), 
                $"Ресурс [{resourceType}] не требуется. Требуемые ресурсы: {allowed}.");
        }

        // 2. Расчет текущего прогресса и валидация лимита
        var alreadyAdded = ResourcesAdded
            .Where(r => r.Type == resourceType)
            .Sum(r => r.Record.Amount);

        if (alreadyAdded + resource.Amount > requiredAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(resource), 
                $"Невозможно инвестировать [{resource.Amount}] единиц [{resourceType}]. " +
                $"Превышение лимита. Требуется: [{requiredAmount}], уже добавлено: [{alreadyAdded}].");
        }

        // 3. Сохранение записи в историю
        ResourcesAdded.Add((resourceType, resource));
    }

    /// <summary>
    /// Возвращает оставшееся количество ресурса, необходимое для завершения разработки по указанному типу.
    /// </summary>
    /// <param name="resourceType">Тип проверяемого ресурса.</param>
    /// <returns>Количество оставшихся единиц ресурса или 0, если ресурс не требуется или уже полностью собран.</returns>
    public int GetMissingResource(ResourceType resourceType)
    {
        if (!ResourcesRequired.TryGetValue(resourceType, out var required)) return 0;
        var added = ResourcesAdded.Where(r => r.Type == resourceType).Sum(r => r.Record.Amount);
        return Math.Max(0, required - added);
    }

    #endregion
}
