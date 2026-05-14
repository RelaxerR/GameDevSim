using GameDevSim.Domain._Models.Enums;

namespace GameDevSim.Domain._Models.Entities;

/// <summary>
/// Представляет персональный планшет игрока, управляющий финансами, сотрудниками, 
/// прогрессом основного проекта и взятыми в разработку дополнительными фичами.
/// </summary>
public class PlayerBoard
{
  #region Fields & Properties: Financials

  private readonly Dictionary<int, int> BalanceIncome = new()
  {
    { 1, 2_000_000 }, { 2, 2_000_000 }, { 3, 2_000_000 }, { 4, 2_000_000 }
  };
  
  private readonly Dictionary<int, int> BalanceSpecialistExpenses = new()
  {
    { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }
  };
  
  private readonly Dictionary<int, int> BalanceOtherExpenses = new()
  {
    { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }
  };

  #endregion

  #region Fields & Properties: Main Project (Fixed 10 Slots)

  // Каждый массив представляет собой 10 слотов прогресса. 
  // Значение в ячейке (0) означает, что слот пуст. 
  // Значение (1..4) указывает номер месяца, в котором этот ресурс был вложен.
  private readonly int[] CodeResourcesAdded = new int[10];
  private readonly int[] ArtResourcesAdded = new int[10];
  private readonly int[] DocResourcesAdded = new int[10];

  #endregion

  #region Fields & Properties: Collections

  /// <summary>
  /// Список специалистов, закрепленных за планшетом игрока.
  /// </summary>
  public List<Specialist> Specialists { get; } = [];
  
  /// <summary>
  /// Дополнительные фичи, которые игрок взял в разработку.
  /// </summary>
  public List<ProjectFeature> CustomFeatures { get; } = [];

  #endregion

  #region Public Methods: Main Project Production

  /// <summary>
  /// Инвестирует ресурсы специалиста напрямую в Основной Проект, заполняя свободные слоты.
  /// </summary>
  /// <param name="month">Номер текущего игрового месяца (от 1 до 4).</param>
  /// <param name="specialist">Сотрудник, чей ресурс тратится.</param>
  /// <param name="resourceType">Тип вкладываемого ресурса сотрудника (может быть Joker).</param>
  /// <param name="targetType">В какую категорию основного проекта идет вклад (Code, Art или Doc).</param>
  /// <param name="amount">Количество единиц ресурса.</param>
  public void InvestToMainProject(int month, Specialist specialist, ResourceType resourceType, ResourceType targetType, int amount)
  {
    ValidateMonth(month);
    ArgumentNullException.ThrowIfNull(specialist);
    if (!Specialists.Contains(specialist)) throw new InvalidOperationException("Специалист не нанят на этом планшете.");

    if (targetType == ResourceType.Joker)
      throw new ArgumentException("Нельзя инвестировать в категорию Joker. Выберите Code, Art или Doc.");

    if (resourceType != targetType && resourceType != ResourceType.Joker)
      throw new ArgumentException($"Несовместимый тип ресурса [{resourceType}] для категории проекта [{targetType}].");

    // Выбираем целевой массив для проверки свободных слотов
    var targetArray = targetType switch
    {
      ResourceType.Code => CodeResourcesAdded,
      ResourceType.Art  => ArtResourcesAdded,
      ResourceType.Doc  => DocResourcesAdded,
      ResourceType.Joker => throw new ArgumentException("Основной проект не содержит отдельной категории для Joker. Выберите конкретную цель."),
      _ => throw new ArgumentOutOfRangeException(nameof(targetType))
    };


    // Считаем текущее количество уже заполненных слотов (где значение > 0)
    var currentFilledSlots = targetArray.Count(v => v > 0);
    if (currentFilledSlots + amount > 10)
    {
      throw new InvalidOperationException($"Превышение лимита проекта [{targetType}]. Заполнено слотов: {currentFilledSlots}/10. Пытаетесь добавить: {amount}.");
    }

    // Списываем штучные ресурсы у сотрудника
    ConsumeSpecialistResources(specialist, resourceType, amount);

    // Заполняем свободные слоты номером текущего месяца
    var allocated = 0;
    for (var i = 0; i < targetArray.Length && allocated < amount; i++)
    {
      if (targetArray[i] != 0)
        continue;
      
      targetArray[i] = month;
      allocated++;
    }
  }

  #endregion

  #region Public Methods: Custom Features

  /// <summary>
  /// Позволяет взять одну дополнительную фичу в начале месяца.
  /// </summary>
  public void TakeCustomFeature(int month, ProjectFeature feature)
  {
    ValidateMonth(month);
    ArgumentNullException.ThrowIfNull(feature);

    var featuresThisMonth = CustomFeatures.Count(f => f.MonthInited == month);
    if (featuresThisMonth >= 1)
      throw new InvalidOperationException($"В месяце {month} уже взята дополнительная фича. Лимит: 1 фича в месяц.");

    feature.TakeToDeveloping(month);
    CustomFeatures.Add(feature);
  }

  /// <summary>
  /// Инвестирует ресурсы специалиста в выбранную дополнительную фичу.
  /// </summary>
  public void InvestToCustomFeature(int month, Specialist specialist, ProjectFeature feature, ResourceType resourceType, int amount)
  {
    ValidateMonth(month);
    ArgumentNullException.ThrowIfNull(specialist);
    ArgumentNullException.ThrowIfNull(feature);

    if (!Specialists.Contains(specialist)) throw new InvalidOperationException("Специалист не нанят на этом планшете.");
    if (!CustomFeatures.Contains(feature)) throw new InvalidOperationException("Эта фича не взята в разработку на данном планшете.");

    ConsumeSpecialistResources(specialist, resourceType, amount);
    
    var record = new InvestedResourceRecord(month, amount);
    feature.InvestResource(resourceType, record);
  }

  #endregion

  #region Public Methods: Score Calculation

  /// <summary>
  /// Вычисляет итоговое количество победных очков на планшете (за основной проект и доп. фичи).
  /// </summary>
  public int CalculateTotalScore(int scorePerFeature = 3, int penaltyPerFeature = 3)
  {
    var score = 0;

    // Проверяем основной проект: все 10 слотов в каждом из массивов должны быть заполнены (значения > 0)
    var isMainProjectCompleted = CodeResourcesAdded.All(v => v > 0) &&
                                 ArtResourcesAdded.All(v => v > 0) &&
                                 DocResourcesAdded.All(v => v > 0);

    if (isMainProjectCompleted)
    {
      score += 10;
    }

    // Расчет очков за дополнительные фичи
    foreach (var feature in CustomFeatures)
    {
      var isFeatureCompleted = feature.ResourcesRequired.All(req => 
        feature.TotalResourcesAdded.TryGetValue(req.Key, out var added) && added >= req.Value);

      if (isFeatureCompleted)
      {
        score += scorePerFeature;
      }
      else
      {
        score -= penaltyPerFeature;
      }
    }

    return score;
  }

  #endregion

  #region Public Methods: Financials & Infrastructure

  public int GetBalanceForMonth(int month)
  {
    ValidateMonth(month);
    return BalanceIncome[month] - BalanceSpecialistExpenses[month] - BalanceOtherExpenses[month];
  }

  public void CalculateSpecialistExpenses(int month)
  {
    ValidateMonth(month);
    BalanceSpecialistExpenses[month] = Specialists.Where(s => s.MonthsHired.Contains(month)).Sum(s => s.Salary);
  }

  public void AddSpecialist(Specialist specialist)
  {
    ArgumentNullException.ThrowIfNull(specialist);
    Specialists.Add(specialist);
  }

  #endregion

  #region Private Methods

  private static void ValidateMonth(int monthNumber)
  {
    if (monthNumber is < 1 or > 4)
    {
      throw new ArgumentOutOfRangeException(nameof(monthNumber), "В игре фиксировано только 4 месяца (от 1 до 4).");
    }
  }

  private static void ConsumeSpecialistResources(Specialist specialist, ResourceType type, int amount)
  {
    var available = specialist.AvailableResources.Count(r => r == type);
    if (available < amount)
    {
      throw new InvalidOperationException($"У специалиста [{specialist.Post}] недостаточно ресурса [{type}]. Доступно: {available}, требуется: {amount}.");
    }

    for (var i = 0; i < amount; i++)
    {
      specialist.AvailableResources.Remove(type);
    }
  }

  #endregion
}
