using System.Drawing;
using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.WindowsAPI;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>
/// Фото контакту (ImagePicker). Усе робиться через його контекстне меню, яке відкривається лише правим кліком.
/// Меню — окреме вікно Popup на робочому столі, а не дочірнє вікно головного, тому шукаємо його серед вікон процесу.
/// Самого зображення в дереві UI Automation немає: є воно чи ні, видно з меню («Löschen» доступне лише з фото),
/// а що саме показано — зі знімка контрола.
/// </summary>
public sealed class ImagePickerControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    private const string ContextMenuId = "ContextMenuImagePicker";
    private const string AddMenuItemId = "mnuImagePickerAdd";
    private const string AddFromFileMenuItemId = "mnuImagePickerAddFileImage";
    private const string DeleteMenuItemId = "mnuImagePickerDelete";

    // Стандартний діалог Windows «Відкрити»: клас вікна і AutomationId поля імені файлу та кнопки «Відкрити»
    // однакові в усіх мовах Windows, на відміну від підписів.
    private const string FileDialogClass = "#32770";
    private const string FileNameEditId = "1148";
    private const string OpenButtonId = "1";

    // JPEG трохи змінює кольори, а краї знімка — рамка й тло контрола.
    private const int ColorTolerance = 40;
    private const double MinColorShare = 0.2;

    /// <summary>Фото є: у контекстному меню доступне «Löschen».</summary>
    public bool HasImage
    {
        get
        {
            var menu = OpenContextMenu();
            try
            {
                return menu.FindByAutomationId(DeleteMenuItemId).IsEnabled;
            }
            finally
            {
                CloseContextMenu();
            }
        }
    }

    /// <summary>Контекстне меню → «Hinzufügen» → «Aus dem Dateisystem» → вибір файлу в діалозі Windows.</summary>
    public void AddFromFile(string path) => Step($"Додати в «{Name}» фото з файлу {Path.GetFileName(path)}", () =>
    {
        var menu = OpenContextMenu();
        menu.FindByAutomationId(AddMenuItemId).Patterns.ExpandCollapse.Pattern.Expand();
        // Підменю — ще одне вікно Popup процесу.
        Wait.For(() => FindInProcessWindows(AddFromFileMenuItemId), "пункту «Aus dem Dateisystem»")
            .Patterns.Invoke.Pattern.Invoke();

        var dialog = Wait.For(FileDialog, "діалогу вибору файлу");
        new TextBoxControl("Dateiname", () => dialog.FindByAutomationId(FileNameEditId).FindFirstChild(cf => cf.ByClassName("Edit")) ?? dialog.FindByAutomationId(FileNameEditId))
            .Fill(path);
        dialog.FindByAutomationId(OpenButtonId).AsButton().Invoke();
        Wait.Until(() => FileDialog() is null, "закриття діалогу вибору файлу");
        Wait.Until(() => HasImage, $"фото в «{Name}»");
    });

    public void ShouldHaveImage() =>
        Step($"Перевірити, що в «{Name}» є фото", () => HasImage.ShouldBeTrue($"У «{Name}» немає фото"));

    /// <summary>
    /// Перевіряє, що показано саме наше фото: на знімку контрола щонайменше п'ята частина пікселів має колір його тла.
    /// </summary>
    public void ShouldShowImageWithBackground(Color background) =>
        Step($"Перевірити, що «{Name}» показує фото з тлом {ColorTranslator.ToHtml(background)}", () =>
        {
            using var capture = Capture.Element(Element);
            var bitmap = capture.Bitmap;
            var matching = 0;
            // Кожен другий піксель: для оцінки частки цього досить.
            var total = 0;
            for (var x = 0; x < bitmap.Width; x += 2)
            {
                for (var y = 0; y < bitmap.Height; y += 2, total++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (Math.Abs(pixel.R - background.R) <= ColorTolerance
                        && Math.Abs(pixel.G - background.G) <= ColorTolerance
                        && Math.Abs(pixel.B - background.B) <= ColorTolerance)
                    {
                        matching++;
                    }
                }
            }

            var share = (double)matching / Math.Max(total, 1);
            share.ShouldBeGreaterThanOrEqualTo(MinColorShare, $"Частка пікселів кольору тла фото в «{Name}»");
        });

    private AutomationElement OpenContextMenu()
    {
        UiInput.RightClick(() => Element, () => FindInProcessWindows(ContextMenuId) is not null, $"контекстного меню «{Name}»");
        return Wait.For(() => FindInProcessWindows(ContextMenuId), $"контекстного меню «{Name}»");
    }

    // Меню закривається клавішею Esc, як у користувача; підменю закривається разом із ним.
    private void CloseContextMenu()
    {
        if (FindInProcessWindows(ContextMenuId) is { } menu)
            UiInput.PressKey(() => menu, VirtualKeyShort.ESCAPE, () => FindInProcessWindows(ContextMenuId) is null, $"закриття контекстного меню «{Name}»");
    }

    private AutomationElement[] ProcessWindows()
    {
        var element = Element;
        var processId = element.Properties.ProcessId.Value;
        return element.Automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(processId));
    }

    private AutomationElement? FindInProcessWindows(string automationId) =>
        ProcessWindows()
            .Select(window => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)))
            .FirstOrDefault(found => found is not null);

    // Діалог Windows — дочірнє вікно головного вікна ADAICA.
    private AutomationElement? FileDialog() =>
        ProcessWindows()
            .Select(window => window.FindFirstChild(cf => cf.ByClassName(FileDialogClass)))
            .FirstOrDefault(found => found is not null);
}
