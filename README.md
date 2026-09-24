# ADAICA_AUTOTEST

UI-автотести для WPF-застосунку ADAICA на **FlaUI (UI Automation) + NUnit**. Звіти формуються в Allure та JUnit XML, CI працює на Jenkins.

## Стек

| Що | Пакет | Версія |
|---|---|---|
| Платформа | .NET SDK | 10.0 (`net10.0-windows`) |
| Робота з UI | FlaUI.UIA3 | 5.0.0 |
| Тестовий фреймворк | NUnit + NUnit3TestAdapter | 4.6.1 / 6.3.0 |
| Перевірки | Shouldly | 4.3.0 |
| Звіт | Allure.NUnit | 2.15.0 |
| Звіт для Jenkins | JunitXml.TestLogger | 8.0.0 |

Версії пакетів задаються в одному місці — `Directory.Packages.props`.

## Структура

```
ADAICA_AUTOTEST.sln
└─ tests/ADAICA_AUTOTEST/
   ├─ Infrastructure/
   │  ├─ AppSession.cs                 запуск і закриття застосунку, пошук вікон
   │  ├─ UiTestBase.cs                 базовий клас тестів, скріншот при падінні
   │  ├─ TestSettings.cs               налаштування (змінні середовища / .runsettings)
   │  ├─ Wait.cs                       очікування елементів і умов
   │  ├─ RawTree.cs                    пошук у raw-дереві UI Automation
   │  ├─ AtomicMouse.cs                клік мишею одним викликом SendInput
   │  └─ TestImageFile.cs              JPG, який тест малює сам і прибирає після себе
   ├─ Controls/                        контроли: заповнення й перевірка окремих полів
   │  ├─ Control.cs                    базовий клас: назва, пошук елемента, доступність
   │  ├─ Locate.cs                     пошук елемента за ланцюжком AutomationId
   │  ├─ TextBoxControl.cs             текстове поле
   │  ├─ NumberBoxControl.cs           числове поле
   │  ├─ PasswordBoxControl.cs         поле пароля
   │  ├─ DatePickerControl.cs          поле дати (DatePicker3)
   │  ├─ DateTimePickerControl.cs      дата з часом
   │  ├─ ComboBoxControl.cs            випадний список, зокрема редагований
   │  ├─ ButtonControl.cs              кнопка
   │  ├─ ToggleButtonControl.cs        кнопка-перемикач
   │  ├─ CheckBoxControl.cs            прапорець
   │  ├─ MenuItemControl.cs            пункт меню, зокрема підменю
   │  ├─ TabItemControl.cs             вкладка
   │  ├─ LabelControl.cs               текст лише для читання
   │  ├─ ImagePickerControl.cs         фото контакту: додавання з файлу через контекстне меню, перевірка за знімком
   │  └─ AdaicaFormat.cs               формати дат і чисел ADAICA
   ├─ Pages/                           Page Objects ADAICA
   │  ├─ AdaicaWindow.cs               вікно FlatWnd: діалоги, вихід із підтвердженням
   │  ├─ LoginPage.cs                  форма входу (користувач, пароль, база, схема)
   │  ├─ MainPage.cs                   робочий простір після входу, вкладки й дерево навігаторів
   │  ├─ RecordDialog.cs               базове вікно нового запису: пошук вікна, OK, Abbrechen
   │  ├─ ProjectDialog.cs, ProjectForm.cs, ProjectData.cs   проєкт: вікно, бланк, дані
   │  ├─ FolderDialog.cs               Ordner: вікно (бланк і дані — ті самі, що в проєкту)
   │  ├─ TaskDialog.cs, TaskForm.cs, TaskData.cs            завдання: вікно, бланк, дані
   │  ├─ AppointmentDialog.cs, AppointmentForm.cs, AppointmentData.cs   термін: вікно, бланк, дані
   │  ├─ NoteDialog.cs, NoteForm.cs, NoteData.cs            інфозапис (Notiz): вікно, бланк, дані
   │  ├─ ContactFindCreateWindow.cs    вікно «Finden & Erstellen» на вкладці Kontakte: пошук перед створенням контакту
   │  ├─ NewCompanyDialog.cs, CompanyForm.cs, CompanyData.cs   фірма: вікно «Neue Person/Firma», картка (Allgemein, Details, Bankverbindungen), дані
   │  ├─ CompanyEditData.cs            дані редагування фірми: вкладки, рядки «Verbindung», банківський рахунок
   │  ├─ CommunicationsList.cs         список «Verbindung» у картці контакту: додавання й читання рядків
   │  ├─ ChildDialog.cs                базове невелике вікно з OK / Abbrechen
   │  ├─ CompanyNameDialog.cs, CategoriesDialog.cs, BankAccountDialog.cs   вікна «Firmendetails», «Kategorie», «Bankkonto»
   │  ├─ AddressFindCreateWindow.cs, NewAddressDialog.cs   нова адреса контакту: «Finden & Erstellen» (Adressen) і «Neue Adresse anlegen»
   │  ├─ MessageBoxDialog.cs           стандартний діалог повідомлення ADAICA
   │  └─ SearchWindow.cs, SearchResult.cs, SearchMode.cs, SearchFilters.cs   вікно пошуку «Finden», рядок результатів, режим пошуку, фільтри
   ├─ Tests/
   │  ├─ AdaicaTestBase.cs             базовий клас тестів ADAICA: вхід, відкриття навігатора, прибирання
   │  ├─ LoginTests.cs                 вхід в ADAICA
   │  ├─ CustomerCase.cs               дані записів: природні назви й примітки, щоразу інші
   │  ├─ SearchTests.cs                повнотекстовий пошук (Elasticsearch): джокери, режими, форми слів, колонки; фікстури
   │  ├─ SearchTests.*.cs              те саме за темами: QuerySyntax, GermanLocale, TextSymbols, Numbers, RecordTypes, Filters
   │  ├─ ContactTests.cs               навігатор Kontakte → створення фірми з унікальною назвою; редагування всіх полів картки
   │  └─ NavigatorTests.cs             навігатор Dokumentation → створення проєкту, Ordner, завдання, терміну, інфозапису; під кожним із них — дочірні записи
   ├─ TestRunSetup.cs                  дії перед усім прогоном (DPI, очищення Allure)
   ├─ ADAICA_AUTOTEST.runsettings      параметри прогону
   └─ allureConfig.json
.claude/agents/adaica-search-tester.md   агент Claude Code для тестування пошуку (див. «Агент для тестів пошуку»)
Jenkinsfile
```

## Локальний запуск

Потрібні Windows і .NET 10 SDK. Під час прогону не чіпайте мишу й клавіатуру: тести керують реальними вікнами.

Пароль передається лише змінною середовища, решта параметрів входу вже є в `ADAICA_AUTOTEST.runsettings`.

```powershell
dotnet build ADAICA_AUTOTEST.sln
$env:ADAICA_PASSWORD = '<пароль>'
dotnet test tests/ADAICA_AUTOTEST

# лише smoke-тести
dotnet test tests/ADAICA_AUTOTEST --filter "Category=Smoke"

# один тест
dotnet test tests/ADAICA_AUTOTEST --filter "FullyQualifiedName=ADAICA_AUTOTEST.Tests.LoginTests.Login_WithValidCredentials_OpensMainWindow"
```

Щоб запускати тести з IDE, задайте `ADAICA_PASSWORD` у змінних середовища користувача або в конфігурації запуску IDE.

Звіт Allure (Allure CLI вже встановлено через scoop):

```powershell
allure serve tests/ADAICA_AUTOTEST/bin/Debug/net10.0-windows/allure-results
```

## Налаштування

Змінна середовища має пріоритет над параметром з `ADAICA_AUTOTEST.runsettings`.

| Змінна середовища | Параметр .runsettings | За замовчуванням |
|---|---|---|
| `ADAICA_APP_PATH` | `AppPath` | обов'язковий (у `.runsettings` зараз задано `C:\adaica\feature_documents-in-postgre\ADAICA.exe`) |
| `ADAICA_APP_START_TIMEOUT` | `AppStartTimeoutSeconds` | 60 |
| `ADAICA_DEFAULT_TIMEOUT` | `DefaultTimeoutSeconds` | 10 |
| `ADAICA_LOGIN_TIMEOUT` | `LoginTimeoutSeconds` | 60 |
| `ADAICA_USER` | `UserName` | обов'язковий |
| `ADAICA_PASSWORD` | `Password` | обов'язковий |
| `ADAICA_DATABASE` | `Database` | обов'язковий |
| `ADAICA_SCHEMA` | `Schema` | обов'язковий |
| `ADAICA_ARTIFACTS_DIR` | `ArtifactsDirectory` | `<папка тестів>/artifacts` |

Паролі в `.runsettings` не зберігайте, передавайте їх через змінні середовища або Jenkins Credentials.

## Особливості ADAICA

- **UI Automation вимкнено за замовчуванням.** Поки `AutomationTests:AutomationPeerFeatureEnabled` в `appsettings.json` дорівнює `false`, ADAICA підміняє AutomationPeer вікон на порожні, і тести не бачать жодного елемента. `AppSession` запускає застосунок зі змінною `AutomationTests__AutomationPeerFeatureEnabled=true` (див. `TestSettings.AppEnvironment`), тому файл змінювати не треба.
- **Одне вікно на все.** Форма входу і робочий простір — це те саме вікно `FlatWnd`. Діалоги ADAICA в дереві UI Automation є його дочірніми вікнами, а не вікнами робочого столу, тому шукайте їх через `AdaicaWindow.FindMessageBox()`.
- **Діалоги під час входу.** Якщо версія клієнта не збігається з сервером, `LoginPage.LoginAs` натискає «Ja». Будь-який інший діалог вважається помилкою входу, і тест одразу падає з його текстом. Без ліцензії після входу відкривається вікно демоверсії, яке `MainPage` закриває.
- **Зміна бази скидає користувача й пароль**, тому `LoginAs` спершу вибирає базу й схему.
- **Поки список (ComboBox) розгорнутий, ADAICA ігнорує закриття вікна.** Після вибору пункту згортайте список.
- **Вихід.** Після кожного тесту `AdaicaTestBase` закриває ADAICA і підтверджує «Wollen Sie ADAICA verlassen?». Якщо за 30 с застосунок не завершився, процес зупиняється примусово.
- **Навігатори.** Кнопка «+» праворуч від вкладок (`PART_AddTab`) відкриває список навігаторів. Кожне відкриття додає нову вкладку в кінець смуги, а відкриті вкладки ADAICA запам'ятовує між сеансами, тож тест має закрити за собою те, що відкрив (див. `OnCleanup` нижче). Назва вкладки змінюється разом із вибраним вузлом, тому `CloseLastNavigatorTabs` закриває вкладки за кількістю, а не за назвою.
- **Лише мишею.** Пункт списку навігаторів і контекстне меню вузла дерева реагують тільки на клік мишею: `SelectionItem`, Enter, Shift+F10, клавіша меню й `ShowContextMenu` з UI Automation не діють. Для таких кліків є `AtomicMouse`. Він переміщує курсор і натискає кнопку одним викликом `SendInput`. Звичайний клік FlaUI робить це двома кроками, і якщо між ними курсор зсуне користувач або програма віддаленого доступу, клік піде в довільне місце екрана, навіть в інший застосунок. Перед кліком `AtomicMouse` перевіряє, що в цій точці екрана саме вікно ADAICA. Якщо ADAICA перекрито, він виводить її на передній план, а якщо це не допомогло, скасовує клік. Під час прогону не рухайте мишею.
- **Дерево навігатора.** До назв вузлів ADAICA додає діапазон записів («Projekte (1-25)»), тому `MainPage.NavigatorNode` порівнює лише назву. У UI Automation дерево — плаский список (`ListBox`), вкладеність видно лише з відступу тексту вузла. Тому `MainPage.SelectedNavigatorNodePath` визначає предків виділеного вузла за відступом: предок — найближчий вузол вище, текст якого починається лівіше.
- **Вікна нових записів.** Це дочірні вікна без AutomationId. `RecordDialog` знаходить їх за головною вкладкою бланку (`tiMainRecordFolder` у проєкті й Ordner, `tiMainRecordTask` у завданні, `tiMainRecordAppoint` у терміні, `tiMainRecordHistory` в інфозаписі). Вікно завдання відкривається до 7 с, тому таймаут на відкриття — 30 с.
- **Вікно проєкту.** «Neu» у контекстному меню вузла Projekte одразу відкриває вікно. Редагуються лише Name, Kurzname, дати, Kosten/Aufwand з одиницями та Bemerkung: клас і відповідальний задаються окремими діалогами, решта полів заповнюється автоматично. Дати й числа ADAICA показує в німецькому форматі (`18.09.2026`, `1500,00`), тому `ProjectData` зберігає суми як `decimal`. Кожне відкриття вікна займає наступний номер проєкту, навіть якщо закрити його без збереження.
- **Вікно Ordner.** «Neu» на вузлі Ordner одразу відкриває вікно «Neuer Ordner». Бланк у ньому той самий, що й у проєкту: ті самі поля й AutomationId, навіть головна вкладка `tiMainRecordFolder`. Тому `FolderDialog` використовує `ProjectForm` і `ProjectData`, а Ordner від проєкту відрізняє лише номер: `OPE…` замість `PRO…`. Після збереження ADAICA виділяє вузол «Назва (Номер)» у вузлі Ordner.
- **Після збереження** (OK) ADAICA сама переходить до нового проєкту: виділяє в дереві вузол «Назва (Номер)» і показує той самий бланк в області перегляду (`InformationView`). `MainPage.WaitForOpenedProject` шукає бланк лише там, бо у відкритому діалозі є поля з тими самими AutomationId. Назви проєктів можуть повторюватися між прогонами, тож свій проєкт тест упізнає за номером.
- **Вікно завдання.** На вузлі Aufgaben «Neu» — підменю, у ньому потрібен пункт «Neue Aufgabe» (`mnuNewTaskDefault`). Дати «Beginnt am», «Fällig am», «Erledigt am» складаються з дати й часу (`DateTimePickerControl`), «Geschlossen an» — лише дата. Поля Standort, Kundennutzen, Hinweis — це значення з довідника плюс довільний текст поруч. Примітку тест вводить на вкладці простого тексту, бо за замовчуванням може відкритися форматований редактор, який не приймає текст через UI Automation. Якщо заповнити «Erledigt am» і «Geschlossen an», статус стає «Aufgabe erledigt». У списку «Priorität» текст пунктів лежить лише в `Name`, і `ComboBoxControl` це враховує.
- **Після збереження завдання** ADAICA виділяє в дереві вузол лише з назвою, без номера, тож номер тест перевіряє в бланку (`MainPage.WaitForOpenedTask`).
- **Вікно терміну.** «Neu» на вузлі Termine одразу відкриває вікно. Beginn і Ende складаються з дати й часу. Поле «Aufwand» можна редагувати лише з увімкненим прапорцем «Aufwand (z.B. Arbeitszeit)». Без прапорця поле показує тривалість між Beginn і Ende. Якщо введені трудовитрати відрізняються від цієї тривалості, ADAICA підсвічує поле й пише попередження в HelpText, але зберегти дозволяє. Тест бере 120 Minute(n) для терміну на 2 години, тож попередження немає.
- **Після збереження терміну** ADAICA виділяє в дереві вузол «ррММдд ГГ:хх/+Nh Назва» (наприклад, «260918 10:00/+2h Besprechung mit Frau Keller»), без номера.
- **Вікно інфозапису (Notiz).** «Neu» на вузлі Notizen одразу відкриває вікно «Neue Notiz». Номера в інфозапису немає, тому тест знаходить збережений запис за «Bemerkung» (`MainPage.WaitForOpenedNote`). Примітка закінчується часом запису («Notiert am 18.09.2026 um 08:46 Uhr.»), тож у кожному прогоні вона інша. Дата «Beginnt» не може бути пізнішою за сьогодні: ADAICA позначає поле зірочкою («Das angegebene Datum darf nicht später als Heute sein») і вимикає OK. Тому тест бере лише минулі дні. У BLANK_IGOR список «Ergebnis» порожній, тож у даних він `null`. Після збереження в дереві виділяється вузол «дд.ММ.рр Назва».
- **Дочірні записи.** У контекстному меню будь-якого запису (проєкту, Ordner, завдання, терміну, інфозапису) «Neu» (`mnuNew`) — підменю з типами записів: Projekt (`mnuNewProject`), Ordner (`mnuNewProcess`), Aufgabe → Neue Aufgabe (`mnuNewTask` → `mnuNewTaskDefault`), Termin (`mnuNewAppoint`), Notiz (`mnuNewHistory`). Пункти в усіх меню мають ті самі id, лише порядок інший (у Ordner немає «Kontakt», у завдання є ще «Formular»). Вікна й бланки ті самі, що й для кореневих вузлів, лише в заголовку додається «(übergeordnet: Батько)». Після збереження ADAICA виділяє запис у дереві під батьківським записом, у папці його типу: «Батько → Aufgaben → Завдання». Назви завдань та інфозаписів можуть повторюватися, бо номера в дереві немає. Тому `MainPage` шукає батька для меню «Neu» серед виділеного вузла та його предків, а не першим збігом у дереві, а `CustomerCase` не дає дочірньому запису ту саму назву, що й батькові. Кореневі вузли Gesamtliste (Aufgaben, Termine, …) дочірніх записів не показують навіть після «Aktualisieren». Новий проєкт і кожен новий запис одразу мають власні `Notizen (1-1)` зі службовою приміткою «… wurde erstellt» і `Kontakte (1-1)`, тож лічильник Notizen під проєктом на один більший.
- **Нова фірма (навігатор Kontakte).** У контекстному меню вузла Gesamtliste пункт «Neue Firma» (`MnuCompanyNew`) відкриває не бланк, а вікно «Finden & Erstellen» на вкладці Kontakte (`ContactFindCreateWindow`): це те саме дочірнє вікно `This`, що й пошук «Finden», тому його впізнаємо за рядком пошуку контактів `ctrPACBox`. Назву вводять у поле «Firma» (`ctrPersonCompanyName` → `txtBoxCompanyName` → `txtBox`). Під рядком ADAICA із затримкою пише дію («Aktion: Firma erstellen»), і стає доступною кнопка «Neu» (`btnNewFindBoxPAC`). Вона відкриває вікно «Neue Person/Firma» (`wndPersonCompanyCreate`) — дочірнє вікно «Finden & Erstellen», а не головного вікна — з назвою вже в полі «Firmenname 1. Zeile». Після OK обидва вікна закриваються, ADAICA виділяє фірму у вузлі «Aktuelle» і показує її картку «Stammdaten» (`txtFullNameCompany`, `txtCompanyShortName`). Номера в картці фірми немає, тому фірму тест упізнає за назвою, яка щоразу унікальна (`CustomerCase.NewCompany`: назва фірми клієнта й номер з дати й часу, наприклад «Keller Treuhand AG 260923161021»). Вузли контактів ADAICA підписує з пробілами в кінці, а в картці контакту є TreeView категорій з тим самим AutomationId `treeView`, що й дерево навігатора, тому `MainPage` шукає дерево лише серед `ListBox`.
- **Картка фірми (редагування).** Поля невибраної вкладки (Allgemein, Details, Bankverbindungen) у дереві UI Automation немає, а картка відкривається на вкладці, яку відкривали останньою, тому `CompanyForm` щоразу вибирає потрібну вкладку, а `MainPage.WaitForOpenedCompany` спершу відкриває Allgemein. Перша зміна переводить картку в режим «Element in Bearbeitung», зберігає її кнопка OK внизу картки (`btnOKCtrMainForm`). Щоб перевірити збережені значення, а не те, що лишилося в картці, `MainPage.ReopenCompany` вибирає вузол «Favoriten» (там список без картки) і повертається до фірми. Що й як редагується:
  - **Назва** в картці лише для читання: це рядки назви з вікна «Firmendetails» (кнопка «...» біля назви) через пробіл. Там же рядок адреси й звертання, які ADAICA складає сама, тому тест їх фіксує («Fixieren»). Вузол фірми в дереві — повна назва й категорія в дужках: «Garage Steiner AG 260923165749 Autowerkstatt und Carrosserie Filiale Chur (Wartungsfirma)».
  - **«Verbindung»** (телефон, факс, e-mail, сайт, інше): кнопка «+» відкриває меню лише на клік мишею. Новий рядок редагується просто в списку (значення, тип, примітка, кнопка збереження) без AutomationId, тому `CommunicationsList` бере поля за порядком. Некоректну e-mail ADAICA не приймає й показує повідомлення.
  - **Категорія**: вікно «Kategorie» (кнопка «...» під списком категорій). Прапорець треба клікати мишею: після TogglePattern він вмикається, але кнопка OK лишається недоступною.
  - **Адреса**: «+» біля «Adressen» (меню лише на клік мишею) → категорія (Zweigstelle, Lieferanschrift, …) відкриває вікно «Finden & Erstellen» на вкладці Adressen (`AddressFindCreateWindow`). У рядку пошуку заповнюються всі поля: Land, Bundesland, «Postleitzahl Ort» (одне поле: «80331 München»), Ortsteil, Straße, Hausnummer. «Neu» стає доступною лише тоді, коли заповнено все, зокрема Bundesland, і такої адреси ще немає в довіднику адрес. Якщо адреса вже є, «Neu» недоступна, а «Finden» її знаходить («Es wurden 1 Datensätze gefunden.»). Адреси лишаються в довіднику після кожного прогону, тому тест тоді бере інший номер будинку (`CustomerCase.AnotherAddress`, до 5 спроб). «Neu» відкриває вікно «Neue Adresse anlegen» (`NewAddressDialog`) з адресою з рядка пошуку. Після OK ADAICA закриває обидва вікна й додає рядок до списку «Adressen» (Kategorie, Land, Postleitzahl, Ort, Ortsteil, Straße, Hausnummer; Bundesland список не показує). У довіднику тестової бази єдина країна — Deutschland, тому фірма зі Швейцарії отримує німецьку філію чи адресу доставки.
  - **Фото**: контрол `imageCompany` (`ImagePickerControl`). Контекстне меню відкривається лише правим кліком мишею й живе в окремому вікні Popup на робочому столі, а не в головному вікні, тому контрол шукає його серед вікон процесу. «Hinzufügen» → «Aus dem Dateisystem» відкриває стандартний діалог Windows «Bitte wählen Sie ein Bild aus.»: у ньому поле імені файлу (`1148`) і кнопку «Відкрити» (`1`) шукаємо за AutomationId, бо підписи залежать від мови Windows. JPG тест малює сам (`TestImageFile`: тло насиченого кольору й назва фірми) у теці `%TEMP%\ADAICA_AUTOTEST`. Щойно фірму збережено, файл видаляється, а якщо тест упаде раніше, його прибирає `OnCleanup` чи наступний прогін. Фото ADAICA зберігає в себе: після видалення файлу воно лишається у фірмі. Самого зображення в дереві UI Automation немає, тож перевірка двояка: у меню стає доступним «Löschen», а на знімку контрола щонайменше п'ята частина пікселів має колір тла нашого фото. Меню після перевірки закривається клавішею Esc.
  - **Банківський рахунок**: вікно «Bankkonto hinzufügen/bearbeiten». Назву банку, SWIFT-BIC і BLZ ввести не можна: їх заповнює вибір банку з німецького довідника. Тест фільтрує довідник за BLZ (кнопка фільтра реагує лише на мишу) і вибирає рядок. IBAN — правильний німецький, з контрольними цифрами (`CustomerCase.GermanIban`).
  - **Не редагуються в тестовій базі:** «Branche» (довідник галузей порожній, вільний текст ADAICA не зберігає), «Vorauswahl Korrespondenz» (Organisationseinheit, Mitarbeiter, Telefon, E-Mail залежать від співробітників фірми; без них Organisationseinheit не зберігається), «Fremdschlüssel» (лише для читання), «Konditionen» (порожній список). «Nicht mehr existent» тест не вмикає навмисно, щоб фірма лишалася чинною для тестів пошуку.
- **Тестові дані лишаються.** `NavigatorTests` щоразу створює в базі новий проєкт, Ordner, завдання, термін та інфозапис, а тести дочірніх записів — ще по одному проєкту, Ordner, завданню, терміну та інфозапису, під кожним з яких проєкт, Ordner, завдання, термін та інфозапис (`NavigatorTests.CreateChildRecords`). Тести їх не видаляють. Назви й примітки дає `CustomerCase`: випадкова «справа клієнта» (фірма, контактна особа, місто, тема), з якої складаються природні німецькі тексти, наприклад проєкт «Umzug ins neue Lager – Müller Logistik AG» і завдання «Terminplan für den Umzug ins neue Lager aktualisieren». Так у базі накопичуються різні записи для перевірки повнотекстового пошуку за назвами й примітками. Записи однієї справи узгоджені між собою. Дати теж випадкові: різні дні в межах 60 днів від сьогодні в минуле й майбутнє, час — робочі години з кроком 15 хвилин. Дні не повторюються в межах тесту, а поки вистачає вільних днів, — і в межах прогону. Порядок дат зберігається: початок раніше за кінець, а в завдання «Beginnt am» раніше за «Erledigt am», а та — раніше за «Geschlossen an». Минулі дати в термінах і завданнях ADAICA приймає. Вибрані тексти й дати видно в кроках звіту. Kurzname у всіх тестових записах — «MAT», за ним їх можна відрізнити від уведених людьми.
- **Пошук «Finden».** «System» (`mnuSystemMenuWndMain`) розгортається через UI Automation, у ньому «Finden…» (`mnuSystemSearchWndMain`) відкриває дочірнє вікно з AutomationId `This`. Тому `SearchWindow` впізнає його за вкладками `tcSearchParts`. На вкладці Dokumentation є:
  - поле «Suchbegriff» (`ctrRecordBox` → `txtBox`), кнопки «Finden» (`btnSearchFindCreateRecord`) і «Abbrechen» (`btnStopFindCreateRecord`);
  - джокери-перемикачі «*Txt» (`fbRecordTabItemPercentToBeginWndFindCreate`, джокер перед словом) і «Txt*» (`…PercentToEnd…`, джокер після слова). «Txt*» увімкнений за замовчуванням. Стан джокерів ADAICA не запам'ятовує;
  - режим (`cmbSearchRecordSearchOpt`), доступний лише для кількох слів: «Alle Wörter», «Exakte Suche», «Еinzelnes Wort». В останньому перша літера кирилична (U+0415), тому `ComboBoxControl.SelectAt` вибирає режим за позицією (`SearchMode`);
  - фільтри «Suchen nach (Klassentypen)» (Aufgaben, Notizen, Ordner, Projekte, Termine тощо) і «Suchen in (Suchkriterien)» (Name, Bemerkung, Kommentar і Text позначені «Volltextsuche», а ще Kurzname, Nummer, ID). Це дерева з прапорцем у кожному пункті, без AutomationId, тому `SearchFilters` шукає прапорець за підписом. Прапорці перемикаються через UI Automation, а стан фільтрів ADAICA між відкриттями вікна не запам'ятовує. Прапорець «Alle auswählen» через UI Automation змінюється лише сам, без дочірніх, після чого ADAICA вважає, що полів не вибрано («Wählen Sie die Suchkriterien der zu findenden Datensätze»), тому тести знімають прапорці окремих пунктів;
  - таблиця результатів `listView` на вкладці «Übersicht» і підсумок внизу вікна («Es wurden 5 Datensätze gefunden.», без AutomationId).

  Пошук триває близько 3 с, і поки він іде, «Abbrechen» доступна: так `FindRecords` визначає, що пошук завершився. Запит лише зі службових слів ADAICA завершує миттєво, і «Abbrechen» не встигає стати доступною. Якщо замість результатів з'явилося повідомлення ADAICA, `FindRecords` закриває його й падає з його текстом. Новий запис Elasticsearch індексує не одразу, тому `WaitUntilFound` (для інфозапису без номера — `WaitUntilFoundWithRemark`) повторює пошук. Перевірки «не знаходить» можна робити лише після цього. Список результатів віртуалізований (у дереві лише видимі рядки, наприклад 12 з 21), тому `ResultFor` гортає його. Колонки «Fundstelle» і «Fundtext» на екрані сховані, але в UI Automation є. Кому в кінці запиту поле «Suchbegriff» прибирає само.
- **Як шукає Elasticsearch** (перевірено в застосунку; випадки — у таблицях `SearchTests*.cs`):

  | Що | Поведінка |
  |---|---|
  | Регістр | не важливий: «umzug» = «UMZUG», також із джокерами й умлаутами («WÄRME*») |
  | Джокери-перемикачі | без них шукається ціле слово: «Wärme» і «pumpe» не знаходять «Wärmepumpe». «Txt*» знаходить початок слова, «*Txt» — кінець, обидва — середину. Для кількох слів джокер додається до кожного слова |
  | Джокери в запиті | «*» — будь-скільки літер на початку, в середині й у кінці («Wär*pumpe»); «?» — рівно одна літера; «%» працює як «*»; «_» — не джокер. «?» — завжди джокер: «Montag?», скопійоване з тексту, без «Txt*» не знаходить «Montag?», зате знаходить «Montage» |
  | Складені слова | не розбиваються: «plan» без джокера не знаходить «Terminplan», «Wärme pumpe» не знаходить «Wärmepumpe» |
  | Умлаути | в індексі «ä» = «ae» в обидва боки: «Müller» знаходить «Mueller», «Mueller» — «Müller», «Göthestraße» — «Goethestrasse». Але «ä» ≠ «a» («Muller» — ні). Тому «?» замість умлаута не спрацьовує (див. нижче) |
  | ß і ss | «ß» = «ss» = «ẞ» у будь-якому поєднанні: «Hauptstrasse» знаходить «Hauptstraße», «Größe» — швейцарське «Grösse». «Masse» знаходить «Maße» |
  | Акценти | «é», «è» = «e»: «Cafe» знаходить «Café», «Creme» — «Crème»; «e» + U+0301 теж знаходить |
  | Основа слова | не виділяється: «Unterlage» не знаходить «Unterlagen», «Parkplatz» — «Parkplätzen», «Tor» — «Tors» |
  | Дефіси й тире | ділять слово: «Meeting» і «off» знаходять «Kick-off-Meeting», «Kick off» — теж, зокрема як фраза; «Kickoff» — ні. «–» і «—» працюють як дефіс, «-слово» не виключає слова |
  | Пробіли | зайві пробіли, табуляція й нерозривний пробіл нічого не змінюють; кома чи крапка з комою між словами — як пробіл |
  | Розділові знаки | «? ! . , ; = ' "», дужки й лапки („…“, «…», "…") навколо слова ігноруються; «&», «:», «/», «*», «^» всередині слова ділять його на слова («Kund:innen» → «Kund» і «innen»). Символи «€», «×», емодзі не індексуються |
  | Оператори | OR, NOT, «-», «+», «~», «^», «поле:», `/регулярний вираз/` не діють; AND/OR/NOT пропускаються як службові слова. Нечіткого пошуку немає |
  | Службові слова | «und», «für», «die», «im», «zur», а також англійські «and», «or», «not», «the» окремо не знаходять нічого, поруч з іншими словами пропускаються, у фразі займають своє місце |
  | Числа | дата «30.09.2026», сума «1'250.50» чи «1.250,50», номер будинку «5a» — одне слово (частина — лише з джокером); час «14:30» ділиться на «14» і «30» |
  | Email | знаходиться цілком і частинами: «empfang», «alpenrose», «alpenrose.ch», «hotel-alpenrose» |
  | Ієрогліфи | кожен ієрогліф — окреме слово: «深» знаходить «深圳». Фраза з ієрогліфами не знаходить свій текст, а «深海» знаходить запис без «海» (див. нижче) |
  | «Alle Wörter» | потрібні всі слова в **одному** полі; слово в назві й слово в примітці разом запис не знаходять. Лапки не роблять фрази |
  | «Exakte Suche» | слова поруч і в тому самому порядку; розділові знаки між ними не заважають |
  | «Einzelnes Wort» | вистачає будь-якого зі слів, але лише в повнотекстових полях: номер, ID і Kurzname з кількома словами не шукаються (див. нижче) |
  | Номер | шукається як ціле слово без урахування регістру («Fundstelle» = Nummer); цифри без префікса — лише з «*Txt». ID шукається так само («Fundstelle» = ID), Kurzname — теж («MAT» знаходить усі тестові записи) |
  | Лише джокер чи знак | «*», «**», «%», «-», `""` і порожній запит повертають усі записи (зараз 400+) з «Fundstelle» = «Kommentar (Volltextsuche)», хоча коментарів у них немає. Потребує підтвердження розробників, тестом не зафіксовано |

- **Колонки результатів.** «Nummer / Version» — номер (в інфозапису порожній). «Klassentyp» — технічний код (TASK, APPOINT, HISTORY, OPERATION, PROJECT), а не німецька назва. «Fundstelle» — поле, де знайдено слова. «Fundtext» зараз завжди дорівнює «Bemerkung», навіть коли слова знайдено в назві. «Erstelldatum» збігається з датою створення в записі. «ID» — `ррММддГГххсс` + тип + 6 цифр.
- **Тести, що падають навмисно.** Це баги ADAICA, а не тестів. Коли баг виправлять, тест стане зеленим без змін. Якщо розробники скажуть, що поведінка задумана, змініть очікування в таблиці випадків.
  - `Finden_ResultColumns` — для одного запису підсумок у множині: «Es wurden 1 Datensätze gefunden.».
  - `Finden_SearchModeNames` — «Еinzelnes Wort» починається з кириличної «Е» (U+0415).
  - `Finden_QuestionMarkForUmlautOrSharpS` — «?» не замінює умлаут чи «ß»: «Hauptstra?e», «Aussent?r», «Gr?sse», «W?rmepumpe» не знаходять запис, а «Hauptstra??e» знаходить (в індексі «ß» — це «ss»). Імовірний баг.
  - `Finden_DecomposedUmlaut` — «a» + U+0308 (так «ä» потрапляє з macOS чи PDF) не знаходить «Wärmepumpe», хоча «e» + U+0301 знаходить «Café». Імовірний баг: запит варто нормалізувати (NFC).
  - `Finden_NumberInSingleWordMode` — у режимі «Einzelnes Wort» номер не шукається: «TAS0000046 Heizkessel» не знаходить TAS0000046. Імовірний баг.
  - `Finden_ChineseTextAsWritten` — фраза «Shenzhen 深圳» не знаходить свій текст, а «深海» знаходить запис лише з «深». Імовірний баг.
- **Raw-дерево.** Звичайний пошук FlaUI не бачить елементів із шаблонів WPF, наприклад заголовка вкладки `PART_Text`. Для них є `FindFirstDescendantRaw` з `RawTree.cs`.
- **Скріншоти й DPI.** `TestRunSetup` вмикає для тестового процесу per-monitor DPI awareness. Без цього на моніторах з масштабом понад 100% скріншоти падінь виходять обрізаними.
- Нові елементи шукайте за `AutomationId`. Якщо в елемента його немає, попросіть розробників додати `AutomationProperties.AutomationId` у XAML. Пошук за текстом або індексом ламається від будь-якої зміни UI, а тексти ADAICA ще й залежать від мови.

## Як написати тест

```csharp
[AllureSuite("Робочий простір")]
public class MyTests : AdaicaTestBase      // ADAICA запускається перед кожним тестом і закривається після
{
    [Test]
    public void Something_Works()
    {
        // Вхід і нова вкладка Dokumentation; після тесту вкладку буде закрито.
        var main = LogInAndOpenDokumentation();

        var search = main.OpenSearchWindow();
        // Прибирання виконається після тесту (і після скріншота падіння), перед виходом з ADAICA.
        OnCleanup(search.CloseIfOpen);
    }
}
```

## Page Objects і контроли

Тести написано за патерном Page Object: тест працює лише з Page Objects, а ті складаються з контролів.

- **Контрол** (`Controls/`) — одне поле чи кнопка. Він знаходить свій елемент, заповнює значення і перевіряє його. Кожна дія й перевірка стає окремим кроком у звіті Allure з назвою поля.

  | Контрол | Дії | Перевірки |
  |---|---|---|
  | `TextBoxControl` | `Fill(text)`, `Text` | `ShouldHaveText` |
  | `NumberBoxControl` | `Fill(decimal)`, `Value` | `ShouldHaveValue` |
  | `DatePickerControl` | `Fill(DateTime)`, `Value` | `ShouldHaveValue` |
  | `DateTimePickerControl` | `Fill(DateTime)` — дата й час до хвилини, `Value` | `ShouldHaveValue` |
  | `ComboBoxControl` | `Select(text)`, `EnterText(text)`, `SelectedText`, `Items` | `ShouldHaveSelected` |
  | `PasswordBoxControl` | `Fill(password)` — значення не потрапляє у звіт | — |
  | `ButtonControl` | `Click()` — падає, якщо кнопка недоступна | — |
  | `ToggleButtonControl` | `SwitchOn()`, `IsOn` | — |
  | `CheckBoxControl` | `SetChecked(bool)`, `IsChecked` | `ShouldBeChecked` |
  | `MenuItemControl` | `Click()`, `Expand()` для підменю | — |
  | `TabItemControl` | `Select()`, `IsSelected` | — |
  | `LabelControl` | `Text` | `ShouldHaveText` |

  Усі контроли мають `IsEnabled`, `ShouldBeEnabled()` і `ShouldBeDisabled()`. `Fill` і `Select` чекають, доки ADAICA покаже нове значення, тож окремо чекати не треба.
- **Page Object** (`Pages/`) — вікно або його частина. Він оголошує поля як властивості-контроли, підписані так, як в ADAICA, і описує сценарії: `LoginAs`, `OpenNavigator`, `Save`. Бланк, що повторюється в кількох місцях, виносьте в окремий клас, як `ProjectForm`, `TaskForm`, `AppointmentForm` і `NoteForm`: вони працюють і у вікні нового запису (`dialog.Form`), і в огляді збереженого запису. Вікно нового запису успадковує `RecordDialog`, який уміє знайти вікно, зберегти (`Save`) і скасувати (`Cancel`).
- **Дані** — record на кшталт `ProjectData`, `TaskData`, `AppointmentData` чи `NoteData`. Тести отримують їх від `CustomerCase` (`CustomerCase.Random().NewTask()`), а не пишуть назви вручну. Бланк заповнює всі поля методом `Fill(data)` і перевіряє їх методом `ShouldHaveValues(data)`. Перевірка повідомляє про всі розбіжності одразу, а не лише про першу.

Найпростіший приклад бланку й вікна — інфозапис:

```csharp
public class NoteForm(AutomationElement root)
{
    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, "txtRecordName"));
    public DateTimePickerControl Begin { get; } = new("Beginnt", Locate.ById(root, "dpBeginDateRecordHistory"));
    public ComboBoxControl Result { get; } = new("Ergebnis", Locate.ById(root, "cmbResultRecordHistory"));
    // ... Fill(NoteData) і ShouldHaveValues(NoteData)
}

public sealed class NoteDialog : RecordDialog
{
    public NoteDialog(Window owner) : base(owner, "tiMainRecordHistory", "інфозапис") => Form = new NoteForm(Root);
    public NoteForm Form { get; }
}
```

Для нового типу запису потрібні такі самі три класи (`…Form`, `…Dialog`, `…Data`), метод `MainPage.OpenNew…Dialog` і `MainPage.WaitForOpened…`, а якщо запис можна створити під іншим записом, — ще `MainPage.OpenNewChild…Dialog`.

AutomationId для нових бланків беріть із FlaUInspect. Якщо AutomationId неунікальний, передайте в `Locate.ById` ланцюжок від контейнера, наприклад `Locate.ById(Window, "cmbDataBase", "combo")`. Якщо елемент треба шукати інакше, передайте в контрол власну функцію пошуку, як `MessageBoxDialog.Button`.

Усе, що тест змінює в збережених налаштуваннях користувача (вкладки, фільтри тощо), повертайте через `OnCleanup`, інакше наступні прогони почнуться з іншого стану.

Правила:

- Тест не шукає елементи сам: поля оголошуються контролами в Page Object, а решта елементів шукається всередині Page Object через `Element("AutomationId")`, який сам чекає появи елемента.
- Не використовуйте `Thread.Sleep`. Для очікування є `Wait.Until(() => умова, "що чекаємо")`.
- Позначайте сценарії Page Object атрибутом `[AllureStep("...")]`, щоб у звіті було видно кроки. Секрети в параметрах позначайте `[AllureParameter(Ignore = true)]`. Атрибут загортає винятки в `TargetInvocationException`, тому для перевірок краще `AllureApi.Step(назва, дія)`, як у `ProjectForm.ShouldHaveValues`.
- Тести не запускаються паралельно (`AssemblyInfo.cs`), бо робочий стіл один.
- Дерево елементів зручно дивитися в **FlaUInspect** (режим UIA3), **Accessibility Insights for Windows** або `Inspect.exe` з Windows SDK. ADAICA для цього запускайте зі змінною `AutomationTests__AutomationPeerFeatureEnabled=true`.

## Агент для тестів пошуку

У `.claude/agents/adaica-search-tester.md` описано агента Claude Code, який тестує пошук «Finden»: придумує випадки за матрицею (поле, тип запису, кількість слів і режим, джокери, регістр, умлаути, ß/ss, акценти, дефіси, пробіли, розділові знаки, синтаксис Elasticsearch, числа, інші алфавіти, фільтри), спершу перевіряє поведінку в застосунку тимчасовим дослідним тестом, відрізняє баги від задуманої поведінки, дописує таблиці в `SearchTests*.cs` і README, проганяє тести. Він дотримується тих самих правил, що й тести: закриває лише свій процес ADAICA, бере пароль лише зі змінної `ADAICA_PASSWORD`.

У Claude Code агента викликають, коли просять протестувати пошук, або явно: «використай агента adaica-search-tester, щоб перевірити пошук інфозаписів за приміткою».

## Jenkins

Пайплайн описано в `Jenkinsfile`. У ньому є стадії Restore, Build і UI tests, а після прогону публікуються JUnit, Allure та скріншоти падінь. Параметри збірки: `APP_PATH` і `TEST_FILTER`. Якщо тести впали, збірка отримує статус UNSTABLE. Якщо звіт взагалі не створився, збірка отримує статус FAILURE.

### Агент

UI-тести працюють лише в **розблокованій інтерактивній сесії Windows**, тому агент налаштовується так:

1. Окрема Windows-машина або VM з міткою `windows-ui`. Якщо мітка інша, змініть її в `Jenkinsfile`.
2. Агент запускається **не як служба Windows**, а як процес у сесії користувача: inbound (JNLP) agent в автозавантаженні цього користувача.
3. Для користувача агента ввімкнено автоматичний вхід (наприклад, Sysinternals Autologon), а блокування екрана, заставку і сон вимкнено.
4. Якщо підключаєтеся по RDP, не згортайте вікно RDP і не відключайтеся звичайним способом: сесія заблокується, і тести впадуть. Щоб відключитися й залишити сесію активною, виконайте від адміністратора `tscon %sessionname% /dest:console`.
5. Роздільна здатність екрана — не менше 1920×1080.
6. На агенті встановлено .NET 10 SDK і є доступ до nuget.org або внутрішнього NuGet-feed.
7. ADAICA встановлено на агенті, шлях до exe передається параметром `APP_PATH`.
8. Агент має доступ до сервера ADAICA, а база й схема для тестів існують. Передайте їх через `ADAICA_DATABASE` і `ADAICA_SCHEMA`, якщо вони відрізняються від `.runsettings`.

### Плагіни та інструменти Jenkins

- Pipeline, Git, JUnit.
- **Allure Jenkins Plugin**, а в *Manage Jenkins → Tools* додано **Allure Commandline**. Без плагіна крок `allure` у `post` завершиться помилкою: або встановіть плагін, або приберіть цей крок.
- Облікові дані ADAICA зберігайте в *Credentials* (див. коментар у `Jenkinsfile`).
