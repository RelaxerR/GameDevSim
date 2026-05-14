namespace GameDevSim.Domain._Models.Enums;

/// <summary>
/// Определяет текущее состояние игровой сессии.
/// </summary>
public enum GameStatus
{
  /// <summary>
  /// Сессия создана, игроки подключаются по коду доступа.
  /// </summary>
  Lobby,

  /// <summary>
  /// Игра запущена, идет активный процесс разработки (1-4 месяцы).
  /// </summary>
  Active,

  /// <summary>
  /// Четвертый месяц завершен, подведены итоги и подсчитаны очки.
  /// </summary>
  Finished
}
