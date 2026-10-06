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
    private static readonly TimeSpan AdaicaPrepareTime = TimeSpan.FromSeconds(4);

    // Звертання надбудова ADAICA дописує в текст за 1–2 с після зміни отримувачів (для відомого контакту — довше).
    private static readonly TimeSpan BodySettleTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BodySettleTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BodyPollInterval = TimeSpan.FromMilliseconds(500);

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
            WaitUntilAdaicaPreparedMail();
            return new OutlookMail(inspector);
        });

    /// <summary>
    /// Відкривши лист, ADAICA ще налаштовує його вікно: через Redemption ставить фокус у поле «Кому» (AutomationId
    /// 4117). Якщо поля в цю мить немає або вікно вже не активне, ADAICA закриває лист («RefreshView … Could not find
    /// control with the Automation Id of 4117», далі «Outlook Email Editor: Canceled»). Сигналу про кінець
    /// налаштування не видно, а воно займає до 2–3 с, тож просто чекаємо. Шукати поле через UI Automation не можна:
    /// обхід дерева вікна листа (RichEdit) валив Outlook. Поки це триває, охорону фокусу треба призупинити
    /// (<see cref="ForegroundGuard.Pause"/>): вона забрала б у вікна листа фокус, і ADAICA так само закрила б лист.
    /// </summary>
    private static void WaitUntilAdaicaPreparedMail() => Thread.Sleep(AdaicaPrepareTime);

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

        /// <summary>
        /// Заповнює тему, текст, «Копія» (якщо <paramref name="cc"/> не порожній) і «Кому». Текст замінює все тіло
        /// листа, разом із підписом Outlook. Адреси — останніми: на зміну отримувачів надбудова ADAICA в Outlook сама
        /// дописує в текст звертання («Sehr geehrte Damen und Herren,» чи Briefanrede знайденого контакту), і якщо
        /// в цю мить тест теж змінює текст, Outlook падає в RICHED20.DLL. Тому після адрес тест чекає, доки текст
        /// перестане змінюватися, і повертає його — зі звертанням надбудови.
        /// </summary>
        public string Fill(string to, string subject, string body, string cc = "") =>
            AllureApi.Step($"Заповнити лист в Outlook: Кому {to}{(cc.Length > 0 ? $", Копія {cc}" : string.Empty)}, тема «{subject}»", () =>
            {
                _item.Subject = subject;
                _item.Body = body;
                if (cc.Length > 0)
                    _item.CC = cc;
                _item.To = to;
                ((string)_item.To).ShouldBe(to, "Поле «Кому» листа в Outlook");

                var text = WaitUntilBodySettled();
                AllureApi.AddAttachment("Текст листа перед надсиланням", "text/plain", System.Text.Encoding.UTF8.GetBytes(text), ".txt");
                return text;
            });

        // Текст листа, коли він не змінюється BodySettleTime поспіль (надбудова ADAICA дописала звертання).
        private string WaitUntilBodySettled()
        {
            var started = DateTime.Now;
            var last = (string)_item.Body;
            var stableSince = DateTime.Now;
            while (DateTime.Now - stableSince < BodySettleTime)
            {
                if (DateTime.Now - started > BodySettleTimeout)
                    throw new TimeoutException($"Текст листа в Outlook змінюється довше за {BodySettleTimeout.TotalSeconds} с.");

                Thread.Sleep(BodyPollInterval);
                var current = (string)_item.Body;
                if (current != last)
                {
                    last = current;
                    stableSince = DateTime.Now;
                }
            }

            return last;
        }

        /// <summary>Надсилає лист; Outlook закриває його вікно, а ADAICA пропонує зберегти лист.</summary>
        public void Send() => AllureApi.Step("Надіслати лист з Outlook", () =>
        {
            // Без явного розпізнавання адрес Send падає з ArgumentException («Value does not fall within the expected
            // range»), коли в Outlook уже є така сама людина з іншою адресою (другий лист тій самій персоні).
            IsOpen.ShouldBeTrue("Вікно листа в Outlook закрилося до надсилання: ADAICA скасувала лист (див. її лог, «Outlook Email Editor: Canceled»)");
            ((bool)_item.Recipients.ResolveAll()).ShouldBeTrue("Outlook не розпізнав адреси листа");
            _item.Send();
            _sent = true;
        });

        /// <summary>Чи вікно листа ще відкрите: закрите вікно COM більше не відповідає.</summary>
        public bool IsOpen
        {
            get
            {
                try
                {
                    _ = (string)_inspector.Caption;
                    return true;
                }
                catch (Exception ex) when (ex is COMException or InvalidComObjectException)
                {
                    return false;
                }
            }
        }

        /// <summary>Для прибирання після тесту: закриває вікно ненадісланого листа без збереження чернетки.</summary>
        public void DiscardIfNotSent()
        {
            if (!_sent && IsOpen)
                _inspector.Close(OlDiscard);
        }
    }
}
