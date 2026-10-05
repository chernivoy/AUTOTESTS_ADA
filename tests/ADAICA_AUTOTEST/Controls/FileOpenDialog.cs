using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>
/// Стандартний діалог Windows «Відкрити», який ADAICA показує для вибору файлу (фото контакту, документ). Це вікно
/// класу #32770 — дочірнє вікно одного з вікон ADAICA. Клас і AutomationId поля імені файлу однакові в усіх мовах
/// Windows, на відміну від підписів.
/// </summary>
public static class FileOpenDialog
{
    private const string DialogClass = "#32770";
    private const string FileNameBoxId = "1148";

    /// <summary>Відкритий діалог застосунку, якому належить <paramref name="anyAppElement"/>, або null.</summary>
    public static AutomationElement? Find(AutomationElement anyAppElement)
    {
        var processId = anyAppElement.Properties.ProcessId.Value;
        return anyAppElement.Automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(processId))
            .Select(window => window.ClassName == DialogClass ? window : window.FindFirstChild(cf => cf.ByClassName(DialogClass)))
            .FirstOrDefault(found => found is not null);
    }

    /// <summary>
    /// Чекає діалог, вводить у поле імені повний шлях <paramref name="path"/> і відкриває файл клавішею Enter.
    /// </summary>
    /// <remarks>
    /// Шлях береться в лапки: у діалозі з вибором кількох файлів (такий ADAICA показує для документів) ім'я з пробілами
    /// без лапок Windows ділить на кілька імен. Кнопку «Відкрити» не натискаємо: AutomationId «1» мають і файли в списку
    /// діалогу, тож пошук за ним знаходить перший файл теки, і відкривається він, а не введений шлях.
    /// </remarks>
    public static void Open(AutomationElement anyAppElement, string path) =>
        AllureApi.Step($"Вибрати у вікні «Відкрити» файл {Path.GetFileName(path)}", () =>
        {
            var dialog = Wait.For(() => Find(anyAppElement), "діалогу вибору файлу");
            AutomationElement FileNameBox()
            {
                var box = dialog.FindByAutomationId(FileNameBoxId);
                return box.FindFirstChild(cf => cf.ByClassName("Edit")) ?? box;
            }

            new TextBoxControl("Dateiname", FileNameBox).Fill($"\"{path}\"");
            UiInput.PressKey(FileNameBox, VirtualKeyShort.RETURN, () => Find(anyAppElement) is null, "закриття діалогу вибору файлу");
        });
}
