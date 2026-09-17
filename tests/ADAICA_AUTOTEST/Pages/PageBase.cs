using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Page Object для одного вікна застосунку. Конструктор чекає, доки вікно з'явиться.
/// </summary>
public abstract class PageBase
{
    protected PageBase(AppSession session, string windowAutomationId)
    {
        Session = session;
        Window = session.WaitForWindow(windowAutomationId);
    }

    public Window Window { get; }

    protected AppSession Session { get; }

    protected AutomationElement Element(string automationId) => Window.FindByAutomationId(automationId);
}
