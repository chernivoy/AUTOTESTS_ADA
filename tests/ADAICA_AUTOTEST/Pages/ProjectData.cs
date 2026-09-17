namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Поля проєкту, які користувач може змінити. Суми — числа: після збереження ADAICA показує «1500» як «1500,00».
/// </summary>
public sealed record ProjectData(
    string Name,
    string ShortName,
    DateTime BeginDate,
    DateTime EndDate,
    decimal Cost,
    string CostCurrency,
    decimal Outlay,
    string OutlayUnit,
    string Comment);
