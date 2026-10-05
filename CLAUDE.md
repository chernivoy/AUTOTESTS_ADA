# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

UI-автотести для WPF-застосунку ADAICA на FlaUI (UI Automation) + NUnit. Спілкування, коментарі в коді й документація — українською. Детальний опис — у `README.md`.

## Команди

```powershell
dotnet build ADAICA_AUTOTEST.sln

# пароль передається лише змінною середовища; логін, база й схема — у ADAICA_AUTOTEST.runsettings
$env:ADAICA_PASSWORD = '<пароль>'
dotnet test tests/ADAICA_AUTOTEST

# один тест або клас
dotnet test tests/ADAICA_AUTOTEST --filter "FullyQualifiedName~DokumentationNavigator_NewProject"
dotnet test tests/ADAICA_AUTOTEST --filter "Category=Smoke"

# звіт після прогону
allure serve tests/ADAICA_AUTOTEST/bin/Debug/net10.0-windows/allure-results

# веб-запускач: вибір тестів чекбоксами, запуск у порядку вибору, звіт Allure (http://127.0.0.1:5080)
dotnet run --project tools/TestRunner
```

Веб-запускач (`tools/TestRunner`) запускає кожен тест окремим `dotnet test` з фільтром `NUnit.Where` і передає тестам `ADAICA_KEEP_ALLURE_RESULTS=true`, щоб `TestRunSetup` не чистив allure-results між тестами. Описи тестів він бере з XML-документації збірки, тож `/// <summary>` над тестом видно на сторінці.

Тести керують реальним вікном ADAICA, але мишу й клавіатуру не займають: вікно стоїть на моніторі з параметра `Monitor` (у `.runsettings` — `secondary`), а кліки йдуть повідомленнями у вікно (`InputMode`), тож користувач тим часом працює на іншому моніторі. Кожен тест запускає власний процес ADAICA і закриває його після себе.

## Архітектура

Три шари, кожен наступний нічого не знає про попередній:

- **`Tests/`** — сценарії. Успадковують `AdaicaTestBase` (вхід/вихід з ADAICA, `LogInAndOpenDokumentation`, `OnCleanup`). Тест не шукає елементи сам.
- **`Pages/`** — Page Objects. Оголошують поля як контроли й описують сценарії (`LoginAs`, `OpenNavigator`, `Save`).
- **`Controls/`** — контроли: одне поле чи кнопка, яке вміє заповнити значення, дочекатися його появи й перевірити (`Fill`, `Select`, `ShouldHave…`).
- **`Infrastructure/`** — запуск застосунку, очікування, налаштування, ввід миші, пошук у raw-дереві.

Ключові рішення, які не видно з окремого файлу:

- **Кожен тест = свіжий процес ADAICA.** `UiTestBase.StartApplication` запускає exe, `StopApplication` знімає скріншот при падінні, виконує прибирання й закриває застосунок. `AdaicaTestBase.CloseApplication` підтверджує вихід, а `OnCleanup` реєструє дії, які повертають UI у попередній стан. Прибирання виконується **після** скріншота, тому знімок показує стан у момент помилки.
- **Контроли шукають свій елемент щоразу заново** (`Control` + `Locate.ById`), бо WPF перебудовує дерево. Кроки звіту контроли створюють через `AllureApi.Step`, а не атрибутом `[AllureStep]`: так у назві кроку є назва поля, а винятки не загортаються в `TargetInvocationException`.
- **Один бланк на два місця.** `ProjectForm`, `TaskForm`, `AppointmentForm`, `NoteForm` працюють і у вікні нового запису (`dialog.Form`), і в області перегляду після збереження (`MainPage.WaitForOpened…`). Ordner має той самий бланк, що й проєкт (ті самі AutomationId), тому `FolderDialog` використовує `ProjectForm`/`ProjectData`; відрізняє його лише номер `OPE…`. Вікна нових записів успадковують `RecordDialog`, який знаходить вікно за головною вкладкою бланку (у вікон немає AutomationId) і вміє `Save`/`Cancel`.
- **Дані записів — record** (`ProjectData`, `TaskData`, …). Бланк має `Fill(data)` і `ShouldHaveValues(data)`; перевірка повідомляє про всі розбіжності одразу.
- **Назви, примітки й дати — природні й щоразу інші.** Тести беруть дані з `CustomerCase`: випадкова справа клієнта дає узгоджені німецькі тексти, наче їх увела людина, а дати — різні дні в межах ±60 днів від сьогодні. Це потрібно для майбутніх тестів повнотекстового пошуку, тому однакових службових назв на кшталт `Test_123` не додавайте.

## Особливості ADAICA, без яких тести не працюють

- **UI Automation вимкнено за замовчуванням.** `AppSession` запускає застосунок зі змінною `AutomationTests__AutomationPeerFeatureEnabled=true` (`TestSettings.AppEnvironment`). Без неї дерево елементів порожнє. Для FlaUInspect застосунок треба запускати з тією ж змінною.
- **Форма входу, робочий простір і діалоги — одне вікно `FlatWnd`;** діалоги є його дочірніми вікнами, а не вікнами робочого столу.
- **Частина UI реагує лише на клік** (список навігаторів, вкладки, вузли дерева, контекстні меню, лупа швидкого пошуку, кнопки-перемикачі): `SelectionItem`, `Toggle`, Enter, Shift+F10 і `ShowContextMenu` не діють. Такі дії йдуть лише через `UiInput.LeftClick/RightClick/PressKey` з умовою, що дія подіяла. `UiInput` надсилає їх повідомленнями у вікно (`WindowMessageInput`: на час кліку `AttachThreadInput` до потоку ADAICA, `SetCapture` її вікном і кнопка, позначена натиснутою, — без цього WPF відкидає клік, коли курсора над вікном немає, а `ButtonBase` не клікається). Не подіяло — у режимі `Background` повторює справжньою мишею (`AtomicMouse`) чи клавіатурою. `AtomicMouse` і `Keyboard` напряму не викликайте. Меню (WPF Popup) живуть лише в активному застосунку, тому правий клік, клавіші й `LeftClickOpeningMenu` спершу активують ADAICA, а `ForegroundGuard` повертає фокус користувачеві, коли меню закриється, — лише поки користувач працює (остання справжня дія не давніше 30 с), інакше фокус лишається в ADAICA й вікна тесту не смикаються; меню шукайте через `Popups` (неактивна ADAICA створює Popup окремим вікном робочого столу). Вікно на тестовий монітор переносить `AppSession.Launch` одразу після запуску; назад не повертаємо, бо ADAICA запам'ятовує місце при виході й наступного разу відкривається вже там (користувач просив, щоб вікно не з'являлося на основному моніторі). Деталі — README, «Робота паралельно з тестами».
- **Розгорнутий ComboBox блокує закриття вікна**, тому `ComboBoxControl` завжди згортає список.
- **Зміна бази на формі входу скидає користувача й пароль**, тому `LoginAs` спершу вибирає базу й схему.
- **Заголовки вкладок та інші елементи шаблонів** видно лише в raw-дереві (`RawTree.FindFirstDescendantRaw`).
- **Результати пошуку «Finden» віртуалізовані:** у дереві лише видимі рядки, тому `SearchWindow` гортає список і бере комірки за заголовками колонок. Новий запис Elasticsearch індексує із затримкою: перед перевірками «не знаходить» викликайте `WaitUntilFound`. Поведінку пошуку (джокери, режими, умлаути, ß/ss, розділові знаки, оператори, службові слова, числа) описано в таблиці README «Як шукає Elasticsearch». Тести пошуку розкладено за темами в `SearchTests.*.cs`; фікстури зі спецсимволами — сталі тексти в `SearchTests.cs`.
- **Тести пошуку, що падають навмисно через баги ADAICA,** перелічено в README («Тести, що падають навмисно»): `Finden_ResultColumns`, `Finden_SearchModeNames`, а також імовірні баги, які ще має підтвердити розробник: `Finden_QuestionMarkForUmlautOrSharpS`, `Finden_DecomposedUmlaut`, `Finden_NumberInSingleWordMode`, `Finden_ChineseTextAsWritten`.
- **Агент для тестування пошуку** — `.claude/agents/adaica-search-tester.md`. Для роботи з пошуком «Finden» (нові випадки, дослідження поведінки, розбір падінь) використовуйте його.
- **Нова фірма** створюється через Kontakte → Gesamtliste → «Neue Firma»: спершу вікно «Finden & Erstellen» (`ContactFindCreateWindow`, поле «Firma» → «Neu»), потім його дочірнє вікно «Neue Person/Firma» (`NewCompanyDialog`). Номера у фірми немає, тому назва щоразу унікальна (`CustomerCase.NewCompany`) і фірму тест упізнає за нею. Картку фірми редагує `CompanyForm.Edit` (дані — `CustomerCase.EditCompany`); які поля в тестовій базі не редагуються й чому — у README («Картка фірми»). Відділ фірми створюється з меню вузла «Mitarbeiter» під фірмою (`MainPage.OpenNewDepartmentDialog`), співробітник — з меню відділу через «Finden & Erstellen» → `NewPersonDialog` (у `ContactTests` це робить `CreateDepartmentWithEmployee`; дочірні записи під співробітником створює `CreateChildRecords`); Briefanrede нового співробітника ADAICA лишає шаблоном (README, «Співробітник відділу фірми»). Персону разом із новою фірмою створює Gesamtliste → «Neue Person»: у рядку пошуку — персона, фірма, адреса (Land і Bundesland — після кнопки з глобусом), телефон і e-mail, а порожні поля «Neue Person/Firma» доповнює `NewPersonDialog.FillMissing` (README, «Нова персона з фірмою»; там же баг зі зсувом Geburtstag).
- **Документ із файлу** створює `MainPage.NewChildDocumentFromFile` (меню запису «Neu» → «Dokument» → «Aus dem Dateisystem» → `FileOpenDialog`), а файл .docx складає `TestWordFile` з даних `CustomerCase.NewWordFile`. У діалозі «Відкрити» кнопку за AutomationId «1» не шукайте — «1» мають і файли списку; `FileOpenDialog` вводить шлях у лапках і натискає Enter. ADAICA не створює документ-дублікат (за назвою чи вмістом) і питає «Festlegung von Aktionen für den Import von doppelten Dateien», тож ім'я й текст файлу мають бути щоразу унікальні (README, «Документ із Word-файлу під фірмою»). Нову версію дає «Bearbeiten»: ADAICA відкриває копію у Word, а `WordApplication` (COM, лише свій документ; Word закриває `Quit`, лише якщо його запустив тест, і ніколи не вбиває процес) дописує абзац, зберігає й закриває документ; вікно версій — `DocumentVersionsWindow`. Документ із файлу — і останній крок `CreateChildRecords` (`CreateDocumentFromWordFile`).
- **Клас і підклас запису** (проєкт, Ordner, завдання, термін, інфозапис, документ — у документа в огляді, з OK) вибирає `RecordClassBox` (кнопка «Klasse» → `ClassSelectWindow`; вузол — лише кліком). Тестовий клас — `CustomerCase.TestClass`, вмикає його `CustomerCase.WithClass()` (тести навігаторів; тести пошуку чекають порожню «Klasse»). Якщо класу у вікні вибору немає, тест створює його через «Konfiguration der Klassenliste» (`ClassesConfigWindow`: меню списку → «Neu», «Sichtbar» увімкнути, прапорець підкласу, усі «Klassentypen», «Alle auswählen», OK) — README, «Клас і підклас запису».
- **Навігатор Dashboard («Today») відкривається лише в одній вкладці**, тому `LogInAndShowSingleNavigator` перемикається на наявну й після тесту повертає попередню. На вузлі користувача (корінь дерева, `MainPage.OpenUserTaskList`) — список «Meine Aufgaben» (`DashboardTaskList`) з фільтрами `TaskListFilter`; як вони поєднуються і як отримати завдання під кожен («Erster» → Akzeptieren, «Ablehnen», серія → Ungelesen/Geplant) — README, «Навігатор Dashboard». Колонки списку вмикає «Spaltenkonfiguration» (`ColumnsConfigDialog`, правий клік по заголовку); ADAICA запам'ятовує їх для користувача, тож тест повертає конфігурацію через `OnCleanup`. Тести — `DashboardTests`.
- **Дерево навігатора в UI Automation — плаский `ListBox`:** вкладеність видно лише з відступу тексту вузла (`MainPage.SelectedNavigatorNodePath`).
- **Дати й числа — у німецькому форматі** (`AdaicaFormat`, `dd.MM.yyyy`, `1500,00`).
- **Скріншоти й координати** правильні лише з per-monitor DPI awareness, яку вмикає `TestRunSetup`.

## Робота з тестовими даними й чужими вікнами

- Записи, створені тестами (Kurzname «MAT»), **лишаються в базі**: тести їх не видаляють. Назви можуть повторюватися, тому свій запис тест упізнає за номером (`PRO…`, `TAS…`, `APP…`), а інфозапис, у якого номера немає, — за «Bemerkung», що закінчується часом запису. Фірма номера теж не має, тому її назва щоразу унікальна.
- Стан, який ADAICA зберігає для користувача (відкриті вкладки навігаторів), тест має повернути через `OnCleanup`.
- На машині зазвичай відкриті власні екземпляри ADAICA користувача. Закривати або зупиняти можна **лише той процес, який запустив сам тест**, і не можна редагувати `appsettings.json` застосунку.

## Додати тест для нового типу запису

1. Знайти AutomationId у FlaUInspect (застосунок запустити зі змінною вище).
2. Створити `…Form` (контроли + `Fill` + `ShouldHaveValues`), `…Data` (record) і `…Dialog : RecordDialog`.
3. Додати в `MainPage` методи `OpenNew…Dialog` і `WaitForOpened…`, а якщо запис створюється і під іншим записом, — `OpenNewChild…Dialog` (меню запису «Neu» → тип запису) і крок у `AdaicaTestBase.CreateChildRecords` (його використовують тести дочірніх записів і в Dokumentation, і у фірми та співробітника в Kontakte).
4. Додати тест у `NavigatorTests`; прибирання — через `OnCleanup`.

## Що варто знати про звіти

- Падіння перевірок Shouldly Allure показує як **broken**, а не **failed**: Allure.NUnit вважає помилкою лише падіння `Assert.That`.
- Скріншот падіння додається до тесту в Allure й у папку `ArtifactsDirectory`.
