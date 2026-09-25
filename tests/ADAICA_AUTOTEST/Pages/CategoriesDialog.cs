using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно «Kategorie» (кнопка «...» під списком категорій контакту): дерево категорій із прапорцями.</summary>
public sealed class CategoriesDialog(Window owner) : CheckTreeDialog(owner, "wndCategories", "Kategorie");
