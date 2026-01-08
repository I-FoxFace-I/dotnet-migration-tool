# MAUI UI Tests

Automatizované UI testy pro MigrationTool.Maui aplikaci pomocí **Appium** a **WinAppDriver**.

## Požadavky

### 1. WinAppDriver

Stáhněte a nainstalujte WinAppDriver z:
https://github.com/microsoft/WinAppDriver/releases

### 2. Developer Mode

Zapněte Developer Mode ve Windows:
- Settings → Update & Security → For developers → Developer mode

### 3. Spuštění WinAppDriver

Před spuštěním testů spusťte WinAppDriver jako administrátor:

```powershell
# Spustit jako administrátor
& "C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"
```

## Spuštění testů

### Build aplikace

```powershell
cd tools/src/MigrationTool/MigrationTool.Maui
dotnet build -c Debug
```

### Spuštění UI testů

```powershell
cd tools/tests/MigrationTool/MigrationTool.Maui.UITests
dotnet test --filter "Category=UI"
```

### Spuštění konkrétní kategorie

```powershell
# Pouze navigační testy
dotnet test --filter "Category=Navigation"

# Pouze Settings testy
dotnet test --filter "Category=Settings"

# Pouze Planner testy
dotnet test --filter "Category=Planner"
```

## Struktura testů

```
MigrationTool.Maui.UITests/
├── Infrastructure/
│   ├── AppiumSetup.cs       # Konfigurace Appium/WinAppDriver
│   └── UITestBase.cs        # Základní třída pro UI testy
├── Tests/
│   ├── ApplicationStartupTests.cs  # Testy spuštění aplikace
│   ├── NavigationTests.cs          # Testy navigace mezi stránkami
│   ├── SettingsPageTests.cs        # Testy Settings stránky
│   ├── PlannerPageTests.cs         # Testy Planner stránky
│   └── ExplorerPageTests.cs        # Testy Explorer stránky
└── UITestCollection.cs      # xUnit collection pro sekvenční běh
```

## Poznámky

- Testy běží sekvenčně (jedna instance aplikace)
- Pokud WinAppDriver neběží, testy jsou přeskočeny
- Testy používají AutomationId pro hledání elementů
- Pro přidání nových testů použijte `UITestBase` jako základ

## Troubleshooting

### WinAppDriver se nespustí
- Zkontrolujte, že máte Developer Mode zapnutý
- Spusťte jako administrátor

### Testy nenajdou elementy
- Zkontrolujte AutomationId v XAML
- Použijte Inspect.exe pro zjištění správných názvů elementů

### Aplikace se nespustí
- Zkontrolujte cestu k exe v `AppiumSetup.Config.AppPath`
- Ujistěte se, že aplikace je zkompilovaná
