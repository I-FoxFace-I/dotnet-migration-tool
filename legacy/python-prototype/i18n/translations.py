"""
Translation strings for Migration Tool.
Supports: English (en), Czech (cs), Polish (pl), Ukrainian (uk)
"""

TRANSLATIONS = {
    # =========================================================================
    # ENGLISH (Default)
    # =========================================================================
    "en": {
        # App header
        "app_title": "🔄 .NET Migration Tool",
        "app_subtitle": "Interactive tool for reorganizing .NET projects and solutions",
        
        # Sidebar - Workspace
        "sidebar_workspace": "📁 Workspace",
        "workspace_path": "Workspace path",
        "workspace_path_help": "Root folder containing .sln files",
        "select_solution": "Select solution",
        "no_solutions_found": "No .sln files found",
        "load_solution": "🔍 Load",
        "reload_solution": "🔄 Reload",
        
        # Sidebar - Navigation
        "sidebar_navigation": "🧭 Navigation",
        "nav_dashboard": "📊 Dashboard",
        "nav_explorer": "📂 Project Explorer",
        "nav_planner": "📋 Migration Planner",
        "nav_settings": "⚙️ Settings",
        
        # Sidebar - Language
        "ui_language": "🌐 Language",
        
        # Dashboard
        "dashboard_title": "📊 Solution Overview",
        "dashboard_no_solution": "No solution loaded. Select a solution in the sidebar.",
        "metric_projects": "📦 Projects",
        "metric_test_projects": "🧪 Test Projects",
        "metric_source_projects": "📝 Source Projects",
        "metric_files": "📄 Files",
        "metric_classes": "🏗️ Classes",
        "metric_tests": "✅ Tests",
        
        # Dashboard - Project types
        "project_types": "📊 Project Types",
        "type_library": "Class Library",
        "type_console": "Console App",
        "type_wpf": "WPF App",
        "type_test": "Test Project",
        "type_web": "Web App",
        "type_other": "Other",
        
        # Dashboard - Dependencies
        "dependencies_title": "🔗 Dependencies",
        "project_references": "Project References",
        "package_references": "Package References",
        
        # Project Explorer
        "explorer_title": "📂 Project Explorer",
        "explorer_no_solution": "Load a solution to explore projects.",
        "explorer_filter": "🔍 Filter projects",
        "explorer_show_tests": "Show test projects",
        "explorer_show_source": "Show source projects",
        "explorer_expand_all": "Expand All",
        "explorer_collapse_all": "Collapse All",
        
        # Project Explorer - Project details
        "project_details": "📋 Project Details",
        "project_name": "Name",
        "project_path": "Path",
        "project_framework": "Framework",
        "project_type": "Type",
        "project_namespace": "Root Namespace",
        "project_output": "Output Type",
        
        # Project Explorer - Files
        "files_title": "📄 Files",
        "files_count": "{count} files",
        "classes_count": "{count} classes",
        "tests_count": "{count} tests",
        "no_files": "No files found",
        
        # Project Explorer - Classes
        "classes_title": "🏗️ Classes & Interfaces",
        "class_type_class": "class",
        "class_type_interface": "interface",
        "class_type_enum": "enum",
        "class_type_struct": "struct",
        "class_type_record": "record",
        
        # Migration Planner
        "planner_title": "📋 Migration Planner",
        "planner_no_solution": "Load a solution to plan migrations.",
        "planner_source": "📤 Source",
        "planner_target": "📥 Target",
        "planner_actions": "⚡ Actions",
        
        # Migration Planner - Actions
        "action_move_file": "Move File",
        "action_move_folder": "Move Folder",
        "action_create_project": "Create Project",
        "action_delete_project": "Delete Project",
        "action_rename_namespace": "Rename Namespace",
        "action_add_reference": "Add Reference",
        "action_remove_reference": "Remove Reference",
        
        # Migration Planner - Plan
        "plan_title": "📝 Migration Plan",
        "plan_empty": "No migration steps defined.",
        "plan_add_step": "➕ Add Step",
        "plan_clear": "🗑️ Clear Plan",
        "plan_execute": "▶️ Execute Plan",
        "plan_export": "💾 Export Plan",
        "plan_import": "📂 Import Plan",
        
        # Migration Planner - Step details
        "step_number": "Step {number}",
        "step_action": "Action",
        "step_source": "Source",
        "step_target": "Target",
        "step_status": "Status",
        "step_remove": "Remove",
        "step_move_up": "Move Up",
        "step_move_down": "Move Down",
        
        # Migration Planner - Status
        "status_pending": "⏳ Pending",
        "status_in_progress": "🔄 In Progress",
        "status_completed": "✅ Completed",
        "status_failed": "❌ Failed",
        "status_skipped": "⏭️ Skipped",
        
        # Migration Planner - Execution
        "execute_title": "▶️ Execute Migration",
        "execute_confirm": "Are you sure you want to execute the migration plan?",
        "execute_warning": "⚠️ This will modify files on disk. Make sure you have a backup or Git commit.",
        "execute_start": "Start Migration",
        "execute_cancel": "Cancel",
        "execute_progress": "Executing step {current} of {total}...",
        "execute_success": "✅ Migration completed successfully!",
        "execute_failure": "❌ Migration failed at step {step}: {error}",
        
        # Settings
        "settings_title": "⚙️ Settings",
        "settings_general": "General",
        "settings_appearance": "Appearance",
        "settings_git": "Git Integration",
        
        # Settings - General
        "setting_auto_save": "Auto-save migration plan",
        "setting_confirm_actions": "Confirm destructive actions",
        "setting_backup_files": "Create backup before changes",
        
        # Settings - Git
        "setting_git_enabled": "Enable Git integration",
        "setting_git_auto_commit": "Auto-commit after migration",
        "setting_git_commit_message": "Default commit message",
        
        # Common
        "loading": "Loading...",
        "error": "Error",
        "warning": "Warning",
        "success": "Success",
        "info": "Info",
        "confirm": "Confirm",
        "cancel": "Cancel",
        "save": "Save",
        "delete": "Delete",
        "edit": "Edit",
        "close": "Close",
        "refresh": "Refresh",
        "search": "Search",
        "filter": "Filter",
        "clear": "Clear",
        "select_all": "Select All",
        "deselect_all": "Deselect All",
        "yes": "Yes",
        "no": "No",
        
        # Errors
        "error_loading_solution": "Failed to load solution: {error}",
        "error_loading_project": "Failed to load project: {error}",
        "error_scanning_files": "Failed to scan files: {error}",
        "error_executing_step": "Failed to execute step: {error}",
        "error_invalid_path": "Invalid path: {path}",
        "error_file_not_found": "File not found: {path}",
        "error_permission_denied": "Permission denied: {path}",
    },
    
    # =========================================================================
    # CZECH (Čeština)
    # =========================================================================
    "cs": {
        # App header
        "app_title": "🔄 .NET Migrační nástroj",
        "app_subtitle": "Interaktivní nástroj pro reorganizaci .NET projektů a řešení",
        
        # Sidebar - Workspace
        "sidebar_workspace": "📁 Pracovní prostor",
        "workspace_path": "Cesta k pracovnímu prostoru",
        "workspace_path_help": "Kořenová složka obsahující .sln soubory",
        "select_solution": "Vyberte řešení",
        "no_solutions_found": "Nebyly nalezeny žádné .sln soubory",
        "load_solution": "🔍 Načíst",
        "reload_solution": "🔄 Obnovit",
        
        # Sidebar - Navigation
        "sidebar_navigation": "🧭 Navigace",
        "nav_dashboard": "📊 Přehled",
        "nav_explorer": "📂 Průzkumník projektů",
        "nav_planner": "📋 Plánovač migrace",
        "nav_settings": "⚙️ Nastavení",
        
        # Sidebar - Language
        "ui_language": "🌐 Jazyk",
        
        # Dashboard
        "dashboard_title": "📊 Přehled řešení",
        "dashboard_no_solution": "Žádné řešení není načteno. Vyberte řešení v postranním panelu.",
        "metric_projects": "📦 Projekty",
        "metric_test_projects": "🧪 Testovací projekty",
        "metric_source_projects": "📝 Zdrojové projekty",
        "metric_files": "📄 Soubory",
        "metric_classes": "🏗️ Třídy",
        "metric_tests": "✅ Testy",
        
        # Dashboard - Project types
        "project_types": "📊 Typy projektů",
        "type_library": "Knihovna tříd",
        "type_console": "Konzolová aplikace",
        "type_wpf": "WPF aplikace",
        "type_test": "Testovací projekt",
        "type_web": "Webová aplikace",
        "type_other": "Ostatní",
        
        # Dashboard - Dependencies
        "dependencies_title": "🔗 Závislosti",
        "project_references": "Reference na projekty",
        "package_references": "Reference na balíčky",
        
        # Project Explorer
        "explorer_title": "📂 Průzkumník projektů",
        "explorer_no_solution": "Načtěte řešení pro procházení projektů.",
        "explorer_filter": "🔍 Filtrovat projekty",
        "explorer_show_tests": "Zobrazit testovací projekty",
        "explorer_show_source": "Zobrazit zdrojové projekty",
        "explorer_expand_all": "Rozbalit vše",
        "explorer_collapse_all": "Sbalit vše",
        
        # Project Explorer - Project details
        "project_details": "📋 Detaily projektu",
        "project_name": "Název",
        "project_path": "Cesta",
        "project_framework": "Framework",
        "project_type": "Typ",
        "project_namespace": "Kořenový namespace",
        "project_output": "Typ výstupu",
        
        # Project Explorer - Files
        "files_title": "📄 Soubory",
        "files_count": "{count} souborů",
        "classes_count": "{count} tříd",
        "tests_count": "{count} testů",
        "no_files": "Žádné soubory nenalezeny",
        
        # Project Explorer - Classes
        "classes_title": "🏗️ Třídy a rozhraní",
        "class_type_class": "třída",
        "class_type_interface": "rozhraní",
        "class_type_enum": "výčet",
        "class_type_struct": "struktura",
        "class_type_record": "záznam",
        
        # Migration Planner
        "planner_title": "📋 Plánovač migrace",
        "planner_no_solution": "Načtěte řešení pro plánování migrací.",
        "planner_source": "📤 Zdroj",
        "planner_target": "📥 Cíl",
        "planner_actions": "⚡ Akce",
        
        # Migration Planner - Actions
        "action_move_file": "Přesunout soubor",
        "action_move_folder": "Přesunout složku",
        "action_create_project": "Vytvořit projekt",
        "action_delete_project": "Smazat projekt",
        "action_rename_namespace": "Přejmenovat namespace",
        "action_add_reference": "Přidat referenci",
        "action_remove_reference": "Odebrat referenci",
        
        # Migration Planner - Plan
        "plan_title": "📝 Plán migrace",
        "plan_empty": "Žádné kroky migrace nejsou definovány.",
        "plan_add_step": "➕ Přidat krok",
        "plan_clear": "🗑️ Vymazat plán",
        "plan_execute": "▶️ Spustit plán",
        "plan_export": "💾 Exportovat plán",
        "plan_import": "📂 Importovat plán",
        
        # Migration Planner - Step details
        "step_number": "Krok {number}",
        "step_action": "Akce",
        "step_source": "Zdroj",
        "step_target": "Cíl",
        "step_status": "Stav",
        "step_remove": "Odebrat",
        "step_move_up": "Posunout nahoru",
        "step_move_down": "Posunout dolů",
        
        # Migration Planner - Status
        "status_pending": "⏳ Čeká",
        "status_in_progress": "🔄 Probíhá",
        "status_completed": "✅ Dokončeno",
        "status_failed": "❌ Selhalo",
        "status_skipped": "⏭️ Přeskočeno",
        
        # Migration Planner - Execution
        "execute_title": "▶️ Spustit migraci",
        "execute_confirm": "Opravdu chcete spustit plán migrace?",
        "execute_warning": "⚠️ Toto upraví soubory na disku. Ujistěte se, že máte zálohu nebo Git commit.",
        "execute_start": "Spustit migraci",
        "execute_cancel": "Zrušit",
        "execute_progress": "Provádím krok {current} z {total}...",
        "execute_success": "✅ Migrace byla úspěšně dokončena!",
        "execute_failure": "❌ Migrace selhala v kroku {step}: {error}",
        
        # Settings
        "settings_title": "⚙️ Nastavení",
        "settings_general": "Obecné",
        "settings_appearance": "Vzhled",
        "settings_git": "Git integrace",
        
        # Settings - General
        "setting_auto_save": "Automaticky ukládat plán migrace",
        "setting_confirm_actions": "Potvrzovat destruktivní akce",
        "setting_backup_files": "Vytvořit zálohu před změnami",
        
        # Settings - Git
        "setting_git_enabled": "Povolit Git integraci",
        "setting_git_auto_commit": "Automaticky commitovat po migraci",
        "setting_git_commit_message": "Výchozí zpráva commitu",
        
        # Common
        "loading": "Načítání...",
        "error": "Chyba",
        "warning": "Varování",
        "success": "Úspěch",
        "info": "Informace",
        "confirm": "Potvrdit",
        "cancel": "Zrušit",
        "save": "Uložit",
        "delete": "Smazat",
        "edit": "Upravit",
        "close": "Zavřít",
        "refresh": "Obnovit",
        "search": "Hledat",
        "filter": "Filtrovat",
        "clear": "Vymazat",
        "select_all": "Vybrat vše",
        "deselect_all": "Zrušit výběr",
        "yes": "Ano",
        "no": "Ne",
        
        # Errors
        "error_loading_solution": "Nepodařilo se načíst řešení: {error}",
        "error_loading_project": "Nepodařilo se načíst projekt: {error}",
        "error_scanning_files": "Nepodařilo se prohledat soubory: {error}",
        "error_executing_step": "Nepodařilo se provést krok: {error}",
        "error_invalid_path": "Neplatná cesta: {path}",
        "error_file_not_found": "Soubor nenalezen: {path}",
        "error_permission_denied": "Přístup odepřen: {path}",
    },
    
    # =========================================================================
    # POLISH (Polski)
    # =========================================================================
    "pl": {
        # App header
        "app_title": "🔄 Narzędzie migracji .NET",
        "app_subtitle": "Interaktywne narzędzie do reorganizacji projektów i rozwiązań .NET",
        
        # Sidebar - Workspace
        "sidebar_workspace": "📁 Przestrzeń robocza",
        "workspace_path": "Ścieżka przestrzeni roboczej",
        "workspace_path_help": "Folder główny zawierający pliki .sln",
        "select_solution": "Wybierz rozwiązanie",
        "no_solutions_found": "Nie znaleziono plików .sln",
        "load_solution": "🔍 Załaduj",
        "reload_solution": "🔄 Odśwież",
        
        # Sidebar - Navigation
        "sidebar_navigation": "🧭 Nawigacja",
        "nav_dashboard": "📊 Panel główny",
        "nav_explorer": "📂 Eksplorator projektów",
        "nav_planner": "📋 Planowanie migracji",
        "nav_settings": "⚙️ Ustawienia",
        
        # Sidebar - Language
        "ui_language": "🌐 Język",
        
        # Dashboard
        "dashboard_title": "📊 Przegląd rozwiązania",
        "dashboard_no_solution": "Nie załadowano rozwiązania. Wybierz rozwiązanie w panelu bocznym.",
        "metric_projects": "📦 Projekty",
        "metric_test_projects": "🧪 Projekty testowe",
        "metric_source_projects": "📝 Projekty źródłowe",
        "metric_files": "📄 Pliki",
        "metric_classes": "🏗️ Klasy",
        "metric_tests": "✅ Testy",
        
        # Dashboard - Project types
        "project_types": "📊 Typy projektów",
        "type_library": "Biblioteka klas",
        "type_console": "Aplikacja konsolowa",
        "type_wpf": "Aplikacja WPF",
        "type_test": "Projekt testowy",
        "type_web": "Aplikacja webowa",
        "type_other": "Inne",
        
        # Dashboard - Dependencies
        "dependencies_title": "🔗 Zależności",
        "project_references": "Referencje projektów",
        "package_references": "Referencje pakietów",
        
        # Project Explorer
        "explorer_title": "📂 Eksplorator projektów",
        "explorer_no_solution": "Załaduj rozwiązanie, aby przeglądać projekty.",
        "explorer_filter": "🔍 Filtruj projekty",
        "explorer_show_tests": "Pokaż projekty testowe",
        "explorer_show_source": "Pokaż projekty źródłowe",
        "explorer_expand_all": "Rozwiń wszystko",
        "explorer_collapse_all": "Zwiń wszystko",
        
        # Project Explorer - Project details
        "project_details": "📋 Szczegóły projektu",
        "project_name": "Nazwa",
        "project_path": "Ścieżka",
        "project_framework": "Framework",
        "project_type": "Typ",
        "project_namespace": "Główna przestrzeń nazw",
        "project_output": "Typ wyjścia",
        
        # Project Explorer - Files
        "files_title": "📄 Pliki",
        "files_count": "{count} plików",
        "classes_count": "{count} klas",
        "tests_count": "{count} testów",
        "no_files": "Nie znaleziono plików",
        
        # Project Explorer - Classes
        "classes_title": "🏗️ Klasy i interfejsy",
        "class_type_class": "klasa",
        "class_type_interface": "interfejs",
        "class_type_enum": "wyliczenie",
        "class_type_struct": "struktura",
        "class_type_record": "rekord",
        
        # Migration Planner
        "planner_title": "📋 Planowanie migracji",
        "planner_no_solution": "Załaduj rozwiązanie, aby zaplanować migracje.",
        "planner_source": "📤 Źródło",
        "planner_target": "📥 Cel",
        "planner_actions": "⚡ Akcje",
        
        # Migration Planner - Actions
        "action_move_file": "Przenieś plik",
        "action_move_folder": "Przenieś folder",
        "action_create_project": "Utwórz projekt",
        "action_delete_project": "Usuń projekt",
        "action_rename_namespace": "Zmień przestrzeń nazw",
        "action_add_reference": "Dodaj referencję",
        "action_remove_reference": "Usuń referencję",
        
        # Migration Planner - Plan
        "plan_title": "📝 Plan migracji",
        "plan_empty": "Nie zdefiniowano kroków migracji.",
        "plan_add_step": "➕ Dodaj krok",
        "plan_clear": "🗑️ Wyczyść plan",
        "plan_execute": "▶️ Wykonaj plan",
        "plan_export": "💾 Eksportuj plan",
        "plan_import": "📂 Importuj plan",
        
        # Migration Planner - Step details
        "step_number": "Krok {number}",
        "step_action": "Akcja",
        "step_source": "Źródło",
        "step_target": "Cel",
        "step_status": "Status",
        "step_remove": "Usuń",
        "step_move_up": "Przesuń w górę",
        "step_move_down": "Przesuń w dół",
        
        # Migration Planner - Status
        "status_pending": "⏳ Oczekuje",
        "status_in_progress": "🔄 W toku",
        "status_completed": "✅ Ukończono",
        "status_failed": "❌ Niepowodzenie",
        "status_skipped": "⏭️ Pominięto",
        
        # Migration Planner - Execution
        "execute_title": "▶️ Wykonaj migrację",
        "execute_confirm": "Czy na pewno chcesz wykonać plan migracji?",
        "execute_warning": "⚠️ To zmodyfikuje pliki na dysku. Upewnij się, że masz kopię zapasową lub commit Git.",
        "execute_start": "Rozpocznij migrację",
        "execute_cancel": "Anuluj",
        "execute_progress": "Wykonuję krok {current} z {total}...",
        "execute_success": "✅ Migracja zakończona pomyślnie!",
        "execute_failure": "❌ Migracja nie powiodła się w kroku {step}: {error}",
        
        # Settings
        "settings_title": "⚙️ Ustawienia",
        "settings_general": "Ogólne",
        "settings_appearance": "Wygląd",
        "settings_git": "Integracja Git",
        
        # Settings - General
        "setting_auto_save": "Automatycznie zapisuj plan migracji",
        "setting_confirm_actions": "Potwierdzaj destrukcyjne akcje",
        "setting_backup_files": "Utwórz kopię zapasową przed zmianami",
        
        # Settings - Git
        "setting_git_enabled": "Włącz integrację Git",
        "setting_git_auto_commit": "Automatyczny commit po migracji",
        "setting_git_commit_message": "Domyślna wiadomość commita",
        
        # Common
        "loading": "Ładowanie...",
        "error": "Błąd",
        "warning": "Ostrzeżenie",
        "success": "Sukces",
        "info": "Informacja",
        "confirm": "Potwierdź",
        "cancel": "Anuluj",
        "save": "Zapisz",
        "delete": "Usuń",
        "edit": "Edytuj",
        "close": "Zamknij",
        "refresh": "Odśwież",
        "search": "Szukaj",
        "filter": "Filtruj",
        "clear": "Wyczyść",
        "select_all": "Zaznacz wszystko",
        "deselect_all": "Odznacz wszystko",
        "yes": "Tak",
        "no": "Nie",
        
        # Errors
        "error_loading_solution": "Nie udało się załadować rozwiązania: {error}",
        "error_loading_project": "Nie udało się załadować projektu: {error}",
        "error_scanning_files": "Nie udało się przeskanować plików: {error}",
        "error_executing_step": "Nie udało się wykonać kroku: {error}",
        "error_invalid_path": "Nieprawidłowa ścieżka: {path}",
        "error_file_not_found": "Nie znaleziono pliku: {path}",
        "error_permission_denied": "Odmowa dostępu: {path}",
    },
    
    # =========================================================================
    # UKRAINIAN (Українська)
    # =========================================================================
    "uk": {
        # App header
        "app_title": "🔄 Інструмент міграції .NET",
        "app_subtitle": "Інтерактивний інструмент для реорганізації проектів та рішень .NET",
        
        # Sidebar - Workspace
        "sidebar_workspace": "📁 Робочий простір",
        "workspace_path": "Шлях робочого простору",
        "workspace_path_help": "Коренева папка, що містить файли .sln",
        "select_solution": "Виберіть рішення",
        "no_solutions_found": "Файли .sln не знайдено",
        "load_solution": "🔍 Завантажити",
        "reload_solution": "🔄 Оновити",
        
        # Sidebar - Navigation
        "sidebar_navigation": "🧭 Навігація",
        "nav_dashboard": "📊 Панель керування",
        "nav_explorer": "📂 Провідник проектів",
        "nav_planner": "📋 Планувальник міграції",
        "nav_settings": "⚙️ Налаштування",
        
        # Sidebar - Language
        "ui_language": "🌐 Мова",
        
        # Dashboard
        "dashboard_title": "📊 Огляд рішення",
        "dashboard_no_solution": "Рішення не завантажено. Виберіть рішення на бічній панелі.",
        "metric_projects": "📦 Проекти",
        "metric_test_projects": "🧪 Тестові проекти",
        "metric_source_projects": "📝 Вихідні проекти",
        "metric_files": "📄 Файли",
        "metric_classes": "🏗️ Класи",
        "metric_tests": "✅ Тести",
        
        # Dashboard - Project types
        "project_types": "📊 Типи проектів",
        "type_library": "Бібліотека класів",
        "type_console": "Консольний додаток",
        "type_wpf": "WPF додаток",
        "type_test": "Тестовий проект",
        "type_web": "Веб-додаток",
        "type_other": "Інше",
        
        # Dashboard - Dependencies
        "dependencies_title": "🔗 Залежності",
        "project_references": "Посилання на проекти",
        "package_references": "Посилання на пакети",
        
        # Project Explorer
        "explorer_title": "📂 Провідник проектів",
        "explorer_no_solution": "Завантажте рішення для перегляду проектів.",
        "explorer_filter": "🔍 Фільтрувати проекти",
        "explorer_show_tests": "Показати тестові проекти",
        "explorer_show_source": "Показати вихідні проекти",
        "explorer_expand_all": "Розгорнути все",
        "explorer_collapse_all": "Згорнути все",
        
        # Project Explorer - Project details
        "project_details": "📋 Деталі проекту",
        "project_name": "Назва",
        "project_path": "Шлях",
        "project_framework": "Фреймворк",
        "project_type": "Тип",
        "project_namespace": "Кореневий простір імен",
        "project_output": "Тип виводу",
        
        # Project Explorer - Files
        "files_title": "📄 Файли",
        "files_count": "{count} файлів",
        "classes_count": "{count} класів",
        "tests_count": "{count} тестів",
        "no_files": "Файли не знайдено",
        
        # Project Explorer - Classes
        "classes_title": "🏗️ Класи та інтерфейси",
        "class_type_class": "клас",
        "class_type_interface": "інтерфейс",
        "class_type_enum": "перелічення",
        "class_type_struct": "структура",
        "class_type_record": "запис",
        
        # Migration Planner
        "planner_title": "📋 Планувальник міграції",
        "planner_no_solution": "Завантажте рішення для планування міграцій.",
        "planner_source": "📤 Джерело",
        "planner_target": "📥 Ціль",
        "planner_actions": "⚡ Дії",
        
        # Migration Planner - Actions
        "action_move_file": "Перемістити файл",
        "action_move_folder": "Перемістити папку",
        "action_create_project": "Створити проект",
        "action_delete_project": "Видалити проект",
        "action_rename_namespace": "Перейменувати простір імен",
        "action_add_reference": "Додати посилання",
        "action_remove_reference": "Видалити посилання",
        
        # Migration Planner - Plan
        "plan_title": "📝 План міграції",
        "plan_empty": "Кроки міграції не визначено.",
        "plan_add_step": "➕ Додати крок",
        "plan_clear": "🗑️ Очистити план",
        "plan_execute": "▶️ Виконати план",
        "plan_export": "💾 Експортувати план",
        "plan_import": "📂 Імпортувати план",
        
        # Migration Planner - Step details
        "step_number": "Крок {number}",
        "step_action": "Дія",
        "step_source": "Джерело",
        "step_target": "Ціль",
        "step_status": "Статус",
        "step_remove": "Видалити",
        "step_move_up": "Перемістити вгору",
        "step_move_down": "Перемістити вниз",
        
        # Migration Planner - Status
        "status_pending": "⏳ Очікує",
        "status_in_progress": "🔄 Виконується",
        "status_completed": "✅ Завершено",
        "status_failed": "❌ Помилка",
        "status_skipped": "⏭️ Пропущено",
        
        # Migration Planner - Execution
        "execute_title": "▶️ Виконати міграцію",
        "execute_confirm": "Ви впевнені, що хочете виконати план міграції?",
        "execute_warning": "⚠️ Це змінить файли на диску. Переконайтеся, що у вас є резервна копія або Git commit.",
        "execute_start": "Почати міграцію",
        "execute_cancel": "Скасувати",
        "execute_progress": "Виконую крок {current} з {total}...",
        "execute_success": "✅ Міграцію успішно завершено!",
        "execute_failure": "❌ Міграція не вдалася на кроці {step}: {error}",
        
        # Settings
        "settings_title": "⚙️ Налаштування",
        "settings_general": "Загальні",
        "settings_appearance": "Зовнішній вигляд",
        "settings_git": "Інтеграція Git",
        
        # Settings - General
        "setting_auto_save": "Автозбереження плану міграції",
        "setting_confirm_actions": "Підтверджувати руйнівні дії",
        "setting_backup_files": "Створювати резервну копію перед змінами",
        
        # Settings - Git
        "setting_git_enabled": "Увімкнути інтеграцію Git",
        "setting_git_auto_commit": "Автоматичний commit після міграції",
        "setting_git_commit_message": "Повідомлення commit за замовчуванням",
        
        # Common
        "loading": "Завантаження...",
        "error": "Помилка",
        "warning": "Попередження",
        "success": "Успіх",
        "info": "Інформація",
        "confirm": "Підтвердити",
        "cancel": "Скасувати",
        "save": "Зберегти",
        "delete": "Видалити",
        "edit": "Редагувати",
        "close": "Закрити",
        "refresh": "Оновити",
        "search": "Пошук",
        "filter": "Фільтр",
        "clear": "Очистити",
        "select_all": "Вибрати все",
        "deselect_all": "Скасувати вибір",
        "yes": "Так",
        "no": "Ні",
        
        # Errors
        "error_loading_solution": "Не вдалося завантажити рішення: {error}",
        "error_loading_project": "Не вдалося завантажити проект: {error}",
        "error_scanning_files": "Не вдалося просканувати файли: {error}",
        "error_executing_step": "Не вдалося виконати крок: {error}",
        "error_invalid_path": "Недійсний шлях: {path}",
        "error_file_not_found": "Файл не знайдено: {path}",
        "error_permission_denied": "Доступ заборонено: {path}",
    },
}
