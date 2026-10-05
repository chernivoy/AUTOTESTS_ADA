using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Список «Verbindung» у картці контакту: групи (Telefon, Fax, E-Mail, …), а в них рядки «значення, тип, примітка».
/// Новий рядок додається кнопкою «+» і редагується просто в списку: два поля, список типу й кнопка збереження.
/// Ні рядки, ні їхні поля не мають AutomationId, тому беремо їх за порядком.
/// </summary>
/// <param name="listId">Список у картці: «lvCommunicationCompany» у фірми, «communications» у персони.</param>
public sealed class CommunicationsList(Window owner, AutomationElement root, string listId = "lvCommunicationCompany")
{
    /// <summary>Додає рядок і зберігає його кнопкою в самому рядку. Некоректне значення ADAICA не приймає й показує повідомлення.</summary>
    public void Add(CommunicationData communication) =>
        AllureApi.Step($"Додати у «Verbindung» {communication.Kind}: {communication.Value}", () =>
        {
            // Кнопка «+» відкриває меню лише на клік: InvokePattern вона не підтримує.
            UiInput.LeftClickOpeningMenu(
                () => root.FindByAutomationId("tbtnAddSpecifyCompanyCommunications"),
                () => FindInPopups(communication.Kind) is not null,
                "меню «Verbindung»");
            new MenuItemControl(communication.Kind, () => Wait.For(() => FindInPopups(communication.Kind), $"пункту «{communication.Kind}» у меню «Verbindung»"))
                .Click();

            var row = Wait.For(EditedRow, "нового рядка у «Verbindung»");
            var edits = row.FindAllChildren(cf => cf.ByControlType(ControlType.Edit));
            new TextBoxControl($"{communication.Kind}: Wert", () => edits[0]).Fill(communication.Value);
            new ComboBoxControl($"{communication.Kind}: Typ", () => Wait.For(() => row.FindFirstChild(cf => cf.ByControlType(ControlType.ComboBox)), "списку типу в рядку"))
                .Select(communication.Type);
            if (communication.Remark.Length > 0)
                new TextBoxControl($"{communication.Kind}: Bemerkung", () => edits[1]).Fill(communication.Remark);

            // Перша кнопка рядка зберігає його, друга скасовує зміни.
            new ButtonControl("Speichern", () => Wait.For(() => row.FindFirstChild(cf => cf.ByControlType(ControlType.Button)), "кнопки збереження рядка")).Click();

            string? error = null;
            Wait.Until(() =>
            {
                if (EditedRow() is null)
                    return true;

                var message = MessageBoxDialog.Find(owner);
                if (message is null)
                    return false;

                error = message.Message.Text;
                message.Button("OK").Click();
                return true;
            }, $"збереження рядка «{communication.Value}» у «Verbindung»");

            if (error is not null)
                throw new InvalidOperationException($"Рядок «{communication.Value}» у «Verbindung» не збережено: {error}");
        });

    /// <summary>Збережені рядки в порядку показу: групи й рядки в групі.</summary>
    public IReadOnlyList<CommunicationData> Items =>
        List.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem))
            .SelectMany(group =>
            {
                var kind = group.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty;
                return group.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem))
                    .Select(item => item.FindAllChildren(cf => cf.ByControlType(ControlType.Text)).Select(text => text.Name).ToArray())
                    .Select(texts => new CommunicationData(kind, texts[0], texts[1], texts.ElementAtOrDefault(2) ?? string.Empty));
            })
            .ToList();

    public void ShouldHaveItems(IReadOnlyList<CommunicationData> expected) =>
        AllureApi.Step("Перевірити список «Verbindung»", () =>
            Items.ShouldBe(expected, ignoreOrder: true, "Список «Verbindung»"));

    private AutomationElement List =>
        Wait.For(() => root.FindByAutomationId(listId).FindFirstChild(cf => cf.ByControlType(ControlType.Tree)), "списку «Verbindung»");

    // Рядок, що редагується, має поля вводу; збережений — лише текст.
    private AutomationElement? EditedRow() =>
        List.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .FirstOrDefault(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Edit)) is not null);

    // Меню кнопки «+» відкривається у WPF Popup (див. Popups).
    private AutomationElement? FindInPopups(string itemName) =>
        Popups.FindDescendant(owner, cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(itemName)));
}
