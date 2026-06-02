using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VoterRegistrationDashboard.Models;
using VoterRegistrationDashboard.Models.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoterRegistrationDashboard.Controllers
{
    /// <summary>
    /// لوحة تحكم الإحصائيات
    /// Dashboard controller for voter registration statistics
    /// </summary>
    public class DashboardController : Controller
    {
        private readonly IExcelParserService _excelParser;
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<DashboardController> _logger;

        // In-memory storage for the session (use database in production)
        // تخزين مؤقت في الذاكرة - استخدم قاعدة بيانات في الإنتاج
        private static List<Inscription> _inscriptions = new();
        private static DateTime? _lastUploadTime;

        public DashboardController(
            IExcelParserService excelParser,
            IStatisticsService statisticsService,
            ILogger<DashboardController> logger)
        {
            _excelParser = excelParser;
            _statisticsService = statisticsService;
            _logger = logger;
        }

        /// <summary>
        /// الصفحة الرئيسية للوحة التحكم
        /// Main dashboard page
        /// </summary>
        public IActionResult Index(FilterViewModel? filters = null, int page = 1)
        {
            filters ??= new FilterViewModel();
            
            var model = _statisticsService.CalculateStatistics(_inscriptions, filters, page);
            
            ViewBag.LastUploadTime = _lastUploadTime;
            ViewBag.HasData = _inscriptions.Any();

            return View(model);
        }

        /// <summary>
        /// صفحة رفع الملف
        /// Upload page
        /// </summary>
        [HttpGet]
        public IActionResult Upload()
        {
            return View(new UploadViewModel());
        }

        /// <summary>
        /// معالجة رفع الملف
        /// Handle file upload
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadViewModel model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                model.Success = false;
                model.Message = "يرجى اختيار ملف Excel صالح";
                return View(model);
            }

            if (!_excelParser.IsValidExcelFile(model.ExcelFile))
            {
                model.Success = false;
                model.Message = "الملف غير صالح. يرجى رفع ملف بصيغة .xlsx";
                return View(model);
            }

            try
            {
                var inscriptions = await _excelParser.ParseExcelFileAsync(model.ExcelFile);

                if (inscriptions.Count == 0)
                {
                    model.Success = false;
                    model.Message = "لم يتم العثور على بيانات في الملف. تأكد من أن الملف يحتوي على الأعمدة العربية المتوقعة.";
                    return View(model);
                }

                // Replace existing data (or merge in production)
                _inscriptions = inscriptions;
                _lastUploadTime = DateTime.Now;

                model.Success = true;
                model.RecordsImported = inscriptions.Count;
                model.Message = $"تم استيراد {inscriptions.Count} سجل بنجاح!";

                _logger.LogInformation("Uploaded file with {Count} records", inscriptions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                model.Success = false;
                model.Message = $"حدث خطأ أثناء معالجة الملف: {ex.Message}";
            }

            return View(model);
        }

        /// <summary>
        /// مسح البيانات
        /// Clear all data
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearData()
        {
            _inscriptions.Clear();
            _lastUploadTime = null;
            _logger.LogInformation("All data cleared");
            return RedirectToAction(nameof(Index));
        }

        #region API Endpoints for AJAX

        /// <summary>
        /// API: Get statistics as JSON (for AJAX chart updates)
        /// </summary>
        [HttpGet]
        public IActionResult GetStatistics([FromQuery] FilterViewModel? filters = null)
        {
            filters ??= new FilterViewModel();
            var model = _statisticsService.CalculateStatistics(_inscriptions, filters);
            
            return Json(new
            {
                total = model.TotalInscriptions,
                approved = model.ApprovedCount,
                rejected = model.RejectedCount,
                pending = model.PendingCount,
                approvalRate = model.ApprovalRate,
                dailyTrends = model.DailyTrends,
                provinceBreakdown = model.ProvinceBreakdown,
                communeBreakdown = model.CommuneBreakdown,
                genderBreakdown = model.GenderBreakdown,
                requestTypeBreakdown = model.RequestTypeBreakdown,
                maritalStatusBreakdown = model.MaritalStatusBreakdown,
                educationBreakdown = model.EducationBreakdown,
                statusBreakdown = model.StatusBreakdown,
                rejectionReasons = model.RejectionReasons
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        /// <summary>
        /// API: Get filtered data table
        /// </summary>
        [HttpGet]
        public IActionResult GetTableData([FromQuery] FilterViewModel? filters = null, int page = 1, int pageSize = 25)
        {
            filters ??= new FilterViewModel();
            var model = _statisticsService.CalculateStatistics(_inscriptions, filters, page, pageSize);

            return Json(new
            {
                data = model.Inscriptions.Select(i => new
                {
                    i.RequestNumber,
                    i.RequestDate,
                    i.RequestType,
                    i.FullName,
                    i.Gender,
                    i.ResidenceProvince,
                    i.ResidenceCommune,
                    i.RequestStatus,
                    i.NationalId
                }),
                total = model.TotalInscriptions,
                currentPage = model.CurrentPage,
                totalPages = model.TotalPages
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// API: Get filter options
        /// </summary>
        [HttpGet]
        public IActionResult GetFilterOptions()
        {
            var stats = _statisticsService.CalculateStatistics(_inscriptions, new FilterViewModel());
            
            return Json(new
            {
                provinces = stats.ProvinceOptions,
                communes = stats.CommuneOptions,
                genders = stats.GenderOptions,
                requestTypes = stats.RequestTypeOptions,
                statuses = stats.StatusOptions
            });
        }

        /// <summary>
        /// Export data to CSV
        /// تصدير البيانات إلى CSV
        /// </summary>
        [HttpGet]
        public IActionResult ExportCsv([FromQuery] FilterViewModel? filters = null)
        {
            filters ??= new FilterViewModel();
            var filtered = _statisticsService.ApplyFilters(_inscriptions, filters);

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, System.Text.Encoding.UTF8);
            
            // Write BOM for Excel to recognize UTF-8
            var bom = System.Text.Encoding.UTF8.GetPreamble();
            ms.Write(bom, 0, bom.Length);

            // Header
            writer.WriteLine("رقم الطلب,تاريخ الطلب,نوع الطلب,الإسم الكامل,الجنس,تاريخ الإزدياد,بلد الإزدياد,عمالة أو إقليم السكن,جماعة السكن,الحالة العائلية,المستوى الدراسي,وضعية الطلب");

            foreach (var i in filtered)
            {
                writer.WriteLine($"{Escape(i.RequestNumber)},{i.RequestDate:yyyy-MM-dd},{Escape(i.RequestType)},{Escape(i.FullName)},{Escape(i.Gender)},{i.BirthDate:yyyy-MM-dd},{Escape(i.BirthCountry)},{Escape(i.ResidenceProvince)},{Escape(i.ResidenceCommune)},{Escape(i.MaritalStatus)},{Escape(i.EducationLevel)},{Escape(i.RequestStatus)}");
            }

            writer.Flush();
            ms.Position = 0;

            return File(ms.ToArray(), "text/csv", $"inscriptions_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        #endregion
    }
}
