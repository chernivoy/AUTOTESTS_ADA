namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Завдання, яке тест створює в списку «Meine Aufgaben» навігатора Dashboard: лише поля, від яких залежить,
/// під які фільтри воно потрапить. Дата null лишає поле порожнім.
/// </summary>
public sealed record DashboardTaskData(
    string Name,
    string ShortName,
    DateTime Begin,
    DateTime? Due,
    DateTime? Done,
    DateTime? ClosedOn,
    string AcceptanceType,
    string Remark);
