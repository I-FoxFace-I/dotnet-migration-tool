# Migration Tool - Specifikace požadavků

> **Stav:** 📝 Draft  
> **Datum:** 2026-01-06  
> **Autor:** AI Assistant + User

---

## 📋 Obsah

1. [Cíle projektu](#cíle-projektu)
2. [Požadavky na funkce](#požadavky-na-funkce)
3. [Technologické možnosti](#technologické-možnosti)
4. [Otázky k upřesnění](#otázky-k-upřesnění)
5. [Rozhodnutí](#rozhodnutí)

---

## 🎯 Cíle projektu

### Primární cíl
Vytvořit interaktivní nástroj pro migraci a reorganizaci .NET projektů (zejména test projektů), který:
- Eliminuje závislost na Visual Studio
- Automatizuje opakující se úkoly (kopírování souborů, úprava namespaces)
- Poskytuje přehledné UI pro plánování a provádění migrací

### Sekundární cíle
- [ ] Znovupoužitelnost pro jiné .NET projekty
- [ ] AI-asistované návrhy kategorizace
- [ ] Integrace s Git pro automatické commity

---

## 🔧 Požadavky na funkce

### 🟢 Must Have (MVP)

| # | Funkce | Popis | Priorita |
|---|--------|-------|----------|
| 1 | **Project Scanner** | Načtení .sln/.csproj struktury, zobrazení stromu projektů | P0 |
| 2 | **Test Discovery** | Nalezení všech testů (`[Fact]`, `[Theory]`, `[Test]`) s metadaty | P0 |
| 3 | **Migration Planner** | Definice cílových projektů a mapování zdrojů → cíle | P0 |
| 4 | **File Migration** | Kopírování/přesun souborů s úpravou namespaces | P0 |
| 5 | **Verification** | Porovnání před/po, report chybějících testů | P0 |

### 🟡 Should Have

| # | Funkce | Popis | Priorita |
|---|--------|-------|----------|
| 6 | **Build Validation** | Spuštění `dotnet build` po migraci | P1 |
| 7 | **Test Runner** | Spuštění `dotnet test` s vizuálním výstupem | P1 |
| 8 | **Solution Generator** | Vytváření nových .sln souborů | P1 |
| 9 | **Git Integration** | Auto-commit po úspěšné migraci | P1 |
| 10 | **Undo/Rollback** | Možnost vrátit migraci | P1 |

### 🔵 Nice to Have

| # | Funkce | Popis | Priorita |
|---|--------|-------|----------|
| 11 | **AI Categorization** | Gemini/Claude návrhy pro kategorizaci testů | P2 |
| 12 | **Batch Operations** | Hromadné operace (rename, move, delete) | P2 |
| 13 | **Project Templates** | Šablony pro nové test projekty | P2 |
| 14 | **NuGet Management** | Správa package references | P2 |
| 15 | **Diff Viewer** | Vizuální porovnání změn | P2 |

---

## 💻 Technologické možnosti

### Varianta A: Python + Streamlit (Doporučeno)

```
┌─────────────────────────────────────────────────┐
│                  Streamlit UI                    │
├─────────────────────────────────────────────────┤
│  Python Core (existující skripty)               │
│  - migrate_files.py                             │
│  - verify_migration.py                          │
│  - create_project.py                            │
├─────────────────────────────────────────────────┤
│  subprocess: dotnet build/test                  │
└─────────────────────────────────────────────────┘
```

| ✅ Výhody | ❌ Nevýhody |
|-----------|-------------|
| Rychlý vývoj (máme hotové skripty) | Omezené UI možnosti |
| Osvědčené (git_report_app) | Pomalejší než nativní |
| Snadná AI integrace | Závislost na Pythonu |
| Žádný JavaScript potřeba | |

**Odhadovaný čas:** 4-6 hodin

---

## ❓ Otázky k upřesnění

### Obecné

1. **Jak často budeš nástroj používat?**
   - [ ] Jednorázově (jen pro tento projekt)
   - [ ] Občas (pár krát ročně)
   - [x] Pravidelně (měsíčně a častěji)

2. **Kdo bude nástroj používat?**
   - [x] Pouze já
   - [x] Tým vývojářů (pravděpodobně pár kamarádů)
   - [ ] Open-source komunita

3. **Jaké je preferované prostředí?**
   - [x] Web browser (localhost)
   - [x] Desktop aplikace
   - [x] CLI s interaktivním módem

### Funkční požadavky

4. **Je potřeba offline režim?**
   - [ ] Ano, musí fungovat bez internetu
   - [x] Ne, internet je vždy k dispozici
   - [x] Pouze pro specifick0 features, základní funkce by měly fungovat offline

5. **Jak důležitá je rychlost?**
   - [ ] Kritická (velké projekty, tisíce souborů)
   - [ ] Důležitá (stovky souborů)
   - [x] Nepodstatná (desítky souborů)

6. **Potřebuješ podporu pro více solution najednou?**
   - [ ] Ano
   - [x] Ne

7. **Jaké typy projektů budeš migrovat?**
   - [ ] Pouze test projekty
   - [x] Jakékoliv .NET projekty
   - [ ] I non-.NET projekty

### AI integrace

8. **Chceš AI asistenci pro:**
   - [ ] Kategorizaci testů (Unit/Integration/etc.)
   - [ ] Návrhy názvů projektů
   - [ ] Detekci problémů (circular dependencies, etc.)
   - [ ] Generování dokumentace
   - [ ] Nic z toho

9. **Preferovaný AI provider:**
   - [ ] Google Gemini (máš API key)
   - [ ] Anthropic Claude
   - [ ] OpenAI GPT
   - [ ] Lokální LLM (Ollama)

### Technické

10. **Máš zkušenosti s těmito technologiemi?**

| Technologie | Úroveň |
|-------------|--------|
| Python | ⭐⭐⭐⭐⭐ |
| C# / .NET | ⭐⭐⭐⭐⭐ |
| JavaScript/TypeScript | ⭐☆☆☆☆ |
| C++ | ⭐⭐⭐☆☆ |
| HTML/CSS | ⭐⭐☆☆☆ |
| SQL | ⭐⭐⭐☆☆ |

11. **Jsi ochotný se naučit novou technologii?**
    - [x] Ano, pokud to přinese významné výhody
    - [x] Raději bych zůstal u známých technologií
    - [ ] Záleží na časové náročnosti

12. **Jak důležitá je údržba do budoucna?**
    - [ ] Velmi (musí být snadno rozšiřitelné)
    - [x] Středně (občasné úpravy)
    - [ ] Málo (jednorázový nástroj)

---

## 🎯 Moje doporučení

### Pro tvůj use-case doporučuji: **Varianta A (Python + Streamlit)**

**Důvody:**

1. **Máme hotovou základnu** - existující skripty (`migrate_files.py`, `verify_migration.py`) fungují
2. **Osvědčený pattern** - `git_report_app` je důkaz, že Streamlit pro tyto účely stačí
3. **Rychlý vývoj** - MVP za 4-6 hodin
4. **Žádné nové jazyky** - nemusíš se učit JS/TS
5. **AI integrace** - můžeme zkopírovat z `git_report_app`
6. **Dostatečné UI** - pro interní nástroj Streamlit bohatě stačí

### Pokud bys chtěl investovat do učení JS/TS:

**Ano, dokázal bys se to naučit rychle**, protože:
- Máš silné základy v C# (syntaxe je podobná)
- TypeScript je "typed JavaScript" = podobný koncept jako C#
- React/Vue mají jasné patterny (komponenty = podobné WPF MVVM)
- Pro tento projekt bys potřeboval jen základy

**Ale:** Pro jednorázový migrační nástroj to není nutné. JS/TS se vyplatí učit pro větší web projekty.

### Hybridní přístup (Python + .NET):

Je možný, ale přidává komplexitu:
- Python volá `dotnet` přes subprocess (to už děláme)
- .NET může volat Python přes `Process.Start`
- Sdílení dat přes JSON/soubory

Pro tento projekt to není potřeba - Python + subprocess `dotnet` stačí.

---

## ✅ Rozhodnutí

> **Vyplň po zodpovězení otázek:**

| Aspekt | Rozhodnutí |
|--------|------------|
| **Technologie** | Python + Streamlit |
| **Prioritní funkce** | Viz požadavky na funkce |
| **AI integrace** | Podpora Gemini/Claude ale není zatím nutné, ještě upřesníme |
| **Časový rámec** | MPV za 4-6 hodin |

---

## 📝 Poznámky

- Chtěl bych aby vznikla aplikace, která mi umožní efektivněji migrovat a refactorovat .NET projety.
- Must have fetures:
  - [ ] Automatické přejmenování namespaces
  - [ ] Náhled na refactoring strukturu před migrací
  - [ ] Možnost undo/rollback jednotlivých kroků
  - [ ] Náhled na aktuální strukturu projektu
  - [ ] Interaktivní UI pro plánování, které umožňuje vybrat které soubory, které adrsáře či které projekty migrovat
  - [ ] Automatické úpravy v souborech na základě změn jiných souborů na kterých tyto soubory závisí
  - [ ] Dependency analýza a zobrazení závislostí mezi soubory a projekty
  - [ ] Error prevention triggerující odpovídající warnings
  - [ ] Analytický pohled, zobrazující podorbnosti o jednotlivých souborech (definice tříd apod.)
  - [ ] Interaktivní UI pro testování 

- Should have features:
  - [ ] Možnost podorobného pohledu na datové typy zobrazující jejich definice, členové, závislosti atd.
  - [ ] Inteligentní správa vazeb mezi soubory a datovými typy s možností změny pomocí UI
  - [ ] Interaktivní podpora pro separaci datového typu do partial class definic
  - [ ] Korekce coding style s možností vybrat které soubory a které části kódu upravit

---

**Další krok:** Vyplň otázky výše a na základě odpovědí upřesníme specifikaci a začneme s implementací.
