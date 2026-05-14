using GameDevSim.Domain._Models.Enums;

namespace GameDevSim.Domain._Models.Entities;

/// <summary>
/// Представляет специалиста (сотрудника студии), нанимаемого для разработки игры.
/// </summary>
/// <param name="post">Название должности сотрудника.</param>
/// <param name="salary">Ежемесячная заработная плата сотрудника.</param>
/// <param name="generatingResources">Список типов ресурсов, которые специалист способен производить.</param>
/// <param name="isAdvancedPost">Флаг, указывающий на продвинутую или руководящую должность.</param>
/// <param name="canRemoveBug">Определяет, обладает ли специалист навыком исправления багов.</param>
/// <param name="canIncreaseMorale">Определяет, способен ли специалист повышать общую мораль команды.</param>
public class Specialist(
  string post, 
  int salary, 
  List<ResourceType> generatingResources, 
  bool isAdvancedPost = false, 
  bool canRemoveBug = false, 
  bool canIncreaseMorale = false)
{
  #region Fields & Properties

  /// <summary>
  /// Уникальный идентификатор специалиста.
  /// </summary>
  public Guid Id { get; init; } = Guid.NewGuid();

  /// <summary>
  /// Название должности специалиста (например: "Junior Developer", "Lead Artist").
  /// </summary>
  public string Post { get; init; } = post;

  /// <summary>
  /// Ежемесячный оклад сотрудника, выплачиваемый в конце каждого игрового месяца.
  /// </summary>
  public int Salary { get; init; } = salary;

  /// <summary>
  /// Признак продвинутой должности (Senior, Lead), дающий доступ к сложным фичам или бонусам.
  /// </summary>
  public bool IsAdvancedPost { get; init; } = isAdvancedPost;

  /// <summary>
  /// Указывает, может ли данный специалист заниматься поиском и устранением багов в проекте.
  /// </summary>
  public bool CanRemoveBug { get; init; } = canRemoveBug;

  /// <summary>
  /// Указывает, обладает ли специалист пассивным навыком или способностью увеличивать моральный дух студии.
  /// </summary>
  public bool CanIncreaseMorale { get; init; } = canIncreaseMorale;

  /// <summary>
  /// Базовый список ресурсов, генерируемых специалистом по умолчанию.
  /// </summary>
  public List<ResourceType> GeneratingResources { get; } = generatingResources;

  /// <summary>
  /// Список ресурсов, доступных для распределения в текущем игровом месяце.
  /// </summary>
  public List<ResourceType> AvailableResources { get; } = [];

  #endregion

  #region State History (Fixed 4 Months)

  /// <summary>
  /// История месяцев, в которых специалист был нанят на работу.
  /// </summary>
  public readonly int[] MonthsHired = new int[4];

  /// <summary>
  /// История месяцев, в которых специалист был занят сторонними задачами.
  /// </summary>
  public readonly int[] MonthsBusy = new int[4];

  /// <summary>
  /// История месяцев, в которых специалист был официально уволен.
  /// </summary>
  public readonly int[] MonthsDismissed = new int[4];

  /// <summary>
  /// История месяцев, в которых специалист выполнял свои основные рабочие обязанности.
  /// </summary>
  public readonly int[] MonthsWorked = new int[4];

  /// <summary>
  /// История месяцев, в которых специалист был недоступен для любых действий.
  /// </summary>
  public readonly int[] MonthsUnavailable = new int[4];

  #endregion

  #region Public Methods

  /// <summary>
  /// Оформляет наем (или продление контракта) специалиста в указанном игровом месяце.
  /// </summary>
  /// <param name="monthNumber">Номер текущего игрового месяца (от 1 до 4).</param>
  public void Hire(int monthNumber)
  {
    ValidateMonth(monthNumber);

    if (MonthsHired.Contains(monthNumber))
      throw new InvalidOperationException($"Специалист уже нанят/продлен в месяце {monthNumber}.");

    if (MonthsDismissed.Any(m => m > 0) || MonthsUnavailable.Contains(monthNumber))
      throw new InvalidOperationException("Невозможно нанять специалиста. Он был уволен ранее и покинул студию навсегда.");

    MonthsHired[monthNumber - 1] = monthNumber;
    ResetAndGrantResources();
  }

  /// <summary>
  /// Проводит увольнение специалиста и автоматически делает его недоступным на все оставшиеся месяцы игры.
  /// </summary>
  /// <param name="monthNumber">Номер текущего игрового месяца (от 1 до 4).</param>
  public void Dismiss(int monthNumber)
  {
    ValidateMonth(monthNumber);

    if (!MonthsHired.Any(m => m > 0))
      throw new InvalidOperationException("Специалист не может быть уволен, так как он никогда не был нанят.");

    if (MonthsDismissed.Any(m => m > 0))
      throw new InvalidOperationException("Специалист уже имеет статус уволенного.");

    MonthsDismissed[monthNumber - 1] = monthNumber;

    for (var i = monthNumber; i <= 4; i++)
    {
      MonthsUnavailable[i - 1] = i;
    }

    AvailableResources.Clear();
  }

  /// <summary>
  /// Переводит специалиста в статус занятости (например, при активации способности), блокируя генерацию доступных ресурсов в текущем месяце.
  /// </summary>
  /// <param name="monthNumber">Номер текущего игрового месяца (от 1 до 4).</param>
  public void Busy(int monthNumber)
  {
    ValidateMonth(monthNumber);

    if (MonthsBusy.Contains(monthNumber))
      throw new InvalidOperationException($"Специалист уже занят в месяце {monthNumber}.");

    if (MonthsUnavailable.Contains(monthNumber))
      throw new InvalidOperationException($"Специалист недоступен в месяце {monthNumber}.");

    MonthsBusy[monthNumber - 1] = monthNumber;
    AvailableResources.Clear();
  }

  /// <summary>
  /// Фиксирует выполнение основной работы специалистом и начисляет доступные ресурсы.
  /// </summary>
  /// <param name="monthNumber">Номер текущего игрового месяца (от 1 до 4).</param>
  private void Work(int monthNumber)
  {
    ValidateMonth(monthNumber);

    if (MonthsWorked.Contains(monthNumber))
      throw new InvalidOperationException($"Специалист уже работал в месяце {monthNumber}.");

    if (!MonthsHired.Contains(monthNumber))
      throw new InvalidOperationException($"Специалист не может работать в месяце {monthNumber}, так как контракт не был заключен/продлен.");

    if (MonthsDismissed.Contains(monthNumber) || MonthsUnavailable.Contains(monthNumber) || MonthsBusy.Contains(monthNumber))
      throw new InvalidOperationException($"Специалист не может работать в месяце {monthNumber}, так как он уволен, недоступен или занят.");

    MonthsWorked[monthNumber - 1] = monthNumber;
    ResetAndGrantResources();
  }

  /// <summary>
  /// Обрабатывает триггер начала нового игрового месяца, актуализируя периоды недоступности для ненанятых сотрудников.
  /// </summary>
  /// <param name="monthNumber">Номер наступившего игрового месяца (от 1 до 4).</param>
  public void MonthStarted(int monthNumber)
  {
    ValidateMonth(monthNumber);
    AvailableResources.Clear();

    for (var i = 1; i < monthNumber; i++)
    {
      if (!MonthsHired.Contains(i) && !MonthsDismissed.Contains(i) && !MonthsWorked.Contains(i))
      {
        MonthsUnavailable[i - 1] = i;
      }
    }
  }
  
  /// <summary>
  /// Обрабатывает этап игровых ситуаций. Автоматически увольняет сотрудника навсегда, если в текущем месяце контракт не был продлен.
  /// </summary>
  /// <param name="monthNumber">Номер текущего игрового месяца (от 1 до 4).</param>
  public void SituationsStarted(int monthNumber)
  {
    ValidateMonth(monthNumber);

    if (monthNumber == 1) return;

    var wasHiredLastMonth = MonthsHired.Contains(monthNumber - 1);
    var isHiredThisMonth = MonthsHired.Contains(monthNumber);

    if (wasHiredLastMonth && !isHiredThisMonth)
    {
      Dismiss(monthNumber);
    }
  }

  /// <summary>
  /// Обрабатывает завершение этапа игровых ситуаций. Автоматически переводит свободного нанятого сотрудника в статус работы, сохраняя его ресурсы.
  /// </summary>
  /// <param name="monthNumber">Номер текущего игрового месяца (от 1 до 4).</param>
  public void SituationsFinished(int monthNumber)
  {
    ValidateMonth(monthNumber);

    var isHired = MonthsHired.Contains(monthNumber);
    var isBusy = MonthsBusy.Contains(monthNumber);
    var isUnavailable = MonthsUnavailable.Contains(monthNumber);

    if (isHired && !isBusy && !isUnavailable)
    {
      Work(monthNumber);
    }
  }

  /// <summary>
  /// Проверяет, доступна ли дополнительная способность (починка бага или бонус к морали) специалиста в указанном месяце.
  /// </summary>
  /// <param name="monthNumber">Номер проверяемого игрового месяца (от 1 до 4).</param>
  /// <returns><see langword="true"/>, если у сотрудника есть способность и он готов её применить; иначе <see langword="false"/>.</returns>
  public bool IsAbilityAvailable(int monthNumber)
  {
    if (monthNumber is < 1 or > 4) return false;

    // Проверяем, обладает ли специалист этой способностью в принципе
    if (!CanRemoveBug && !CanIncreaseMorale) return false;

    var isHired = MonthsHired.Contains(monthNumber);
    var isBusy = MonthsBusy.Contains(monthNumber);
    var isDismissed = MonthsDismissed.Contains(monthNumber);
    var isUnavailable = MonthsUnavailable.Contains(monthNumber);

    // Доступно, если нанят и не находится ни в одном блокирующем статусе
    return isHired && !isBusy && !isDismissed && !isUnavailable;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Сбрасывает текущие доступные ресурсы и заново наполняет их из базового пула генерации.
  /// </summary>
  private void ResetAndGrantResources()
  {
    AvailableResources.Clear();
    AvailableResources.AddRange(GeneratingResources);
  }

  /// <summary>
  /// Проверяет, что переданный месяц укладывается в рамки фиксированной 4-месячной сессии.
  /// </summary>
  private static void ValidateMonth(int monthNumber)
  {
    if (monthNumber is < 1 or > 4)
    {
      throw new ArgumentOutOfRangeException(nameof(monthNumber), "В игре фиксировано только 4 месяца (от 1 до 4).");
    }
  }

  #endregion
}
