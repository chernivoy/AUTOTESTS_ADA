namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Стан адреси на вкладці «Kontakte» вікна збереження листа (<see cref="MailSaveDialog"/>). На екрані його видно
/// зі значка в колонці «Funktion», якого UI Automation не бачить, тож тест упізнає стан із контекстного меню рядка.
/// </summary>
public enum MailContactState
{
    /// <summary>Адресу зв'язано з контактом (значок персони): відправник-користувач або знайдений чи створений контакт.</summary>
    Linked,

    /// <summary>Контакт не знайдено (значок «?»): лист не зберегти, поки адресу не зв'язано або не вирішено інакше.</summary>
    Unknown,

    /// <summary>«Keine Kontaktzuordnung» (значок AUTO): зберегти адресу без контакту.</summary>
    NoContact,

    /// <summary>«Ignorieren» (червоний хрестик): адресу не зберігати.</summary>
    Ignored,
}

/// <summary>Контекстні меню рядка для кожного <see cref="MailContactState"/>, як їх показує ADAICA (перевірено 05.10.2026).</summary>
public static class MailContactStates
{
    public static IReadOnlyList<MenuEntry> Menu(MailContactState state) => state switch
    {
        MailContactState.Linked =>
        [
            new("Zugehörigen Kontakt finden/erstellen", false),
            new("Weiteren Kontakt hinzufügen", true),
            new("Öffnen", true),
            new("Ignorieren", true),
            new("Aktualisieren", true),
        ],
        MailContactState.Unknown =>
        [
            new("Zugehörigen Kontakt finden/erstellen", true),
            new("Weiteren Kontakt hinzufügen", true),
            new("Öffnen", false),
            new("Keine Kontaktzuordnung", true),
            new("Ignorieren", true),
            new("Aktualisieren", true),
        ],
        MailContactState.NoContact =>
        [
            new("Zugehörigen Kontakt finden/erstellen", true),
            new("Weiteren Kontakt hinzufügen", true),
            new("Öffnen", false),
            new("Manuell", true),
            new("Ignorieren", true),
            new("Aktualisieren", true),
        ],
        MailContactState.Ignored =>
        [
            new("Zugehörigen Kontakt finden/erstellen", true),
            new("Weiteren Kontakt hinzufügen", true),
            new("Öffnen", false),
            new("Keine Kontaktzuordnung", true),
            new("Manuell", true),
            new("Wiederherstellen", true),
            new("Aktualisieren", true),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
