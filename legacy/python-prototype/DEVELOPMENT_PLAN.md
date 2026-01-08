# Migration Tool - Development Plan & Rules

> **Stav:** 🚧 In Progress  
> **Datum:** 2026-01-06  
> **Verze:** 0.1.0 (MVP)

---

## 📋 Pravidla vývoje

### 1. Modulární architektura
- Každá funkčnost v samostatném modulu
- Moduly musí být testovatelné nezávisle
- Žádné cyklické závislosti

### 2. Testování
- **Před použitím modulu v UI** musí být otestován
- Testy v `tests/` složce
- Minimálně smoke test pro každý modul

### 3. Inkrementální vývoj
- Malé commity s jasným popisem
- Každá fáze musí být funkční
- Netestovat v produkci

### 4. Kódovací standardy
- Docstringy pro všechny veřejné funkce
- Type hints všude
- Logování místo print()

---

## 🏗️ Architektura

```
scripts/migration_tool/
├── app.py                      # Main Streamlit entry point
├── requirements.txt            # Dependencies
├── README.md                   # User documentation
├── DEVELOPMENT_PLAN.md         # This file
├── SPECIFICATION.md            # Requirements
├── UI_DESIGN.md                # UI wireframes
│
├── core/                       # Core business logic
│   ├── __init__.py
│   ├── solution_parser.py      # .sln file parsing
│   ├── project_parser.py       # .csproj file parsing
│   ├── file_scanner.py         # C# file scanning
│   ├── namespace_fixer.py      # Namespace modifications
│   ├── migration_engine.py     # Migration orchestration
│   └── git_manager.py          # Git operations
│
├── models/                     # Data models
│   ├── __init__.py
│   ├── solution.py             # Solution, Project models
│   ├── file_info.py            # FileInfo, ClassInfo models
│   └── migration_plan.py       # MigrationPlan, MigrationStep
│
├── ui/                         # Streamlit UI components
│   ├── __init__.py
│   ├── sidebar.py              # Sidebar configuration
│   ├── dashboard.py            # Dashboard view
│   ├── project_explorer.py     # Project tree view
│   ├── migration_planner.py    # Migration planning UI
│   ├── execution.py            # Execution progress UI
│   └── components.py           # Reusable UI components
│
├── utils/                      # Utilities
│   ├── __init__.py
│   ├── logging_config.py       # Logging setup
│   └── file_utils.py           # File operations
│
└── tests/                      # Test suite
    ├── __init__.py
    ├── test_solution_parser.py
    ├── test_project_parser.py
    ├── test_file_scanner.py
    ├── test_namespace_fixer.py
    ├── test_migration_engine.py
    └── run_tests.py            # Test runner
```

---

## 📅 Fáze vývoje

### Fáze 1: Core Infrastructure (Aktuální)
**Cíl:** Základní parsování a datové modely

| # | Úkol | Stav | Poznámka |
|---|------|------|----------|
| 1.1 | Vytvořit strukturu složek | ⏳ | |
| 1.2 | Data models (Solution, Project, File) | ⏳ | |
| 1.3 | Solution parser (.sln) | ⏳ | |
| 1.4 | Project parser (.csproj) | ⏳ | |
| 1.5 | File scanner (C# files) | ⏳ | |
| 1.6 | Testy pro core moduly | ⏳ | |

### Fáze 2: Basic UI
**Cíl:** Zobrazení struktury projektu

| # | Úkol | Stav | Poznámka |
|---|------|------|----------|
| 2.1 | Streamlit app skeleton | ⏳ | |
| 2.2 | Sidebar s konfigurací | ⏳ | |
| 2.3 | Dashboard s quick stats | ⏳ | |
| 2.4 | Project Explorer tree view | ⏳ | |

### Fáze 3: Migration Planning
**Cíl:** Plánování migrace souborů

| # | Úkol | Stav | Poznámka |
|---|------|------|----------|
| 3.1 | Migration plan model | ⏳ | |
| 3.2 | Source/Target side-by-side view | ⏳ | |
| 3.3 | File selection & move | ⏳ | |
| 3.4 | Namespace mapping | ⏳ | |

### Fáze 4: Migration Execution
**Cíl:** Provádění migrace

| # | Úkol | Stav | Poznámka |
|---|------|------|----------|
| 4.1 | Namespace fixer | ⏳ | |
| 4.2 | File copy/move operations | ⏳ | |
| 4.3 | Git integration | ⏳ | |
| 4.4 | Progress UI | ⏳ | |
| 4.5 | Verification report | ⏳ | |

### Fáze 5: Polish & Extensions
**Cíl:** Vylepšení a rozšíření

| # | Úkol | Stav | Poznámka |
|---|------|------|----------|
| 5.1 | Test runner integration | ⏳ | |
| 5.2 | Dependency visualization | ⏳ | |
| 5.3 | AI integration (optional) | ⏳ | |
| 5.4 | Save/Load migration plans | ⏳ | |

---

## 🔧 Technické detaily

### Dependencies (requirements.txt)
```
streamlit>=1.28.0
pathlib
dataclasses
typing
```

### Streamlit konfigurace
- Port: 8502 (aby nekolidoval s git_report_app na 8501)
- Theme: Dark (konzistentní s git_report_app)

### Logging
- Level: INFO (DEBUG pro vývoj)
- Output: Console + file (migration_tool.log)

---

## ✅ Definition of Done

Modul je hotový když:
1. ✅ Má docstringy a type hints
2. ✅ Má alespoň smoke test
3. ✅ Test prochází
4. ✅ Je zdokumentován v README (pokud je veřejný)
5. ✅ Nemá hardcoded cesty

---

## 📝 Poznámky k implementaci

### Solution Parser
- .sln soubory mají specifický formát (ne XML)
- Regex pro extrakci projektů
- Reference: https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file

### Project Parser
- .csproj je XML
- Zajímavé elementy: `<ItemGroup>`, `<ProjectReference>`, `<PackageReference>`

### C# File Scanner
- Regex pro `namespace`, `class`, `interface`, `[Fact]`, `[Theory]`
- Pozor na nested classes a partial classes

---

## 🚀 Spuštění

```bash
cd scripts/migration_tool
pip install -r requirements.txt
python -m streamlit run app.py --server.port 8502
```

---

**Aktuální fáze:** 1 - Core Infrastructure  
**Další krok:** Vytvořit strukturu složek a data modely
