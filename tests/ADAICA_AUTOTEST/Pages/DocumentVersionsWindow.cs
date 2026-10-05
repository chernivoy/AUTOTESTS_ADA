using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно версій документа (кнопка «Version» у картці документа): таблиця версій, нова зверху. Колонки вмикає
/// «Spaltenkonfiguration» з контекстного меню заголовка. Клітинки рядка йдуть у порядку заголовків, а текст кожної —
/// у вкладеному TextBlock (порожня клітинка його не має).
/// </summary>
public sealed class DocumentVersionsWindow
{
    private const string WindowId = "wndRecDocVersion";

    private readonly Window _mainWindow;

    public DocumentVersionsWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(() => FindRoot(mainWindow), "вікна версій документа");
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnCancelWndRecDocVersion"));
    }

    public ButtonControl OkButton { get; }

    public bool IsOpen => FindRoot(_mainWindow) is not null;

    /// <summary>Підписи колонок у порядку показу.</summary>
    public IReadOnlyList<string> ColumnNames =>
        List.FindAllDescendants(cf => cf.ByAutomationId("PART_Content")).Select(header => header.Name).ToList();

    /// <summary>Рядки таблиці (нова версія зверху): підпис колонки → показаний текст.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows
    {
        get
        {
            var columns = ColumnNames;
            return List.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
                .Select(row =>
                {
                    var cells = row.FindAllChildren();
                    return (IReadOnlyDictionary<string, string>)columns
                        .Select((column, index) => (column, text: index < cells.Length ? CellText(cells[index]) : string.Empty))
                        .ToDictionary(pair => pair.column, pair => pair.text);
                })
                .ToList();
        }
    }

    private Window Root { get; }

    private AutomationElement List => Root.FindByAutomationId("lvRecDocVersion");

    /// <summary>
    /// Відкриває «Spaltenkonfiguration»: правий клік по заголовку колонки → «Änderung der Spaltenkonfiguration».
    /// У пункту меню немає AutomationId, тож шукаємо за підписом.
    /// </summary>
    public ColumnsConfigDialog OpenColumnsConfig() =>
        AllureApi.Step("Відкрити «Spaltenkonfiguration» вікна версій: заголовок колонки → Änderung der Spaltenkonfiguration", () =>
        {
            AutomationElement? ConfigItem() =>
                Popups.FindDescendant(Root, cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Änderung der Spaltenkonfiguration")));

            UiInput.RightClick(
                () => List.FindFirstDescendant(cf => cf.ByAutomationId("PART_Content")) ?? List,
                () => ConfigItem() is not null,
                "контекстного меню заголовка таблиці версій");
            new MenuItemControl("Änderung der Spaltenkonfiguration", () => Wait.For(ConfigItem, "пункту «Änderung der Spaltenkonfiguration»"))
                .Click();
            return new ColumnsConfigDialog(Root, _mainWindow);
        });

    public void Close() => AllureApi.Step("Закрити вікно версій документа", () =>
    {
        OkButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна версій документа");
    });

    /// <summary>Для прибирання: відкрите вікно версій не дає ADAICA завершитися.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    private static Window? FindRoot(Window mainWindow) =>
        mainWindow.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow();

    private static string CellText(AutomationElement cell) =>
        string.Concat(cell.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)).Select(text => text.Name));
}
