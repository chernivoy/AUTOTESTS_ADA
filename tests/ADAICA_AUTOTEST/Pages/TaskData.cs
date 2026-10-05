namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Поля завдання, які користувач може змінити. Поля з довідника (Standort, Kundennutzen, Hinweis)
/// складаються зі значення довідника і довільного тексту поруч.
/// </summary>
/// <param name="Class">Клас і підклас («Klasse» / «Unterklasse»); null — не вибирати й не перевіряти.</param>
public sealed record TaskData(
    string Name,
    string ShortName,
    DateTime Begin,
    DateTime Due,
    DateTime Done,
    DateTime ClosedOn,
    string AcceptanceType,
    string Priority,
    string Importance,
    decimal EstimatedDuration,
    string EstimatedDurationUnit,
    decimal ActualDuration,
    string ActualDurationUnit,
    string OrderNumber,
    string PurchaseOrderNumber,
    decimal PercentDone,
    string Location,
    string LocationText,
    string CustomerBenefit,
    string CustomerBenefitText,
    string Hint,
    string HintText,
    string Remark,
    RecordClass? Class = null);
