using System.Collections.Frozen;

namespace MigrationTool.Localization;

/// <summary>
/// Contains all translations for the Migration Tool.
/// </summary>
public static class Translations
{
    /// <summary>
    /// Supported languages.
    /// </summary>
    public static readonly FrozenDictionary<string, string> SupportedLanguages = new Dictionary<string, string>
    {
        ["en"] = "English",
        ["cs"] = "Čeština",
        ["pl"] = "Polski",
        ["uk"] = "Українська"
    }.ToFrozenDictionary();

    /// <summary>
    /// All translations indexed by language code and key.
    /// </summary>
    public static readonly FrozenDictionary<string, FrozenDictionary<string, string>> All = CreateAllTranslations();

    private static FrozenDictionary<string, FrozenDictionary<string, string>> CreateAllTranslations()
    {
        return new Dictionary<string, FrozenDictionary<string, string>>
        {
            ["en"] = CreateEnglish().ToFrozenDictionary(),
            ["cs"] = CreateCzech().ToFrozenDictionary(),
            ["pl"] = CreatePolish().ToFrozenDictionary(),
            ["uk"] = CreateUkrainian().ToFrozenDictionary()
        }.ToFrozenDictionary();
    }

    private static Dictionary<string, string> CreateEnglish() => new()
    {
        // App
        ["AppTitle"] = "Migration Tool",
        ["AppDescription"] = ".NET Project Migration & Refactoring Tool",

        // Navigation
        ["NavDashboard"] = "Dashboard",
        ["NavExplorer"] = "Project Explorer",
        ["NavPlanner"] = "Migration Planner",
        ["NavSettings"] = "Settings",

        // Dashboard
        ["DashboardTitle"] = "Dashboard",
        ["DashboardDescription"] = "Overview of your solution and migration progress.",
        ["TotalProjects"] = "Total Projects",
        ["TestProjects"] = "Test Projects",
        ["SourceFiles"] = "Source Files",
        ["TotalClasses"] = "Total Classes",
        ["TotalTests"] = "Total Tests",
        ["DashboardWelcome"] = "Welcome to Migration Tool",
        ["DashboardNoSolution"] = "No solution loaded",
        ["DashboardSelectSolution"] = "Select a solution to get started",
        ["DashboardProjectCount"] = "Projects",
        ["DashboardTestProjectCount"] = "Test Projects",
        ["DashboardSourceProjectCount"] = "Source Projects",
        ["DashboardFileCount"] = "Files",
        ["DashboardClassCount"] = "Classes",
        ["DashboardTestCount"] = "Tests",

        // Explorer
        ["ExplorerTitle"] = "Project Explorer",
        ["ProjectExplorerTitle"] = "Project Explorer",
        ["ProjectExplorerDescription"] = "Browse projects, files, and code structure.",
        ["ExplorerProjects"] = "Projects",
        ["ExplorerFiles"] = "Files",
        ["ExplorerTests"] = "Tests",
        ["ExplorerTreeView"] = "Tree View",
        ["ExplorerListView"] = "List View",
        ["ExplorerExpandAll"] = "Expand All",
        ["ExplorerCollapseAll"] = "Collapse All",
        ["ExplorerNoFiles"] = "No files found",
        ["ExplorerNoTests"] = "No tests found",
        ["FilesTree"] = "Files (Tree)",
        ["NoFilesFound"] = "No files found in this project.",
        ["NoProjectsLoaded"] = "No projects loaded. Please select a solution in settings.",
        ["Project"] = "Project",
        ["FileName"] = "File Name",
        ["ClassName"] = "Class Name",
        ["Tests"] = "Tests",
        ["FilePath"] = "File Path",

        // Planner
        ["PlannerTitle"] = "Migration Planner",
        ["MigrationPlannerTitle"] = "Migration Planner",
        ["MigrationPlannerDescription"] = "Plan and execute your project migrations.",
        ["PlannerNewPlan"] = "New Plan",
        ["PlannerLoadPlan"] = "Load Plan",
        ["PlannerSavePlan"] = "Save Plan",
        ["PlannerExecute"] = "Execute",
        ["PlannerValidate"] = "Validate",
        ["PlannerAddStep"] = "Add Step",
        ["PlannerRemoveStep"] = "Remove Step",
        ["PlannerStepSource"] = "Source",
        ["PlannerStepTarget"] = "Target",
        ["PlannerStepAction"] = "Action",
        ["CreatePlan"] = "Create Plan",
        ["LoadPlan"] = "Load Plan",
        ["SavePlan"] = "Save Plan",
        ["ExecutePlan"] = "Execute Plan",
        ["PlanDetails"] = "Plan Details",
        ["Source"] = "Source",
        ["Target"] = "Target",

        // Actions
        ["ActionMoveFile"] = "Move File",
        ["ActionMoveFolder"] = "Move Folder",
        ["ActionCopyFile"] = "Copy File",
        ["ActionCopyFolder"] = "Copy Folder",
        ["ActionCreateProject"] = "Create Project",
        ["ActionDeleteProject"] = "Delete Project",
        ["ActionRenameNamespace"] = "Rename Namespace",

        // Settings
        ["SettingsTitle"] = "Settings",
        ["SettingsDescription"] = "Configure application settings.",
        ["SettingsLanguage"] = "Language",
        ["SettingsTheme"] = "Theme",
        ["SettingsThemeLight"] = "Light",
        ["SettingsThemeDark"] = "Dark",
        ["SettingsWorkspace"] = "Workspace Path",
        ["SettingsBrowse"] = "Browse",
        ["WorkspacePath"] = "Workspace Path",
        ["SelectSolution"] = "Select Solution",
        ["SelectWorkspace"] = "Please select a workspace path.",
        ["SolutionsFound"] = "Solutions found:",
        ["NoSolutionsFound"] = "No solutions found in the workspace.",
        ["Language"] = "Language",
        ["Browse"] = "Browse",

        // Common
        ["CommonName"] = "Name",
        ["CommonPath"] = "Path",
        ["CommonType"] = "Type",
        ["CommonStatus"] = "Status",
        ["CommonActions"] = "Actions",
        ["CommonSave"] = "Save",
        ["CommonCancel"] = "Cancel",
        ["CommonDelete"] = "Delete",
        ["CommonEdit"] = "Edit",
        ["CommonRefresh"] = "Refresh",
        ["CommonLoading"] = "Loading...",
        ["CommonError"] = "Error",
        ["CommonSuccess"] = "Success",
        ["CommonWarning"] = "Warning",
        ["CommonInfo"] = "Info",
        ["CommonYes"] = "Yes",
        ["CommonNo"] = "No",
        ["CommonConfirm"] = "Confirm",
        ["Loading"] = "Loading...",
        ["Success"] = "Success",
        ["Select"] = "Select",
        ["Add"] = "Add",
        ["Delete"] = "Delete",
        ["Actions"] = "Actions",
        ["Type"] = "Type",
        ["NoData"] = "No data available.",
        ["NotImplemented"] = "This feature is not yet implemented.",
        ["Apply"] = "Apply",
        ["ChangesSaved"] = "Changes saved successfully!",

        // Errors
        ["ErrorSolutionNotFound"] = "Solution not found",
        ["ErrorProjectNotFound"] = "Project not found",
        ["ErrorFileNotFound"] = "File not found",
        ["ErrorInvalidPath"] = "Invalid path",
        ["ErrorMigrationFailed"] = "Migration failed",

        // Messages
        ["MessageMigrationComplete"] = "Migration completed successfully",
        ["MessagePlanSaved"] = "Plan saved successfully",
        ["MessagePlanLoaded"] = "Plan loaded successfully",
        ["MessageValidationPassed"] = "Validation passed",
        ["MessageValidationFailed"] = "Validation failed"
    };

    private static Dictionary<string, string> CreateCzech() => new()
    {
        // App
        ["AppTitle"] = "Migrační nástroj",
        ["AppDescription"] = "Nástroj pro migraci a refaktoring .NET projektů",

        // Navigation
        ["NavDashboard"] = "Přehled",
        ["NavExplorer"] = "Průzkumník projektů",
        ["NavPlanner"] = "Plánovač migrace",
        ["NavSettings"] = "Nastavení",

        // Dashboard
        ["DashboardTitle"] = "Přehled",
        ["DashboardWelcome"] = "Vítejte v migračním nástroji",
        ["DashboardNoSolution"] = "Žádné řešení není načteno",
        ["DashboardSelectSolution"] = "Vyberte řešení pro začátek",
        ["DashboardProjectCount"] = "Projekty",
        ["DashboardTestProjectCount"] = "Testovací projekty",
        ["DashboardSourceProjectCount"] = "Zdrojové projekty",
        ["DashboardFileCount"] = "Soubory",
        ["DashboardClassCount"] = "Třídy",
        ["DashboardTestCount"] = "Testy",

        // Explorer
        ["ExplorerTitle"] = "Průzkumník projektů",
        ["ExplorerProjects"] = "Projekty",
        ["ExplorerFiles"] = "Soubory",
        ["ExplorerTests"] = "Testy",
        ["ExplorerTreeView"] = "Stromové zobrazení",
        ["ExplorerListView"] = "Seznamové zobrazení",
        ["ExplorerExpandAll"] = "Rozbalit vše",
        ["ExplorerCollapseAll"] = "Sbalit vše",
        ["ExplorerNoFiles"] = "Žádné soubory nenalezeny",
        ["ExplorerNoTests"] = "Žádné testy nenalezeny",

        // Planner
        ["PlannerTitle"] = "Plánovač migrace",
        ["PlannerNewPlan"] = "Nový plán",
        ["PlannerLoadPlan"] = "Načíst plán",
        ["PlannerSavePlan"] = "Uložit plán",
        ["PlannerExecute"] = "Spustit",
        ["PlannerValidate"] = "Ověřit",
        ["PlannerAddStep"] = "Přidat krok",
        ["PlannerRemoveStep"] = "Odebrat krok",
        ["PlannerStepSource"] = "Zdroj",
        ["PlannerStepTarget"] = "Cíl",
        ["PlannerStepAction"] = "Akce",

        // Actions
        ["ActionMoveFile"] = "Přesunout soubor",
        ["ActionMoveFolder"] = "Přesunout složku",
        ["ActionCopyFile"] = "Kopírovat soubor",
        ["ActionCopyFolder"] = "Kopírovat složku",
        ["ActionCreateProject"] = "Vytvořit projekt",
        ["ActionDeleteProject"] = "Smazat projekt",
        ["ActionRenameNamespace"] = "Přejmenovat namespace",

        // Settings
        ["SettingsTitle"] = "Nastavení",
        ["SettingsLanguage"] = "Jazyk",
        ["SettingsTheme"] = "Motiv",
        ["SettingsThemeLight"] = "Světlý",
        ["SettingsThemeDark"] = "Tmavý",
        ["SettingsWorkspace"] = "Cesta k pracovnímu prostoru",
        ["SettingsBrowse"] = "Procházet",

        // Common
        ["CommonName"] = "Název",
        ["CommonPath"] = "Cesta",
        ["CommonType"] = "Typ",
        ["CommonStatus"] = "Stav",
        ["CommonActions"] = "Akce",
        ["CommonSave"] = "Uložit",
        ["CommonCancel"] = "Zrušit",
        ["CommonDelete"] = "Smazat",
        ["CommonEdit"] = "Upravit",
        ["CommonRefresh"] = "Obnovit",
        ["CommonLoading"] = "Načítání...",
        ["CommonError"] = "Chyba",
        ["CommonSuccess"] = "Úspěch",
        ["CommonWarning"] = "Varování",
        ["CommonInfo"] = "Info",
        ["CommonYes"] = "Ano",
        ["CommonNo"] = "Ne",
        ["CommonConfirm"] = "Potvrdit",

        // Errors
        ["ErrorSolutionNotFound"] = "Řešení nenalezeno",
        ["ErrorProjectNotFound"] = "Projekt nenalezen",
        ["ErrorFileNotFound"] = "Soubor nenalezen",
        ["ErrorInvalidPath"] = "Neplatná cesta",
        ["ErrorMigrationFailed"] = "Migrace selhala",

        // Messages
        ["MessageMigrationComplete"] = "Migrace byla úspěšně dokončena",
        ["MessagePlanSaved"] = "Plán byl úspěšně uložen",
        ["MessagePlanLoaded"] = "Plán byl úspěšně načten",
        ["MessageValidationPassed"] = "Validace proběhla úspěšně",
        ["MessageValidationFailed"] = "Validace selhala"
    };

    private static Dictionary<string, string> CreatePolish() => new()
    {
        // App
        ["AppTitle"] = "Narzędzie migracji",
        ["AppDescription"] = "Narzędzie do migracji i refaktoryzacji projektów .NET",

        // Navigation
        ["NavDashboard"] = "Panel",
        ["NavExplorer"] = "Eksplorator projektów",
        ["NavPlanner"] = "Planer migracji",
        ["NavSettings"] = "Ustawienia",

        // Dashboard
        ["DashboardTitle"] = "Panel",
        ["DashboardWelcome"] = "Witamy w narzędziu migracji",
        ["DashboardNoSolution"] = "Brak załadowanego rozwiązania",
        ["DashboardSelectSolution"] = "Wybierz rozwiązanie, aby rozpocząć",
        ["DashboardProjectCount"] = "Projekty",
        ["DashboardTestProjectCount"] = "Projekty testowe",
        ["DashboardSourceProjectCount"] = "Projekty źródłowe",
        ["DashboardFileCount"] = "Pliki",
        ["DashboardClassCount"] = "Klasy",
        ["DashboardTestCount"] = "Testy",

        // Explorer
        ["ExplorerTitle"] = "Eksplorator projektów",
        ["ExplorerProjects"] = "Projekty",
        ["ExplorerFiles"] = "Pliki",
        ["ExplorerTests"] = "Testy",
        ["ExplorerTreeView"] = "Widok drzewa",
        ["ExplorerListView"] = "Widok listy",
        ["ExplorerExpandAll"] = "Rozwiń wszystko",
        ["ExplorerCollapseAll"] = "Zwiń wszystko",
        ["ExplorerNoFiles"] = "Nie znaleziono plików",
        ["ExplorerNoTests"] = "Nie znaleziono testów",

        // Planner
        ["PlannerTitle"] = "Planer migracji",
        ["PlannerNewPlan"] = "Nowy plan",
        ["PlannerLoadPlan"] = "Załaduj plan",
        ["PlannerSavePlan"] = "Zapisz plan",
        ["PlannerExecute"] = "Wykonaj",
        ["PlannerValidate"] = "Sprawdź",
        ["PlannerAddStep"] = "Dodaj krok",
        ["PlannerRemoveStep"] = "Usuń krok",
        ["PlannerStepSource"] = "Źródło",
        ["PlannerStepTarget"] = "Cel",
        ["PlannerStepAction"] = "Akcja",

        // Actions
        ["ActionMoveFile"] = "Przenieś plik",
        ["ActionMoveFolder"] = "Przenieś folder",
        ["ActionCopyFile"] = "Kopiuj plik",
        ["ActionCopyFolder"] = "Kopiuj folder",
        ["ActionCreateProject"] = "Utwórz projekt",
        ["ActionDeleteProject"] = "Usuń projekt",
        ["ActionRenameNamespace"] = "Zmień nazwę przestrzeni nazw",

        // Settings
        ["SettingsTitle"] = "Ustawienia",
        ["SettingsLanguage"] = "Język",
        ["SettingsTheme"] = "Motyw",
        ["SettingsThemeLight"] = "Jasny",
        ["SettingsThemeDark"] = "Ciemny",
        ["SettingsWorkspace"] = "Ścieżka obszaru roboczego",
        ["SettingsBrowse"] = "Przeglądaj",

        // Common
        ["CommonName"] = "Nazwa",
        ["CommonPath"] = "Ścieżka",
        ["CommonType"] = "Typ",
        ["CommonStatus"] = "Status",
        ["CommonActions"] = "Akcje",
        ["CommonSave"] = "Zapisz",
        ["CommonCancel"] = "Anuluj",
        ["CommonDelete"] = "Usuń",
        ["CommonEdit"] = "Edytuj",
        ["CommonRefresh"] = "Odśwież",
        ["CommonLoading"] = "Ładowanie...",
        ["CommonError"] = "Błąd",
        ["CommonSuccess"] = "Sukces",
        ["CommonWarning"] = "Ostrzeżenie",
        ["CommonInfo"] = "Info",
        ["CommonYes"] = "Tak",
        ["CommonNo"] = "Nie",
        ["CommonConfirm"] = "Potwierdź",

        // Errors
        ["ErrorSolutionNotFound"] = "Nie znaleziono rozwiązania",
        ["ErrorProjectNotFound"] = "Nie znaleziono projektu",
        ["ErrorFileNotFound"] = "Nie znaleziono pliku",
        ["ErrorInvalidPath"] = "Nieprawidłowa ścieżka",
        ["ErrorMigrationFailed"] = "Migracja nie powiodła się",

        // Messages
        ["MessageMigrationComplete"] = "Migracja zakończona pomyślnie",
        ["MessagePlanSaved"] = "Plan zapisany pomyślnie",
        ["MessagePlanLoaded"] = "Plan załadowany pomyślnie",
        ["MessageValidationPassed"] = "Walidacja przeszła pomyślnie",
        ["MessageValidationFailed"] = "Walidacja nie powiodła się"
    };

    private static Dictionary<string, string> CreateUkrainian() => new()
    {
        // App
        ["AppTitle"] = "Інструмент міграції",
        ["AppDescription"] = "Інструмент для міграції та рефакторингу .NET проектів",

        // Navigation
        ["NavDashboard"] = "Панель",
        ["NavExplorer"] = "Провідник проектів",
        ["NavPlanner"] = "Планувальник міграції",
        ["NavSettings"] = "Налаштування",

        // Dashboard
        ["DashboardTitle"] = "Панель",
        ["DashboardWelcome"] = "Ласкаво просимо до інструменту міграції",
        ["DashboardNoSolution"] = "Рішення не завантажено",
        ["DashboardSelectSolution"] = "Виберіть рішення для початку",
        ["DashboardProjectCount"] = "Проекти",
        ["DashboardTestProjectCount"] = "Тестові проекти",
        ["DashboardSourceProjectCount"] = "Вихідні проекти",
        ["DashboardFileCount"] = "Файли",
        ["DashboardClassCount"] = "Класи",
        ["DashboardTestCount"] = "Тести",

        // Explorer
        ["ExplorerTitle"] = "Провідник проектів",
        ["ExplorerProjects"] = "Проекти",
        ["ExplorerFiles"] = "Файли",
        ["ExplorerTests"] = "Тести",
        ["ExplorerTreeView"] = "Деревоподібний вигляд",
        ["ExplorerListView"] = "Списковий вигляд",
        ["ExplorerExpandAll"] = "Розгорнути все",
        ["ExplorerCollapseAll"] = "Згорнути все",
        ["ExplorerNoFiles"] = "Файли не знайдено",
        ["ExplorerNoTests"] = "Тести не знайдено",

        // Planner
        ["PlannerTitle"] = "Планувальник міграції",
        ["PlannerNewPlan"] = "Новий план",
        ["PlannerLoadPlan"] = "Завантажити план",
        ["PlannerSavePlan"] = "Зберегти план",
        ["PlannerExecute"] = "Виконати",
        ["PlannerValidate"] = "Перевірити",
        ["PlannerAddStep"] = "Додати крок",
        ["PlannerRemoveStep"] = "Видалити крок",
        ["PlannerStepSource"] = "Джерело",
        ["PlannerStepTarget"] = "Ціль",
        ["PlannerStepAction"] = "Дія",

        // Actions
        ["ActionMoveFile"] = "Перемістити файл",
        ["ActionMoveFolder"] = "Перемістити папку",
        ["ActionCopyFile"] = "Копіювати файл",
        ["ActionCopyFolder"] = "Копіювати папку",
        ["ActionCreateProject"] = "Створити проект",
        ["ActionDeleteProject"] = "Видалити проект",
        ["ActionRenameNamespace"] = "Перейменувати простір імен",

        // Settings
        ["SettingsTitle"] = "Налаштування",
        ["SettingsLanguage"] = "Мова",
        ["SettingsTheme"] = "Тема",
        ["SettingsThemeLight"] = "Світла",
        ["SettingsThemeDark"] = "Темна",
        ["SettingsWorkspace"] = "Шлях до робочої області",
        ["SettingsBrowse"] = "Огляд",

        // Common
        ["CommonName"] = "Назва",
        ["CommonPath"] = "Шлях",
        ["CommonType"] = "Тип",
        ["CommonStatus"] = "Статус",
        ["CommonActions"] = "Дії",
        ["CommonSave"] = "Зберегти",
        ["CommonCancel"] = "Скасувати",
        ["CommonDelete"] = "Видалити",
        ["CommonEdit"] = "Редагувати",
        ["CommonRefresh"] = "Оновити",
        ["CommonLoading"] = "Завантаження...",
        ["CommonError"] = "Помилка",
        ["CommonSuccess"] = "Успіх",
        ["CommonWarning"] = "Попередження",
        ["CommonInfo"] = "Інформація",
        ["CommonYes"] = "Так",
        ["CommonNo"] = "Ні",
        ["CommonConfirm"] = "Підтвердити",

        // Errors
        ["ErrorSolutionNotFound"] = "Рішення не знайдено",
        ["ErrorProjectNotFound"] = "Проект не знайдено",
        ["ErrorFileNotFound"] = "Файл не знайдено",
        ["ErrorInvalidPath"] = "Недійсний шлях",
        ["ErrorMigrationFailed"] = "Міграція не вдалася",

        // Messages
        ["MessageMigrationComplete"] = "Міграція успішно завершена",
        ["MessagePlanSaved"] = "План успішно збережено",
        ["MessagePlanLoaded"] = "План успішно завантажено",
        ["MessageValidationPassed"] = "Перевірка пройдена успішно",
        ["MessageValidationFailed"] = "Перевірка не пройдена"
    };
}
