namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Клас і підклас запису («Klasse» / «Unterklasse» на бланку) з короткими назвами, як їх задають у конфігурації класів.
/// У вікні вибору й у списках конфігурації ADAICA підписує їх «Назва (коротка назва)».
/// </summary>
public sealed record RecordClass(string Class, string ClassShortName, string SubClass, string SubClassShortName)
{
    public string ClassNode => $"{Class} ({ClassShortName})";

    public string SubClassNode => $"{SubClass} ({SubClassShortName})";

    public override string ToString() => $"{ClassNode} → {SubClassNode}";
}
