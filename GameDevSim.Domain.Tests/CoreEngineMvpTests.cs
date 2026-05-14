using Xunit;
using GameDevSim.Domain._Models.Entities;
using GameDevSim.Domain._Models.Enums;

namespace GameDevSim.Domain.Tests;

public class CoreEngineMvpTests
{
    [Fact]
    public void CalculateSpecialistExpenses_ShouldCalculateCorrectSalariesForMonth()
    {
        // Arrange
        var board = new PlayerBoard();
        const int month = 1;
        
        // Создаем двух специалистов
        var programmer = new Specialist("Senior Dev", 150_000, [ResourceType.Code]);
        var designer = new Specialist("Lead Artist", 200_000, [ResourceType.Art]);
        
        board.AddSpecialist(programmer);
        board.AddSpecialist(designer);
        
        // Оформляем их на работу в 1-м месяце
        programmer.Hire(month);
        designer.Hire(month);

        // Act
        board.CalculateSpecialistExpenses(month);

        // Assert
        // Ожидаем 150к + 200к = 350к расходов на зарплаты в 1-м месяце
        Assert.Equal(350_000, board.GetBalanceForMonth(month) == 0 ? 0 : 2_000_000 - board.GetBalanceForMonth(month));
        // Чистая прибыль за месяц: 2 000 000 - 350 000 = 1 650 000
        Assert.Equal(1_650_000, board.GetBalanceForMonth(month));
    }

    [Fact]
    public void InvestToMainProject_ShouldFillCells_And_ThrowExceptionIfLimitExceeded()
    {
        // Arrange
        var board = new PlayerBoard();
        const int month = 1;
        
        // Создаем программиста, который дает 1 единицу ресурса Code
        var programmer = new Specialist("Junior Dev", 100_000, [ResourceType.Code]);
        board.AddSpecialist(programmer);
        programmer.Hire(month);
        
        // Имитируем завершение фазы ивентов, чтобы у сотрудника появились ресурсы в AvailableResources
        programmer.MonthStarted(month);
        programmer.SituationsFinished(month); // Внутри вызовется Work() и добавит Code в пул

        // Act & Assert
        // 1. Инвестируем 1 доступный ресурс в Код основного проекта
        board.InvestToMainProject(month, programmer, ResourceType.Code, ResourceType.Code, 1);
        
        // Проверяем, что ресурс списался у сотрудника
        Assert.Empty(programmer.AvailableResources);

        // 2. Пытаемся инвестировать еще раз, когда у сотрудника уже нет ресурсов в пуле
        var exception = Assert.Throws<InvalidOperationException>(() =>
            board.InvestToMainProject(month, programmer, ResourceType.Code, ResourceType.Code, 1)
        );
        Assert.Contains("недостаточно ресурса", exception.Message);
    }
}
