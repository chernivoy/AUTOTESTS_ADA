using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>Записи під вузлами DMS для тестів workflow: документи, на яких його запускають, і цілі публікації.</summary>
public abstract partial class AdaicaTestBase
{
    /// <summary>
    /// Документ справи з Word-файлу під вузлом DMS <paramref name="parentNode"/> («DMS – Workflows» чи «DMS –
    /// Veröffentlichungen»). Файл прибирається після тесту, документ лишається в базі.
    /// </summary>
    protected TestWordFile CreateDmsDocument(MainPage mainPage, CustomerCase customer, string parentNode) =>
        AllureApi.Step($"Створити документ під «{parentNode}»", () =>
        {
            var file = TestWordFile.CreateDocx(customer.NewWordFile());
            OnCleanup(file.Dispose);
            mainPage.NewChildDocumentFromFile(parentNode, file.Path);
            mainPage.WaitForOpenedDocument(file.Name);
            return file;
        });

    /// <summary>
    /// Ordner справи під «DMS – Veröffentlichungen» (меню вузла → «Neu» → «Ordner»): ціль публікації workflow. Повертає
    /// підпис «назва (OPE…)», яким Ordner підписано і в дереві навігатора (під «Ordner»), і в дереві шляхів публікації.
    /// </summary>
    protected string CreatePublicationFolder(MainPage mainPage, CustomerCase customer) =>
        AllureApi.Step($"Створити Ordner під «{MainPage.PublicationsNodeName}»", () =>
        {
            var dialog = mainPage.OpenNewChildFolderDialog(MainPage.PublicationsNodeName);
            OnCleanup(dialog.CancelIfOpen);
            var folder = customer.NewFolder();
            dialog.Form.Fill(folder);
            var number = dialog.Form.Number.Text;
            dialog.Save();
            mainPage.WaitForOpenedFolder(number);
            return NodeWithNumber(folder.Name, number);
        });
}
