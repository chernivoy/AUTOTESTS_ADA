namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Опції пошти: Konfiguration → Einstellungen → вкладка «Kontakte» → вузол «E-Mail» (<see cref="OptionsWindow"/>).
/// ADAICA застосовує їх лише після перезапуску («Änderungen werden erst nach dem Neustart der Anwendung wirksam»).
/// </summary>
/// <param name="Rule">«Regeln zur Zuordnung von E-Mail-Adressen zu Kontakten…» — для всієї системи.</param>
/// <param name="MultipleContacts">«Algorythmus der Auswahl von Kontakten…, wenn eine E-Mail-Adresse mehreren Kontakten… zugeordnet wurde» — для всієї системи.</param>
/// <param name="AddressSuggestionsUser">«Vorschlagsliste E-Mailadressen…» — «Anwender».</param>
/// <param name="AddressSuggestionsSystem">«Vorschlagsliste E-Mailadressen…» — «Systemweit».</param>
/// <param name="SalutationUser">«Anreden von E-Mail-Empfängern hinzufügen» — «Anwender».</param>
/// <param name="SalutationSystem">«Anreden von E-Mail-Empfängern hinzufügen» — «Systemweit».</param>
public sealed record MailOptions(
    MailRecognitionRule Rule,
    MailMultipleContactsMode MultipleContacts,
    bool AddressSuggestionsUser,
    bool AddressSuggestionsSystem,
    bool SalutationUser,
    bool SalutationSystem);

/// <summary>
/// Які адреси листа мають бути зв'язані з контактами, щоб лист можна було зберегти (код ADAICA:
/// <c>enEmailAddressesRecognitionMode</c>, <c>EmailContactLinksViewModelBase.ContainsRequiredContacts</c>).
/// </summary>
public enum MailRecognitionRule
{
    /// <summary>«Einfache Regel»: відправник і ОДИН «An» — з реальним контактом або з «System-By-Default» («Keine Kontaktzuordnung»).</summary>
    Simple,

    /// <summary>«Wenig strenge Regel»: відправник і ОДИН «An» — лише з реальними контактами.</summary>
    SenderAndOneRecipient,

    /// <summary>«Strenge Regel»: відправник і ВСІ «An» — з реальними контактами.</summary>
    SenderAndAllRecipients,

    /// <summary>«Sehr strenge Regel»: відправник, ВСІ «An» і ВСІ «CC» — з реальними контактами.</summary>
    SenderAllRecipientsAndCopies,
}

/// <summary>
/// Що робити, коли адреса є в кількох контактах ADAICA (код: <c>enEmailAddressLinkMode</c>). Головний контакт
/// стає «An»/«CC», решта — додатковими рядками «Kontakt».
/// </summary>
public enum MailMultipleContactsMode
{
    /// <summary>«Kontakterkennung nach Priorität»: лише один контакт.</summary>
    Priority,

    /// <summary>«Speichern … zu allen Kontakten …, die als Empfänger adressiert wurden»: усі, якщо адреса в «An» (не в «CC»).</summary>
    Recipients,

    /// <summary>«Speichern … zu allen Kontakten … (inklusive aller Empfänger von Kopien (CC))».</summary>
    All,
}
