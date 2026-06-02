# دليل النشر - لوحة تحكم التسجيلات
# Deployment Guide - Voter Registration Dashboard

## متطلبات النظام / System Requirements

- **.NET 8.0 SDK** or later
- **Windows Server 2016+** or **Windows 10/11**
- **IIS 10+** (for IIS deployment) or use **Kestrel**
- **10 MB** minimum free disk space

---

## 1. بناء المشروع / Build the Project

```bash
# Restore NuGet packages
dotnet restore

# Build in Release mode
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish
```

---

## 2. النشر على IIS / Deploy on IIS

### أولاً: تثبيت Hosting Bundle
Download and install the **.NET 8.0 Hosting Bundle** from:
https://dotnet.microsoft.com/download/dotnet/8.0

### ثانياً: إعداد IIS
```powershell
# Create application pool
Import-Module WebAdministration
New-Item -Path IIS:\AppPools\VoterDashboard -ItemType AppPool
Set-ItemProperty -Path IIS:\AppPools\VoterDashboard -Name "managedRuntimeVersion" -Value ""

# Create website
New-Item -Path IIS:\Sites\VoterDashboard -PhysicalPath "C:\inetpub\wwwroot\VoterDashboard" -Bindings @{protocol="http";bindingInformation=":8080:"}
Set-ItemProperty -Path IIS:\Sites\VoterDashboard -Name applicationPool -Value VoterDashboard
```

### ثالثاً: web.config
Create `web.config` in the publish folder:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\VoterRegistrationDashboard.dll" stdoutLogEnabled="false" hostingModel="InProcess" />
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="10485760" />
      </requestFiltering>
    </security>
  </system.webServer>
  <system.web>
    <globalization fileEncoding="utf-8" requestEncoding="utf-8" responseEncoding="utf-8" culture="ar" uiCulture="ar" />
  </system.web>
</configuration>
```

---

## 3. تشغيل باستخدام Kestrel / Run with Kestrel

```bash
# Navigate to publish folder
cd ./publish

# Run the application
dotnet VoterRegistrationDashboard.dll

# Or with specific port
dotnet VoterRegistrationDashboard.dll --urls "http://localhost:5000"
```

For production hosting with Kestrel, use a reverse proxy (IIS, Nginx, or Apache).

---

## 4. خدمة Windows / Windows Service

```powershell
# Create Windows Service
sc create VoterDashboard binPath= "C:\publish\VoterRegistrationDashboard.exe"
sc config VoterDashboard start= auto
sc start VoterDashboard
```

Update `Program.cs` for Windows Service:
```csharp
builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>();
```

---

## 5. حزم NuGet المطلوبة / Required NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| EPPlus | 7.0.0 | Read .xlsx files without Excel |
| System.Text.Json | 8.0.0 | JSON serialization |
| Microsoft.AspNetCore.Localization | 2.2.0 | Arabic localization |
| Microsoft.Extensions.Localization | 8.0.0 | Resource localization |

Install via Package Manager:
```powershell
Install-Package EPPlus -Version 7.0.0
Install-Package System.Text.Json -Version 8.0.0
Install-Package Microsoft.AspNetCore.Localization -Version 2.2.0
Install-Package Microsoft.Extensions.Localization -Version 8.0.0
```

Or via CLI:
```bash
dotnet add package EPPlus --version 7.0.0
dotnet add package System.Text.Json --version 8.0.0
dotnet add package Microsoft.AspNetCore.Localization --version 2.2.0
dotnet add package Microsoft.Extensions.Localization --version 8.0.0
```

---

## 6. هيكل المشروع / Project Structure

```
VoterRegistrationDashboard/
├── VoterRegistrationDashboard.csproj    # Project file with NuGet refs
├── Program.cs                            # App entry point & DI config
├── appsettings.json                      # App configuration
├── appsettings.Development.json          # Dev settings
├── DEPLOYMENT.md                         # This file
│
├── Models/
│   ├── Inscription.cs                    # Main data model
│   ├── DashboardViewModel.cs             # Dashboard view model
│   └── Services/
│       ├── IExcelParserService.cs        # Excel parser interface
│       ├── ExcelParserService.cs         # EPPlus implementation
│       ├── IStatisticsService.cs         # Stats interface
│       └── StatisticsService.cs          # Stats implementation
│
├── Controllers/
│   └── DashboardController.cs            # Main controller + API
│
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   └── _Layout.cshtml               # RTL layout with Cairo font
│   └── Dashboard/
│       ├── Index.cshtml                  # Dashboard with charts
│       └── Upload.cshtml                 # File upload page
│
└── wwwroot/
    ├── css/
    │   └── site.css                      # RTL styles
    ├── js/
    └── uploads/                          # Temp upload folder
```

---

## 7. ملاحظات هامة / Important Notes

1. **No Excel Required**: The app uses EPPlus library - Microsoft Excel does NOT need to be installed on the server.

2. **EPPlus License**: The project uses `LicenseContext.NonCommercial`. For commercial use, you need a license from EPPlus Software.

3. **In-Memory Storage**: Data is stored in a static variable for simplicity. For production:
   - Use SQL Server / SQLite / PostgreSQL
   - Implement Entity Framework Core
   - Add proper data persistence

4. **File Upload Size**: Maximum upload size is set to 10MB. Adjust in `Program.cs` if needed.

5. **Arabic Text**: The app handles Arabic text correctly with UTF-8 encoding throughout.

6. **RTL Layout**: All views use `dir="rtl"` and Bootstrap RTL for proper Arabic layout.

7. **Security**: In production, add:
   - Authentication & Authorization
   - Input validation
   - Anti-forgery tokens
   - Rate limiting for uploads
   - File type validation

---

## 8. API Endpoints / نقاط الوصول

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Dashboard (redirects to /Dashboard) |
| `/Dashboard` | GET | Main dashboard view |
| `/Dashboard/Upload` | GET/POST | Upload Excel file |
| `/Dashboard/GetStatistics` | GET | JSON statistics for AJAX |
| `/Dashboard/GetTableData` | GET | JSON table data for AJAX |
| `/Dashboard/GetFilterOptions` | GET | Filter dropdown options |
| `/Dashboard/ExportCsv` | GET | Export filtered data to CSV |
| `/Dashboard/ClearData` | POST | Clear all data |

---

## 9. استكشاف الأخطاء / Troubleshooting

### Arabic text shows as ??? (question marks)
- Ensure `web.config` has UTF-8 encoding settings
- Check database collation if using SQL Server (use Arabic_CI_AS)

### EPPlus License Error
- Make sure `ExcelPackage.LicenseContext = LicenseContext.NonCommercial;` is set before using EPPlus

### File upload fails
- Check IIS request limits (`maxAllowedContentLength`)
- Verify the upload folder has write permissions
- Check `FormOptions.MultipartBodyLengthLimit`

### Charts not displaying
- Verify Chart.js CDN is accessible
- Check browser console for JavaScript errors
- Ensure model data is not empty
