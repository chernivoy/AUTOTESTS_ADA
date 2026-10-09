using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Лист в області перегляду (вкладка «Übersicht»): заголовок «Von», «An», «Versanddatum», «Betreff» і текст.
/// Поля заголовка — текстові поля без AutomationId, кожне йде за своїм підписом, тож шукаємо їх за підписом.
/// Текст листа показує вбудований переглядач, якого UI Automation не бачить.
/// </summary>
public sealed class MailView(AutomationElement view)
{
    public const string HeaderId = "InfoHeaderControl";

    public string From => Field("Von");

    public string To => Field("An");

    public string Subject => Field("Betreff");

    /// <summary>«05.10.2026 16:09», як ADAICA показує дату надсилання.</summary>
    public string SentAt => Field("Versanddatum");

    /// <summary>Значення поля за підписом або null, якщо такого поля в заголовку немає.</summary>
    public static string? Field(AutomationElement view, string label)
    {
        var parts = view.FindFirstDescendant(cf => cf.ByAutomationId(HeaderId))?.FindAllChildren();
        if (parts is null)
            return null;

        var index = Array.FindIndex(parts, part => part.ControlType == ControlType.Text && part.Name == label);
        return index < 0
            ? null
            : parts.Skip(index + 1).FirstOrDefault(part => part.ControlType == ControlType.Edit)?.Patterns.Value.Pattern.Value.Value;
    }

    /// <summary>
    /// Перевіряє заголовок листа: відправник — обліковий запис Outlook <paramref name="accountAddress"/>, отримувач —
    /// створена персона з адресою листа, тема, дата надсилання — між <paramref name="sentFrom"/> і <paramref name="sentTo"/>.
    /// </summary>
    public void ShouldHaveValues(MailData expected, string accountAddress, DateTime sentFrom, DateTime sentTo) =>
        AllureApi.Step("Перевірити заголовок листа", () =>
        {
            var fromMinute = new DateTime(sentFrom.Ticks - sentFrom.Ticks % TimeSpan.TicksPerMinute, sentFrom.Kind);
            var sentAt = SentAt.Split(' ');
            this.ShouldSatisfyAllConditions(
                () => From.ShouldEndWith($"<{accountAddress}>", Case.Sensitive, "Поле «Von»"),
                () => To.ShouldBe(expected.RecipientLine, "Поле «An»"),
                () => Subject.ShouldBe(expected.Subject, "Поле «Betreff»"),
                () => (AdaicaFormat.ParseDate(sentAt[0])!.Value + AdaicaFormat.ParseTime(sentAt.ElementAtOrDefault(1) ?? string.Empty)!.Value)
                    .ShouldBeInRange(fromMinute, sentTo, "Поле «Versanddatum»"));
        });

    /// <summary>
    /// Вкладка «Beziehungen» → «Zugriffspfade»: записи, контакти й об'єкти, з якими зв'язано лист
    /// (<see cref="RecordAccessPaths.Read"/>).
    /// </summary>
    public RecordAccessPaths ReadAccessPaths() => RecordAccessPaths.Read(view, "листа");

    private string Field(string label) =>
        Field(view, label) ?? throw new InvalidOperationException($"У заголовку листа немає поля «{label}».");
}
