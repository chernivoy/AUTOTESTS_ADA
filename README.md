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
   │  └─ ComboBoxExtensions.cs         вибір пункту у ComboBox зі згортанням списку
   ├─ Pages/                           Page Objects ADAICA
   │  ├─ AdaicaWindow.cs               вікно FlatWnd: діалоги, вихід із підтвердженням
   │  ├─ LoginPage.cs                  форма входу (користувач, пароль, база, схема)
   │  ├─ MainPage.cs                   робочий простір після входу, вкладки й дерево навігаторів
   │  ├─ ProjectForm.cs                бланк проєкту (у вікні та в огляді збереженого проєкту)
   │  ├─ ProjectDialog.cs              вікно проєкту («Neues Projekt»): OK, Abbrechen
   │  ├─ ProjectData.cs                значення полів проєкту
   │  └─ MessageBoxDialog.cs           стандартний діалог повідомлення ADAICA
   ├─ Tests/
   │  ├─ AdaicaTestBase.cs             базовий клас тестів ADAICA
   │  ├─ LoginTests.cs                 вхід в ADAICA
   │  └─ NavigatorTests.cs             навігатор Dokumentation → створення і перевірка проєкту
   ├─ TestRunSetup.cs                  дії перед усім прогоном (DPI, очищення Allure)
   ├─ ADAICA_AUTOTEST.runsettings      параметри прогону
   └─ allureConfig.json
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
- **Лише мишею.** Пункт списку навігаторів і контекстне меню вузла дерева реагують тільки на клік мишею: `SelectionItem`, Enter, Shift+F10, клавіша меню й `ShowContextMenu` з UI Automation не діють. Для таких кліків є `AtomicMouse`. Він переміщує курсор і натискає кнопку одним викликом `SendInput`. Звичайний клік FlaUI робить це двома кроками, і якщо між ними курсор зсуне користувач або програма віддаленого доступу, клік піде в довільне місце екрана, навіть в інший застосунок. Під час прогону не рухайте мишею.
- **Дерево навігатора.** До назв вузлів ADAICA додає діапазон записів («Projekte (1-25)»), тому `MainPage.NavigatorNode` порівнює лише назву.
- **Вікно проєкту.** «Neu» у контекстному меню вузла Projekte відкриває дочірнє вікно без AutomationId; `ProjectDialog` знаходить його за вкладкою `tiMainRecordFolder`. Редагуються лише Name, Kurzname, дати, Kosten/Aufwand з одиницями та Bemerkung: клас і відповідальний задаються окремими діалогами, решта полів заповнюється автоматично. Дати й числа ADAICA показує в німецькому форматі (`18.09.2026`, `1500,00`), тому `ProjectData` зберігає суми як `decimal`. Кожне відкриття вікна займає наступний номер проєкту, навіть якщо закрити його без збереження.
- **Після збереження** (OK) ADAICA сама переходить до нового проєкту: виділяє в дереві вузол «Назва (Номер)» і показує той самий бланк в області перегляду (`InformationView`). `MainPage.WaitForOpenedProject` шукає бланк лише там, бо у відкритому діалозі є поля з тими самими AutomationId. Назви проєктів між прогонами повторюються, тож свій проєкт тест упізнає за номером.
- **Тестові дані лишаються.** `NavigatorTests` щоразу створює в базі новий проєкт `Main_autotest_project` і не видаляє його.
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
        var main = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);

        // Прибирання виконається після тесту (і після скріншота падіння), перед виходом з ADAICA.
        var tabCount = main.NavigatorTabNames.Count;
        OnCleanup(() => main.CloseLastNavigatorTabs(keep: tabCount));

        main.OpenNavigator("Dokumentation");
        main.ActiveNavigatorName.ShouldBe("Dokumentation");
    }
}
```

Усе, що тест змінює в збережених налаштуваннях користувача (вкладки, фільтри тощо), повертайте через `OnCleanup`, інакше наступні прогони почнуться з іншого стану.

Правила:

- Елементи шукайте лише через Page Object, а всередині нього — через `Element("AutomationId")`: цей метод сам чекає появи елемента.
- Не використовуйте `Thread.Sleep`. Для очікування є `Wait.Until(() => умова, "що чекаємо")`.
- Позначайте методи Page Object атрибутом `[AllureStep("...")]`, щоб у звіті було видно кроки. Секрети в параметрах позначайте `[AllureParameter(Ignore = true)]`.
- Тести не запускаються паралельно (`AssemblyInfo.cs`), бо робочий стіл один.
- Дерево елементів зручно дивитися в **FlaUInspect** (режим UIA3), **Accessibility Insights for Windows** або `Inspect.exe` з Windows SDK. ADAICA для цього запускайте зі змінною `AutomationTests__AutomationPeerFeatureEnabled=true`.

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
