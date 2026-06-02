# Electoral Lists Statistics — ASP.NET Core 8

A trilingual (Darija / Français / English) web app that ingests Moroccan
electoral list Excel exports (LEG), persists them to SQLite, and renders
live statistics per day, per commune, per circumscription, per type
(inscription / radiation / modification), gender and age bucket.

## 1. Tech stack

| Layer | Choice |
|------|--------|
| Runtime | .NET 8 (ASP.NET Core MVC) |
| DB | SQLite (file `electoral.db`) via EF Core 8 |
| Excel parsing | ClosedXML 0.102 |
| Real-time | SignalR (`/hubs/stats`) |
| Charts | Chart.js 4 (CDN) |
| UI | Bootstrap 5.3 + Bootstrap RTL |
| i18n | `Microsoft.AspNetCore.Localization` with `.resx` (ar-MA, fr-FR, en-US) |

Default culture = **ar-MA** (Moroccan Arabic, RTL). Switcher in the navbar.

## 2. Prerequisites

- Windows / macOS / Linux
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (optional) Visual Studio 2022 17.8+ or VS Code with C# Dev Kit

## 3. Run it locally

```bash
cd ElectoralStats
dotnet restore
dotnet run
```

Open http://localhost:5080 — the SQLite DB is auto-created on first launch.

In Visual Studio: open `ElectoralStats.csproj` and press **F5**.

## 4. Make it a desktop executable

Two options:

### Option A — self-contained single file (recommended)

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Outputs `bin/Release/net8.0/win-x64/publish/ElectoralStats.exe`.
Double-click to launch — it auto-opens on port 5080. Replace `win-x64` with
`linux-x64` or `osx-x64` as needed.

### Option B — wrap in Electron / WebView2

Add a thin desktop shell (e.g. [Photino.NET](https://github.com/tryphotino/photino.NET))
that boots Kestrel in-process and shows a native window pointing to
`http://localhost:5080`. The ASP.NET project itself doesn't change.

## 5. Usage guide

1. **Pick a language** — top-right buttons (العربية / FR / EN).
2. **Upload → Import** — the page exposes two pickers:
   - **Folder picker** (works in Chromium browsers): select a whole
     folder; only `.xlsx` / `.xls` are kept.
   - **Multi-file picker**: Ctrl/Cmd-click individual files.
   Click **Import now**. Each file is parsed server-side.
3. **Auto-detection**:
   - If the sheet has the column `سبب التشطيب` → recorded as **Radiation**.
   - Otherwise → **Inscription**. If `تاريخ التعديل > تاريخ التسجيل`
     it is reclassified as **Modification**.
4. **Stats** — the dashboard subscribes to SignalR; every upload pushes a
   fresh snapshot. Refresh-safe (data is in SQLite).
5. **Reset data** — red button on the dashboard wipes the table.

## 6. Expected Excel schema

The importer matches Arabic column headers exactly (the same 32/33 headers
your LEG exports use):

```
رقم الوثيقة, وثيقة التعريف, مكان الاقامة, جماعة الاقامة, العنوان,
نوع العمل, المهنة, المستوى الدراسي, عدد الأطفال, الحالة العائلية,
جنس, تاريخ الازدياد, مكان الازدياد آخر, جماعة الازدياد,
الاسم العائلي بالفرنسية, الاسم الشخصي بالفرنسية, اسم الأب و الأم,
الاسم العائلي, الاسم الشخصي, علاقة التسجيل بالجماعة, الرقم الترتيبي,
الدائرة الانتخابية, الجماعة, رمز المستعمل, الدائرة الانتخابية سابقا,
الجماعة سابقا, تاريخ التسجيل, تاريخ التعديل, رمز الناخب,
رقم مكتب التصويت, اسم مكتب التصويت[, سبب التشطيب]
```

To add or rename columns, edit `Services/ExcelImportService.cs` (the `Map`
dictionary) and the matching field in `Models/VoterRecord.cs`.

## 7. Project layout

```
ElectoralStats/
├── Program.cs                  # bootstrap, i18n, SignalR
├── Controllers/                # Home, Upload, Stats
├── Models/VoterRecord.cs       # EF entity
├── Data/AppDbContext.cs
├── Services/
│   ├── ExcelImportService.cs   # ClosedXML → DB
│   └── StatsService.cs         # aggregation
├── Hubs/StatsHub.cs            # SignalR push
├── Views/                      # Razor + IViewLocalizer
├── Resources/Views/.../*.resx  # ar-MA, fr-FR, en-US
└── wwwroot/                    # CSS, JS, Chart.js binding
```

## 8. Adding a new language

1. Copy each `*.ar-MA.resx` to `*.xx-XX.resx`, translate the values.
2. Append the culture to `supportedCultures` in `Program.cs`.
3. Add a button in `Views/Shared/_Layout.cshtml`.

## 9. Security note

This is an internal IT-engineering tool — there is **no authentication**
out of the box. Before deploying for real use, add ASP.NET Core Identity
or Windows Authentication, and restrict the Upload/Stats controllers with
`[Authorize]`.

## 10. Troubleshooting

| Symptom | Fix |
|---|---|
| `dotnet: command not found` | Install .NET 8 SDK and re-open the shell. |
| Arabic text shows as `??` | Ensure your Excel file is saved as `.xlsx` (not legacy `.xls` with codepage issues). |
| Dashboard stays at 0 | Check the Network tab on `/Upload/Upload` — the JSON `results` array will show `error` per file. |
| Folder picker greyed out | Use the second (multi-file) picker — folder selection only works in Chromium-based browsers. |
