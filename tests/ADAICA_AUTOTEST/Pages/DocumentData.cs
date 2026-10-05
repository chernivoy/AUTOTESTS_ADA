namespace ADAICA_AUTOTEST.Pages;

/// <summary>Документ (Dokument) у тому вигляді, як його показує огляд запису.</summary>
/// <param name="Name">«Dokumentname»: ADAICA бере ім'я файлу без розширення.</param>
/// <param name="FileType">«Dateityp»: розширення файлу без крапки, «docx».</param>
/// <param name="FileSize">«Dateigröße» у форматі ADAICA (<see cref="Controls.AdaicaFormat.FormatFileSize"/>).</param>
/// <param name="Creator">«Erstellt durch»: «Прізвище, Ім'я».</param>
/// <param name="Version">Поточна версія й кількість версій: «1 / 1».</param>
/// <param name="Class">Клас і підклас («Klasse» / «Unterklasse»); null — не перевіряти.</param>
public sealed record DocumentData(
    string Name,
    string ShortName,
    string FileType,
    string FileSize,
    string Creator,
    string Version,
    string Status,
    string Remark,
    RecordClass? Class = null);
