using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Titel» (кнопка «...» біля поля «Titel» у «Neue Person/Firma»): дерево титулів із прапорцями
/// (Prof., Dr., Dipl.-Ing., …). Після OK вибрані титули стоять у полі «Titel».
/// </summary>
public sealed class TitlesDialog(Window owner) : CheckTreeDialog(owner, "wndTitles", "Titel");
