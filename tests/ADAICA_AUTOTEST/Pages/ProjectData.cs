namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Поля проєкту, які користувач може змінити. Суми — числа: після збереження ADAICA показує «1500» як «1500,00».
/// </summary>
/// <param name="Class">Клас і підклас («Klasse» / «Unterklasse»); null — не вибирати й не перевіряти.</param>
public sealed record ProjectData(
    string Name,
    string ShortName,
    DateTime BeginDate,
    DateTime EndDate,
    decimal Cost,
    string CostCurrency,
    decimal Outlay,
    string OutlayUnit,
    string Comment,
    RecordClass? Class = null);
