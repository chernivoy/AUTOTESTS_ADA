// UI-автотести ADAICA_AUTOTEST.
// Вимоги до агента та плагінів описано в README.md, розділ «Jenkins».
pipeline {
    // Windows-агент з розблокованою інтерактивною сесією користувача.
    agent { label 'windows-ui' }

    parameters {
        string(name: 'APP_PATH', defaultValue: '', trim: true,
               description: 'Повний шлях до exe ADAICA на агенті. Порожньо — AppPath з ADAICA_AUTOTEST.runsettings.')
        string(name: 'TEST_FILTER', defaultValue: '', trim: true,
               description: 'Фільтр dotnet test, напр. Category=Smoke або FullyQualifiedName~LoginTests. Порожньо — усі тести.')
    }

    options {
        timeout(time: 60, unit: 'MINUTES')
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '30'))
    }

    environment {
        DOTNET_NOLOGO = '1'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        CONFIGURATION = 'Release'
        TEST_PROJECT = 'tests/ADAICA_AUTOTEST/ADAICA_AUTOTEST.csproj'
        ADAICA_APP_PATH = "${params.APP_PATH}"
        // Облікові дані ADAICA беріть з Jenkins Credentials, наприклад:
        //   ADAICA = credentials('adaica-test-user')
        // і передавайте в тести як ADAICA_USER=%ADAICA_USR%, ADAICA_PASSWORD=%ADAICA_PSW%.
    }

    stages {
        stage('Restore') {
            steps {
                bat 'dotnet --info'
                bat 'dotnet restore ADAICA_AUTOTEST.sln'
            }
        }

        stage('Build') {
            steps {
                bat 'dotnet build ADAICA_AUTOTEST.sln -c %CONFIGURATION% --no-restore'
            }
        }

        stage('UI tests') {
            steps {
                bat '''
                    if exist "%WORKSPACE%\\TestResults" rmdir /s /q "%WORKSPACE%\\TestResults"
                    if exist "%WORKSPACE%\\artifacts" rmdir /s /q "%WORKSPACE%\\artifacts"
                '''
                script {
                    def filter = params.TEST_FILTER ? "--filter \"${params.TEST_FILTER}\"" : ''
                    def status = bat returnStatus: true, script: """
                        set "ADAICA_ARTIFACTS_DIR=%WORKSPACE%\\artifacts"
                        dotnet test %TEST_PROJECT% -c %CONFIGURATION% --no-build ${filter} ^
                            --results-directory "%WORKSPACE%\\TestResults" ^
                            --logger "console;verbosity=normal" ^
                            --logger "junit;LogFilePath=%WORKSPACE%\\TestResults\\junit.xml"
                    """
                    if (!fileExists('TestResults/junit.xml')) {
                        error "dotnet test завершився з кодом ${status} і не створив звіт — див. лог вище."
                    }
                    if (status != 0) {
                        unstable "Є тести, що впали (код ${status})."
                    }
                }
            }
        }
    }

    post {
        always {
            junit testResults: 'TestResults/junit.xml', allowEmptyResults: true
            // allure-csharp пише результати поруч зі збіркою тестів.
            allure results: [[path: "tests/ADAICA_AUTOTEST/bin/${env.CONFIGURATION}/net10.0-windows/allure-results"]]
            archiveArtifacts artifacts: 'artifacts/**', allowEmptyArchive: true
        }
    }
}
