using System.Runtime.InteropServices;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Outlook, у якому ADAICA відкриває новий лист (вузол «E-Mails» → «Neu»). Тест керує ним через COM-автоматизацію
/// (Outlook.Application) — без миші, клавіатури й залежності від мови інтерфейсу Outlook — і чіпає лише вікно нового
/// листа, яке відкрила ADAICA: Outlook зазвичай уже відкритий у користувача, і ADAICA працює з тим самим процесом.
/// Сам Outlook тест не закриває ніколи.
/// </summary>
/// <remarks>
/// Поля вікна листа через UI Automation заповнити не вдалося: «Кому» в Outlook — RichEdit, і SetValue на ньому
/// падає з ElementNotAvailableException. PowerShell, запущений поза тестом, Outlook через COM не бачить
/// (MK_E_UNAVAILABLE, інший рівень доступу), а тестовий процес — бачить.
/// </remarks>
public static class OutlookApplication
{
    // olDiscard: закрити вікно листа без збереження чернетки.
    private const int OlDiscard = 1;

    // Outlook реєструється в таблиці запущених об'єктів не одразу, а вікно листа відкривається кілька секунд.
    private static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Скільки вікон листів (Inspectors) відкрито зараз; передайте в <see cref="WaitForNewMail"/>.</summary>
    public static int OpenMailWindowCount() => TryAttach() is { } app ? (int)((dynamic)app).Inspectors.Count : 0;

    /// <summary>
    /// SMTP-адреса облікового запису Outlook за замовчуванням — з неї Outlook надішле лист, і цей запис ADAICA
    /// пропонує в асистенті «Mail-Einstellungen».
    /// </summary>
    public static string DefaultAccountAddress()
    {
        dynamic outlook = Attach();
        dynamic accounts = outlook.Session.Accounts;
        int count = accounts.Count;
        for (var index = 1; index <= count; index++)
        {
            string address = accounts.Item(index).SmtpAddress;
            if (!string.IsNullOrEmpty(address))
                return address;
        }

        throw new InvalidOperationException("В Outlook немає облікового запису з SMTP-адресою.");
    }

    /// <summary>
    /// Чекає вікно нового листа, яке відкрила ADAICA: з'явилося після <paramref name="mailWindowsBefore"/> вікон,
    /// лист ще не надісланий і без отримувачів.
    /// </summary>
    public static OutlookMail WaitForNewMail(int mailWindowsBefore) =>
        AllureApi.Step("Дочекатися вікна нового листа в Outlook", () =>
        {
            dynamic outlook = Attach();
            var inspector = Wait.For(
                () =>
                {
                    dynamic inspectors = outlook.Inspectors;
                    int count = inspectors.Count;
                    if (count <= mailWindowsBefore)
                        return null;

                    dynamic last = inspectors.Item(count);
                    dynamic item = last.CurrentItem;
                    return !(bool)item.Sent && (int)item.Recipients.Count == 0 ? (object)last : null;
                },
                "вікна нового листа в Outlook",
                OpenTimeout);
            return new OutlookMail(inspector);
        });

    private static object Attach() => Wait.For(TryAttach, "запущеного Outlook (COM, Outlook.Application)", OpenTimeout);

    private static object? TryAttach()
    {
        try
        {
            CLSIDFromProgID("Outlook.Application", out var classId);
            GetActiveObject(ref classId, 0, out var application);
            return application;
        }
        catch (COMException)
        {
            return null;
        }
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void CLSIDFromProgID(string progId, out Guid classId);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid classId, nint reserved, [MarshalAs(UnmanagedType.IUnknown)] out object application);

    /// <summary>Новий лист у вікні Outlook, яке відкрила ADAICA.</summary>
    public sealed class OutlookMail
    {
        private readonly dynamic _inspector;
        private readonly dynamic _item;
        private bool _sent;

        internal OutlookMail(object inspector)
        {
            _inspector = inspector;
            _item = _inspector.CurrentItem;
        }

        /// <summary>Заповнює «Кому», тему й текст. Текст замінює все тіло листа, разом із підписом Outlook.</summary>
        public void Fill(string to, string subject, string body) =>
            AllureApi.Step($"Заповнити лист в Outlook: Кому {to}, тема «{subject}»", () =>
            {
                _item.To = to;
                _item.Subject = subject;
                _item.Body = body;
                ((string)_item.To).ShouldBe(to, "Поле «Кому» листа в Outlook");
            });

        /// <summary>Надсилає лист; Outlook закриває його вікно, а ADAICA пропонує зберегти лист.</summary>
        public void Send() => AllureApi.Step("Надіслати лист з Outlook", () =>
        {
            _item.Send();
            _sent = true;
        });

        /// <summary>Для прибирання після тесту: закриває вікно ненадісланого листа без збереження чернетки.</summary>
        public void DiscardIfNotSent()
        {
            if (!_sent)
                _inspector.Close(OlDiscard);
        }
    }
}
