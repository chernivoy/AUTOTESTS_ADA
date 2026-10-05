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
   │  ├─ UiInput.cs                    клік і клавіші без миші, з повтором мишею, якщо не подіяло
   │  ├─ WindowMessageInput.cs         клік і клавіші повідомленнями у вікно ADAICA
   │  ├─ AtomicMouse.cs                клік справжньою мишею одним викликом SendInput (запасний шлях)
   │  ├─ MonitorPlacement.cs           перенесення вікна ADAICA на інший монітор і назад
   │  ├─ ForegroundGuard.cs            повертає користувачеві фокус, який забрала ADAICA
   │  ├─ Popups.cs                     пошук спливних вікон (списки, меню) у будь-якому стані ADAICA
   │  ├─ TestImageFile.cs              JPG, який тест малює сам і прибирає після себе
   │  ├─ TestWordFile.cs               Word-файл (.docx), який тест складає сам (без Word) і прибирає після себе
   │  ├─ WordApplication.cs            правка документа у Word через COM (Word.Application): дописати абзац, зберегти, закрити
   │  └─ OutlookApplication.cs         новий лист в Outlook через COM (Outlook.Application): Кому, тема, текст, надіслати
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
   │  ├─ FileOpenDialog.cs             стандартний діалог Windows «Відкрити»: шлях у поле імені й Enter
   │  └─ AdaicaFormat.cs               формати дат і чисел ADAICA
   ├─ Pages/                           Page Objects ADAICA
   │  ├─ AdaicaWindow.cs               вікно FlatWnd: діалоги, вихід із підтвердженням
   │  ├─ LoginPage.cs                  форма входу (користувач, пароль, база, схема)
   │  ├─ MainPage.cs                   робочий простір після входу, вкладки й дерево навігаторів
   │  ├─ RecordDialog.cs               базове вікно нового запису: пошук вікна, OK, Abbrechen
   │  ├─ RecordClassBox.cs, RecordClass.cs           «Klasse» / «Unterklasse» на бланку запису: вибір класу, створення через конфігурацію
   │  ├─ ClassSelectWindow.cs, ClassesConfigWindow.cs, ClassNameDialog.cs  вікно вибору класу, «Konfiguration der Klassenliste», новий клас/підклас
   │  ├─ ProjectDialog.cs, ProjectForm.cs, ProjectData.cs   проєкт: вікно, бланк, дані
   │  ├─ FolderDialog.cs               Ordner: вікно (бланк і дані — ті самі, що в проєкту)
   │  ├─ TaskDialog.cs, TaskForm.cs, TaskData.cs            завдання: вікно, бланк, дані
   │  ├─ AppointmentDialog.cs, AppointmentForm.cs, AppointmentData.cs   термін: вікно, бланк, дані
   │  ├─ NoteDialog.cs, NoteForm.cs, NoteData.cs            інфозапис (Notiz): вікно, бланк, дані
   │  ├─ DocumentForm.cs, DocumentData.cs                  документ (Dokument): огляд запису, дані
   │  ├─ DocumentVersionsWindow.cs                         вікно версій документа: колонки, рядки, «Spaltenkonfiguration»
   │  ├─ ContactFindCreateWindow.cs    вікно «Finden & Erstellen» на вкладці Kontakte: пошук перед створенням контакту
   │  ├─ NewCompanyDialog.cs, CompanyForm.cs, CompanyData.cs   фірма: вікно «Neue Person/Firma», картка (Allgemein, Details, Bankverbindungen), дані
   │  ├─ DepartmentDialog.cs, DepartmentForm.cs   відділ фірми (Organisationseinheit): вікно «Neue Organisationseinheit hinzufügen», картка
   │  ├─ NewPersonDialog.cs, PersonForm.cs, EmployeeData.cs   нова персона (співробітник): вікно «Neue Person/Firma», картка персони, дані
   │  ├─ NewContactData.cs             дані персони й фірми, створених разом: адреса, телефон, e-mail, титул тощо
   │  ├─ CompanyEditData.cs            дані редагування фірми: вкладки, рядки «Verbindung», банківський рахунок
   │  ├─ CommunicationsList.cs         список «Verbindung» у картці контакту: додавання й читання рядків
   │  ├─ ChildDialog.cs                базове невелике вікно з OK / Abbrechen
   │  ├─ CompanyNameDialog.cs, BankAccountDialog.cs   вікна «Firmendetails», «Bankkonto»
   │  ├─ CheckTreeDialog.cs, CategoriesDialog.cs, TitlesDialog.cs   вікна з деревом прапорців: «Kategorie» контакту, «Titel» персони
   │  ├─ AddressFindCreateWindow.cs, NewAddressDialog.cs   нова адреса контакту: «Finden & Erstellen» (Adressen) і «Neue Adresse anlegen»
   │  ├─ MailSettingsWizard.cs         асистент «Mail-Einstellungen»: обліковий запис Outlook для синхронізації
   │  ├─ MailSaveDialog.cs, MailView.cs, MailData.cs   «E-Mail-Speicherndialog» (контакт для отримувача), огляд листа, дані листа
   │  ├─ RecordContactsList.cs         контакти запису (вузол «Kontakte» під записом, наприклад під листом)
   │  ├─ MessageBoxDialog.cs           стандартний діалог повідомлення ADAICA
   │  └─ SearchWindow.cs, SearchResult.cs, SearchMode.cs, SearchFilters.cs   вікно пошуку «Finden», рядок результатів, режим пошуку, фільтри
   ├─ Tests/
   │  ├─ AdaicaTestBase.cs             базовий клас тестів ADAICA: вхід, відкриття навігатора, дочірні записи (`CreateChildRecords`), прибирання
   │  ├─ LoginTests.cs                 вхід в ADAICA
   │  ├─ CustomerCase.cs               дані записів: природні назви й примітки, щоразу інші
   │  ├─ SearchTests.cs                повнотекстовий пошук (Elasticsearch): джокери, режими, форми слів, колонки; фікстури
   │  ├─ SearchTests.*.cs              те саме за темами: QuerySyntax, GermanLocale, TextSymbols, Numbers, RecordTypes, Filters
   │  ├─ ContactTests.cs               навігатор Kontakte → створення фірми з унікальною назвою; редагування всіх полів картки; дочірні записи фірми; відділ фірми зі співробітником; дочірні записи співробітника; персона разом із новою фірмою
   │  ├─ MailTests.cs                  лист через Outlook з вузла E-Mails: обліковий запис пошти, збереження листа, персона з отримувача
   │  └─ NavigatorTests.cs             навігатор Dokumentation → створення проєкту, Ordner, завдання, терміну, інфозапису; під кожним із них — дочірні записи
   ├─ TestRunSetup.cs                  дії перед усім прогоном (DPI, очищення Allure)
   ├─ ADAICA_AUTOTEST.runsettings      параметри прогону
   └─ allureConfig.json
tools/TestRunner/                      веб-запускач: вибір тестів у браузері, запуск у заданому порядку, звіт Allure
   ├─ Program.cs                       сервер на 127.0.0.1:5080 і API
   ├─ TestCatalog.cs                   список тестів зі збірки та їхні описи з XML-документації
   ├─ RunManager.cs                    прогін: збирання, dotnet test для кожного тесту, TRX, allure generate
   └─ wwwroot/                         сторінка (index.html, app.css, app.js)
.claude/agents/adaica-search-tester.md   агент Claude Code для тестування пошуку (див. «Агент для тестів пошуку»)
Jenkinsfile
```

## Локальний запуск

Потрібні Windows і .NET 10 SDK. Тести керують реальним вікном ADAICA, але мишу й клавіатуру не займають: вікно стоїть на другому моніторі, а кліки йдуть повідомленнями у вікно. Тож під час прогону можна працювати на першому моніторі; що для цього треба знати — у розділі «Робота паралельно з тестами».

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

### Веб-запускач

Замість команд тести можна вибрати й запустити на сторінці в браузері:

```powershell
dotnet run --project tools/TestRunner
```

Запускач відкриває http://127.0.0.1:5080 (сторінка доступна лише з цієї машини). Зліва — усі тести, згруповані за класами, з описами з коментарів `/// <summary>` і позначками Smoke. Праворуч — користувач, пароль, база, схема й шлях до ADAICA.exe, заповнені тим самим, що бачать тести: `ADAICA_AUTOTEST.runsettings` і змінні `ADAICA_USER`, `ADAICA_PASSWORD`, `ADAICA_DATABASE`, `ADAICA_SCHEMA`, `ADAICA_APP_PATH`. Шлях запускач перевіряє перед прогоном: якщо файлу немає, прогін не почнеться. Під ними — черга: тести йдуть у тому порядку, в якому їх позначено, порядок можна змінити стрілками. Черга й поля (крім пароля) зберігаються в браузері.

«Запустити» збирає тести, а потім запускає кожен окремим `dotnet test` у порядку черги, передаючи параметри входу змінними середовища. Порядок тримає сам запускач: в одному `dotnet test` NUnit упорядкував би тести по-своєму. Кожен тест вибирається фільтром NUnit `NUnit.Where=class == … && method == …`, тож разом з усіма випадками TestCase і без тестів зі схожими назвами. Щоб звіт Allure був один на весь прогін, запускач сам очищає `allure-results` перед прогоном і передає тестам `ADAICA_KEEP_ALLURE_RESULTS=true` (тоді `TestRunSetup` теку не чистить). Унизу сторінки видно статус кожного тесту, а після прогону — «Тести завершились», час кожного тесту за TRX, перша помилка й кнопка «Відкрити Allure-звіт». Звіт генерує `allure generate` у `.testrunner/reports/<прогін>`, і запускач віддає його сам. «Зупинити» завершує поточний `dotnet test` разом з ADAICA, яку запустив тест; інші екземпляри ADAICA не зачіпаються. TRX і підсумок кожного прогону лежать у `.testrunner/runs/<прогін>`.

Угорі праворуч — перемикач теми: «Авто» (як у Windows), «Світла», «Темна»; вибір зберігається в браузері.

«Оновити список» перезбирає тести й перечитує список — наприклад, після додавання нового тесту. Інший порт: `dotnet run --project tools/TestRunner -- --urls http://127.0.0.1:5090`; без автоматичного відкриття браузера — `-- --no-browser=true`.

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
| `ADAICA_MONITOR` | `Monitor` | не задано: вікно лишається, де його відкрила ADAICA (у `.runsettings` — `secondary`) |
| `ADAICA_INPUT_MODE` | `InputMode` | `Background` |

`Monitor`: `secondary` — перший неосновний монітор, `primary` — основний, або ім'я пристрою (`\\.\DISPLAY2`). Якщо такого монітора немає (наприклад, на Jenkins), вікно лишається на місці.

`InputMode` — як тести клікають там, де UI Automation не допомагає (див. «Робота паралельно з тестами»):

| Значення | Що робить |
|---|---|
| `Background` | Повідомленнями у вікно ADAICA; якщо дія не подіяла за таймаут — повторює її справжньою мишею чи клавіатурою й пише про це у звіт |
| `BackgroundOnly` | Лише повідомленнями; не подіяла дія — тест падає. Миша й клавіатура не потрібні ніколи |
| `Mouse` | Лише справжньою мишею й клавіатурою, як було раніше |

Паролі в `.runsettings` не зберігайте, передавайте їх через змінні середовища або Jenkins Credentials.

## Особливості ADAICA

- **UI Automation вимкнено за замовчуванням.** Поки `AutomationTests:AutomationPeerFeatureEnabled` в `appsettings.json` дорівнює `false`, ADAICA підміняє AutomationPeer вікон на порожні, і тести не бачать жодного елемента. `AppSession` запускає застосунок зі змінною `AutomationTests__AutomationPeerFeatureEnabled=true` (див. `TestSettings.AppEnvironment`), тому файл змінювати не треба.
- **Одне вікно на все.** Форма входу і робочий простір — це те саме вікно `FlatWnd`. Діалоги ADAICA в дереві UI Automation є його дочірніми вікнами, а не вікнами робочого столу, тому шукайте їх через `AdaicaWindow.FindMessageBox()`.
- **Діалоги під час входу.** Якщо версія клієнта не збігається з сервером, `LoginPage.LoginAs` натискає «Ja». Будь-який інший діалог вважається помилкою входу, і тест одразу падає з його текстом. Без ліцензії після входу відкривається вікно демоверсії, яке `MainPage` закриває.
- **Зміна бази скидає користувача й пароль**, тому `LoginAs` спершу вибирає базу й схему.
- **Поки список (ComboBox) розгорнутий, ADAICA ігнорує закриття вікна.** Після вибору пункту згортайте список.
- **Вихід.** Після кожного тесту `AdaicaTestBase` закриває ADAICA і підтверджує «Wollen Sie ADAICA verlassen?». Якщо за 30 с застосунок не завершився, процес зупиняється примусово.
- **Навігатори.** Кнопка «+» праворуч від вкладок (`PART_AddTab`) відкриває список навігаторів. Кожне відкриття додає нову вкладку в кінець смуги, а відкриті вкладки ADAICA запам'ятовує між сеансами, тож тест має закрити за собою те, що відкрив (див. `OnCleanup` нижче). Назва вкладки змінюється разом із вибраним вузлом, тому `CloseLastNavigatorTabs` закриває вкладки за кількістю, а не за назвою.
- **Лише клік.** Пункт списку навігаторів, вкладка навігатора, вузол дерева, контекстні меню (вузла, рядка й заголовка таблиці, фото), лупа швидкого пошуку й кнопки-перемикачі («+» біля «Adressen» і «Verbindung», глобус, прапорці «Kategorie») реагують тільки на клік: `SelectionItem`, `Toggle`, Enter, Shift+F10, клавіша меню й `ShowContextMenu` з UI Automation не діють. Такі кліки йдуть через `UiInput` — без миші, повідомленнями у вікно (див. «Робота паралельно з тестами»).
- **Дерево навігатора.** До назв вузлів ADAICA додає діапазон записів («Projekte (1-25)»), тому `MainPage.NavigatorNode` порівнює лише назву. У UI Automation дерево — плаский список (`ListBox`), вкладеність видно лише з відступу тексту вузла. Тому `MainPage.SelectedNavigatorNodePath` визначає предків виділеного вузла за відступом: предок — найближчий вузол вище, текст якого починається лівіше. Список віртуалізований: вузлів поза видимою областю в дереві UI Automation немає, тому `NavigatorNode` гортає дерево від початку, доки не знайде вузол.
- **Вікна нових записів.** Це дочірні вікна без AutomationId. `RecordDialog` знаходить їх за головною вкладкою бланку (`tiMainRecordFolder` у проєкті й Ordner, `tiMainRecordTask` у завданні, `tiMainRecordAppoint` у терміні, `tiMainRecordHistory` в інфозаписі). Вікно завдання відкривається до 7 с, тому таймаут на відкриття — 30 с.
- **Вікно проєкту.** «Neu» у контекстному меню вузла Projekte одразу відкриває вікно. Редагуються лише Name, Kurzname, дати, Kosten/Aufwand з одиницями та Bemerkung: клас і відповідальний задаються окремими діалогами, решта полів заповнюється автоматично. Дати й числа ADAICA показує в німецькому форматі (`18.09.2026`, `1500,00`), тому `ProjectData` зберігає суми як `decimal`. Кожне відкриття вікна займає наступний номер проєкту, навіть якщо закрити його без збереження.
- **Вікно Ordner.** «Neu» на вузлі Ordner одразу відкриває вікно «Neuer Ordner». Бланк у ньому той самий, що й у проєкту: ті самі поля й AutomationId, навіть головна вкладка `tiMainRecordFolder`. Тому `FolderDialog` використовує `ProjectForm` і `ProjectData`, а Ordner від проєкту відрізняє лише номер: `OPE…` замість `PRO…`. Після збереження ADAICA виділяє вузол «Назва (Номер)» у вузлі Ordner.
- **Після збереження** (OK) ADAICA сама переходить до нового проєкту: виділяє в дереві вузол «Назва (Номер)» і показує той самий бланк в області перегляду (`InformationView`). `MainPage.WaitForOpenedProject` шукає бланк лише там, бо у відкритому діалозі є поля з тими самими AutomationId. Назви проєктів можуть повторюватися між прогонами, тож свій проєкт тест упізнає за номером.
- **Вікно завдання.** На вузлі Aufgaben «Neu» — підменю, у ньому потрібен пункт «Neue Aufgabe» (`mnuNewTaskDefault`). Дати «Beginnt am», «Fällig am», «Erledigt am» складаються з дати й часу (`DateTimePickerControl`), «Geschlossen an» — лише дата. Поля Standort, Kundennutzen, Hinweis — це значення з довідника плюс довільний текст поруч. Примітку тест вводить на вкладці простого тексту, бо за замовчуванням може відкритися форматований редактор, який не приймає текст через UI Automation. Якщо заповнити «Erledigt am» і «Geschlossen an», статус стає «Aufgabe erledigt». У списку «Priorität» текст пунктів лежить лише в `Name`, і `ComboBoxControl` це враховує.
- **Після збереження завдання** ADAICA виділяє в дереві вузол лише з назвою, без номера, тож номер тест перевіряє в бланку (`MainPage.WaitForOpenedTask`).
- **Клас і підклас запису.** На бланках проєкту, Ordner, завдання, терміну, інфозапису й документа (і у вікні нового запису, і в огляді) є той самий контрол із різними AutomationId — `ctrVUBClassSelect` у проєкту й Ordner, `ctrVUBClassSelectRecordTask`, `ctrVUBClassSelectRecordAppoint`, `ctrVUBClassSelectRecordHistory`, `ctrVUBClassSelectRecordDocument`: кнопка «Klasse» (`btnVUBClassSelectBoxSelectClass`) і поля назв класу й підкласу (`txtVUBClassName`, `txtVUBSubClassName`, без коротких назв). Кнопка відкриває «Wählen Sie eine Klasse und ggf. auch eine Unterklasse aus» (`wndVUBClassAndSubclassSelect`, `ClassSelectWindow`): дерево «Klasse» → «Blanko-Eintrag» і класи, ввімкнені для цього типу запису → їхні підкласи, підписані «Назва (коротка назва)». Дерево — плаский ListBox, рівень видно з відступу; розгортається трикутником (Toggle діє), а вибирається вузол лише кліком — тоді стає доступною OK. Кнопка з шестернею — «Konfiguration der Klassenliste» (`btnConfigurationWndVUBClassSelect`) — відкриває вікно «Klasse» (`wndVubClassesSubClasses`, `ClassesConfigWindow`): список класів, список підкласів із прапорцями (прапорець пов'язує підклас із виділеним класом), «Klassentypen» (Aufgabe, Berichte, Dateien, Formular, Notizen, Ordner, Projekt, Termine, Vorlagen) і дерево «Verwendung für Objekte» з «Alle auswählen». Типи й об'єкти задаються для виділеної пари клас + підклас, а прапорці доступні лише, коли її виділено. Новий клас чи підклас — контекстне меню списку → «Neu» (`mnuNewVubClassList`, `mnuNewVubSubClassList`; меню — Popup вікна конфігурації, а не головного) → вікно `wndNewEditVubClassSubClass` (`ClassNameDialog`): назва, коротка назва й «Sichtbar», яке за замовчуванням вимкнено — невидимий клас списки показують лише з «Unsichtbare anzeigen». OK зберігає конфігурацію в базу одразу, перезапуск не потрібен. Вибраний клас ADAICA підставляє в «Kurzname» короткі назви класу й підкласу разом («KUAKKORR»), тож бланк вибирає клас першим, а «Kurzname» («MAT») вводить після нього. Інфозапис із класом дерево навігатора підписує «дд.ММ.рр Назва (Klasse - Unterklasse)» (`AdaicaTestBase.NoteNode`); підписи проєкту, Ordner, завдання, терміну й документа клас не змінює. Документ із файлу ADAICA зберігає одразу, без вікна, тож клас йому тест вибирає в огляді: після вибору стають доступні OK / «Übernehmen» (`btnOKCtrMainForm`), і тест після класу вводить «MAT» у «Kurzname» й зберігає OK (`DocumentForm.SetClass`, крок `CreateDocumentFromWordFile` у `CreateChildRecords`). Окремий тест документа з версіями (`KontakteNavigator_NewDocumentFromWordFile`) клас не вибирає. Тести навігаторів (`CustomerCase.WithClass()`: усі записи в `NavigatorTests` і дочірні записи під фірмою й співробітником) вибирають клас «Kundenakte (KUAK)» → «Korrespondenz (KORR)» (`CustomerCase.TestClass`); якщо у вікні вибору його немає, `RecordClassBox.Select` створює клас і підклас, пов'язує їх і вмикає всі «Klassentypen» і «Alle auswählen». У BLANK_IGOR їх створено 05.10.2026; там є й чужий «Class (cls)» → «Class_For_all (csa)». Тести пошуку клас не вибирають: вони перевіряють, що «Klasse» порожня.
- **Навігатор Dashboard («Today»): список «Meine Aufgaben».** Dashboard може бути відкритий лише в одній вкладці: коли вона є, у списку «+» пункт «Dashboard» неактивний. Тому `AdaicaTestBase.LogInAndShowSingleNavigator` перемикається на наявну вкладку (клік по заголовку) і після тесту повертає ту, що була активною, а нову відкриває, лише якщо вкладки немає. Корінь дерева — вузол залогіненого користувача («Chernivoy, Igor»), його вибирає `MainPage.OpenUserTaskList`. Праворуч `TodayView` показує «Meine Aufgaben» (`gvTasksGeneralView`), «Meine Termine» і «Meine Mails». Над завданнями — швидкий пошук (`txtQuickSearch`; шукає в назві, Kurzname, номері й примітці, застосовується лише кліком по лупі `btnQuickSearchApplyFilter` або Enter), 11 кнопок-фільтрів (`TaskListFilter`, `tbtnUnreadTasks` … `tbtnPlannedTask`, працюють через Toggle) і «+» (`btnAdd`), що відкриває звичайне вікно «Neue Aufgabe». Список (`DashboardTaskList`) показує завдання, де користувач виконавець, з датами поблизу сьогодні, відсортовані за «Beginnt am»; номера в ньому немає. Стан фільтрів ADAICA не зберігає: у новому сеансі ввімкнено лише «Unerledigt». Як працюють фільтри (перевірено в застосунку й у коді `RecordListHelper.FilterTask`):
  - Групи «Ungelesen/Akzeptieren/Angenommen/Abgelehnt», «Erledigt/Unerledigt», «Offen/Geschlossen», «Aktuell/Geplant» і «Überfällig» поєднуються через «і». Група нічого не відсіює, коли в ній увімкнено всі кнопки або жодної. Фільтр застосовується одразу, синхронно з натисканням.
  - Непрочитане завдання фільтри прийняття не показують, лише «Ungelesen». Власне завдання автор-виконавець уже прочитав. Непрочитаними ADAICA створює наступні завдання серії.
  - «Akzeptieren» — не прийняте й не відхилене. З Annahmeart «Bestätigung» (за замовчуванням) чи «Automatisch» автор-виконавець приймає завдання одразу, з «Erster» — ні. «Abgelehnt» — після «Ablehnen» у контекстному меню рядка (правий клік; у меню «Annehmen», «Ablehnen», «Bearbeitung anfangen/beenden», «Erledigt»). Пункт меню виконується асинхронно, тож `Refuse` чекає, доки стане доступним «Annehmen».
  - «Erledigt» — заповнено «Erledigt am», «Geschlossen» — «Geschlossen am» (одне без іншого теж можна). «Überfällig» — «Fällig am» уже минуло, а «Erledigt am» порожнє.
  - «Geplant» — статус завдання «geplant», який ставить сервер лише завданням серії, що ще не стали актуальними. Завдання з майбутнім «Beginnt am» лишається «Aktuell». Серію задає кнопка «Serientyp» (`btnTypeOfReiterationRecordTask`) → вікно «Aufgabenzyklus» (`TaskSeriesDialog`: «Täglich», «Endet nach N Aufgaben» — поле без AutomationId), а запускає «Serie starten» на вкладці «Aufgabenzyklus» (`TaskDialog.StartDailySeries`, можна ще до збереження). Після збереження ADAICA одразу створює решту завдань серії з тією ж назвою й приміткою. За замовчуванням наступне завдання стає актуальним за добу до початку: серія з початком сьогодні о 00:00 дає завтрашнє завдання «Aktuell», а післязавтрашнє «Geplant».
  - **Колонки.** Правий клік по заголовку колонки відкриває меню DevExpress (`GridContextMenu`; у пунктів AutomationId з випадковим GUID, тож `DashboardTaskList.OpenColumnsConfig` шукає «Änderung der Spaltenkonfiguration» за підписом). Вікно «Spaltenkonfiguration» (`wndColumnsConfig`, `ColumnsConfigDialog`) — віртуалізований список з 40 колонок із прапорцями (без AutomationId): спершу увімкнені в порядку показу, далі решта за абеткою; OK — `btnClearWndCacheManager`. За замовчуванням увімкнено 6: Erledigt, Name, Beginnt am, Erstellt durch, Kurzname, Bemerkung. Конфігурацію ADAICA зберігає для користувача, тож тест повертає її через `OnCleanup`. Заголовок колонки не має AutomationId, його Name — ідентифікатор комірки (`RowData.Row.Name`); підписи колонок і ці ідентифікатори — у `DashboardTaskList.ColumnIds`. Показаний текст є лише в комірках на екрані (поле `PART_Editor`), тому `CellTexts` гортає таблицю праворуч. Назва комірки поза екраном — «сире» значення: дата без часу, порожня дата — «01.01.0001» чи «11.11.1911». Що показують колонки: дати — «dd.MM.yyyy HH:mm» (і «Geschlossen am», хоча в бланку лише дата: «29.09.2026 00:00»), тривалості — «2 Tag(e)», довідники (Standort, Kundennutzen, Hinweis) — значення довідника, а «… (Wert)» — текст поруч, «Bemerkung» — перші 100 символів і «...», «Status» — «Aufgabe erledigt», «Klassentyp» — «TASK», «ID» — `ррММддГГххсс` + `TASK` + 6 цифр. «Erledigt» — прапорець («Angewählt»), «Aktion» й «Icon» — картинки (UI Automation називає стан англійською: «Done», «Accepted»). Klasse й Unterklasse в BLANK_IGOR не задати: у вікні класу є лише «Blanko-Eintrag», і OK з нею недоступна. Тест `DashboardNavigator_TaskListAllColumns` вмикає всі колонки, створює завдання з усіма полями (`CustomerCase.NewCurrentTask`) і перевіряє текст у кожній колонці; час створення, зміни й прийняття — у межах від відкриття вікна завдання до збереження.
  - Тест `DashboardNavigator_TaskListFilter` на кожен фільтр створює кнопкою «+» відповідне завдання, звужує список швидким пошуком за номером (у завдань серії номер першого завдання стоїть у примітці), вмикає фільтри по одному й перевіряє, під якими завдання видно.
- **Вікно терміну.** «Neu» на вузлі Termine одразу відкриває вікно. Beginn і Ende складаються з дати й часу. Поле «Aufwand» можна редагувати лише з увімкненим прапорцем «Aufwand (z.B. Arbeitszeit)». Без прапорця поле показує тривалість між Beginn і Ende. Якщо введені трудовитрати відрізняються від цієї тривалості, ADAICA підсвічує поле й пише попередження в HelpText, але зберегти дозволяє. Тест бере 120 Minute(n) для терміну на 2 години, тож попередження немає.
- **Після збереження терміну** ADAICA виділяє в дереві вузол «ррММдд ГГ:хх/+Nh Назва» (наприклад, «260918 10:00/+2h Besprechung mit Frau Keller»), без номера.
- **Вікно інфозапису (Notiz).** «Neu» на вузлі Notizen одразу відкриває вікно «Neue Notiz». Номера в інфозапису немає, тому тест знаходить збережений запис за «Bemerkung» (`MainPage.WaitForOpenedNote`). Примітка закінчується часом запису («Notiert am 18.09.2026 um 08:46 Uhr.»), тож у кожному прогоні вона інша. Дата «Beginnt» не може бути пізнішою за сьогодні: ADAICA позначає поле зірочкою («Das angegebene Datum darf nicht später als Heute sein») і вимикає OK. Тому тест бере лише минулі дні. У BLANK_IGOR список «Ergebnis» порожній, тож у даних він `null`. Після збереження в дереві виділяється вузол «дд.ММ.рр Назва».
- **Дочірні записи.** У контекстному меню будь-якого запису (проєкту, Ordner, завдання, терміну, інфозапису), а також фірми в навігаторі Kontakte «Neu» (`mnuNew`) — підменю з типами записів: Projekt (`mnuNewProject`), Ordner (`mnuNewProcess`), Aufgabe → Neue Aufgabe (`mnuNewTask` → `mnuNewTaskDefault`), Termin (`mnuNewAppoint`), Notiz (`mnuNewHistory`). Пункти в усіх меню мають ті самі id, лише порядок інший (у Ordner немає «Kontakt», у завдання є ще «Formular», у фірми — «Dokument», «E-Mail», «Chat», «Kontakt», «Objekt»). Вікна й бланки ті самі, що й для кореневих вузлів, лише в заголовку додається «(übergeordnet: Батько)». Після збереження ADAICA виділяє запис у дереві під батьківським записом, у папці його типу: «Батько → Aufgaben → Завдання». Назви завдань та інфозаписів можуть повторюватися, бо номера в дереві немає. Тому `MainPage` шукає батька для меню «Neu» серед виділеного вузла та його предків, а не першим збігом у дереві, а `CustomerCase` не дає дочірньому запису ту саму назву, що й батькові. Кореневі вузли Gesamtliste (Aufgaben, Termine, …) дочірніх записів не показують навіть після «Aktualisieren». Новий проєкт і кожен новий запис одразу мають власні `Notizen (1-1)` зі службовою приміткою «… wurde erstellt» і `Kontakte (1-1)`, тож лічильник Notizen під проєктом на один більший.
- **Нова фірма (навігатор Kontakte).** У контекстному меню вузла Gesamtliste пункт «Neue Firma» (`MnuCompanyNew`) відкриває не бланк, а вікно «Finden & Erstellen» на вкладці Kontakte (`ContactFindCreateWindow`): це те саме дочірнє вікно `This`, що й пошук «Finden», тому його впізнаємо за рядком пошуку контактів `ctrPACBox`. Назву вводять у поле «Firma» (`ctrPersonCompanyName` → `txtBoxCompanyName` → `txtBox`). Під рядком ADAICA із затримкою пише дію («Aktion: Firma erstellen»), і стає доступною кнопка «Neu» (`btnNewFindBoxPAC`). Вона відкриває вікно «Neue Person/Firma» (`wndPersonCompanyCreate`) — дочірнє вікно «Finden & Erstellen», а не головного вікна — з назвою вже в полі «Firmenname 1. Zeile». Після OK обидва вікна закриваються, ADAICA виділяє фірму у вузлі «Aktuelle» і показує її картку «Stammdaten» (`txtFullNameCompany`, `txtCompanyShortName`). Номера в картці фірми немає, тому фірму тест упізнає за назвою, яка щоразу унікальна (`CustomerCase.NewCompany`: назва фірми клієнта й номер з дати й часу, наприклад «Keller Treuhand AG 260923161021»). Вузли контактів ADAICA підписує з пробілами в кінці, а в картці контакту є TreeView категорій з тим самим AutomationId `treeView`, що й дерево навігатора, тому `MainPage` шукає дерево лише серед `ListBox`.
- **Картка фірми (редагування).** Поля невибраної вкладки (Allgemein, Details, Bankverbindungen) у дереві UI Automation немає, а картка відкривається на вкладці, яку відкривали останньою, тому `CompanyForm` щоразу вибирає потрібну вкладку, а `MainPage.WaitForOpenedCompany` спершу відкриває Allgemein. Перша зміна переводить картку в режим «Element in Bearbeitung», зберігає її кнопка OK внизу картки (`btnOKCtrMainForm`). Щоб перевірити збережені значення, а не те, що лишилося в картці, `MainPage.ReopenCompany` вибирає вузол «Favoriten» (там список без картки) і повертається до фірми. Що й як редагується:
  - **Назва** в картці лише для читання: це рядки назви з вікна «Firmendetails» (кнопка «...» біля назви) через пробіл. Там же рядок адреси й звертання, які ADAICA складає сама, тому тест їх фіксує («Fixieren»). Вузол фірми в дереві — повна назва й категорія в дужках: «Garage Steiner AG 260923165749 Autowerkstatt und Carrosserie Filiale Chur (Wartungsfirma)».
  - **«Verbindung»** (телефон, факс, e-mail, сайт, інше): кнопка «+» відкриває меню лише на клік. Новий рядок редагується просто в списку (значення, тип, примітка, кнопка збереження) без AutomationId, тому `CommunicationsList` бере поля за порядком. Некоректну e-mail ADAICA не приймає й показує повідомлення.
  - **Категорія**: вікно «Kategorie» (кнопка «...» під списком категорій). Прапорець треба клікати: після TogglePattern він вмикається, але кнопка OK лишається недоступною.
  - **Адреса**: «+» біля «Adressen» (меню лише на клік) → категорія (Zweigstelle, Lieferanschrift, …) відкриває вікно «Finden & Erstellen» на вкладці Adressen (`AddressFindCreateWindow`). У рядку пошуку заповнюються всі поля: Land, Bundesland, «Postleitzahl Ort» (одне поле: «80331 München»), Ortsteil, Straße, Hausnummer. «Neu» стає доступною лише тоді, коли заповнено все, зокрема Bundesland, і такої адреси ще немає в довіднику адрес. Якщо адреса вже є, «Neu» недоступна, а «Finden» її знаходить («Es wurden 1 Datensätze gefunden.»). Адреси лишаються в довіднику після кожного прогону, тому тест тоді бере інший номер будинку (`CustomerCase.AnotherAddress`, до 5 спроб). «Neu» відкриває вікно «Neue Adresse anlegen» (`NewAddressDialog`) з адресою з рядка пошуку. Після OK ADAICA закриває обидва вікна й додає рядок до списку «Adressen» (Kategorie, Land, Postleitzahl, Ort, Ortsteil, Straße, Hausnummer; Bundesland список не показує). У довіднику тестової бази єдина країна — Deutschland, тому фірма зі Швейцарії отримує німецьку філію чи адресу доставки.
  - **Фото**: контрол `imageCompany` (`ImagePickerControl`). Контекстне меню відкривається лише правим кліком і живе в окремому вікні Popup на робочому столі, а не в головному вікні, тому контрол шукає його серед вікон процесу. «Hinzufügen» → «Aus dem Dateisystem» відкриває стандартний діалог Windows «Bitte wählen Sie ein Bild aus.»: у ньому поле імені файлу (`1148`) і кнопку «Відкрити» (`1`) шукаємо за AutomationId, бо підписи залежать від мови Windows. JPG тест малює сам (`TestImageFile`: тло насиченого кольору й назва фірми) у теці `%TEMP%\ADAICA_AUTOTEST`. Щойно фірму збережено, файл видаляється, а якщо тест упаде раніше, його прибирає `OnCleanup` чи наступний прогін. Фото ADAICA зберігає в себе: після видалення файлу воно лишається у фірмі. Самого зображення в дереві UI Automation немає, тож перевірка двояка: у меню стає доступним «Löschen», а на знімку контрола щонайменше п'ята частина пікселів має колір тла нашого фото. Меню після перевірки закривається клавішею Esc.
  - **Банківський рахунок**: вікно «Bankkonto hinzufügen/bearbeiten». Назву банку, SWIFT-BIC і BLZ ввести не можна: їх заповнює вибір банку з німецького довідника. Тест фільтрує довідник за BLZ (кнопка фільтра реагує лише на клік) і вибирає рядок. IBAN — правильний німецький, з контрольними цифрами (`CustomerCase.GermanIban`).
  - **Не редагуються в тестовій базі:** «Branche» (довідник галузей порожній, вільний текст ADAICA не зберігає), «Vorauswahl Korrespondenz» (Organisationseinheit, Mitarbeiter, Telefon, E-Mail залежать від співробітників фірми; без них Organisationseinheit не зберігається), «Fremdschlüssel» (лише для читання), «Konditionen» (порожній список). «Nicht mehr existent» тест не вмикає навмисно, щоб фірма лишалася чинною для тестів пошуку.
- **Співробітник відділу фірми.** Під фірмою в дереві є вузол «Mitarbeiter» (його видно, коли фірму розгорнуто кліком по трикутнику `ToggleButton`). Його контекстне меню: «Mitarbeiter zur Firma hinzufügen» (`MnuEmployeesEmployeeNew`) і «Neue Organisationseinheit hinzufügen» (`MnuEmployeesDepartmentNew`). Останній відкриває вікно без AutomationId (`DepartmentDialog` упізнає його за кнопкою `btnOkWndDepartment`) з полем «Name» (`txtName`). OK доступна, лише коли назву введено. Після OK ADAICA додає відділ під «Mitarbeiter» (поруч зі стандартним «andere Tätigkeiten»), виділяє його й відкриває картку (`txtFullNameDepartment`, на вкладці «Mitarbeiter» список співробітників). Меню відділу: «Mitarbeiter zur Organisationseinheit hinzufügen» (`MnuDepartmentEmployeeNew`), «Löschen», «Aktualisieren», «Eigenschaften». Перший пункт відкриває «Finden & Erstellen» з фокусом на «Vorname Nachname» (`txtBoxPersonName` → `txtBox`, дія «Person erstellen»), а «Neu» — «Neue Person/Firma» (`wndPersonCompanyCreate`) із полями персони. Якщо персона з таким ім'ям уже є (імена тестових співробітників з часом повторюються), ADAICA спершу показує «Eine gleichnamige Person ist bereits vorhanden. Meinten Sie vielleicht…» (`wndExistObjects`): «Ja» бере вибрану наявну персону, «Nein» (`btnCancelWndExistObjects`) продовжує створення нової. `ContactFindCreateWindow.OpenNewPersonDialog` натискає «Nein». Фірма й відділ (`cmbWndPCCreateDepartment`, недоступний) уже підставлені, персона — «Nachname, Vorname», Kurzname береться з фірми. Anrede ADAICA вгадує за ім'ям (тест вибирає її явно), а Funktion має єдиний пункт «Mitarbeiter». Після OK вузол персони «Nachname, Vorname (Funktion)» з'являється під відділом, і відкривається картка персони (`tabPerson`, ім'я `txtGeneralName`: «Herr Vorname Nachname»). Меню співробітника має те саме підменю «Neu» (`mnuNew`), що й меню проєкту чи фірми, тому дочірні записи під ним створює `CreateChildRecords` (тест `KontakteNavigator_NewEmployeeWithChildRecords`). Що помічено:
  - **Briefanrede нового співробітника лишається шаблоном** «Sehr geehrte/r Frau/Herr ,», хоча Anrede й прізвище збережено. Так було в 4 із 5 спроб, незалежно від того, чи вибрано Funktion. Правильна «Sehr geehrte Frau Hartmann,» вийшла лише тоді, коли збереження супроводжувала помилка нижче. Імовірний баг, його має підтвердити розробник. Тест `KontakteNavigator_NewCompanyDepartmentWithEmployee` Briefanrede поки не перевіряє.
  - **«Maximale Anzahl von gleichzeitig geöffneten Transaktionen mit der Datenbank erreicht.»** Один раз це повідомлення з'явилося після OK у «Neue Person/Firma», коли картка щойно створеного відділу ще була відкрита. Персону все одно створено. Повторити не вдалося. Повідомлення ADAICA показує дочірнім вікном головного вікна, тому `NewPersonDialog` шукає його й там, і тест падає з його текстом.
  - Якщо Anrede не чіпати, ADAICA може її не зберегти: персона стає «Andrea Weber» без «Frau».
- **Документ із Word-файлу під фірмою.** Контекстне меню фірми → «Neu» (`mnuNew`) → «Dokument» (`mnuNewDocument`) → «Aus dem Dateisystem» (`mnuNewDocumentFromFile`). Поруч є «Word-Dokument» (`mnuNewDocumentFromWord`), «Excel-Dokument», «PowerPoint-Dokument», «Scannen...» і «Editor»; «Word-Dokument» запускає сам Word, щоб написати документ з нуля, тому тест бере готовий файл. ADAICA відкриває стандартний діалог Windows «Відкрити» з вибором кількох файлів (`Multiselect`), і `FileOpenDialog` вводить шлях у лапках (без них ім'я з пробілами Windows ділить на кілька) й натискає Enter. Кнопку «Відкрити» за AutomationId «1» шукати не можна: «1» мають і файли в списку діалогу, тож знаходиться перший файл теки, який і відкривається. Вікна редагування немає: ADAICA одразу зберігає документ, виділяє його в дереві «фірма → Dateien → ім'я файлу» й показує огляд (`DocumentForm`): «Dokumentname» — ім'я файлу без розширення, «Dateityp» — `docx`, «Dateigröße» — у форматі ADAICA (`AdaicaFormat.FormatFileSize`: «1,026 KB»), «Erstellt durch» — «Прізвище, Ім'я» поточного користувача, «Version: 1 / 1», «Status» — «aktuell», «Kurzname» і «Bemerkung» порожні. Вміст документа ADAICA показує в DevExpress RichEdit, але тексту в дереві UI Automation немає, тож вміст перевіряє розмір. Файл складає `TestWordFile` (найменший набір XML Office Open XML у ZIP), текст — `CustomerCase.NewWordFile`: пропозиція чи протокол у справі клієнта. Якщо такий файл (за назвою чи вмістом) уже є, ADAICA показує вікно «Festlegung von Aktionen für den Import von doppelten Dateien» (`lvImportFiles`, `lvDuplicateDocuments`); тому назва фірми з її номером є і в імені файлу, і в тексті, а `MainPage.WaitForOpenedDocument` падає одразу, якщо це вікно все ж з'явилося. Тест — `KontakteNavigator_NewDocumentFromWordFile`. Так само (`AdaicaTestBase.CreateDocumentFromWordFile`) документ створюється останнім із дочірніх записів у `CreateChildRecords` — під фірмою, співробітником і кожним записом Dokumentation; у дереві він теж у вузлі «Dateien» батьківського запису. Там фірми в ADAICA немає, тож у файлі — назва фірми клієнта з новим номером клієнта (`CustomerCase.NewWordFile()`).
  - **Нова версія документа.** «Bearbeiten» (`btnEditDocumentCRecMainFormBase`) кладе копію файлу в теку ADAICA (`%LOCALAPPDATA%\ADAICA_Schweiz_AG\ADAICA\Data\…\Temp\…\<ім'я документа>.docx`), відкриває її у Word (наш мінімальний .docx Word відкриває в режимі сумісності) і стежить за нею; картка показує «Element in Bearbeitung», а «Bearbeiten» недоступна. Коли Word збереже й закриє файл, ADAICA сама зберігає його як нову версію: «Version: 2 / 2», розмір — збереженого Word файлу, внизу знову «Element zur Bearbeitung frei». Word тест не клікає й не друкує в нього: `WordApplication` бере запущений Word через COM (`GetActiveObject("Word.Application")`), знаходить документ за ім'ям файлу, дописує абзац (`CustomerCase.NewWordAddendum`), зберігає й закриває лише його. Якщо Word уже був відкритий у користувача, ADAICA відкриває документ у ньому ж, тому сам Word тест закриває (`Quit`), лише якщо той запустився під час тесту й документів не лишилося. Примусово завершувати Word не можна: при наступному запуску він відновить документ.
  - **Вікно версій** — кнопка «Version» (`btnViewVersionRecordDocument`) → `wndRecDocVersion` (`DocumentVersionsWindow`), таблиця `lvRecDocVersion`, нова версія зверху. Колонки вмикає контекстне меню заголовка → «Änderung der Spaltenkonfiguration» → те саме вікно `wndColumnsConfig`, що й у Dashboard (`ColumnsConfigDialog`); воно буває дочірнім і вікна версій, і головного вікна. ADAICA запам'ятовує колонки для користувача, тож тест повертає їх через `OnCleanup`. Колонки (26): Versions-Nr., Herkunft, Dokumentname, Versions-Erstelldatum, Ersteller der Version, Dateigröße, Dateityp, Bemerkung, Quelldatei Name / Erstelldatum / Besitzer / Änderungsdatum / geändert von, Kurzname, Klasse, Unterklasse, Rec-ID, ID-Constant, OCR erfolgreich, OCR-Bearbeitung-Datum, Fremdschlüssel, Aktuelle Version, Status, Systembemerkung, Flag-komprimierte-PDF, PDF-Koprimierungsdatum. Клітинки рядка йдуть у порядку заголовків, текст — у вкладеному TextBlock. Що показує ADAICA: у версії 2 «Herkunft» — «1» (номер версії, з якої вона походить), «Rec-ID» — запис документа (`…BLOB…`), «Aktuelle Version» — «Ja»; у версії 1 «Herkunft» — «ADAICA», «Rec-ID» — новий запис версії (`…DOCV…`), «Aktuelle Version» — «Nein»; «ID-Constant» — 22 цифри; «Status» обох — «aktuell»; «OCR erfolgreich» і «Fremdschlüssel» — «Nein»; «Quelldatei…», Kurzname, Klasse, Bemerkung, поля PDF — порожні, хоча документ створено з файлу.
- **Нова персона з фірмою.** Gesamtliste → «Neue Person» (`MnuPersonNew`) відкриває те саме вікно «Finden & Erstellen», але з фокусом на «Vorname Nachname». Рядок пошуку: «Vorname Nachname» (`txtBoxPersonName`), «#», «Firma» (`txtBoxCompanyName`), адреса (`ctrAddress`: `txtBoxPostCode` — «Postleitzahl Ort» одним полем, `txtBoxCommunityPart`, `txtBoxStreet`, `txtBoxHouseNumber`), «Telefon» (`txtBoxPACPhone`) і «E-Mail» (`txtBoxPACEmail`), у кожного поле `txtBox`. Поля Land і Bundesland (`txtBoxCountry`, `txtBoxState`) рядок показує лише після кнопки з глобусом (`chkCountryStateHide`, лише клік; стан між сеансами не зберігається). Без Bundesland адресу не створити: над рядком з'являється червоне «Unvollständige Angaben zum Erstellen einer Adresse … Es fehlen Angaben für Bundesland», і «Aktion» лишається порожньою. Із заповненим рядком дія — «Firma erstellen, Person erstellen, Mitarbeiter zur Firma hinzufügen», а «Neu» відкриває «Neue Person/Firma» з усім з рядка пошуку:
  - **Лише для читання:** назва фірми (і 2-й рядок, якщо ввімкнути «Firma zweizeilig», тож його не заповнити), «Person» («Nachname, Vorname»), телефон, e-mail і адреса, крім індексу. Land, Länderkürzel і Bundesland — з рядка пошуку.
  - **Що доповнює тест:** Kommunikationsart для телефону й e-mail, Korrespondenz-Präferenz, Kurzname, HRG-Nr., Anrede (ADAICA вгадує її за ім'ям), Titel (кнопка «...» → вікно «Titel» `wndTitles` з деревом прапорців, як у «Kategorie»), Namenszusatz, Geburtstag, Geburtsname, Funktion. Organisationseinheit нової фірми — лише «andere Tätigkeiten». «Branche» не заповнюється: довідник галузей порожній.
  - **Кому що належить:** кнопки «особа / фірма» праворуч від телефону, e-mail і адреси (`fbPerson`, `fbCompany`, без стану в UI Automation) за замовчуванням дають телефон і e-mail персоні, а адресу — фірмі. Тому в картці персони в «Verbindung» є телефон і e-mail, в «Adressen» — адреса фірми з категорією «Straßenanschrift (Назва фірми)», а в картці фірми «Verbindung» порожній, в «Adressen» — «Straßenanschrift».
  - Після OK ADAICA виділяє персону в дереві «Aktuelle → Фірма → Mitarbeiter → andere Tätigkeiten → von Прізвище, Ім'я Titel (Mitarbeiter)» і відкриває її картку «Frau Dr. Ім'я von Прізвище». Вікно «Finden & Erstellen» закривається трохи пізніше за «Neue Person/Firma», тому тест чекає його закриття (`WaitUntilClosed`).
  - **Geburtstag зсувається на день назад для літніх дат до 1980 року:** 06.06.1970 → 05.06.1970, 10.07.1978 → 09.07.1978. Зимові дати (06.01.1970) і пізніші (10.07.1981, 1985, 1995) зберігаються правильно, як у вікні нової персони, так і в картці. У Німеччині літній час діє з 1980 року, тож, імовірно, клієнт і сервер по-різному рахують літній час для старих дат. Імовірний баг, його має підтвердити розробник. `CustomerCase.NewContact` поки бере дати народження з 1981 року.
  - Briefanrede і тут лишається шаблоном: «Sehr geehrte/r Frau/Herr Dr. von ,» (див. вище).
- **Лист через Outlook і персона з отримувача.** ADAICA не надсилає пошту сама, а працює з Outlook, відкритим на тій самій машині (зазвичай це Outlook користувача — тест чіпає в ньому лише свій лист і ніколи його не закриває). Тест — `MailTests.DokumentationNavigator_NewMailToNewContact`.
  - **Поштовий обліковий запис.** «Benutzereinstellungen» (`mnuUserConfiguration`) → «Maileinstellungen» (`mnuMailConfigurationSettings`) відкриває асистент «Mail-Einstellungen» (`MailSettingsWizard`; AutomationId у вікна й більшості елементів немає, тож вікно — за заголовком, перемикачі й кнопки — за підписами). «Neues Mail-Konto einrichten» + «Neues Microsoft Outlook Konto erstellen» → «Weiter» → запис Outlook зі списку «Outlook-Konten» (`PART_NonActiveItems`, рядок «адреса [Exchange via Outlook]») кнопкою «>» (`Activate`) у «Synchronisierende Konten» (`PART_ActiveItems`) → «Weiter» → «Standardmäßige wählen» (Eingang, Gesendet, Entwürfe, Ausgang, Unerwünscht, Gelöschte) і «Innerhalb der letzten» (`chkLastDays`) 30 Tage → «Weiter» → «Beenden». Запис ADAICA зберігає для користувача, і він діє й у його власній ADAICA. Чи запис уже є, видно з першої сторінки: «Kontoeinstellungen ändern», «Cache löschen» і «Mail-Konto löschen» доступні лише тоді; а на другій сторінці наявний запис уже стоїть у «Synchronisierende Konten», і «Weiter» недоступна. Тому тест додає запис, лише якщо його немає, і не видаляє його після себе («Mail-Konto löschen» стирає й закешовані листи).
  - **Новий лист.** Вузол «E-Mails» у Gesamtliste → «Neu» (`mnuFolderNewRecords`) відкриває вікно нового листа в Outlook. Поля Outlook тест заповнює через COM (`OutlookApplication`: Inspectors → CurrentItem → To, Subject, Body, Send), а не через UI Automation: поле «Кому» — RichEdit, і SetValue на ньому падає, а підписи залежать від мови Outlook. Адреса отримувача — `ім'я.прізвище.ррММддГГххсс@example.com` (`CustomerCase.NewMail`): домен example.com зарезервований для прикладів, тож лист нікому не дійде (Outlook лише отримає звіт про недоставку), а номер робить адресу унікальною, як назви фірм. Outlook надсилає лист через COM без запиту безпеки.
  - **Збереження листа.** Після надсилання ADAICA питає «Soll diese Nachricht gespeichert werden?» (`tbMessage`, «Ja»), далі — «E-Mail-Speicherndialog» (`MailSaveDialog`, впізнаємо за списком листів `lvMail`). На вкладці «Kontakte» — «Von» (користувач) і «An»; невідомий отримувач позначений «?», і OK недоступна («Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen»). **Вміст вкладок зв'язків (`recordLinkView`) UI Automation не показує навіть у raw-дереві** — лише заголовки вкладок (та сама панель, що в базі AUTOTEST не дає зберегти інфозапис під контактом). Тому `MailSaveDialog.OpenFindCreateContact` клікає правою кнопкою за координатами рядка: середина першого рядка — 64,5, висота рядка — 20, колонка «Funktion» — 60 одиниць WPF від кута `recordLinkView`, помножені на масштаб DPI вікна; клік справжньою мишею туди не влучав, а повідомленням (`WindowMessageInput` з точкою) — так. Меню рядка — `ContactMenu`: «Zugehörigen Kontakt finden/erstellen», «Weiteren Kontakt hinzufügen», «Öffnen», «Keine Kontaktzuordnung», «Ignorieren», «Aktualisieren».
  - **Персона з отримувача.** «Zugehörigen Kontakt finden/erstellen» відкриває «Finden & Erstellen» (`ContactFindCreateWindow`, дочірнє вікно вікна збереження) з порожнім рядком пошуку — адресу листа ADAICA туди не підставляє, тож тест вводить «Vorname Nachname» і e-mail. «Neu» → «Neue Person/Firma» (`NewPersonDialog`) без фірми й без Kurzname: персона «Nachname, Vorname», e-mail, Anrede; Briefanrede тут заповнюється правильно («Sehr geehrter Herr Weber,»), на відміну від співробітника відділу. Після OK рядок «An» показує персону, і OK у вікні збереження стає доступною. Kurzname «MAT» такій персоні не задати, тож її впізнаємо за унікальною адресою.
  - **Збережений лист.** ADAICA виділяє його в дереві «Gesamtliste → E-Mails → дд.ММ.рррр  Тема Von: Прізвище, Ім'я;» і показує огляд (`MailView`): «Von» («Ім'я Прізвище <адреса облікового запису>»), «An» («Thomas Weber <адреса>»), «Versanddatum», «Betreff» — текстові поля без AutomationId, кожне за своїм підписом у `InfoHeaderControl`; текст листа (вбудований переглядач) UI Automation не бачить. Під листом — вузли «Kontakte» (`RecordContactsList`: «(Von)» користувач і «(An)» нова персона з «E-Mail: адреса»; до «(An)» ADAICA дописує пробіл у кінці) і «Notizen» з автоматичним інфозаписом «Dokumentations-Element wurde erstellt.» (Klasse «E-Mail», Unterklasse «E-Mail erstellt»).
- **Пошук контактів («Finden & Erstellen» → Kontakte).** Перевірено вручну 25.09.2026 на фірмі з усіма заповненими полями, її контактній особі, відділі й співробітнику з записами. Повне вікно пошуку відкриває кнопка «Create» у заголовку (`btnFindCreateTitleBarQuickCommands`), вкладки: Dokumentation, Kontakte, Objekte, Adressen. Кнопка «Find» (`btnFindTitleBarQuickCommands`) — те саме вікно з назвою «Finden». Якщо в рядку немає імені чи назви (лише телефон, e-mail чи адреса), ADAICA питає «Es wurde kein Kontaktname eingegeben … Möchten Sie dennoch fortfahren?» (Ja / Nein). «Navigieren» відкриває знайдений контакт у вузлі «Aktuelle» навігатора, а знайдений запис — у вкладці Dokumentation. Швидкий пошук «Suchbegriff eingeben…» у навігаторі є лише над списками (Favoriten, вкладки записів у картці), на Gesamtliste його немає.

  | Що шукаємо | Результат |
  |---|---|
  | Фірма за назвою | знаходиться за повною назвою, 1-м рядком, словом із середини («260925164409»), 2-м чи 3-м рядком («Hauptsitz Thun»), у будь-якому регістрі; обрізаний кінець («…16440») теж. Початок слова без джокера («Seebl») знаходить, обрізаний початок («eeblick») — ні, з «*» — так. Поле «Firma» шукає і в Kurzname: «MAT» знаходить 49 фірм |
  | Фірма за телефоном | повний номер з пробілами чи без («+41332672863») — так; частина номера чи національний формат «033 …» — ні |
  | Фірма за e-mail | лише точна адреса; домен окремо («seeblick-thun.ch») — ні. Великі літери знаходять, але ADAICA пише «Bitte geben Sie eine gültige E-Mail-Adresse ein» |
  | Фірма за адресою | за будь-якою з її адрес (також другою «Lieferanschrift»), «Seestrasse» = «Seestraße». У рядку результату — завжди основна адреса, навіть якщо знайдено за іншою |
  | Персона за ім'ям | «Vorname Nachname», лише прізвище, лише ім'я, з «von», з титулом («Prof. Andreas Graf»), у будь-якому регістрі — так. Порядок «Nachname Vorname» чи «Nachname, Vorname» (так ADAICA показує персону в результатах і дереві) — **ні**. Geburtsname — ні |
  | Персона за телефоном, e-mail, власною адресою | так, але в рядку результату — адреса фірми. Тому «Andreas Graf» + «01067 Dresden» (адреса, яку видно в рядку) нічого не знаходить |
  | Відділ | не шукається ні за назвою, ні за своєю адресою |
  | Записи контакту | лише у вікні «Finden» (кнопка «Find»): там прапорець «Suche von mit Kontakten verknüpften Daten» доступний, а у «Finden & Erstellen» (кнопка «Create», режим пошуку й створення) — ні. Рядок пошуку у «Finden» інший: одне поле «Vorname Nachname # Firma» (`txtBoxPersonName`), «Hsnr. / Adresse», «Wert des Fremdschlüssels» (`txtBoxPACFSK`), кнопки «Neu» немає. Щоб з'явилися вкладки записів, потрібен ще прапорець «Mit Daten verknüpfte Kontakte suchen» (`chkShowFindRecInContactResultWndFindCreate`) у «Filtereinstellung zur Suche von Daten…». Тоді поруч із «Person \ Firma» з'являються вкладки Übersicht, Ordner, Projekt, Aufgaben, Termine, Notiz (лише для типів, які знайдено): для співробітника — усі 5 його записів і нотатки «Kontakt-Änderung», для фірми — лише власні записи фірми. Із третім прапорцем «Verwendung von "Suchen in (Suchkriterien)"…» (`chkFindRecInContactWithFilterWndFindCreate`) для фірми додаються й записи її співробітників (19 замість 12), але «Fundstelle» в усіх рядках — «Bemerkung (Volltextsuche)», хоча там немає назви фірми. На Dokumentation фільтра за конкретним контактом немає, а ім'я чи назва контакту в запиті пов'язаних записів не знаходять |

  Інші спостереження:
  - **Дублікати.** «Aktion» пропонує створити контакт, хоча такий уже є: «Firma erstellen» для точного 1-го рядка назви наявної фірми, «Person erstellen» для «andreas graf» (з малої), а для «Andreas Graf» — ні. Для персони з прізвищем і адресою, що вже є, теж «Person erstellen».
  - **Адреса співробітника дістається фірмі.** Адреса, введена в рядку пошуку при створенні співробітника з вузла відділу, за замовчуванням (кнопка «фірма» біля адреси) стає ще однією «Straßenanschrift» фірми, а не адресою персони. У фірми після цього дві основні адреси.
  - **Записи співробітників** не видно на вкладках картки фірми (лише власні записи фірми), але видно в «Letzte Aktivitäten» картки відділу.
  - **Технічна назва відділу в нотатках:** зміна «Verbindung» фірми записується як «Organisationseinheit "_General": Es wurde Kommunikationstyp Telefon … angelegt».
  - **Відділ:** у меню немає «Neu», тож записи під відділом створити не можна, хоча в картці є вкладки Ordner, Projekte тощо. «Verbindung» зберігається одразу з рядка, прапорці й адреси — кнопкою OK. Адреси відділу — лише Straßenanschrift, Postfach, Großempfänger; у списку адрес відділу «Hausnummer» стоїть перед «Straße» (у фірми й персони навпаки). У «Vorauswahl Korrespondenz» телефон і e-mail — лише співробітника, власний телефон відділу там не з'являється; співробітник показаний як «Graf, Andreas» без «von» і титулу.
  - **Незбережені зміни картки** блокують перехід на інший вузол дерева без жодного повідомлення: клік просто не діє, доки не натиснути OK чи Abbrechen.
  - **Одне слово в полі «Vorname Nachname»** ADAICA трактує по-різному: «Graf» → «Person erstellen», «Verkauf» → «Firma erstellen».
  - Підсумок пошуку на Dokumentation — «Es wurden 0 Datensätze gefunden.», на Kontakte — «Keine Daten gefunden.».
- **База AUTOTEST (користувач angela).** Перевірено 29.09.2026 зі збіркою `C:\adaica\feature_documents-in-postgre – копія\ADAICA.exe`. Запуск: `ADAICA_APP_PATH`, `ADAICA_USER=angela`, `ADAICA_DATABASE=AUTOTEST`, `ADAICA_SCHEMA=VUDATA` (схема не збігається з назвою бази) і пароль у `ADAICA_PASSWORD`. Відмінності від BLANK_IGOR, які впливають на тести:
  - **Інфозапис прямо під контактом не зберегти.** У вікні «Neue Notiz» під фірмою чи співробітником OK недоступна, внизу — «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen», а вкладка зв'язків «Dokumentation» позначена «Es fehlt eine Verknüpfung». Отже, конфігурація бази вимагає зв'язку інфозапису із записом Dokumentation. Під проєктом, Ordner, завданням і терміном (зокрема під контактом) інфозапис зберігається: зв'язок дає батьківський запис. Вміст панелі зв'язків (список, поле «Suchbegriff eingeben…», кнопки пошуку й зв'язку) у дереві UI Automation відсутній навіть у raw-дереві. Кнопки на клік мишею не реагують, контекстного меню немає, а поле «Suchbegriff» лише фільтрує список. Тому `KontakteNavigator_NewCompanyWithChildRecords` і `KontakteNavigator_NewEmployeeWithChildRecords` на AUTOTEST падають на інфозаписі.
  - **Рядок пошуку «Finden & Erstellen».** Поля «Bundesland» і «Ortsteil» сховані, доки не ввімкнено «Bundesland berücksichtigen» і «Ortsteil berücksichtigen» (`fbStatesPACWndFindCreate`, `fbCommPartsPACWndFindCreate`). Це робить `OptionalLineFields`.
  - **Колонки списку «Adressen» інші.** У картці фірми немає «Ortsteil», а в картці персони «Straße» стоїть перед «Ortsteil». Тому `CompanyForm.AddressRowsIn` бере комірки за заголовками й порівнює лише показані колонки.
  - **Дерево.** Під кожним записом є додаткові папки конфігурації: «Formular Neueinstellung», «Formular Basisdaten» (завдання), «common», «Inspektion (Termin 5)», «Empty» (термін), «DynamicDocument(Project)» (проєкт), «SubClassForAll» (Ordner). Під проєктом, створеним під фірмою, «Kontakte (1-2)». Після створення дочірнього запису в «Notizen» батька з'являються службові «Dokumentations-Element wurde erstellt.» і «Dokumentation-Änderung». Дерево віртуалізоване: вузлів поза видимою областю в UI Automation немає, тому `MainPage.NavigatorNode` гортає дерево, доки вузол не з'явиться. `SelectedNavigatorNodePath` бачить лише видимих предків.
  - **«Finden»**, відкрите з навігатора Kontakte, починає з вкладки Kontakte, а не Dokumentation.
  - **Що перевірено вручну.** Фірма, під нею проєкт, Ordner, завдання й термін, під кожним — проєкт, Ordner, завдання, термін та інфозапис. Те саме під співробітником відділу іншої фірми. Усього 48 записів, значення в бланку після збереження збігаються з уведеними. Пошук записів («Finden» → Dokumentation): фраза з назв знаходить рівно ті записи, у назві яких вона є (14 з 14). Номер знаходиться з «Fundstelle» = Nummer, ID — з «Fundstelle» = ID (разом зі службовою нотаткою, у тексті якої є цей ID), фраза з примітки — з «Fundstelle» = «Bemerkung (Volltextsuche)». «Bemerkung», «Erstelldatum», «Nummer / Version», «ID» збігаються з записом. Нові колонки цієї збірки: «Quelle / Art / Wert des Fremdschlüssels» (порожні).
  - **Що помічено** (імовірні баги, потребують підтвердження розробників):
    - «Fundtext» і далі завжди дорівнює «Bemerkung», також коли слова знайдено в назві чи в номері; підсумок для одного запису — «Es wurden 1 Datensätze gefunden.».
    - Пошук контактів із пов'язаними даними («Suche von mit Kontakten verknüpften Daten» + «Mit Daten verknüpfte Kontakte suchen») показує лише записи, прив'язані до контакту безпосередньо: для фірми — 4 її записи й «Kontakt wurde erstellt.», для співробітника — 4 його записи й 2 службові нотатки. Записів, створених під цими записами (другий рівень ієрархії), там немає. Колонка «Fundstelle» у цьому режимі порожня.
    - З третім прапорцем («Verwendung von "Suchen in (Suchkriterien)"…») пошук фірми з одним співробітником тривав від 1,5 до 4,5 хв, без нього — кілька секунд. Знайдено записи співробітника, але «Fundstelle» в усіх рядках — «Bemerkung (Volltextsuche)», хоча назви фірми там немає.
    - Із третім прапорцем у рядку з'являється поле «Dokumentation - Suchbegriff» (`txtBoxRecord`). Пошук фірми разом із «Thun» у цьому полі тривав майже 8 хв. Потім ADAICA показала «Keine Daten gefunden.», хоча знайшла термін співробітника «Begehung vor Ort in Thun» (вкладка «Termine»). Список «Person \ Firma» лишився порожнім, вкладки «Übersicht» немає, а підсумок змінився з «Es wurden 0 Datensätze gefunden.» на «Es wurden 1 Datensätze gefunden.».
    - У результатах пошуку контактів рядок фірми має порожню колонку «Firma», а Anrede, Vorname, Nachname і Briefanrede — від «Primärer Briefempfänger» (першого співробітника). У рядку співробітника назва фірми в колонці «Firma» має пробіли в кінці. Briefanrede співробітника тут правильна («Sehr geehrter Herr Schneider,»).
    - На вкладках пов'язаних записів дати проєкту й Ordner («Datum», «Endet») показано з часом «00:00». В Ordner колонка «Wert des Fremdschlüssels» стоїть між «Aufwand» і «Kosten», а в проєкту — після «Kosten».
- **Тестові дані лишаються.** `NavigatorTests` щоразу створює в базі новий проєкт, Ordner, завдання, термін та інфозапис, а тести дочірніх записів — ще по одному проєкту, Ordner, завданню, терміну та інфозапису, під кожним з яких проєкт, Ordner, завдання, термін, інфозапис і документ із Word-файлу (`AdaicaTestBase.CreateChildRecords`). `ContactTests` так само створює фірми, а під фірмою й співробітником — ті самі дочірні записи. `DashboardTests` на кожен із 11 фільтрів створює завдання користувача, а для «Ungelesen» і «Geplant» — щоденну серію з трьох завдань (два з них лишаються непрочитаними). Тести їх не видаляють. Назви й примітки дає `CustomerCase`: випадкова «справа клієнта» (фірма, контактна особа, місто, тема), з якої складаються природні німецькі тексти, наприклад проєкт «Umzug ins neue Lager – Müller Logistik AG» і завдання «Terminplan für den Umzug ins neue Lager aktualisieren». Так у базі накопичуються різні записи для перевірки повнотекстового пошуку за назвами й примітками. Записи однієї справи узгоджені між собою. Дати теж випадкові: різні дні в межах 60 днів від сьогодні в минуле й майбутнє, час — робочі години з кроком 15 хвилин. Дні не повторюються в межах тесту, а поки вистачає вільних днів, — і в межах прогону. Порядок дат зберігається: початок раніше за кінець, а в завдання «Beginnt am» раніше за «Erledigt am», а та — раніше за «Geschlossen an». Минулі дати в термінах і завданнях ADAICA приймає. Вибрані тексти й дати видно в кроках звіту. Kurzname у всіх тестових записах — «MAT», за ним їх можна відрізнити від уведених людьми.
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
- **Скріншоти й DPI.** `TestRunSetup` вмикає для тестового процесу per-monitor DPI awareness. Без цього на моніторах з масштабом понад 100% скріншоти падінь виходять обрізаними. Скріншот падіння знімає лише монітор, на якому стоїть ADAICA: вікна користувача з іншого монітора у звіт не потрапляють.

## Робота паралельно з тестами

Тест можна запустити на другому моніторі й тим часом працювати на першому. Для цього:

- **Вікно на другому моніторі.** Щойно ADAICA показала вікно, `AppSession.Launch` переносить його на монітор із налаштування `Monitor` (у `.runsettings` — `secondary`) і розгортає там, не активуючи (`MonitorPlacement`); `AdaicaWindow` перевіряє це ще раз після входу. Назад вікно не повертається: ADAICA при виході запам'ятовує, де воно стояло, тож наступного разу одразу відкривається на тестовому моніторі й не блимає на вашому. Те саме місце запам'ятає й ваша власна ADAICA з тієї ж папки — перетягніть її вікно, куди треба.
- **Майже все — через UI Automation.** Поля, кнопки, списки, прапорці, вкладки бланків і пункти меню працюють через патерни UI Automation (`Value`, `Invoke`, `Toggle`, `SelectionItem`, `ExpandCollapse`, `Scroll`), які не потребують ні миші, ні фокусу.
- **Решта — повідомленнями у вікно.** Для UI, що реагує лише на клік (див. «Лише клік»), `WindowMessageInput` надсилає `WM_LBUTTONDOWN`/`WM_RBUTTONDOWN`/`WM_KEYDOWN` прямо у вікно ADAICA. Курсор не рухається, а вікно може бути перекрите. Щоб WPF прийняла такий клік, потрібні дві речі (видно з вихідного коду WPF, `HwndMouseInputProvider`): подію миші для вікна, над яким немає курсора, WPF приймає, лише якщо це вікно захопило мишу, а кнопки WPF (`ButtonBase`) клікаються, лише коли система вважає кнопку миші натиснутою. Тому на час кліку тестовий потік підключається до черги введення потоку ADAICA (`AttachThreadInput`), захоплює мишу її вікном, позначає кнопку натиснутою лише в цій черзі й надсилає повідомлення синхронно. Клавіші (Esc у контекстному меню) — звичайні повідомлення `WM_KEYDOWN`/`WM_KEYUP` у вікно меню.
- **Запасний шлях.** Чи подіяв клік, видно лише з результату, тож `UiInput` отримує від кожного місця умову (відкрилося меню, виділився вузол, з'явилася вкладка). Якщо за таймаут її немає, у режимі `Background` дія повторюється справжньою мишею (`AtomicMouse`: переміщення й натискання одним `SendInput`, із перевіркою, що в точці кліку вікно ADAICA) чи клавіатурою (спершу ADAICA виводиться на передній план), а у звіті Allure з'являється крок «Повідомлення вікну не подіяло … — повтор: клік мишею». У режимі `BackgroundOnly` тест натомість падає. У режимі `BackgroundOnly` перевірено всі тести Dashboard, Dokumentation і входу, тести контактів і частину тестів пошуку.

Що варто знати:

- **Меню й списки потребують активної ADAICA, тож на мить вона забирає фокус.** Список навігаторів, контекстні меню й меню кнопок «+» — це WPF Popup, а WPF закриває Popup, щойно застосунок стає неактивним (та й клавіші, як-от Esc у меню, WPF доставляє лише активному застосунку). Тому перед правим кліком, кліком по кнопці з меню (`UiInput.LeftClickOpeningMenu`) і натисканням клавіші ADAICA стає активною. Крім того, ADAICA сама активує вікно під час запуску, після входу й відкриваючи списки. Щоб фокус не лишався в ній, `ForegroundGuard` стежить за активним вікном і повертає його у вікно, де ви працювали, приблизно через 0,4 с після того, як у ADAICA закрилося останнє спливне вікно (поки дія `UiInput` не подіяла, він чекає). Робить він це лише поки ви справді працюєте: ваша остання дія мишею чи клавіатурою була не давніше 30 с (`GetLastInputInfo`; повідомлення, які тест надсилає у вікно, введенням не вважаються). Коли працює тільки тест, фокус лишається в ADAICA з усіма її вікнами (діалоги, повідомлення) і не смикається. «Вашим» вважається лише вікно, активне одразу після вашої дії, а не будь-яке, яке Windows активує сама. За тест це трапляється від 10 до 40 разів, кожен раз на пів секунди–секунду. Якщо ви саме друкуєте, символи підуть в ADAICA, а Enter чи літера у відкритому меню можуть вибрати пункт і зламати тест. Фокус не повертається, коли курсор над вікном ADAICA (схоже, ви клікнули в неї самі). Повністю позбутися цього можна лише окремим сеансом Windows для тестів (віртуальна машина, окремий комп'ютер чи агент Jenkins).
- **Спливні вікна без власника.** Поки ADAICA неактивна, WPF створює Popup як окреме вікно робочого столу, а не дочірнє вікно головного (`Popup.ConnectedToForegroundWindow`), тому сторінки шукають меню через `Popups`.
- **Не клікайте у вікні ADAICA й не перетягуйте на нього свої вікна.** Рухати мишею над ним можна, але клік у ньому змінить стан тесту. Перевірка фото контакту (`ImagePickerControl.ShouldShowImageWithBackground`) порівнює пікселі з екрана, тож перекрите вікно її зламає.
- **Тест у режимі `Mouse`** (або запасний клік мишею) займає курсор, як раніше; на цей час не рухайте мишею.
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
