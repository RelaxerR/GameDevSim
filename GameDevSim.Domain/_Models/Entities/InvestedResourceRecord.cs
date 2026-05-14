namespace GameDevSim.Domain._Models.Entities;

/// <summary>
/// Представляет запись об инвестированном объеме ресурса в конкретном игровом месяце.
/// </summary>
/// <param name="MonthNumber">Номер игрового месяца, в котором была произведена инвестиция.</param>
/// <param name="Amount">Количество инвестированных единиц ресурса.</param>
public record InvestedResourceRecord(int MonthNumber, int Amount)
{
    #region Operators

  /// <summary>
  /// Складывает объемы двух инвестиционных записей. Номер месяца берется из левого операнда.
  /// </summary>
  public static InvestedResourceRecord operator +(InvestedResourceRecord a, InvestedResourceRecord b)
  {
    return a with { Amount = a.Amount + b.Amount };
  }

  /// <summary>
  /// Вычитает объем одной инвестиционной записи из другой. Номер месяца берется из левого операнда.
  /// </summary>
  public static InvestedResourceRecord operator -(InvestedResourceRecord a, InvestedResourceRecord b)
  {
    return a with { Amount = a.Amount - b.Amount };
  }

    #endregion

    #region Overrides

  /// <inheritdoc />
  public override string ToString() => $"Month = {MonthNumber} | Amount = {Amount}";

    #endregion
}
