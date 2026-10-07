using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно співробітника з меню його вузла: «Mitarbeiter bearbeiten: Nachname, Vorname» чи «Mitarbeiter umziehen: …».
/// У вікон і їхніх кнопок OK / Abbrechen немає AutomationId, тому вікно шукаємо за заголовком, а кнопки — за підписом.
/// В обох — таблиця функцій співробітника у відділі й прапорці «Primärer Briefempfänger…».
/// </summary>
public abstract class EmployeeWindow
{
    private readonly Window _owner;
    private readonly string _title;

    /// <param name="owner">Головне вікно ADAICA.</param>
    /// <param name="title">Заголовок вікна без імені співробітника: «Mitarbeiter bearbeiten».</param>
    protected EmployeeWindow(Window owner, string title)
    {
        _owner = owner;
        _title = title;
        Root = Wait.For(
            () => owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .FirstOrDefault(window => window.Name.StartsWith($"{title}: ", StringComparison.Ordinal))?.AsWindow(),
            $"вікна «{title}»");

        Functions = new EmployeeFunctionsGrid(Root);
        OkButton = new ButtonControl("OK", () => Button("OK"));
        CancelButton = new ButtonControl("Abbrechen", () => Button("Abbrechen"));
    }

    public EmployeeFunctionsGrid Functions { get; }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх (як ChildDialog).
    public bool IsOpen
    {
        get
        {
            try
            {
                return _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    protected Window Root { get; }

    /// <summary>Натискає OK і чекає, доки вікно закриється; якщо ADAICA показала повідомлення, падає з його текстом.</summary>
    public void Confirm() => AllureApi.Step($"Підтвердити вікно «{Root.Name}»", () =>
    {
        OkButton.Click();

        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;

            var message = new[] { Root, _owner }.Select(MessageBoxDialog.Find).FirstOrDefault(found => found is not null);
            if (message is null)
                return false;

            error = $"{message.Title}: {message.Message.Text}";
            return true;
        }, $"закриття вікна «{_title}»", TimeSpan.FromSeconds(30));

        if (error is not null)
            throw new InvalidOperationException($"Вікно «{_title}» не закрилося, ADAICA показала діалог «{error}».");
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{_title}» без змін", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{_title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    protected AutomationElement Button(string caption) =>
        Wait.For(
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(caption))),
            $"кнопки «{caption}» у вікні «{_title}»");

    protected AutomationElement CheckBox(string caption) =>
        Wait.For(
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox).And(cf.ByName(caption))),
            $"прапорця «{caption}» у вікні «{_title}»");
}

/// <summary>«Mitarbeiter bearbeiten»: функції співробітника в його відділі.</summary>
public sealed class EmployeeEditWindow(Window owner) : EmployeeWindow(owner, "Mitarbeiter bearbeiten");

/// <summary>
/// «Mitarbeiter umziehen»: з відділу «aus Organisationseinheit» (лише для читання) в «in Organisationseinheit». Список
/// відділів не має ні поточного відділу, ні «andere Tätigkeiten»: туди повертає лише «Mitarbeiter aus der
/// Organisationseinheit lösen». Поки відділ не вибрано, таблиця функцій і OK недоступні. Таблиця пропонує поточні
/// функції співробітника як нові («*»); стандартну «Mitarbeiter» перша додана функція замінює, інші лишаються. «Mitarbeiter kopieren» лишає співробітника й у
/// старому відділі; з «andere Tätigkeiten» копіювати не можна.
/// </summary>
public sealed class EmployeeMoveWindow : EmployeeWindow
{
    public EmployeeMoveWindow(Window owner)
        : base(owner, "Mitarbeiter umziehen")
    {
        SourceDepartment = new TextBoxControl("aus Organisationseinheit",
            () => Wait.For(() => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Edit)), "поля «aus Organisationseinheit»"));
        TargetDepartment = new ComboBoxControl("in Organisationseinheit", Locate.ById(Root, "cmbTargetDepartmentsWndEmployeeMove"));
        CopyEmployee = new CheckBoxControl("Mitarbeiter kopieren", () => CheckBox("Mitarbeiter kopieren"));
    }

    public TextBoxControl SourceDepartment { get; }

    public ComboBoxControl TargetDepartment { get; }

    public CheckBoxControl CopyEmployee { get; }
}
