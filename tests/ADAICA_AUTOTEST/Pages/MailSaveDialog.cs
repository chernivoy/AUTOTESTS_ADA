using System.Drawing;
using System.Runtime.InteropServices;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «E-Mail-Speicherndialog» — асистент збереження листа в ADAICA після надсилання з Outlook. Угорі — листи, що
/// зберігаються, унизу — вкладки зв'язків «Kontakte», «Dokumentation», «Objekte». На «Kontakte» — відправник («Von»)
/// і отримувачі («An»); отримувач, якого ADAICA не знайшла серед контактів, позначений «?», і поки його не зв'язано
/// з контактом, OK недоступна («Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen»).
/// </summary>
/// <remarks>
/// Вміст вкладок зв'язків (список контактів, рядок пошуку, кнопки) UI Automation не показує навіть у raw-дереві —
/// видно лише заголовки вкладок. Тому рядок контакту тест знаходить за координатами в межах «recordLinkView»: висота
/// заголовків вкладок і таблиці й висота рядка задані в незалежних від DPI одиницях WPF, а масштаб беремо з DPI вікна.
/// </remarks>
public sealed class MailSaveDialog
{
    private const string WindowTitle = "E-Mail-Speicherndialog";
    private const string FindCreateContactItem = "Zugehörigen Kontakt finden/erstellen";

    // Розмітка списку «Kontakte» в одиницях WPF (1/96 дюйма): середина першого рядка й висота рядка,
    // а по горизонталі — колонка «Funktion».
    private const double FirstContactRowCenter = 64.5;
    private const double ContactRowHeight = 20;
    private const double FunctionColumnCenter = 60;

    // Після OK ADAICA зберігає лист і його вкладення.
    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(60);

    private readonly Window _mainWindow;

    public MailSaveDialog(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(
            () => mainWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("lvMail")) is not null)?.AsWindow(),
            $"вікна «{WindowTitle}»");

        OkButton = new ButtonControl("OK", () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))));
        CancelButton = new ButtonControl("Abbrechen", () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Abbrechen"))));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    /// <summary>Теми листів у списку «Betreff».</summary>
    public IReadOnlyList<string> Subjects =>
        Root.FindByAutomationId("lvMail")
            .FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => row.FindFirstDescendant(cf => cf.ByAutomationId("TextElement"))?.Name ?? string.Empty)
            .ToList();

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => _mainWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    public void ShouldHaveSubjects(params string[] expected) =>
        AllureApi.Step($"Перевірити листи у вікні «{WindowTitle}»: {string.Join("; ", expected)}",
            () => Subjects.ShouldBe(expected, $"Список «Betreff» у вікні «{WindowTitle}»"));

    /// <summary>
    /// Контекстне меню рядка <paramref name="contactRow"/> (з нуля: «Von», далі отримувачі) на вкладці «Kontakte» →
    /// «Zugehörigen Kontakt finden/erstellen». ADAICA відкриває пошук контакту «Finden &amp; Erstellen» як дочірнє
    /// вікно цього вікна.
    /// </summary>
    public ContactFindCreateWindow OpenFindCreateContact(int contactRow) =>
        AllureApi.Step($"Рядок {contactRow + 1} на вкладці «Kontakte» → контекстне меню → «{FindCreateContactItem}»", () =>
        {
            AutomationElement? Menu() =>
                Popups.FindDescendant(Root, cf => cf.ByAutomationId("ContactMenu"))
                ?? Popups.FindDescendant(_mainWindow, cf => cf.ByAutomationId("ContactMenu"));

            var links = Root.FindByAutomationId("recordLinkView");
            var scale = GetDpiForWindow(Root.Properties.NativeWindowHandle.Value) / 96.0;
            var row = new Point(
                (int)Math.Round(FunctionColumnCenter * scale),
                (int)Math.Round((FirstContactRowCenter + contactRow * ContactRowHeight) * scale));
            UiInput.RightClick(() => links, () => Menu() is not null, "контекстного меню контакту у вікні збереження листа", offset: row);

            new MenuItemControl(FindCreateContactItem, () => Wait.For(
                () => Menu()?.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(FindCreateContactItem))),
                $"пункту «{FindCreateContactItem}»")).Click();
            return new ContactFindCreateWindow(Root);
        });

    /// <summary>OK: зберігає лист із зв'язками; ADAICA закриває вікно й відкриває збережений лист.</summary>
    public void Save() => AllureApi.Step($"Зберегти лист кнопкою OK у вікні «{WindowTitle}»", () =>
    {
        Wait.Until(() => OkButton.IsEnabled, $"доступної кнопки OK у вікні «{WindowTitle}» (усі зв'язки задано)");
        OkButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{WindowTitle}»", SaveTimeout);
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{WindowTitle}» без збереження листа", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{WindowTitle}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
}
