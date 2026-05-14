using GameDevSim.Domain._Models.Enums;

namespace GameDevSim.Domain._Models.Entities;

/// <summary>
/// Управляет многопользовательской игровой сессией, координируя смену месяцев и фазы игроков.
/// </summary>
public class GameSession
{
  #region Fields & Properties

  /// <summary>
  /// Уникальный идентификатор игровой сессии.
  /// </summary>
  public Guid Id { get; init; } = Guid.NewGuid();

  /// <summary>
  /// Короткий код доступа для подключения игроков к лобби (например, "XF4G9").
  /// </summary>
  public string AccessCode { get; init; }

  /// <summary>
  /// Текущий статус игровой сессии.
  /// </summary>
  public GameStatus Status { get; private set; } = GameStatus.Lobby;

  /// <summary>
  /// Текущий активный игровой месяц (от 1 до 4). Равен 0, если игра еще в лобби.
  /// </summary>
  public int CurrentMonth { get; private set; } = 0;

  /// <summary>
  /// Карта распределения планшетов игроков, где ключ — уникальный идентификатор игрока.
  /// </summary>
  public Dictionary<Guid, PlayerBoard> Players { get; } = [];

  #endregion

  #region Constructor

  /// <summary>
  /// Инициализирует новый экземпляр игровой сессии с кодом доступа.
  /// </summary>
  /// <param name="accessCode">Строковый код для подключения к сессии.</param>
  public GameSession(string accessCode)
  {
    if (string.IsNullOrWhiteSpace(accessCode))
      throw new ArgumentException("Код доступа не может быть пустым.", nameof(accessCode));
    
    AccessCode = accessCode;
  }

  #endregion

  #region Public Methods: Session Management

  /// <summary>
  /// Регистрирует нового игрока и его планшет в текущей сессии.
  /// </summary>
  /// <param name="playerId">Уникальный идентификатор подключающегося игрока.</param>
  /// <param name="board">Персональный планшет игрока.</param>
  public void AddPlayer(Guid playerId, PlayerBoard board)
  {
    if (Status != GameStatus.Lobby)
      throw new InvalidOperationException("Нельзя добавлять игроков после старта сессии.");
    if (Players.ContainsKey(playerId))
      throw new InvalidOperationException("Игрок с таким идентификатором уже находится в сессии.");

    Players.Add(playerId, board ?? throw new ArgumentNullException(nameof(board)));
  }

  /// <summary>
  /// Переводит сессию в активное состояние и запускает первый игровой месяц.
  /// </summary>
  public void StartGame()
  {
    if (Status != GameStatus.Lobby)
      throw new InvalidOperationException("Игра может быть запущена только из статуса Lobby.");
    if (Players.Count == 0)
      throw new InvalidOperationException("Нельзя запустить игру без участников.");

    Status = GameStatus.Active;
    CurrentMonth = 1;

    TriggerMonthStarted();
  }

  /// <summary>
  /// Запускает этап обработки игровых ситуаций (ивентов) для текущего месяца.
  /// </summary>
  public void RunSituationsPhase()
  {
    EnsureGameIsActive();

    foreach (var specialist in Players.Values.SelectMany(board => board.Specialists))
    {
      specialist.SituationsStarted(CurrentMonth);
    }
  }

  /// <summary>
  /// Фиксирует завершение этапа игровых ситуаций и начисляет свободным сотрудникам ресурсы.
  /// </summary>
  public void FinishSituationsPhase()
  {
    EnsureGameIsActive();

    foreach (var specialist in Players.Values.SelectMany(board => board.Specialists))
    {
      specialist.SituationsFinished(CurrentMonth);
    }
  }

  /// <summary>
  /// Завершает текущий месяц, рассчитывает финансовые расходы на зарплаты и переводит игру на следующий этап.
  /// </summary>
  public void AdvanceMonth()
  {
    EnsureGameIsActive();

    // Фиксируем финансовые итоги уходящего месяца для всех игроков
    foreach (var board in Players.Values)
    {
      board.CalculateSpecialistExpenses(CurrentMonth);
    }

    if (CurrentMonth < 4)
    {
      CurrentMonth++;
      TriggerMonthStarted();
    }
    else
    {
      Status = GameStatus.Finished;
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Гарантирует, что сессия находится в состоянии активного раунда.
  /// </summary>
  private void EnsureGameIsActive()
  {
    if (Status != GameStatus.Active)
      throw new InvalidOperationException("Данное действие доступно только во время активной фазы игры.");
  }

  /// <summary>
  /// Оповещает все доменные модели специалистов на планшетах о начале нового месяца.
  /// </summary>
  private void TriggerMonthStarted()
  {
    foreach (var specialist in Players.Values.SelectMany(board => board.Specialists))
    {
      specialist.MonthStarted(CurrentMonth);
    }
  }

  #endregion
}
